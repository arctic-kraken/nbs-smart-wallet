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

		public class DailySpendings
		{
			public DateTime BookingDate { get; set; }
			public decimal Balance { get; set; }
		}

		public List<DailySpendings> GetSpendingsFor(int month, int year)
		{
			var startOfMonth = new DateTime(year, month, 1);
			var endOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
			var trxs = _db.RevTransactions
				.Where(x => x.BookingDateTime.ToUniversalTime() >= startOfMonth && x.BookingDateTime.ToUniversalTime() <= endOfMonth)
				.OrderBy(x => x.BookingDateTime.ToUniversalTime())
				.ToList();

			trxs = trxs.DistinctBy(x => x.BookingDateTime.Date).ToList();
			var spendings = new List<DailySpendings>();
			foreach (var trx in trxs)
			{
				spendings.Add(
					new DailySpendings
					{
						BookingDate = trx.BookingDateTime,
						Balance = trx.BalanceAmount
					}
				);
			}
			
			return spendings;
		}
	}
}
