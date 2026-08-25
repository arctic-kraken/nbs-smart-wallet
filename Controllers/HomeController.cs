using JsonConverter.Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace nbs_smart_wallet.Controllers;

public class HomeController : Controller
{
    private RevolutProxy _revolutProxy;
    private ILogger<HomeController> _logger;
    private UserManager<ApplicationUser> _userManager;
    public HomeController(RevolutProxy revolutProxy, ILogger<HomeController> logger)
    {
        _revolutProxy = revolutProxy;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    //[AllowAnonymous]
    public IActionResult Landing()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
	public async Task<ActionResult> Register(Register request)
	{
        // activation email etc etc in the future will be nice
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
			return StatusCode(StatusCodes.Status500InternalServerError);

		// clean strings!
		var newUser = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Username,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await _userManager.CreateAsync(newUser, request.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError);

		return RedirectToAction("Landing");
	}

	[HttpPost]
    public async Task<ActionResult> Login(Login request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

			var authClaims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, user.UserName),
					new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				};

			//foreach (var userRole in userRoles)
			//{
			//	authClaims.Add(new Claim(ClaimTypes.Role, userRole));
			//}

			//var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

			//var token = new JwtSecurityToken(
			//	issuer: _configuration["JWT:ValidIssuer"],
			//	audience: _configuration["JWT:ValidAudience"],
			//	expires: DateTime.Now.AddHours(3),
			//	claims: authClaims,
			//	signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
			//	);

			//return Ok(new
			//{
			//	token = new JwtSecurityTokenHandler().WriteToken(token),
			//	expiration = token.ValidTo
			//});
		}

        return View("Index");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    [Route("/jwk/auth")]
    public ActionResult jwk()
    {
        _logger.LogInformation("Authentication begun");
        try
        {
			var response = JsonConvert.SerializeObject(_revolutProxy.GetJWK());
            if (String.IsNullOrEmpty(response))
                throw new Exception();

			return Ok(response);
		} catch(Exception e)
        {
            Log.Error(e, "{Timestamp:HH:mm} [{Level}] {Message}{NewLine}{Exception}");
            return Problem(
                    detail: "Failed to get Json Web Key",
                    statusCode: StatusCodes.Status500InternalServerError
                );
        }
    }

    [HttpGet]
    [Route("/auth")]
    public async Task<ActionResult> Auth()
    {
        var client_creds = await _revolutProxy.GetClientCredentialToken();
        var account_consent = await _revolutProxy.CreateAccountAccessConsent();

        return Redirect(_revolutProxy.GetAuthUrl(account_consent.Data.ConsentId));
	}

	[HttpGet]
    [Route("/jwk/auth/callback")]
    public async Task<ActionResult> redirect_target(string code, string id_token, string state)
    {
        Debug.WriteLine($"{code} {id_token} {state}");
        // code is only valid for 2 mins
        // get access token now
        _ = await _revolutProxy.GetAccessToken(code, id_token, state);

		return View("Index");
    }

	[HttpGet]
	[Route("/accounts")]
	public async Task<ActionResult> Accounts()
	{
		var response = await _revolutProxy.GetAccounts();
		return Ok(response);
	}
}
