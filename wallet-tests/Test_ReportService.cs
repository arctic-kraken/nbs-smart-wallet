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
using smart_wallet.Models.DbSets;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace wallet_tests
{
	[TestClass]
	public sealed class Test_ReportService
	{
		public nbsDbContext db;
		public Mock<AppService> appService;
		public ReportService reportService;


		[TestInitialize]
		public void Init()
		{
			db = TestDataHelper.PrepTestDatabase();
			appService = TestDataHelper.PrepAndGetAppServiceMoq();

			var testUser = TestDataHelper.TestUsers().First();

			appService.Setup(x => x.WhoIsCurrentUser()).Returns(Guid.Parse(testUser.Id));

			reportService = new ReportService(db, appService.Object);
		}

		[TestMethod]
		public void Test_GetBudgetSpendingFor()
		{
			var accountId = TestDataHelper.GetTestRevAccountId();
			// Arrange // omitted

			// Act 
			var baseResult = reportService.GetBudgetSpendingFor(accountId, 1, 2026);

			// Assert
			Assert.IsNotEmpty(baseResult);
			Assert.HasCount(expected: 4, baseResult);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BudgetName == "Amazon"));
			Assert.AreEqual(expected: 10m, actual: baseResult.First(x => x.BudgetName == "Amazon").BudgetTotal);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BudgetName == "Deliveroo"));
			Assert.AreEqual(expected: 30.90m, actual: baseResult.First(x => x.BudgetName == "Deliveroo").BudgetTotal);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BudgetName == "Them Bills"));
			Assert.AreEqual(expected: 200.20m, actual: baseResult.First(x => x.BudgetName == "Them Bills").BudgetTotal);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BudgetName == "Unassigned"));
			Assert.AreEqual(expected: 0, actual: baseResult.First(x => x.BudgetName == "Unassigned").BudgetTotal);

			// Arrange // omitted

			// Act
			var result = reportService.GetBudgetSpendingFor(accountId, 1, 2027);

			// Assert
			Assert.IsNotEmpty(result);
			Assert.HasCount(expected: 4, result);
			Assert.IsNotNull(result.FirstOrDefault(x => x.BudgetName == "Amazon"));
			Assert.AreEqual(expected: 0, actual: result.First(x => x.BudgetName == "Amazon").BudgetTotal);
			Assert.IsNotNull(result.FirstOrDefault(x => x.BudgetName == "Deliveroo"));
			Assert.AreEqual(expected: 0, actual: result.First(x => x.BudgetName == "Deliveroo").BudgetTotal);
			Assert.IsNotNull(result.FirstOrDefault(x => x.BudgetName == "Them Bills"));
			Assert.AreEqual(expected: 0, actual: result.First(x => x.BudgetName == "Them Bills").BudgetTotal);
			Assert.IsNotNull(result.FirstOrDefault(x => x.BudgetName == "Unassigned"));
			Assert.AreEqual(expected: 0, actual: result.First(x => x.BudgetName == "Unassigned").BudgetTotal);
		}

		[TestMethod]
		public void Test_GetSpendingsFor()
		{
			var accountId = TestDataHelper.GetTestRevAccountId();
			// Arrange // omitted

			// Act 
			var baseResult = reportService.GetSpendingsFor(accountId, 1, 2026);

			// Assert
			Assert.IsNotEmpty(baseResult);
			Assert.HasCount(expected: 4, baseResult);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BookingDate == new DateTime(2026, 1, 1)));
			Assert.AreEqual(expected: 140m, actual: baseResult.First(x => x.BookingDate == new DateTime(2026, 1, 1)).Balance);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BookingDate == new DateTime(2026, 1, 5)));
			Assert.AreEqual(expected: 100m, actual: baseResult.First(x => x.BookingDate == new DateTime(2026, 1, 5)).Balance);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BookingDate == new DateTime(2026, 1, 17)));
			Assert.AreEqual(expected: 100m, actual: baseResult.First(x => x.BookingDate == new DateTime(2026, 1, 17)).Balance);
			Assert.IsNotNull(baseResult.FirstOrDefault(x => x.BookingDate == new DateTime(2026, 1, 25)));
			Assert.AreEqual(expected: 2100m, actual: baseResult.First(x => x.BookingDate == new DateTime(2026, 1, 25)).Balance);

			// Arrange // omitted

			// Act
			var result = reportService.GetSpendingsFor(accountId, 1, 2027);

			// Assert
			Assert.IsEmpty(result);

		}

		[TestCleanup]
		public void Cleanup()
		{
			db.Database.EnsureDeleted();
		}
	}
}
