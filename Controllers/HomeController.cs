using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace nbs_smart_wallet.Controllers;

[Authorize]
public class HomeController : Controller
{
    private SignInManager<ApplicationUser> _signInManager;
    private UserManager<ApplicationUser> _userManager;
    public HomeController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Landing(string infoMessages)
    {

        return View();
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

	[HttpGet]
	public IActionResult LogOut()
	{
        return View();
	}

	[HttpPost]
	public async Task<ActionResult> LogOutConfirm()
	{
        await _signInManager.SignOutAsync();

		return RedirectToAction("Landing");
	}

	[HttpPost]
    [AllowAnonymous]
	public async Task<ActionResult> Register(Register request)
	{
        var errorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();
        if (!ModelState.IsValid)
        {
			return View("Register", new Register
			{
				errorMessages = errorMessages
			});
		}

        // activation email etc etc in the future will be nice
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            errorMessages.Add("A User with that e-mail already exists");
            return View("Register", new Register
            {
                errorMessages = errorMessages
            });
        }

		// clean strings!
		var newUser = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Username,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await _userManager.CreateAsync(newUser, request.Password);
        // if fail, go back to register page and show why it failed
        if (!result.Succeeded)
        {
            errorMessages.AddRange(result.Errors.Select(x => x.Description));
            return View("Register", new Register
            {
                errorMessages = errorMessages
            });
        }

        TempData["infoMessages"] = new string[] { "Registration successfull!" };
		return RedirectToAction("Landing");
	}

	[HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> Login(Login request)
    {
        var rememberMe = false;
        var errorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();

		if (!ModelState.IsValid)
        {
            return View("Landing", new Login
            {
                errorMessages = errorMessages
            });
		}

        var result = await _signInManager.PasswordSignInAsync(
            request.Username, request.Password, isPersistent: rememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        errorMessages.Add("Invalid Username or Password");
        return View("Landing", new Login { errorMessages = errorMessages });
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
