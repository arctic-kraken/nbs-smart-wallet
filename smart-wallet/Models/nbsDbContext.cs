using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models.Authentication;
using smart_wallet.Models.Authentication;
using nbs_smart_wallet.Models.DbSets;
using smart_wallet.Models.DbSets;

namespace nbs_smart_wallet.Models
{
	public class nbsDbContext : IdentityDbContext<ApplicationUser>
	{
		public nbsDbContext(DbContextOptions options) : base(options) { }

		public virtual DbSet<ApplicationUser> Users { get; set; }
		public virtual DbSet<ApplicationRole> Roles { get; set; }
		public virtual DbSet<RevAccount> RevAccounts { get; set; }
		public virtual DbSet<RevBankAccount> RevBankAccounts { get; set; }
		public virtual DbSet<RevTransaction> RevTransactions { get; set; }
		public virtual DbSet<Budget> Budgets { get; set; }
	}
}
