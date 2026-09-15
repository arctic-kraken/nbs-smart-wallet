using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;

namespace nbs_smart_wallet.Controllers
{
	[Authorize]
	public class ReportController : Controller
	{
		private ReportService _serivce;
		public ReportController(ReportService reportService) {
			_serivce = reportService;
		}

		public IActionResult Index()
		{
			return View();
		}

		public class SpendingsModel
		{
			public List<decimal> currentMonthBalances = new List<decimal>();
			public List<int> dayNumbers = new List<int>();
		}

		public IActionResult Spendings()
		{
			var model = new SpendingsModel();
			model.currentMonthBalances = _serivce.GetSpendingsFor(8, 2026);
			var endOfMonth = new DateTime(2026, 8, 1).AddMonths(1).AddDays(-1);
			
			for (int i = 1; i <= endOfMonth.Day; i++)
				model.dayNumbers.Add(i);

			return View(model);
		}
	}
}
