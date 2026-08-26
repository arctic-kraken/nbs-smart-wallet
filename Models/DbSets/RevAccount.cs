using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nbs_smart_wallet.Models.DbSets
{
	public class RevAccount
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public Guid AspNetUserId { get; set; }
		public Guid RevAccountId { get; set; }
		public string Currency { get; set; } = string.Empty;
		public string AccountType { get; set; } = string.Empty;
		public string AccountSubType { get; set; } = string.Empty;
		public string Nickname { get; set; } = string.Empty;
	}
}
