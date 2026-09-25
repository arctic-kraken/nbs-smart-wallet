using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;
using smart_wallet.Models.DbSets;

namespace nbs_smart_wallet.Controllers
{
	[Authorize]
	public class BudgetController : Controller
	{
		public BudgetService _service;
		public BudgetController(BudgetService budgetService) {
			_service = budgetService;
		}

		public IActionResult List()
		{
			var budgets = _service.GetUserBudgets();
			return View(budgets);
		}

		[HttpGet]
		public IActionResult Edit(int? id)
		{
			if (id != null)
				return View(_service.GetBudget((int)id));

			var newBudget = _service.NewBudget();

			return View(newBudget);
		}

		[HttpPost]
		public IActionResult Edit(int id, Budget modified)
		{
			if (!_service.UpdateTitleAndAmount(id, modified))
			{
				TempData["errorMessages"] = new string[] { $"Failed to save budget" };
				return View(_service.GetBudget(id));
			}

			TempData["infoMessages"] = new string[] { $"Budget saved successfully!" };
			return RedirectToAction("List");
		}

		[HttpGet]
		[Route("/Budget/Edit/{id}/AddClause/{clause}")]
		public IActionResult AddClause(int id, string clause)
		{
			if (!_service.AddClauseToBudget(id, clause))
				TempData["errorMessages"] = new string[] { $"Failed to add clause: {clause}" };
			else
				TempData["infoMessages"] = new string[] { $"Clause added successfully!" };

			return Redirect($"/Budget/Edit/{id}");
		}

		[HttpGet]
		[Route("/Budget/Edit/{id}/DeleteClause/{clause}")]
		[Authorize(Roles = "Admin")]
		public IActionResult DeleteClause(int id, string clause)
		{
			if (!_service.DeleteClauseFromBudget(id, clause))
				TempData["errorMessages"] = new string[] { $"Failed to remove clause: {clause}" };
			else
				TempData["infoMessages"] = new string[] { $"Clause removed successfully!" };

			return Redirect($"/Budget/Edit/{id}");
		}

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			if (!_service.DeleteBudget(id))
				TempData["errorMessages"] = new string[] { $"Failed to remove budget" };
			else
				TempData["infoMessages"] = new string[] { $"Budget removed successfully!" };

			return RedirectToAction("List");
		}
	}
}
