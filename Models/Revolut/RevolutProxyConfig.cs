namespace nbs_smart_wallet.Models.Revolut
{
	public class RevolutProxyConfig
	{
		public string client_id { get; set; } = string.Empty;
		public string url { get; set; } = string.Empty;
		public string auth_url { get; set; } = string.Empty;
		public string jwk_endpoint { get; set; } = string.Empty;
		public JsonWebKey jwk { get; set; } = new JsonWebKey();
		public string auth_redirect_endpoint { get; set; } = string.Empty;
		public string access_token_cookie_name { get; set; } = string.Empty;
		public string refresh_token_cookie_name { get; set; } = string.Empty;
		public string revolut_financial_id { get; set; } = string.Empty;
	}

	public class JsonWebKey
	{
		public List<Key> keys { get; set; } = new List<Key>();
		public class Key
		{
			public string e { get; set; } = string.Empty;
			public string n { get; set; } = string.Empty;
			public string kid { get; set; } = string.Empty;
			public string kty { get; set; } = string.Empty;
			public string use { get; set; } = string.Empty;
			public List<string> x5c { get; set; } = new List<string>();
		}
	}
}
