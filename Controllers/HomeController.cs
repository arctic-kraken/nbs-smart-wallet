using JsonConverter.Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using nbs_smart_wallet.Models;
using Newtonsoft.Json;

namespace nbs_smart_wallet.Controllers;

public class HomeController : Controller
{


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

    public class JWKResponse
    {
        public List<Key> keys { get; set; } = new List<Key>();
        public class Key
        {
            public string e { get; set; } = string.Empty;
            public string n { get; set; } = string.Empty;
			public string kid { get; set; } = string.Empty;
			public string kty { get; set; } = string.Empty;
			public string use { get; set; } = string.Empty;
			public List<string> x5c { get; set; } = new List<string>();
        }
    }

    [HttpGet]
    [Route("/auth")]
    public ActionResult auth()
    {
        // sandbox credentials
        var jwk = new JWKResponse();
        jwk.keys.Add(new JWKResponse.Key
        {
            e = "AQAB",
            n = "oL9VFL2g5dcjvB362HohzDhqq2MsT81N8himG_Le1E-BH_sNOQvLlHk5P-kuWk19uFL17wFWenfwX9Fo3wwlK6m6qw_eGYAE4XEv9zVVSh0Z2v8EZ-L795-5Dvvr4PUAXiYlSHFnoREuTB9KA2le1XUI3_Ddrvt0vhQCXpxqAdpJmW6BN4AL7gO3fwq1ekQnatNX98G8vIVMTe8PtvsZ9wFDHBjX5GFjc0EK_4yDVA0UlnQnWhfiRJGp8ZL-O3yDLBWWXsYJClaEj_PGKyPJCGyTw5yYUbyXu9Bhw-zg_g7m3O_Rg5NSdp7bZDxmxbMi6CitIS_SniH31W6d_ddFSQ",
            kid = "pallasathena",
            kty = "RSA",
            use = "sig",
            x5c = new List<string> {
				"MIIEejCCAmKgAwIBAgIFAOKNqYUwDQYJKoZIhvcNAQELBQAwYDELMAkGA1UEBhMCVUsxDzANBgNVBAgMBkxvbmRvbjEQMA4GA1UECgwHUmV2b2x1dDEQMA4GA1UECwwHU2FuZGJveDEcMBoGA1UEAwwTc2FuZGJveC5yZXZvbHV0LmNvbTAeFw0yNjA4MTExNTI3MDVaFw0yNzA4MTExNTI3MDVaMIGcMQswCQYDVQQGEwJHQjEUMBIGA1UECgwLdGVzdGNvbXBhbnkxGzAZBgNVBAsMEjAwMTU4MDAwMDEwM1VBdkFBTTEfMB0GA1UEAwwWMmtpWFF5bzB0ZWRqVzJzb21qU2dINzE5MDcGA1UEYQwwUFNEVUstUkVWQ0EtZDE3MzhjODAtZmYwMi00YzkzLWE1MDctNjZlYjNhYjQzOWEzMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoL9VFL2g5dcjvB362HohzDhqq2MsT81N8himG/Le1E+BH/sNOQvLlHk5P+kuWk19uFL17wFWenfwX9Fo3wwlK6m6qw/eGYAE4XEv9zVVSh0Z2v8EZ+L795+5Dvvr4PUAXiYlSHFnoREuTB9KA2le1XUI3/Ddrvt0vhQCXpxqAdpJmW6BN4AL7gO3fwq1ekQnatNX98G8vIVMTe8PtvsZ9wFDHBjX5GFjc0EK/4yDVA0UlnQnWhfiRJGp8ZL+O3yDLBWWXsYJClaEj/PGKyPJCGyTw5yYUbyXu9Bhw+zg/g7m3O/Rg5NSdp7bZDxmxbMi6CitIS/SniH31W6d/ddFSQIDAQABMA0GCSqGSIb3DQEBCwUAA4ICAQAxWOoQQLzaBWxnE6Z9zFvzbFrFuURgXuheRiYAtU8gPV3BhdCeylgTf0T2ZOUxLgvloHwXaYvjK1JByREpwX55IubJcBiVvgKHOFz/w21H93pvGYZKDvfWOQzmsaN4SjWxG/S4wHWSEak6ljBc+OYWm/btVVhd/PbzwBkLpSB/K32OccLwQHNl5VDTBG2qBvLeEe9KiHs0fHF+2MJKlyX1lxyI/UgVifVQPkDxrbodr/NGlJft6mYjQQit1oq6z15kiS0EZpW3hV++OBELufF9BGZ214t+5tG6a8vHVodilfVyi4IWRsjxV3Qu5XJaiOqfiJUk2iY0pzvQQGg8SVFBWjEZXBuMgmTipFE1obAGPgqNsXTvnoYnaZBHfzgvGVFGFqxdpdwxQ45ihZfSb8AezcSGm8yfU/DV/xbACN+n71AR84wXt+lpsDuvLTQPjJwb6q782x3mLR24teccKX6zFaEhLrKAxy1ugtC5+P6gRdrlIWgBOuZAAJuOjInozVERAZlRzMw67lswvosBDau8clhonLb3KeR2sOVe0LXprZBeyurdLwO4L1R5t6j5zBhOPHr3415hSVwnWZYeFlyUxm5ggrHCPI7BOvEc0M/+Nly6s7S76bYfr/04TLc6fjuGJRzDiM5m8eEczJByb4ju7GcMkS5JIcOvipoGo4u3uw=="
			}
        });
        var response = JsonConvert.SerializeObject(jwk);

        return Ok(response);
    }
}
