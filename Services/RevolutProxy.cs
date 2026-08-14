
using JsonConverter.Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace nbs_smart_wallet.Services
{
	public class RevolutProxy
	{
		public const string sandbox_url = "https://sandbox-oba-auth.revolut.com";
		public const string sandbox_client_id = "2e62762a-c15b-4aa9-9278-18c280797854";
		private HttpClient _client;
		private string client_credential_access_token = string.Empty;
		private string client_credential_refresh_token = string.Empty;
		public RevolutProxy(IHttpClientFactory clientFactory)
		{
			_client = clientFactory.CreateClient("revolut");
			_client.BaseAddress = new Uri(sandbox_url);
		}

		public class ClientCredentialTokenResponse
		{
			public string access_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
		}

		public async Task<bool> GetClientCredentialToken()
		{
			string url = $"/token?grant_type=client_credentials&scope=accounts&client_id={sandbox_client_id}";
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

			using var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();
			var credential = JsonConvert.DeserializeObject<ClientCredentialTokenResponse>(content);
			client_credential_access_token = credential.access_token;

			return true;
		}

		public class AccountAccessConsentRequestBody
		{
			public Data Data { get; set; } = new Data();
			public Risk Risk { get; set; } = new Risk();
		}

		public class Data
		{
			public string Status { get; set; } = string.Empty;
			public DateTime StatusUpdateDateTime { get; set; }
			public DateTime CreationDateTime { get; set; }
			public List<string> Permissions { get; set; } = new List<string>
				{
					"ReadAccountsBasic",
					"ReadAccountsDetail",
				};
			public DateTime ExpirationDateTime { get; set; }
			public DateTime TransactionFromDateTime { get; set; }
			public DateTime TransactionToDateTime { get; set; }
			public Guid ConsentId { get; set; }

		}
		public class Risk
		{

		}

		public class Links 
		{
			public string Self { get; set; } = string.Empty;
		}

		public class Meta
		{
			public int TotalPages { get; set; }
		}

		public class AccountAccessConsentResponse
		{
			public Data Data { get; set; } = new Data();
			public Risk Risk { get; set; } = new Risk();
			public Links Links { get; set; } = new Links();
			public Meta Meta { get; set; } = new Meta();
		}

		public async Task<AccountAccessConsentResponse> CreateAccountAccessConsent()
		{
			if (String.IsNullOrEmpty(client_credential_access_token))
				return new AccountAccessConsentResponse();

			string url = $"/account-access-consents";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("x-fapi-financial-id", "001580000103UAvAAM");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client_credential_access_token);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			var body = new AccountAccessConsentRequestBody();
			body.Data.ExpirationDateTime = DateTime.Now.AddHours(1.0);
			body.Data.TransactionFromDateTime = DateTime.Now.AddMonths(-3);
			body.Data.TransactionToDateTime = DateTime.Now;

			request.Content = JsonContent.Create(body);

			using var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();
			var account_access_consent = JsonConvert.DeserializeObject<AccountAccessConsentResponse>(content);
			// log 
			if (account_access_consent == null)
				throw new ArgumentNullException();
			
			return account_access_consent;
		}

		public class JWTHeader
		{
			public string alg { get; set; } = "PS256";
			public string kid { get; set; } = string.Empty;
		}

		public class JWTPayload
		{
			public string response_type { get; set; } = "code id_token";
			public string client_id { get; set; } = sandbox_client_id;
			public string redirect_uri { get; set; } = string.Empty;
			public string aud = string.Empty;
			public string scope = string.Empty;
			public string state = "somestate";
			public string nbf { get; set; } = string.Empty;
			public string exp{ get; set; } = string.Empty;
			public Claims claims { get; set; } = new Claims();
		}

		public class Claims
		{
			public Id_token id_token { get; set; } = new Id_token();
			
		}
		public class Id_token
		{
			public OpenBanking_intent_id openbanking_intent_id { get; set; } = new OpenBanking_intent_id();

		}
		public class OpenBanking_intent_id
		{
			public string value { get; set; } = string.Empty;
		}


		public string GetSignedJWTFor(Guid consentId)
		{
			var header = new JWTHeader();
			header.kid = "pallasathena";

			var payload = new JWTPayload();
			payload.redirect_uri = "";
			payload.aud = "https://sandbox-oba-auth.revolut.com";
			payload.scope = "accounts";
			//payload.state = "state";
			payload.claims.id_token.openbanking_intent_id.value = consentId.ToString();

			var cert = X509Certificate2.CreateFromPemFile(@"C:\Users\JakubKiepas\transport.pem", @"C:\Users\JakubKiepas\private.key");
			var key = new RsaSecurityKey(cert.GetRSAPrivateKey());
			key.KeyId = header.kid;
			var handler = new JsonWebTokenHandler();
			var desc = new SecurityTokenDescriptor
			{
				Issuer = cert.Issuer,
				Audience = "https://sandbox-oba-auth.revolut.com",
				Expires = DateTime.Now.AddMinutes(5.0),
				NotBefore = DateTime.Now,
				SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha256),
				Claims = new Dictionary<string, object>
				{
					{ "scope", payload.scope },
					{ "state", payload.state },
					{ "client_id", payload.client_id },
					{ "response_type", payload.response_type },
					{ "redirect_uri", payload.redirect_uri },
					{ "claims", new Dictionary<string, object>
						{
							{ "id_token", new Dictionary<string, object>
								{
									{  "openbanking_intent_id", new Dictionary<string, object>
										{
											{ "value", consentId.ToString() } 
										} 
									} 
								}
							}
						}
					},
				},
				IncludeKeyIdInHeader = true
			};
			string jwt = handler.CreateToken(desc);

			return jwt;
		}

		
	}
}
