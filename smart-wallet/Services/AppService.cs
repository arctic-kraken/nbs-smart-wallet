using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using nbs_smart_wallet.Models.Authentication;
using Serilog;

namespace nbs_smart_wallet.Services
{
	public class AppService
	{
		private IHttpContextAccessor _accessor;
		private UserManager<ApplicationUser> _userManager;
		private readonly IDataProtector _protector;
		public AppService(IHttpContextAccessor contextAccessor, UserManager<ApplicationUser> userManager, IDataProtectionProvider protectionProvider)
		{
			_accessor = contextAccessor;
			_userManager = userManager;
			_protector = protectionProvider.CreateProtector("AppProtection");
		}

		public virtual Guid WhoIsCurrentUser()
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

		public string Encrypt(string plainText)
		{
			return _protector.Protect(plainText);
		}

		public string Decrypt(string encryptedText)
		{
			try
			{
				return _protector.Unprotect(encryptedText);
			} catch (Exception ex)
			{
				Log.Error(ex, "Decryption Failed for {text}", encryptedText);
				return "Decryption Failed";
			}
			
		}

	}
}
