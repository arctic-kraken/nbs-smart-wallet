
using JsonConverter.Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;

namespace nbs_smart_wallet.Services
{
	public class RevolutProxy
	{
		public const string sandbox_url = "https://sandbox-oba.revolut.com";
		public const string sandbox_auth_url = "https://sandbox-oba-auth.revolut.com";
		public const string sandbox_client_id = "2e62762a-c15b-4aa9-9278-18c280797854";
		public const string sandbox_jwk_endpoint = "jwk/auth";
		public const string sandbox_redirect_uri = "jwk/auth/callback";
		private const string access_token_name = "wallet_atoken";
		private const string refresh_token_name = "wallet_rtoken";
		private HttpClient _client;
		private IHttpContextAccessor _accessor;
		private string client_credential_access_token = string.Empty;
		private string client_credential_refresh_token = string.Empty;
		private string access_token = string.Empty;
		private string refresh_token = string.Empty;

		public RevolutProxy(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor)
		{
			_client = clientFactory.CreateClient("revolut");
			_accessor = contextAccessor;
			
			//_client.BaseAddress = new Uri(sandbox_url);
		}

		public class ClientCredentialTokenResponse
		{
			public string access_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
		}

		public async Task<bool> GetClientCredentialToken()
		{
			string url = $"{sandbox_auth_url}/token";
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			var form = new Dictionary<string, string> 
			{
				{ "grant_type", "client_credentials" },
				{ "scope", "accounts" },
				{ "client_id", sandbox_client_id },
			};
			var formEncoded = new FormUrlEncodedContent(form);
			request.Content = formEncoded;

			//_client.BaseAddress = new Uri(sandbox_auth_url);
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
			public List<AppAccount> Account { get; set; } = new List<AppAccount>();

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

			string url = $"{sandbox_auth_url}/account-access-consents";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("x-fapi-financial-id", "001580000103UAvAAM");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client_credential_access_token);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			var body = new AccountAccessConsentRequestBody();
			body.Data.ExpirationDateTime = DateTime.Now.AddHours(1.0);
			body.Data.TransactionFromDateTime = DateTime.Now.AddMonths(-3);
			body.Data.TransactionToDateTime = DateTime.Now;

			request.Content = JsonContent.Create(body, new MediaTypeHeaderValue("application/json"), System.Text.Json.JsonSerializerOptions.Default);

			//_client.BaseAddress = new Uri("");
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

		public static HttpClientHandler GetDefaultRevolutHandler()
		{
			var handler = new HttpClientHandler();
			//var certificateWithKey = X509Certificate2.CreateFromPemFile(@"C:\Users\JakubKiepas\transport.pem", @"C:\Users\JakubKiepas\private.key");
			// netcore is retarded, turns out I have to turn the pem and pk into pfx and load that one for it to auth
			var cert = new X509Certificate2(@"C:\Users\JakubKiepas\transport.pfx");


			handler.ClientCertificates.Add(cert);
			handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
			handler.ClientCertificateOptions = ClientCertificateOption.Manual;
			handler.AllowAutoRedirect = true;
			handler.MaxAutomaticRedirections = 1;

			return handler;
		}

