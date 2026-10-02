using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;

namespace nbs_smart_wallet.Controllers
{
	[Authorize]
	public class ReportController : Controller
	{
		private ReportService _service;
		private RevolutService _revolut;
		public ReportController(ReportService reportService, RevolutService revolutService) {
			_service = reportService;
			_revolut = revolutService;
		}

		public IActionResult Index()
		{
			return View();
		}

		public class SpendingsModel
		{
			public List<ReportService.DailySpendings> Spendings { get; set; } = new List<ReportService.DailySpendings>();
			public List<ReportService.BudgetSpendings> BudgetSpending { get; set; } = new List<ReportService.BudgetSpendings>();
			public List<ReportService.BudgetSpendings> BudgetIncome { get; set; } = new List<ReportService.BudgetSpendings>();
			public List<ReportService.IndicatorTotal> IndicatorTotals { get; set; } = new List<ReportService.IndicatorTotal>();
			public int selectedMonth { get; set; }
			public int selectedYear { get; set; }
			public int AccountId { get; set; }
		}

		[HttpGet]
		public IActionResult AccountList()
		{
			return View(_revolut.GetAccounts());
		}

		[HttpGet]
		public IActionResult Spendings(int id, int? month, int? year)
		{
			var model = new SpendingsModel();
			var account = _revolut.GetAccount(id);
			if (account == null)
			{
				return NotFound();
			}

			if ((month == null) || (month <= 0 || month > 12))
				month = DateTime.UtcNow.Month;

			if (year == null || (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year))
				year = DateTime.UtcNow.Year;

			model.selectedMonth = (int)month;
			model.selectedYear = (int)year;
			model.AccountId = id;
			model.Spendings = _service.GetSpendingsFor(account.RevAccountId, (int)month, (int)year);
			model.BudgetSpending = _service.GetBudgetSpendingFor(account.RevAccountId, (int)month, (int)year);
			model.BudgetIncome = _service.GetBudgetIncomeFor(account.RevAccountId, (int)month, (int)year);
			model.IndicatorTotals = _service.GetCreditDebitTotal(account.RevAccountId, (int)month, (int)year);
			
			return View(model);
		}

		[HttpPost]
		public IActionResult Spendings(int id, SpendingsModel model)
		{
			return Spendings(id, model.selectedMonth, model.selectedYear);
		}
	}
}
