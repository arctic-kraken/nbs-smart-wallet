using Microsoft.AspNetCore.Identity;
using nbs_smart_wallet.Models.Authentication;
using Serilog;

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

		public Guid WhoIsCurrentUser()
		{
			if (_accessor.HttpContext == null)
				throw new ArgumentNullException($"{nameof(AppService)}: Called {nameof(WhoIsCurrentUser)} with null context");

			var guidStr = _userManager.GetUserId(_accessor.HttpContext.User);
			if (String.IsNullOrEmpty(guidStr))
			{
				// Should never happen
				string msg = "AppService : current user not found within context";
				Log.Error(msg);
				throw new Exception(msg);
			}

			return Guid.Parse(guidStr);
		}

	}
}
