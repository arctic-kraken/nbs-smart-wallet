using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Models.DbSets;
using nbs_smart_wallet.Models.Revolut;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using smart_wallet.Models.DbSets;
using System.Xml;

namespace wallet_tests
{
	public class TestDataHelper
	{
		public static Guid GetTestUserId() => Guid.Parse("99184ebe-f49b-429e-86da-c24fdbedbd5b");
		public static Guid GetTestRevAccountId() => Guid.Parse("61dc1e97-cd15-4004-a208-5103215b32cd");

		public static List<RevAccount> TestAccounts()
		{
			return new List<RevAccount>
			{
				new RevAccount
				{
					RevAccountId = GetTestRevAccountId(),
					AspNetUserId = GetTestUserId(),
				},
			};
		}

		public static List<ApplicationUser> TestUsers()
		{
			return new List<ApplicationUser> {
				new ApplicationUser
				{
					Id = GetTestUserId().ToString(),
					UserName = "Test",

				}
			};
		}

		public static RevTransaction GetBaseTestTransaction()
		{
			return new RevTransaction
			{
				RevAccountId = GetTestRevAccountId(),
				CurrencyExchangeJson = JsonConvert.SerializeObject(new CurrencyExchange()),
				CreditDebitIndicator = AppConsts.Accounting.Credit,
				RevCreditorAccountJson = JsonConvert.SerializeObject(new BankAccount()),
				RevDebtorAccountJson = JsonConvert.SerializeObject(new BankAccount()),
				SupplementaryData = JsonConvert.SerializeObject(""),
				TransactionInformation = "Amazon",
				Currency = AppConsts.Currency.BritishPound,
				BalanceCurrency = AppConsts.Currency.BritishPound,
				Amount = 10.00m,
				BalanceAmount = 140m,
				BookingDateTime = new DateTime(2026, 1, 1),
				ValueDateTime = new DateTime(2026, 1, 1),
				Status = "Booked",
			};
		}

		public static List<RevTransaction> TestTransactions()
		{
			var baseTrx = GetBaseTestTransaction();

			var deliveroo_trx = GetBaseTestTransaction();
			deliveroo_trx.Amount = 30.90m;
			deliveroo_trx.BalanceAmount = 100.00m;
			deliveroo_trx.TransactionInformation = "Deliveroo";
			deliveroo_trx.CreditDebitIndicator = AppConsts.Accounting.Credit;
			deliveroo_trx.BookingDateTime = new DateTime(2026, 1, 5);
			deliveroo_trx.ValueDateTime = new DateTime(2026, 1, 5);

			var bills_trx = GetBaseTestTransaction();
			bills_trx.Amount = 100.10m;
			bills_trx.BalanceAmount = 100.00m;
			bills_trx.CreditDebitIndicator = AppConsts.Accounting.Credit;
			bills_trx.TransactionInformation = "Bills";
			bills_trx.BookingDateTime = new DateTime(2026, 1, 17);
			bills_trx.ValueDateTime = new DateTime(2026, 1, 17);

			var bills_trx2 = GetBaseTestTransaction();
			bills_trx2.Amount = 100.10m;
			bills_trx2.BalanceAmount = 100.00m;
			bills_trx2.CreditDebitIndicator = AppConsts.Accounting.Credit;
			bills_trx2.TransactionInformation = "Bills";
			bills_trx2.BookingDateTime = new DateTime(2026, 1, 17);
			bills_trx2.ValueDateTime = new DateTime(2026, 1, 17);

			var income_trx = GetBaseTestTransaction();
			income_trx.Amount = 2000.00m;
			income_trx.BalanceAmount = 2100.00m;
			income_trx.CreditDebitIndicator = AppConsts.Accounting.Debit;
			income_trx.TransactionInformation = "Paycheck";
			income_trx.BookingDateTime = new DateTime(2026, 1, 25);
			income_trx.ValueDateTime = new DateTime(2026, 1, 25);

			return new List<RevTransaction>
			{
				baseTrx,
				deliveroo_trx,
				bills_trx,
				bills_trx2,
				income_trx				
			};
		}

		public static List<Budget> TestBudgets()
		{
			return new List<Budget>
			{
				new Budget
				{
					Id = 1,
					Amount = 100m,
					Title = "Amazon",
					UserId = GetTestUserId(),
					Clauses = JsonConvert.SerializeObject(new List<string> {
						"Amazon"
					})
				},
				new Budget
				{
					Id = 2,
					Amount = 100m,
					Title = "Deliveroo",
					UserId = GetTestUserId(),
					Clauses = JsonConvert.SerializeObject(new List<string> {
						"Deliveroo"
					})
				},
				new Budget
				{
					Id = 3,
					Amount = 100m,
					Title = "Them Bills",
					UserId = GetTestUserId(),
					Clauses = JsonConvert.SerializeObject(new List<string> {
						"Bills"
					})
				},
			};
		}

		public static nbsDbContext PrepTestDatabase()
		{
			var options = new DbContextOptionsBuilder<nbsDbContext>()
				.UseInMemoryDatabase(databaseName: "test_db")
				.Options;

			var m_db = new nbsDbContext(options);

			m_db.RevAccounts.AddRange(TestAccounts());
			m_db.Users.AddRange(TestUsers());
			m_db.RevTransactions.AddRange(TestTransactions());
			m_db.Budgets.AddRange(TestBudgets());
			m_db.SaveChanges();

			return m_db;
		}

		public static Mock<UserManager<ApplicationUser>> PrepAndGetUserManagerMoq()
		{
			var store = new Mock<IUserStore<ApplicationUser>>();
			var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
			userManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
			userManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

			return userManager;
		}

		public static Mock<AppService> PrepAndGetAppServiceMoq()
		{
			var accessor = new Mock<IHttpContextAccessor>();
			var userManager = PrepAndGetUserManagerMoq();

			return new Mock<AppService>(accessor.Object, userManager.Object);
		}
	}
}
