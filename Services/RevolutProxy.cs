using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using nbs_smart_wallet.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;

namespace nbs_smart_wallet.Services
{
	public class RevolutProxy
	{
		private HttpClient _client;
		private IHttpContextAccessor _accessor;
		private RevolutProxyConfig _config;
		private string client_credential_access_token = string.Empty;
		private string client_credential_refresh_token = string.Empty;
		private string access_token = string.Empty;
		private string refresh_token = string.Empty;
		

		public RevolutProxy(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor, IConfiguration config)
		{
			_client = clientFactory.CreateClient("revolut");
			_accessor = contextAccessor;
			_config = config.GetSection("RevolutProxyConfig").Get<RevolutProxyConfig>() 
				?? throw new Exception("Failed to get Revolut Proxy Config"); // this will never throw, get rids of warning though
		}

		public static HttpClientHandler GetDefaultRevolutHandler(string pfx_contents)
		{
			var handler = new HttpClientHandler();
			handler.ClientCertificates.Add(GetSigningCertificateWith(pfx_contents));
			handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
			handler.ClientCertificateOptions = ClientCertificateOption.Manual;
			handler.AllowAutoRedirect = true;
			handler.MaxAutomaticRedirections = 1;

			return handler;
		}

		// netcore is retarded, turns out I have to turn the pem and pk into pfx and load that one for it to auth
		private static X509Certificate2 GetSigningCertificateWith(string contents)
		{
			var pfxBytes = Convert.FromBase64String(contents);
			var cert = X509CertificateLoader.LoadPkcs12(
				pfxBytes,
				null,
				keyStorageFlags: X509KeyStorageFlags.MachineKeySet
			);
			return cert;
		}

		private X509Certificate2 GetSigningCertificate() => GetSigningCertificateWith(_config.pfx_content);

		public class ClientCredentialTokenResponse
		{
			public string access_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
		}

		public async Task<bool> GetClientCredentialToken()
		{
			string url = $"{_config.auth_url}/token";
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			var form = new Dictionary<string, string> 
			{
				{ "grant_type", "client_credentials" },
				{ "scope", "accounts" },
				{ "client_id", _config.client_id },
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

			string url = $"{_config.auth_url}/account-access-consents";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("x-fapi-financial-id", _config.revolut_financial_id);
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


		public string GetSignedJWTFor(Guid consentId)
		{
			var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");

			//var cert = X509Certificate2.CreateFromPemFile(@"C:\Users\JakubKiepas\transport.pem", @"C:\Users\JakubKiepas\private.key");
			var cert = GetSigningCertificate();
			var key = new RsaSecurityKey(cert.GetRSAPrivateKey());
			key.KeyId = _config.jwk.keys.First().kid;
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
					{ "scope", "accounts" },
					{ "state", "somestate" },
					{ "client_id", _config.client_id },
					{ "response_type", "code id_token" },
					{ "redirect_uri", _config.auth_redirect_endpoint },
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
			var hostname = _accessor.HttpContext?.Request.Host.Value;
			var coder = UrlEncoder.Create();
			return $"{_config.url}/ui/index.html?response_type=code%20id_token&scope=accounts&redirect_uri={$"https://{hostname}/{_config.auth_redirect_endpoint}"}&client_id={_config.client_id}&request={GetSignedJWTFor(consentId)}";
		}
		
		public nbs_smart_wallet.Models.JsonWebKey GetJWK() => _config.jwk;

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
			string url = $"{_config.auth_url}/token";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			var form = new Dictionary<string, string>
			{
				{ "grant_type", "authorization_code" },
				{ "code", code },
				{ "client_id", _config.client_id},
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
				_accessor.HttpContext?.Response.Cookies.Append(_config.access_token_cookie_name, access_token, options);

			if (!String.IsNullOrEmpty(refresh_token))
				_accessor.HttpContext?.Response.Cookies.Append(_config.refresh_token_cookie_name, refresh_token, options);
		}

		private string GetAccessTokenFromCookie()
		{
			if (_accessor.HttpContext == null)
				return string.Empty;
			var a_token = _accessor.HttpContext.Request.Cookies.SingleOrDefault(x => x.Key == _config.access_token_cookie_name);
			
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
			public string Identification { get; set; } = string.Empty;
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
			string url = $"{_config.auth_url}/accounts";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("x-fapi-financial-id", _config.revolut_financial_id);
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
