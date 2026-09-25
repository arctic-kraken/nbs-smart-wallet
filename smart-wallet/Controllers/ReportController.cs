using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;

namespace nbs_smart_wallet.Controllers
{
	[Authorize]
	public class ReportController : Controller
	{
		private ReportService _service;
		public ReportController(ReportService reportService) {
			_service = reportService;
		}

		public IActionResult Index()
		{
			return View();
		}

		public class SpendingsModel
		{
			public List<ReportService.DailySpendings> Spendings { get; set; } = new List<ReportService.DailySpendings>();
		}

		public IActionResult Spendings()
		{
			var model = new SpendingsModel();
			
			model.Spendings = _service.GetSpendingsFor(DateTime.UtcNow.Month, DateTime.UtcNow.Year);
			
			return View(model);
		}
	}
}
