using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Revolut;
using Newtonsoft.Json;

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

		public async Task<List<AppAccount>?> GetAccountsSeed()
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
	}
}
