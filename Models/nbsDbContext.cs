using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models.Authentication;

namespace nbs_smart_wallet.Models
{
	public class nbsDbContext : IdentityDbContext<ApplicationUser>
	{
		public nbsDbContext(DbContextOptions options) : base(options) { }

		public DbSet<ApplicationUser> Users { get; set; }
	}
}
