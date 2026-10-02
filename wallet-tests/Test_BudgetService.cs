using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using nbs_smart_wallet.Controllers;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Models.DbSets;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using smart_wallet.Models.DbSets;

namespace wallet_tests
{
	[TestClass]
	public sealed class Test_Budgets
	{
		public Mock<AppService> appService { get; set; }
		public BudgetService budgetService { get; set; }
		public BudgetController budgetController { get; set; }
		public nbsDbContext db { get; set; }

		public BudgetService PrepBudgetService()
		{
			var testUser = TestDataHelper.TestUsers().First();

			appService.Setup(x => x.WhoIsCurrentUser()).Returns(Guid.Parse(testUser.Id));

			return new BudgetService(db, appService.Object);
		}

		[TestInitialize]
		public void Init()
		{
			db = TestDataHelper.PrepTestDatabase();
			appService = TestDataHelper.PrepAndGetAppServiceMoq();
			budgetService = PrepBudgetService();
			budgetController = new BudgetController(budgetService);
		}

		[TestMethod]
		public void Test_CreateAndModifyBudget()
		{
			// Arrange 
			Assert.HasCount(expected: 3, db.Budgets);

			// Act 
			budgetController.Edit(null);
			var budgets = db.Budgets.ToList();

			// Assert
			Assert.HasCount(expected: 4, budgets);
			var newBudget = db.Budgets.First(x => x.Title == string.Empty);

			Assert.AreEqual(expected: 0, actual: newBudget.Amount);
			Assert.AreEqual(expected: JsonConvert.SerializeObject(new List<string>()), actual: newBudget.Clauses);
			Assert.AreEqual(expected: TestDataHelper.GetTestUserId(), actual: newBudget.UserId);

			// Arrange
			var modifiedBudget = new Budget
			{
				Id = newBudget.Id,
				UserId = TestDataHelper.GetTestUserId(),
				Title = "Test Budget no.1",
				Amount = 9.99m
			};

			// Act
			try
			{
				budgetController.Edit(newBudget.Id, modifiedBudget);
			} catch (Exception ex) 
			{
				// Endpoints do have TempData, which is null when testing, so we expect this
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when modifying a budget: " + ex.Message);
			}
			
			// Assert
			budgets = db.Budgets.ToList();
			var resultBudget = db.Budgets.First(x => x.Id == newBudget.Id);

			Assert.HasCount(expected: 4, budgets);

			Assert.AreEqual(expected: "Test Budget no.1", actual: resultBudget.Title);
			Assert.AreEqual(expected: 9.99m, actual: resultBudget.Amount);
			Assert.AreEqual(expected: JsonConvert.SerializeObject(new List<string>()), actual: resultBudget.Clauses);
			Assert.AreEqual(expected: TestDataHelper.GetTestUserId(), actual: resultBudget.UserId);

		}

		[TestMethod]
		public void Test_AddAndDeleteClauses()
		{
			// Arrange
			var budget = db.Budgets.First(x => x.Id == 1);
			var clauses = new List<string>
			{
				"amazon",
				"WHERE 1=1",
			};

			var currentClauses = JsonConvert.DeserializeObject<List<string>>(budget.Clauses);

			// Act
			try
			{
				budgetController.AddClause(1, clauses[0]);
			} catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when adding a clause to a budget: " + ex.Message);
			}

			try
			{
				budgetController.AddClause(1, clauses[1]);
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when adding a clause to a budget: " + ex.Message);
			}

			// Assert
			budget = db.Budgets.First(x => x.Id == 1);
			var modifiedClauses = JsonConvert.DeserializeObject<List<string>>(budget.Clauses);
			Assert.IsNotNull(currentClauses);
			Assert.IsNotNull(modifiedClauses);
			Assert.HasCount(expected: 1, currentClauses);
			Assert.HasCount(expected: 3, modifiedClauses);

			Assert.Contains(expected: "amazon", modifiedClauses);
			Assert.Contains(expected: "WHERE 1=1", modifiedClauses);

			// Arrange // omitted

			// Act
			try
			{
				budgetController.DeleteClause(1, clauses[0]);
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when deleting a clause from a budget: " + ex.Message);
			}

			try
			{
				budgetController.DeleteClause(1, clauses[1]);
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when deleting a clause from a budget: " + ex.Message);
			}

			// Assert
			budget = db.Budgets.First(x => x.Id == 1);
			var finalClauses = JsonConvert.DeserializeObject<List<string>>(budget.Clauses);
			Assert.IsNotNull(finalClauses);
			Assert.HasCount(expected: 1, finalClauses);

			Assert.DoesNotContain(notExpected: "amazon", finalClauses);
			Assert.DoesNotContain(notExpected: "WHERE 1=1", finalClauses);
		}

		[TestMethod]
		public void Test_DeleteBudget()
		{
			// Arrange 
			Assert.HasCount(expected: 3, db.Budgets);

			// Act 
			try
			{
				budgetController.Delete(2);
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
					Assert.Fail("An unexpected exception occured when deleting a clause from a budget: " + ex.Message);
			}

			// Assert
			var budgets = db.Budgets.ToList();
			Assert.HasCount(expected: 2, budgets);
			var deletedBudget = db.Budgets.FirstOrDefault(x => x.Id == 2);

			Assert.IsNull(deletedBudget);
		}

		[TestCleanup]
		public void Cleanup()
		{
			db.Database.EnsureDeleted();
		}
		
	}
}
