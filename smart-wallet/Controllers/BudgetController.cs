using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Services;
using Serilog;
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
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Serving Budget List for {ip}", requestIp);
				var budgets = _service.GetUserBudgets();
				Log.Information("Successfully received Budget List for {ip}, serving page", requestIp);

				return View(budgets);
			}
			catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		public IActionResult Edit(int? id)
		{
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Serving Budget page for {ip}", requestIp);
				if (id != null)
				{
					Log.Information("Displaying Budget edit page for {ip}, id: ", requestIp, id);
					return View(_service.GetBudget((int)id));
				}

				Log.Information("Creating new Budget for {ip}", requestIp);
				var newBudget = _service.NewBudget();
				Log.Information("Successfully created new Budget for {ip}, serving page", requestIp);

				return View(newBudget);
			} catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpPost]
		public IActionResult Edit(int id, Budget modified)
		{
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Posting changes for Budget for {ip}, id: {id}", requestIp, id);
				if (!_service.UpdateTitleAndAmount(id, modified))
				{
					Log.Warning("Failed posting changes for Budget for {ip}, id: {id}", requestIp, id);
					TempData["errorMessages"] = new string[] { $"Failed to save budget" };
					return View(_service.GetBudget(id));
				}

				TempData["infoMessages"] = new string[] { $"Budget saved successfully!" };
				Log.Information("Successfully posted changes for Budget for {ip}, id: {id}, serving List page", requestIp, id);
				return RedirectToAction("List");
			} catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		[Route("/Budget/Edit/{id}/AddClause/{clause}")]
		public IActionResult AddClause(int id, string clause)
		{
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Adding clause for Budget for {ip}, id: {id}", requestIp, id);
				if (!_service.AddClauseToBudget(id, clause))
				{
					Log.Warning("Failed to add clause to Budget for {ip}, id: {id}", requestIp, id);
					TempData["errorMessages"] = new string[] { $"Failed to add clause: {clause}" };
				}
				else
				{
					Log.Information("Successfully added clause to Budget for {ip}, id: {id}", requestIp, id);
					TempData["infoMessages"] = new string[] { $"Clause added successfully!" };
				}

				return Redirect($"/Budget/Edit/{id}");
			} catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		[Route("/Budget/Edit/{id}/DeleteClause/{clause}")]
		[Authorize(Roles = "Admin")]
		public IActionResult DeleteClause(int id, string clause)
		{
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Deleting clause from Budget for {ip}, id: {id}", requestIp, id);
				if (!_service.DeleteClauseFromBudget(id, clause))
				{
					Log.Warning("Failed to remove clause from Budget for {ip}, id: {id}", requestIp, id);
					TempData["errorMessages"] = new string[] { $"Failed to remove clause: {clause}" };
				}
				else
				{
					Log.Information("Successfully removed clause from Budget for {ip}, id: {id}", requestIp, id);
					TempData["infoMessages"] = new string[] { $"Clause removed successfully!" };
				}
				
				return Redirect($"/Budget/Edit/{id}");
			} catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
				return RedirectToAction("Index", "Home");
			}
		}

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
			try
			{
				Log.Information("Deleting Budget for {ip}, id: {id}", requestIp, id);
				if (!_service.DeleteBudget(id))
				{
					Log.Warning("Failed to delete Budget for {ip}, id: {id}", requestIp, id);
					TempData["errorMessages"] = new string[] { $"Failed to remove budget" };
				}
				else
				{
					Log.Information("Successfully deleted Budget for {ip}, id: {id}", requestIp, id);
					TempData["infoMessages"] = new string[] { $"Budget removed successfully!" };
				}

			} catch (Exception ex)
			{
				Log.Error(ex, ex.Message);
			}

			return RedirectToAction("List");
		}
	}
}
