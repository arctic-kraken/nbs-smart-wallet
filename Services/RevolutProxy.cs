
using JsonConverter.Newtonsoft.Json;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace nbs_smart_wallet.Services
{
	public class RevolutProxy
	{
		public const string sandbox_url = "https://sandbox-oba-auth.revolut.com";
		public const string sandbox_client_id = "2e62762a-c15b-4aa9-9278-18c280797854";
		private readonly IHttpClientFactory _clientFactory;
		private HttpClient _client;
		public RevolutProxy(IHttpClientFactory clientFactory)
		{
			_clientFactory = clientFactory;
			_client = _clientFactory.CreateClient("revolut");
			_client.BaseAddress = new Uri(sandbox_url);
		}

		public class ClientCredentialTokenResponse
		{
			public string access_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
		}

		public async Task<ClientCredentialTokenResponse> GetClientCredentialToken()
		{
			string url = $"/token?grant_type=client_credentials&scope=accounts&client_id={sandbox_client_id}";
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

			using var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<ClientCredentialTokenResponse>(content);
		}

		public async Task<bool> CreateAccountAccessConsent(string client_credential_access_token)
		{
			string url = $"/account-access-consents";

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("x-fapi-financial-id", "001580000103UAvAAM");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client_credential_access_token);

			using var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			string content = await response.Content.ReadAsStringAsync();


			return false;
		}
	}
}
