
using JsonConverter.Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

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

			request.Content = JsonContent.Create(body)

			using var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();
			var account_access_consent = JsonConvert.DeserializeObject<AccountAccessConsentResponse>(content);
			// log 
			if (account_access_consent == null)
				throw new ArgumentNullException();
			
			return account_access_consent;
		}

		
	}
}
