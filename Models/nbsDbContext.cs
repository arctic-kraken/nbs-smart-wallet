using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Models.DbSets;

namespace nbs_smart_wallet.Models
{
	public class nbsDbContext : IdentityDbContext<ApplicationUser>
	{
		public nbsDbContext(DbContextOptions options) : base(options) { }

		public DbSet<ApplicationUser> Users { get; set; }
		public DbSet<RevAccount> RevAccounts { get; set; }
		public DbSet<RevBankAccount> RevBankAccounts { get; set; }
		public DbSet<RevTransaction> RevTransactions { get; set; }
	}
}
