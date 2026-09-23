using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using nbs_smart_wallet.Models.Revolut;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Serilog;

namespace nbs_smart_wallet.Services
{
	public class RevolutProxy
	{
		private HttpClient _client;
		private IHttpContextAccessor _accessor;
		private RevolutProxyConfig _config;
		private string client_credential_access_token = string.Empty;
		private AppService _app;
		private const string dateFormat = "yyyy-MM-ddTHH:mm:ssZ";

		public RevolutProxy(IHttpClientFactory clientFactory, IHttpContextAccessor contextAccessor, AppService appService)
		{
			_client = clientFactory.CreateClient("revolut");
			_accessor = contextAccessor;
			var config_str = Environment.GetEnvironmentVariable("RevolutProxyConfig");
			if (String.IsNullOrEmpty(config_str))
				throw new Exception("Failed to load Revolut Proxy Config at HOME");
			var config = JsonConvert.DeserializeObject<RevolutProxyConfig>(config_str);
			_config = config ?? throw new Exception("Revolut Proxy Config has not been deserialized because a null was given");

			_app = appService;
		}

		public static HttpClientHandler GetDefaultRevolutHandler(string pfx_contents)
		{
			var handler = new HttpClientHandler();
			handler.ClientCertificates.Add(GetSigningCertificateWith(pfx_contents));
			handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
			handler.ClientCertificateOptions = ClientCertificateOption.Manual;
			handler.AllowAutoRedirect = true;
			handler.MaxAutomaticRedirections = 5;

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

		private X509Certificate2 GetSigningCertificate() => GetSigningCertificateWith(Environment.GetEnvironmentVariable("pfx_content") ?? "");

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

			using var response = await _client.SendAsync(request);
			Dictionary<string, object>? tokenResponse = await HandleResponse(response);
			if (tokenResponse == null)
				return false; // In HandleResponse we log, so just return

			tokenResponse.TryGetValue("access_token", out var accessToken);
			// keep in mind this is a access token of type "client_credentials"
			string? convertedToken = Convert.ToString(accessToken);
			if (String.IsNullOrEmpty(convertedToken))
			{
				Log.Error("Failed to extract access token, it was null or empty");
				return false;
			}

			client_credential_access_token = convertedToken; // prob change this to not store in a Proxy variable
			return true;
		}

		public async Task<RevolutPayload> CreateAccountAccessConsent()
		{
			if (String.IsNullOrEmpty(client_credential_access_token))
				return new RevolutPayload(); // this is not right

			string url = $"{_config.auth_url}/account-access-consents";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("x-fapi-financial-id", _config.revolut_financial_id);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client_credential_access_token);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			var body = new RevolutPayload();
			body.Data.ExpirationDateTime = DateTime.Now.AddHours(1.0);
			body.Data.TransactionFromDateTime = DateTime.Now.AddMonths(-3);
			body.Data.TransactionToDateTime = DateTime.Now;

			request.Content = JsonContent.Create(body, new MediaTypeHeaderValue("application/json"), System.Text.Json.JsonSerializerOptions.Default);

			using var response = await _client.SendAsync(request);
			var payload = await HandleResponse(response, false, null);
			if (payload == null)
				return new RevolutPayload();

			return payload;
		}


		public string GetSignedJWTFor(Guid consentId)
		{
			var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");
			// get correct url here

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
					{ "redirect_uri", $"{tunnelUrl}{_config.auth_redirect_endpoint}" },
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
			string hostname = "";
			if (String.IsNullOrEmpty(tunnelUrl))
				hostname = $"https://{_accessor.HttpContext?.Request.Host.Value}/";
			else
				hostname = tunnelUrl;

			var coder = UrlEncoder.Create();
			return $"{_config.url}/ui/index.html?response_type=code%20id_token&scope=accounts&redirect_uri={$"{hostname}{_config.auth_redirect_endpoint}"}&client_id={_config.client_id}&request={GetSignedJWTFor(consentId)}";
		}
		
		public Models.Revolut.JsonWebKey GetJWK() => _config.jwk;

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
			Dictionary<string, object>? tokenResponse = await HandleResponse(response);
			if (tokenResponse == null)
				return false; // already logged if it went wrong by HandleResponse

			tokenResponse.TryGetValue("access_token", out var accessTokenObj);
			tokenResponse.TryGetValue("refresh_token", out var refreshTokenObj);
			string? convertedAccessToken = Convert.ToString(accessTokenObj);
			string? convertedRefreshToken = Convert.ToString(refreshTokenObj);
			if (String.IsNullOrEmpty(convertedAccessToken) || String.IsNullOrEmpty(convertedRefreshToken))
			{
				Log.Error("Failed to extract access token, refresh token or both, null or empty");
				return false;
			}

			// remember about cookie security
			AddTokenCookies(convertedAccessToken, convertedRefreshToken);
			Log.Information("Successfully gained access and refresh tokens");

