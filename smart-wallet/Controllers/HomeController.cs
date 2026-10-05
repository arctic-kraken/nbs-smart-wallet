using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using Serilog;
using smart_wallet.Models.Authentication;
using System.Diagnostics;

namespace nbs_smart_wallet.Controllers;

[Authorize]
public class HomeController : Controller
{
    private SignInManager<ApplicationUser> _signInManager;
    private UserManager<ApplicationUser> _userManager;
    private RoleManager<ApplicationRole> _roleManager;
    public HomeController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult Index()
    {
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		Log.Information("Serving Home/Index page for {ip}", requestIp);
		return View();
    }

    [AllowAnonymous]
    public IActionResult Landing(string infoMessages)
    {
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		Log.Information("Serving public Landing page for {ip}", requestIp);

		return View();
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		Log.Information("Serving public Register page for {ip}", requestIp);
		return View();
    }

	[HttpGet]
	public IActionResult LogOut()
	{
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		Log.Information("Serving LogOut page for {ip}", requestIp);
		return View();
	}

	[HttpPost]
	public async Task<ActionResult> LogOutConfirm()
	{
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		try
		{
			Log.Information("LogOut request begun for {ip}", requestIp);
			await _signInManager.SignOutAsync();
			Log.Information("LogOut request successful for {ip}", requestIp);

			return RedirectToAction("Landing");
		} catch (Exception ex) 
		{
			Log.Error(ex, $"Unexpected error occured in {nameof(HomeController)} when logging out");
			return RedirectToAction("Error");
		}
	}

	[HttpPost]
    [AllowAnonymous]
	public async Task<ActionResult> Register(Register request)
	{
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		try
        {
			Log.Information("Registration request begun processing for {ip}", requestIp);

			var errorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();
			if (!ModelState.IsValid)
			{
				Log.Error("Registration request failed for {ip} with errors: {errs}", requestIp, errorMessages);
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
				Log.Error("Registration request failed for {ip} with errors: {errs}", requestIp, errorMessages);
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
				Log.Error("Registration request failed for {ip} with errors: {errs}", requestIp, errorMessages);
				return View("Register", new Register
				{
					errorMessages = errorMessages
				});
			}

			var addRoleResult = await _userManager.AddToRoleAsync(newUser, "Admin");
			if (!addRoleResult.Succeeded)
			{
				errorMessages.AddRange(result.Errors.Select(x => x.Description));
				Log.Error("Registration request failed for {ip} with errors: {errs}", requestIp, errorMessages);
				return View("Register", new Register
				{
					errorMessages = errorMessages
				});
			}

			TempData["infoMessages"] = new string[] { "Registration successfull!" };
			Log.Information("Registration successful for {ip}", requestIp);
			return RedirectToAction("Landing");
		}
        catch (Exception ex) 
        {
			Log.Error(ex, $"Unexpected error occured in {nameof(HomeController)}");
			return RedirectToAction("Error");
		}
	}

	[HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> Login(Login request)
    {
		var requestIp = HttpContext?.Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
		try
		{
			Log.Information("Login request begun processing for {ip}", requestIp);
			var rememberMe = false;
			var errorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();

			if (!ModelState.IsValid)
			{
				Log.Warning("Login request unsuccessful for {ip} with errors: {errs}", requestIp, errorMessages);
				return View("Landing", new Login
				{
					errorMessages = errorMessages
				});
			}

			var result = await _signInManager.PasswordSignInAsync(
				request.Username, request.Password, isPersistent: rememberMe, lockoutOnFailure: false);

			if (result.Succeeded)
			{
				Log.Information("Login request successful for {ip}", requestIp);
				return RedirectToAction("Index", "Home");
			}
			
			errorMessages.Add("Invalid Username or Password");
			Log.Warning("Login request unsuccessful for {ip} with errors: {errs}", requestIp, errorMessages);

			return View("Landing", new Login { errorMessages = errorMessages });
		} catch (Exception ex)
		{
			Log.Error(ex, $"Unexpected error occured in {nameof(HomeController)} when logging in");
			return RedirectToAction("Error");
		}
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
