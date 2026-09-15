using nbs_smart_wallet.Models;

namespace nbs_smart_wallet.Services
{
	public class ReportService
	{
		private nbsDbContext _db;

		public ReportService(nbsDbContext context)
		{
			_db = context;
		}

		public List<decimal> GetSpendingsFor(int month, int year)
		{
			var startOfMonth = new DateTime(year, month, 1);
			var endOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
			var trxs = _db.RevTransactions
				.Where(x => x.BookingDateTime >= startOfMonth && x.BookingDateTime <= endOfMonth)
				.OrderByDescending(x => x.BookingDateTime)
				.ToList();
			//Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported. Note that it's not possible to mix DateTimes with different Kinds in an array, range, or multirange. (Parameter 'value')'


			var amounts = trxs.Select(x => x.BalanceAmount).ToList();

			return amounts;
		}
	}
}
