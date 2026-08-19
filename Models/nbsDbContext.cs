using Microsoft.EntityFrameworkCore;

namespace nbs_smart_wallet.Models
{
	public class nbsDbContext : DbContext
	{
		public nbsDbContext(DbContextOptions options) : base(options) { }


	}
}
