using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Models.Authentication;
using nbs_smart_wallet.Models.DbSets;
using nbs_smart_wallet.Services;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace nbs_smart_wallet.Controllers;

[Authorize(Roles = "Admin")]
public class RevolutController : Controller
{
    private RevolutProxy _revolutProxy;
    private RevolutService _service;
    public RevolutController(RevolutService service, RevolutProxy revolutProxy)
    {
        _revolutProxy = revolutProxy;
        _service = service;
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
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		try
        {
            Log.Information("JWK endpoint reached by {ip}", requestIp);

			var response = JsonConvert.SerializeObject(_revolutProxy.GetJWK());
            if (String.IsNullOrEmpty(response))
                throw new Exception();

			Log.Information("Responded with JWK to {ip}", requestIp);
			return Ok(response);
		} catch(Exception e)
        {
            Log.Error(e, e.Message);
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
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Authentication begun for {ip}", requestIp);
        var client_creds = await _revolutProxy.GetClientCredentialToken();
        var account_consent = await _revolutProxy.CreateAccountAccessConsent();

        Log.Information("Redirecting {ip} to Revolut Auth", requestIp);
        return Redirect(_revolutProxy.GetAuthUrl(account_consent.Data.ConsentId));
	}

	[HttpGet]
    [Route("/jwk/auth/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> redirect_target(string code, string id_token, string state)
    {
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Successful callback, getting access token");
        // code is only valid for 2 mins
        // get access token now
        var result = await _revolutProxy.GetAccessToken(code, id_token, state);
        if (result)
            Log.Information("Successfully received tokens for {ip}", requestIp);
        else
			Log.Warning("Failed to receive tokens for {ip}", requestIp);

		Log.Information("Redirecting {ip} to AuthSuccess page", requestIp);
		return RedirectToAction("AuthSuccess");
    }

    public IActionResult AuthSuccess()
    {
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Serving AuthSuccess page for {ip}", requestIp);

		return View();
    }

    public IActionResult PleadForAuth()
    {
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Serving PleadForAuth page for {ip}", requestIp);

		return View();
    }

	[HttpGet]
	[Route("/accounts")]
	public async Task<ActionResult> Accounts()
	{
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Serving Accounts page for {ip}", requestIp);

		if (!_revolutProxy.IsLoggedIntoRevolut())
        {
			Log.Information("Pleading {ip} to re-auth with Revolut", requestIp);
			return RedirectToAction("PleadForAuth");
		}
        
        try
        {
			Log.Information("Syncing Revolut Accounts of {ip}", requestIp);
			var result = await _service.SyncAccounts();
			if (result)
				Log.Information("Successful sync of Revolut Accounts for {ip}", requestIp);
			else
				Log.Warning("Failed to sync Revolut Accounts for {ip}", requestIp);

			Log.Information("Getting Accounts for {ip}", requestIp);
			var accounts = _service.GetAccounts();
			Log.Information("Serving Accounts page for {ip}", requestIp);
			return View(accounts);
		}
        catch (Exception e)
        {
            Log.Error(e, e.Message);
			// middleware shows generic error 500 page on prod
			throw;
		}
	}

	// Removing this endpoint from production
	//[HttpGet]
	//[Route("/accounts/seed")]
	//public ActionResult SeedAccounts()
	//{        
 //       try
	//	{
	//		var response = _service.GetAccountsSeed();
 //           //_service.SeedRandomTransactions();
            
	//		return Ok();
	//	}
	//	catch (Exception e)
	//	{
	//		Log.Error(e, e.Message);
	//		// middleware shows generic error 500 page on prod
	//	}

	//	return Problem();
	//}

	[HttpGet]
    public ActionResult SyncDetails()
    {
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		try
		{
			Log.Information("Processing Sync of Details for {ip}", requestIp);
			_service.SyncRevAccounts();
			Log.Information("Sync of Details complete for {ip}", requestIp);
			return Ok();
		}
		catch (Exception ex)
		{
			Log.Error(ex, ex.Message);
			return Problem();
		}
    }

	public class AccountDetailsEditModel
	{
		public List<RevTransaction> Transactions { get; set; } = new List<RevTransaction>();
		public List<RevBankAccount> BankAccounts { get; set; } = new List<RevBankAccount>();
	}

	[HttpGet]
    public async Task<ActionResult> AccountDetails(Guid id)
    {
		var requestIp = HttpContext.Request.HttpContext?.Connection.RemoteIpAddress?.ToString();
		Log.Information("Serving Account Details page for {ip}", requestIp);

		if (!_revolutProxy.IsLoggedIntoRevolut())
		{
			Log.Information("Pleading {ip} to re-auth with Revolut", requestIp);
			return RedirectToAction("PleadForAuth");
		}

		try
		{
			Log.Information("Syncing Revolut Transactions of {ip}", requestIp);
			var result = await _service.SyncTransactionsOf(id);
			if (result)
				Log.Information("Successful sync of Revolut Transactions for {ip}", requestIp);
			else
				Log.Warning("Failed to sync Revolut Transactions for {ip}", requestIp);

			Log.Information("Getting Bank Accounts and Transactions for {ip}", requestIp);
			var model = new AccountDetailsEditModel();
			model.Transactions = _service.GetTransactionsFor(id);
            model.BankAccounts = _service.GetBankAccountsFor(id);
			Log.Information("Serving Account Details page for {ip}", requestIp);
			return View(model);
		}
		catch (Exception e)
		{
			Log.Error(e, e.Message);
			return View();
		}
    }
}
