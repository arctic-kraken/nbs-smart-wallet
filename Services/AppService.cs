using Microsoft.AspNetCore.Identity;
using nbs_smart_wallet.Models.Authentication;

namespace nbs_smart_wallet.Services
{
	public class AppService
	{
		private IHttpContextAccessor _accessor;
		private UserManager<ApplicationUser> _userManager;
		public AppService(IHttpContextAccessor contextAccessor, UserManager<ApplicationUser> userManager)
		{
			_accessor = contextAccessor;
			_userManager = userManager;
		}

		public string WhoIsCurrentUser()
		{
			var id = _userManager.GetUserId(_accessor.HttpContext.User);


			return id;
		}

		//public static string WhoIsCurrentUser(UserManager<ApplicationUser> manager, HttpContext context)
		//{
		//	string id = manager.GetUserId(context.User);

		//	return id;
		//}

	}
}
