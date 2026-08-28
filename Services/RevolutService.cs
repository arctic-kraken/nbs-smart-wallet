using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.DbSets;
using nbs_smart_wallet.Models.Revolut;
using Newtonsoft.Json;
using Serilog;

namespace nbs_smart_wallet.Services
{
	public class RevolutService
	{
		private RevolutProxy _proxy;
		private nbsDbContext _db;
		private AppService _app;
		public RevolutService(RevolutProxy proxy, nbsDbContext dbContext, AppService appService)
		{
			_proxy = proxy;
			_db = dbContext;
			_app = appService;
		}

		public List<AppAccount> GetAccountsSeed()
		{
			string json = """
				[
				  {
				    "accountId": "6c27fab0-1624-424a-97a2-b3ff77eeb272",
				    "currency": "GBP",
				    "accountType": "Personal",
				    "accountSubType": "CurrentAccount",
				    "nickname": ""
				  },
				  {
				    "accountId": "7c9357bf-36c8-4808-9a7a-16c173b99283",
				    "currency": "USD",
				    "accountType": "Personal",
				    "accountSubType": "CurrentAccount",
				    "nickname": ""
				  },
				  {
				    "accountId": "1286ce66-7e54-4a3c-b941-2673ce19f85a",
				    "currency": "EUR",
				    "accountType": "Personal",
				    "accountSubType": "CurrentAccount",
				    "nickname": ""
				  }
				]
				""";
			var accounts = JsonConvert.DeserializeObject<List<AppAccount>>(json);

			return accounts;
		}

		public Transaction GetTestTransaction()
		{
			string json = """
								{
					"AccountId": "6c27fab0-1624-424a-97a2-b3ff77eeb272",
					"Amount": {
					  "Amount": "20.54",
					  "Currency": "GBP"
					},
					"Balance": {
					  "Amount": {
						"Amount": "2088.55",
						"Currency": "GBP"
					  },
					  "CreditDebitIndicator": "Credit",
					  "Type": "InterimBooked"
					},
					"BookingDateTime": "2024-12-27T06:05:20.625880Z",
					"ValueDateTime": "2024-12-27T06:05:21.162872Z",
					"CreditDebitIndicator": "Debit",
					"CurrencyExchange": {
					  "InstructedAmount": {
						"Amount": "20.54",
						"Currency": "GBP"
					  },
					  "SourceCurrency": "GBP",
					  "TargetCurrency": "EUR",
					  "UnitCurrency": "GBP",
					  "ExchangeRate": "1.07123405660663439638574742941974304"
					},
					"CreditorAccount": {
					  "SchemeName": "UK.OBIE.IBAN",
					  "Identification": "LT111111111111111111",
					  "Name": "Receiver Co."
					},
					"DebtorAccount": {
					  "SchemeName": "UK.OBIE.IBAN",
					  "Identification": "GB95REVO00997053872360",
					  "Name": "John Doe"
					},
					"ProprietaryBankTransactionCode": {
					  "Code": "TRANSFER",
					  "Issuer": "Revolut"
					},
					"Status": "Booked",
					"TransactionId": "fdd62279-ed58-4eb8-9bf1-ea9b7821bf4a",
					"TransactionInformation": "To Receiver Co.",
					"SupplementaryData": {
					  "UserComments": "test"
					}
				}
				""";
			var trx = JsonConvert.DeserializeObject<Transaction>(json);

			return trx;
		}

		public void SyncRevAccounts()
		{
			var currentUserId = _app.WhoIsCurrentUser();
			var accs = GetAccountsSeed();

			var trx = GetTestTransaction();

			foreach (var a in accs)
			{
				_db.RevAccounts.Add(new RevAccount
				{
					AspNetUserId = currentUserId,
					RevAccountId = a.AccountId,
					Currency = a.Currency,
					AccountType = a.AccountType,
					AccountSubType = a.AccountSubType,
					Nickname = a.Nickname,
				});
			}
			_db.SaveChanges();
			var acc = _db.RevAccounts
				.First(x => x.AspNetUserId == currentUserId && x.Currency == AppConsts.Currency.BritishPound);

			_db.RevTransactions.Add(new RevTransaction
			{
				RevAccountId = acc.RevAccountId,
				Amount = trx.Amount.Amount,
				Currency = trx.Amount.Currency,
				BalanceAmount = trx.Balance.Amount.Amount,
				BalanceCurrency = trx.Balance.Amount.Currency,
				BookingDateTime = trx.BookingDateTime,
				ValueDateTime = trx.ValueDateTime,
				CurrencyExchangeJson = JsonConvert.SerializeObject(trx.CurrencyExchange),
				CreditDebitIndicator = trx.CreditDebitIndicator,
				RevCreditorAccountJson = JsonConvert.SerializeObject(trx.CreditorAccount),
				RevDebtorAccountJson = JsonConvert.SerializeObject(trx.DebtorAccount),
				RevTransactionId = trx.TransactionId,
				Status = trx.Status,
				SupplementaryData = JsonConvert.SerializeObject(trx.SupplementaryData),
				TransactionInformation = trx.TransactionInformation
			});
			_db.SaveChanges();

			//return true;
		}

		public List<RevAccount> GetAccounts()
		{
			var userId = _app.WhoIsCurrentUser();

			var accounts = _db.RevAccounts.Where(x => x.AspNetUserId == userId).ToList();

			return accounts;
		}

		public List<RevTransaction> GetTransactionsFor(Guid accountId)
		{
			var userId = _app.WhoIsCurrentUser();

			var account = _db.RevAccounts.SingleOrDefault(x => x.RevAccountId == accountId && x.AspNetUserId == userId);
			if (account == null)
			{
				Log.Warning("Failed to find rev account {id} for user {uId} when getting transactions", accountId, userId);
				return new List<RevTransaction>();
			}

			var trxs = _db.RevTransactions.Where(x => x.RevAccountId == account.RevAccountId)
				.OrderByDescending(x => x.BookingDateTime)
				.Take(15)
				.ToList();

			return trxs;
		}
	}
}
