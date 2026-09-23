using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nbs_smart_wallet.Models.DbSets
{
	/// <summary>
	/// This fella only stores Bank Accounts of the Owner of the parent Account obj.
	/// DO NOT SAVE BANK ACCOUNTS FROM TRANSACTION INFORMATION HERE
	/// </summary>
	public class RevBankAccount
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public Guid RevAccountId { get; set; }
		public string SchemeName { get; set; } = string.Empty;
		public string Identification { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string SecondaryIdentification { get; set; } = string.Empty;
	}
}
