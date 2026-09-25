using nbs_smart_wallet.Models;
using Newtonsoft.Json;
using smart_wallet.Models.DbSets;

namespace nbs_smart_wallet.Services
{
	public class BudgetService
	{
		private nbsDbContext _db;
		private AppService _app;

		public BudgetService(nbsDbContext context, AppService appService)
		{
			_db = context;
			_app = appService;
		}

		public List<Budget> GetUserBudgets()
		{
			var userId = _app.WhoIsCurrentUser();
			
			return _db.Budgets.Where(x => x.UserId == userId).ToList();
		}

		public Budget? GetBudget(int id)
		{
			var userId = _app.WhoIsCurrentUser();
			return _db.Budgets.FirstOrDefault(x => x.Id == id && x.UserId == userId);
		}

		public Budget NewBudget()
		{
			var userId = _app.WhoIsCurrentUser();
			var budget = new Budget
			{
				Amount = 0,
				Title = "",
				UserId = userId
			};
			_db.Budgets.Add(budget);
			_db.SaveChanges();

			return budget;
		}

		public bool AddClauseToBudget(int id, string clause)
		{
			if (String.IsNullOrEmpty(clause))
				return false;

			var budget = GetBudget(id);
			if (budget == null)
				return false;

			var clauses = JsonConvert.DeserializeObject<List<string>>(budget.Clauses);
			if (clauses == null)
				clauses = new List<string>();

			clauses.Add(clause);
			budget.Clauses = JsonConvert.SerializeObject(clauses);
			_db.SaveChanges();

			return true;
		}

		public bool UpdateTitleAndAmount(int id, Budget modified)
		{
			var budget = GetBudget(id);
			if (budget == null)
				return false;

			budget.Title = modified.Title;
			budget.Amount = modified.Amount;
			_db.SaveChanges();

			return true;
		}
	}
}
