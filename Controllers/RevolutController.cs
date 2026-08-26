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
public class RevolutController : Controller
{
    private RevolutProxy _revolutProxy;
    private const string errMessageTemplate = "{Timestamp:HH:mm} [{Level}] {Message}{NewLine}{Exception}";
    public RevolutController(RevolutProxy revolutProxy)
    {
        _revolutProxy = revolutProxy;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    [Route("/jwk/auth")]
    [AllowAnonymous]
    public ActionResult jwk()
    {
        try
        {
			var response = JsonConvert.SerializeObject(_revolutProxy.GetJWK());
            if (String.IsNullOrEmpty(response))
                throw new Exception();

			return Ok(response);
		} catch(Exception e)
        {
            Log.Error(e, errMessageTemplate);
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
        Log.Information("Authentication begun for x");
        var client_creds = await _revolutProxy.GetClientCredentialToken();
        var account_consent = await _revolutProxy.CreateAccountAccessConsent();

        return Redirect(_revolutProxy.GetAuthUrl(account_consent.Data.ConsentId));
	}

	[HttpGet]
    [Route("/jwk/auth/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> redirect_target(string code, string id_token, string state)
    {
        Log.Information("Successful callback, getting access token");
        // code is only valid for 2 mins
        // get access token now
        _ = await _revolutProxy.GetAccessToken(code, id_token, state);

		return RedirectToAction("AuthSuccess");
    }

    public IActionResult AuthSuccess()
    {
        return View();
    }

    public IActionResult PleadForAuth()
    {
        return View();
    }

	[HttpGet]
	[Route("/accounts")]
	public async Task<ActionResult> Accounts()
	{
        if (!_revolutProxy.IsLoggedIntoRevolut())
            RedirectToAction("PleadForAuth");

        // add Account viewing page
        
        return View();
	}

	[HttpGet]
	[Route("/accounts/get")]
	public async Task<ActionResult> GetAccounts()
	{
		if (!_revolutProxy.IsLoggedIntoRevolut())
			RedirectToAction("PleadForAuth");

		try
		{
			var response = await _revolutProxy.GetAccounts();
			return View(response);
		}
		catch (Exception e)
		{
			Log.Error(e, errMessageTemplate);
            // middleware shows generic error 500 page on prod
		}
		
		return Problem();
    }

	[HttpGet]
    public async Task<ActionResult> SyncDetails()
    {
        return Ok("Soon to be added");
    }
}
