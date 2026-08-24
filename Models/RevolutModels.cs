namespace nbs_smart_wallet.Models
{
	public class RevolutModels
	{
		public class Account
		{
			public Guid accountId { get; set; }
			public string currency { get; set; } = string.Empty;
			public string accountType { get; set; } = string.Empty;
			public string accountSubType { get; set; } = string.Empty;
			public string nickname { get; set; } = string.Empty;
		}
	}
}
