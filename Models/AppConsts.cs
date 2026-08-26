namespace nbs_smart_wallet.Models
{
	public static class AppConsts
	{
		public static class Currency
		{
			public const string BritishPound = "GBP";
			public const string AmericanDollar = "USD";
			public const string UnitedEuro = "EUR";
		}

		public static class Accounting
		{
			public const string Debit = "Debit";
			public const string Credit = "Credit";

		}

		public static class SchemeNames
		{
			public const string UK_IBAN = "UK.OBIE.IBAN";
			public const string UK_SCAN = "UK.OBIE.SortCodeAccountNumber";
			public const string UK_RevInternal = "UK.Revolut.InternalAccountId";
			public const string US_RNAN = "US.RoutingNumberAccountNumber";
			public const string US_BCAN = "US.BranchCodeAccountNumber";
		}

		public static class AccountType
		{
			public static string Personal = "Personal";
			public static string Business = "Business";
		}

		public static class AccountSubType
		{
			public static string CurrentAccount = "CurrentAccount";
			public static string Savings = "Savings";
			public static string CreditCard = "CreditCard";
		}

		public static class CreditDebitIndicator
		{
			public static string Credit = "Credit";
			public static string Debit = "Debit";
		}
	}
}