		public string GetSignedJWTFor(Guid consentId)
		{
			var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");
			var header = new JWTHeader();
			header.kid = "pallasathena";

			var payload = new JWTPayload();
			//payload.redirect_uri = $"{tunnelUrl}{sandbox_jwk_endpoint}/"; // has to be different than this one
			payload.redirect_uri = $"{tunnelUrl}{sandbox_redirect_uri}/";
			payload.aud = "https://sandbox-oba-auth.revolut.com";
			payload.scope = "accounts";
			//payload.state = "state";
			payload.claims.id_token.openbanking_intent_id.value = consentId.ToString();

			// TODO - change to grab the pfx
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

		public string GetAuthUrl(Guid consentId)
		{
			var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");
			var coder = UrlEncoder.Create();
			return $"{sandbox_url}/ui/index.html?response_type=code%20id_token&scope=accounts&redirect_uri={$"{tunnelUrl}{sandbox_redirect_uri}"}&client_id={sandbox_client_id}&request={GetSignedJWTFor(consentId)}";
		}

		public class AccessTokenResponse
		{
			public string access_token { get; set; } = string.Empty;
			public Guid access_token_id { get; set; }
			public string token_type { get; set; } = string.Empty;
			public int expires_in { get; set; }
			public string refresh_token { get; set; } = string.Empty;
			// Unix timestamp below
			public string refresh_token_expires_at { get; set; } = string.Empty;

		}

		public async Task<bool> GetAccessToken(string code, string id_token, string state)
		{
			var coder = UrlEncoder.Create();
			string url = $"{sandbox_auth_url}/token";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			var form = new Dictionary<string, string>
			{
				{ "grant_type", "authorization_code" },
				{ "code", code },
				{ "client_id", sandbox_client_id },
			};
			var formEncoded = new FormUrlEncodedContent(form);
			request.Content = formEncoded;

			using var response = await _client.SendAsync(request);
			//response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();
			// log 
			var cookie_jar = new CookieContainer();
			var token_response = JsonConvert.DeserializeObject<AccessTokenResponse>(content);
			if (token_response == null)
				return false;

			var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");

			// remember about cookie security
			AddTokenCookies(token_response.access_token, token_response.refresh_token);

			return true;
		}

		private void AddTokenCookies(string? access_token, string? refresh_token)
		{
			var options = new CookieOptions
			{
				HttpOnly = true,
				IsEssential = true,
				Secure = true,
			};
			// TODO - encrypt these cookies and decrypt etc etc
			if (!String.IsNullOrEmpty(access_token))
				_accessor.HttpContext?.Response.Cookies.Append(access_token_name, access_token, options);

			if (!String.IsNullOrEmpty(refresh_token))
				_accessor.HttpContext?.Response.Cookies.Append(refresh_token_name, refresh_token, options);
		}

		private string GetAccessTokenFromCookie()
		{
			if (_accessor.HttpContext == null)
				return string.Empty;
			var a_token = _accessor.HttpContext.Request.Cookies.SingleOrDefault(x => x.Key == access_token_name);
			
			return a_token.Value;
		} 

		public class AppAccount
		{
			public Guid AccountId { get; set; }
			public string Currency { get; set; } = string.Empty;
			public string AccountType { get; set; } = string.Empty;
			public string AccountSubType { get; set; } = string.Empty;
			public string Nickname { get; set; } = string.Empty;
			public List<BankAccount> Account = new List<BankAccount>();
		}

		public class BankAccount
		{
			public string SchemeName { get; set; } = string.Empty;
			public string Identification { get; set; }
			public string Name { get; set; } = string.Empty;
			public string SecondaryIdentification { get; set; } = string.Empty;

		}

		public class AccountResponse
		{
			public Data Data { get; set; } = new Data();
			public Links Links { get; set; } = new Links();
			public Meta Meta { get; set; } = new Meta();
		}

		public async Task<List<AppAccount>> GetAccounts()
		{
			var token = GetAccessTokenFromCookie();
			if (String.IsNullOrEmpty(token))
				throw new InvalidOperationException("access_token is empty");

			// https://developer.revolut.com/blog/2025-03-04-open-banking-fapi1-advanced#new-subdomains-and-mandatory-mtls
			// https://developer.revolut.com/updates/2025-03-04-open-banking-fapi1-advanced#new-subdomains-and-mandatory-mtls
			string url = $"{sandbox_auth_url}/accounts";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("x-fapi-financial-id", "001580000103UAvAAM");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			using var response = await _client.SendAsync(request);
			//response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();
			var accounts = JsonConvert.DeserializeObject<AccountResponse>(content);
			// log 
			if (accounts == null)
				throw new ArgumentNullException();

			return accounts.Data.Account;
		}
		
	}
}
