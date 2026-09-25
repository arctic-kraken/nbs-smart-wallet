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
		}

		[HttpGet]
		public IActionResult AccountList()
		{
			return View(_revolut.GetAccounts());
		}

		[HttpGet]
		public IActionResult Spendings(int id)
		{
			var model = new SpendingsModel();
			var account = _revolut.GetAccount(id);
			if (account == null)
			{
				return NotFound();
			}

			model.Spendings = _service.GetSpendingsFor(account.RevAccountId, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
			model.BudgetSpending = _service.GetBudgetSpendingFor(account.RevAccountId, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
			
			return View(model);
		}
	}
}
