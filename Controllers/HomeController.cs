using JsonConverter.Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Models;
using Newtonsoft.Json;
using nbs_smart_wallet.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace nbs_smart_wallet.Controllers;

public class HomeController : Controller
{
    private RevolutProxy _revolutProxy;
    public HomeController(RevolutProxy revolutProxy)
    {
        _revolutProxy = revolutProxy;
    }

    public IActionResult Index()
    {
        return View();
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
        try
        {
			var response = JsonConvert.SerializeObject(_revolutProxy.GetJWK());
            if (String.IsNullOrEmpty(response))
                throw new Exception();

			return Ok(response);
		} catch(Exception e)
        {
            // log e
            return Problem(
                    detail: "Failed to get Json Web Key",
                    statusCode: StatusCodes.Status500InternalServerError
                );
        }
    }

    [HttpGet]
    [Route("/auth")]
    public async Task<ActionResult> auth()
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