			return true;
		}

		/// <summary>
		/// Refreshes Access Token, using a Refresh Token
		/// </summary>
		/// <returns>True when access token refreshed, false when refresh failed</returns>
		public async Task<bool> RefreshAccessToken()
		{
			var refreshToken = GetRefreshTokenFromCookie();
			if (String.IsNullOrEmpty(refreshToken))
			{
				Log.Information("Failed to Refresh Revolut access token. Try authenticating.");
				return false;
			}
			string url = $"{_config.auth_url}/token";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content?.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			var form = new Dictionary<string, string>
			{
				{ "grant_type", "refresh_token" },
				{ "refresh_token", refreshToken },
				{ "client_id", _config.client_id},
			};
			var formEncoded = new FormUrlEncodedContent(form);
			request.Content = formEncoded;

			using var response = await _client.SendAsync(request);
			Dictionary<string, object>? tokenResponse = await HandleResponse(response);
			if (tokenResponse == null)
				return false; // In HandleResponse we log, so just return
			
			tokenResponse.TryGetValue("access_token", out var accessToken);
			string? convertedToken = Convert.ToString(accessToken);
			if (String.IsNullOrEmpty(convertedToken))
			{
				Log.Error("Failed to extract access token, it was null or empty");
				return false;
			}
				
			AddTokenCookies(convertedToken, null);

			Log.Information("Access Token Successfully refreshed");
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

		private string GetRefreshTokenFromCookie()
		{
			if (_accessor.HttpContext == null)
				return string.Empty;
			var a_token = _accessor.HttpContext.Request.Cookies.SingleOrDefault(x => x.Key == _config.refresh_token_cookie_name);

			return a_token.Value;
		}

		public bool IsLoggedIntoRevolut()
		{
			var token = GetRefreshTokenFromCookie();
			return String.IsNullOrEmpty(token) ? false : true;
		}

		public async Task<List<AppAccount>?> FetchAccounts()
		{	
			var token = GetAccessTokenFromCookie();
			if (String.IsNullOrEmpty(token))
			{
				Log.Warning("Non-authenticated user attempted FetchAccounts");
				return null;
			}
				
			// if token is empty here, it's ok because we can send the request and get a refreshed one

			// https://developer.revolut.com/blog/2025-03-04-open-banking-fapi1-advanced#new-subdomains-and-mandatory-mtls
			// https://developer.revolut.com/updates/2025-03-04-open-banking-fapi1-advanced#new-subdomains-and-mandatory-mtls
			string url = $"{_config.auth_url}/accounts";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("x-fapi-financial-id", _config.revolut_financial_id);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			using var response = await _client.SendAsync(request);
			var payload = await HandleResponse(response, true, request);
			// logging done by HandleResponse

			return payload != null ? payload.Data.Account : null;
		}

		public async Task<List<Transaction>?> FetchTransactionsFor(Guid revAccountId, DateTime? fromDateTime, DateTime? toDateTime)
		{
			var token = GetAccessTokenFromCookie();
			if (String.IsNullOrEmpty(token))
			{
				Log.Warning("Non-authenticated user attempted FetchTransactionsFor");
				return null;
			}
			string url = $"{_config.auth_url}/accounts/{revAccountId}/transactions";
			if (fromDateTime != null && toDateTime != null)
			{
				url += $"?fromBookingDateTime={fromDateTime?.ToString(dateFormat)}" +
					 $"&toBookingDateTime={toDateTime?.ToString(dateFormat)}";
			}

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("x-fapi-financial-id", _config.revolut_financial_id);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			using var response = await _client.SendAsync(request);
			var payload = await HandleResponse(response, true, request);
			// logging done by HandleResponse

			return payload != null ? payload.Data.Transaction : null;
		}


		private async Task<RevolutPayload?> HandleResponse(HttpResponseMessage msg, bool tryRefreshTokenIfForbidden, HttpRequestMessage? req)
		{
			if (msg == null) throw new ArgumentNullException("RevolutProxy : Response Message was null");

			string content = await msg.Content.ReadAsStringAsync();
			if (!msg.IsSuccessStatusCode)
				Log.Error("Revolut Open Banking API returned non-success: {statusCode} : content : {content}", msg.StatusCode, content);

			if (!msg.IsSuccessStatusCode && msg.StatusCode == HttpStatusCode.Unauthorized && tryRefreshTokenIfForbidden)
			{
				if (req == null) throw new ArgumentNullException("RevolutProxy : Request to be retried was null");

				Log.Information("RevolutProxy : Attempting access token refresh");
				var accessTokenRefreshed = await RefreshAccessToken();
				if (!accessTokenRefreshed)
					return null; // We logged in RefreshAccessToken

				Log.Information("RevolutProxy : Retrying request with new access token");
				var token = GetAccessTokenFromCookie();
				var newMessage = new HttpRequestMessage();
				newMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
				req.Headers.Remove("Authorization");
				foreach (var h in req.Headers)
					newMessage.Headers.Add(h.Key, h.Value);

				newMessage.Content = req.Content;
				newMessage.RequestUri = req.RequestUri;


				using var response = await _client.SendAsync(newMessage);
				return await HandleResponse(msg, false, null);
			}
			else if (!msg.IsSuccessStatusCode)
				return null;

			var payload = JsonConvert.DeserializeObject<RevolutPayload>(content);
			if (payload == null)
			{
				Log.Error("RevolutProxy : Failed to Deserialize to Revolut Open Banking Payload {@content}", content);
				return null;
			}

			return payload;
		}

		private async Task<Dictionary<string, object>?> HandleResponse(HttpResponseMessage msg)
		{
			if (msg == null) throw new ArgumentNullException("RevolutProxy : Response Message was null");

			string content = await msg.Content.ReadAsStringAsync();
			if (!msg.IsSuccessStatusCode)
			{
				Log.Error("RevolutProxy : Open Banking API returned non-success: {statusCode} : content : {content}", msg.StatusCode, content);
				return null;
			}

			var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
			if (payload == null)
			{
				Log.Error("RevolutProxy : Failed to Deserialize to Revolut Open Banking Payload {@content}", content);
				return null;
			}

			return payload;
		} 
		
	}
}
