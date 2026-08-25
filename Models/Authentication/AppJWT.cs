namespace nbs_smart_wallet.Models.Authentication
{
	public class AppJWT
	{
		public string iss { get; set; } = string.Empty;
		public string aud { get; set; } = string.Empty;
		public string signing_key { get; set; } = string.Empty;
	}
}
