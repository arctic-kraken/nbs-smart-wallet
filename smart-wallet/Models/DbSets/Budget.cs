using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smart_wallet.Models.DbSets
{
	public class Budget
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public Guid UserId { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public string Clauses { get; set; } = JsonConvert.SerializeObject(new List<string>());
	}
}
