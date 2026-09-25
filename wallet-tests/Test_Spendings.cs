using nbs_smart_wallet.Models;
using Moq;
using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models.DbSets;
using Moq.EntityFrameworkCore;

namespace wallet_tests
{
	[TestClass]
	public sealed class Test_Spendings
	{
		public Mock<nbsDbContext> m_db = new Mock<nbsDbContext>();
			

		[TestInitialize]
		public void Init()
		{
			
		}

		[TestMethod]
		public void TestMethod1()
		{
			m_db.Setup(x => x.RevAccounts)
				.ReturnsDbSet(TestDataHelper.TestAccounts());
		}
	}
}
