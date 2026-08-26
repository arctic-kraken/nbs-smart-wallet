using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nbs_smart_wallet.Models.DbSets
{
	public class RevTransaction
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public Guid RevAccountId { get; set; }
		public Guid RevTransactionId { get; set; }
		public string Currency { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public DateTime BookingDateTime { get; set; }
		public DateTime ValueDateTime { get; set; }
		public string BalanceCurrency { get; set; } = string.Empty;
		public decimal BalanceAmount { get; set; }
		public string CurrencyExchangeJson { get; set; } = string.Empty;
		public string CreditDebitIndicator { get; set; } = string.Empty;
		public string RevCreditorAccountJson { get; set; } = string.Empty;
		public string RevDebtorAccountJson { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public string TransactionInformation { get; set; } = string.Empty;
		public string SupplementaryData{ get; set; } = string.Empty;

	}
}
