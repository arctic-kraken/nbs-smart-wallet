using static nbs_smart_wallet.Services.RevolutProxy;

namespace nbs_smart_wallet.Models.Revolut
{
	public class RevolutPayload
	{
		public Data Data { get; set; } = new Data();
		public Risk Risk { get; set; } = new Risk();
		public Links Links { get; set; } = new Links();
		public Meta Meta { get; set; } = new Meta();
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
}
