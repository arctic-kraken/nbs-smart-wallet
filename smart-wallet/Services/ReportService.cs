using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.DbSets;
using Newtonsoft.Json;
using System.Buffers;

namespace nbs_smart_wallet.Services
{
	public class ReportService
	{
		private nbsDbContext _db;
		private AppService _app;

		public ReportService(nbsDbContext context, AppService appService)
		{
			_db = context;
			_app = appService;
		}

		public class DailySpendings
		{
			public DateTime BookingDate { get; set; }
			public decimal Balance { get; set; }
		}
		public class BudgetSpendings
		{
			public string BudgetName { get; set; } = string.Empty;
			public decimal BudgetTotal { get; set; }
		}

		public class IndicatorTotal
		{
			public string IndicatorName { get; set; }
			public decimal TotalSum { get; set; }
		}

		public List<IndicatorTotal> GetCreditDebitTotal(Guid revAccountId, int month, int year)
		{
			var userId = _app.WhoIsCurrentUser();
			var account = _db.RevAccounts.FirstOrDefault(x => x.AspNetUserId == userId && x.RevAccountId == revAccountId);
			if (account == null)
				return new List<IndicatorTotal>();

			var startOfMonth = new DateTime(year, month, 1);
			var endOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
			var debitTrxs = _db.RevTransactions
				.Where(x => x.BookingDateTime.ToUniversalTime() >= startOfMonth && x.BookingDateTime.ToUniversalTime() <= endOfMonth
				&& x.RevAccountId == account.RevAccountId && x.CreditDebitIndicator == AppConsts.CreditDebitIndicator.Debit)
				.OrderBy(x => x.BookingDateTime.ToUniversalTime())
				.ToList();

			var creditTrxs = _db.RevTransactions
				.Where(x => x.BookingDateTime.ToUniversalTime() >= startOfMonth && x.BookingDateTime.ToUniversalTime() <= endOfMonth
				&& x.RevAccountId == account.RevAccountId && x.CreditDebitIndicator == AppConsts.CreditDebitIndicator.Credit)
				.OrderBy(x => x.BookingDateTime.ToUniversalTime())
				.ToList();



			return new List<IndicatorTotal>
			{
				new IndicatorTotal { IndicatorName = "Total Credits", TotalSum = creditTrxs.Select(x => x.Amount).Sum() },
				new IndicatorTotal { IndicatorName = "Total Debits", TotalSum = debitTrxs.Select(x => x.Amount).Sum() }
			};
		}

		public List<DailySpendings> GetSpendingsFor(Guid revAccountId, int month, int year)
		{
			var userId = _app.WhoIsCurrentUser();
			var account = _db.RevAccounts.FirstOrDefault(x => x.AspNetUserId == userId && x.RevAccountId == revAccountId);
			if (account == null)
				return new List<DailySpendings>();

			var startOfMonth = new DateTime(year, month, 1);
			var endOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
			var trxs = _db.RevTransactions
				.Where(x => x.BookingDateTime.ToUniversalTime() >= startOfMonth && x.BookingDateTime.ToUniversalTime() <= endOfMonth && x.RevAccountId == account.RevAccountId)
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

		public List<BudgetSpendings> GetBudgetIncomeFor(Guid revAccountId, int month, int year) =>
			GetBudgetFiguresFor(revAccountId, month, year, AppConsts.CreditDebitIndicator.Debit);

		public List<BudgetSpendings> GetBudgetSpendingFor(Guid revAccountId, int month, int year) =>
			GetBudgetFiguresFor(revAccountId, month, year, AppConsts.CreditDebitIndicator.Credit);

		public List<BudgetSpendings> GetBudgetFiguresFor(Guid revAccountId, int month, int year, string indicator)
		{
			var userId = _app.WhoIsCurrentUser();
			var account = _db.RevAccounts.FirstOrDefault(x => x.AspNetUserId == userId && x.RevAccountId == revAccountId);
			if (account == null)
				return new List<BudgetSpendings>();

			var startOfMonth = new DateTime(year, month, 1);
			var endOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
			var trxs = _db.RevTransactions
				.Where(x => x.BookingDateTime.ToUniversalTime() >= startOfMonth && x.BookingDateTime.ToUniversalTime() <= endOfMonth 
				&& x.RevAccountId == account.RevAccountId && x.CreditDebitIndicator == indicator)
				.OrderBy(x => x.BookingDateTime.ToUniversalTime())
				.ToList();

			var budgets = _db.Budgets.Where(x => x.UserId == userId).ToList();
			if (!budgets.Any())
				return new List<BudgetSpendings>();

			var budgetSpending = new List<BudgetSpendings>();
			foreach (var budget in budgets)
			{
				var clauses = JsonConvert.DeserializeObject<List<string>>(budget.Clauses);
				if (clauses == null)
					continue;

				var amounts = trxs.Where(x => clauses.Any(y => x.TransactionInformation.Contains(y))).Select(x => x.Amount);
				budgetSpending.Add(new BudgetSpendings
				{
					BudgetName = budget.Title,
					BudgetTotal = amounts.Sum()
				});

				trxs = trxs.Where(x => !clauses.Any(y => x.TransactionInformation.Contains(y))).ToList();
			}

			var leftoverAmounts = trxs.Select(x => x.Amount);
			budgetSpending.Add(new BudgetSpendings
			{
				BudgetName = "Unassigned",
				BudgetTotal = leftoverAmounts.Sum()
			});

			return budgetSpending;
		}
	}
}
