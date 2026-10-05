using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;
using Serilog;

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
			var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
			Log.Information("Serving Reports Account List page for {ip}", requestIp);

			return View(_revolut.GetAccounts());
		}

		[HttpGet]
		public IActionResult Spendings(int id, int? month, int? year)
		{
			var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Serving Spendings page for {ip}", requestIp);
				var model = new SpendingsModel();
				var account = _revolut.GetAccount(id);
				if (account == null)
				{
					Log.Warning("Failed to find account for {ip}, account no. {id}", requestIp, id);
					return NotFound();
				}

				if ((month == null) || (month <= 0 || month > 12))
					month = DateTime.UtcNow.Month;

				if (year == null || (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year))
					year = DateTime.UtcNow.Year;

				model.selectedMonth = (int)month;
				model.selectedYear = (int)year;
				model.AccountId = id;
				Log.Information("Beginning calculations for spendings for {ip}", requestIp);
				model.Spendings = _service.GetSpendingsFor(account.RevAccountId, (int)month, (int)year);
				model.BudgetSpending = _service.GetBudgetSpendingFor(account.RevAccountId, (int)month, (int)year);
				model.BudgetIncome = _service.GetBudgetIncomeFor(account.RevAccountId, (int)month, (int)year);
				model.IndicatorTotals = _service.GetCreditDebitTotal(account.RevAccountId, (int)month, (int)year);
				Log.Information("Spending calculations complete for {ip}, serving page", requestIp);

				return View(model);
			}
			catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpPost]
		public IActionResult Spendings(int id, SpendingsModel model)
		{
			var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
			Log.Information("Changed viewing month, {ip} redirected to Get Spendings endpoint", requestIp);

			return Spendings(id, model.selectedMonth, model.selectedYear);
		}
	}
}
