#r "Newtonsoft.Json"

using System.Net;
using System.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
            
// NOTE: For that code to work, you need install System.IdentityModel.Tokens.Jwt package from NuGet (the link includes the latest stable version)
// Link: https://www.nuget.org/packages/System.IdentityModel.Tokens.Jwt/4.0.2.206221351


// a sample jwt encoded token string which is supposed to be extracted from 'Authorization' HTTP header in your Web Api controller
var tokenString = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9.eyJhdWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8wMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJpYXQiOiIxNDI4MDM2NTM5IiwibmJmIjoiMTQyODAzNjUzOSIsImV4cCI6IjE0MjgwNDA0MzkiLCJ2ZXIiOiIxLjAiLCJ0aWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJhbXIiOiJwd2QiLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJlbWFpbCI6Impkb2VAbGl2ZS5jb20iLCJwdWlkIjoiSm9obiBEb2UiLCJpZHAiOiJsaXZlLmNvbSIsImFsdHNlY2lkIjoiMTpsaXZlLmNvbTowMDAwMDAwMDAwMDAwMDAwIiwic3ViIjoieHh4eHh4eHh4eHh4eHh4eC15eXl5eSIsImdpdmVuX25hbWUiOiJKb2huIiwiZmFtaWx5X25hbWUiOiJEb2UiLCJuYW1lIjoiSm9obiBEb2UiLCJncm91cHMiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJ1bmlxdWVfbmFtZSI6ImxpdmUuY29tI2pkb2VAbGl2ZS5jb20iLCJhcHBpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMCIsImFwcGlkYWNyIjoiMCIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsImFjciI6IjEifQ.K7BCa0NO-A5f9exFiWcIXFMGnLmmt3V2HVP0itMT-GsAxnQROWzJFDIQNFo4QhiW0NCCqJykVELeVBCy_7Dex2-szUPZ69rmmDVJhy_qkmAiHhS1mNZDvJ1sB-whb5wOJ_QPIlByVzubhTcNnuliTVjnTeuOurVJJcn0Vugx9UDkGgky0etHXzmKukWYp4nzA68Wf1xnzlMZBz7PfoPGhjgzQfceOkZJVXIBRMB_7tsyW7gYNbHB_aTiT47cEjkh-UdrZEdp2UaAKugC-es3m076kRHMJqx31x-zDLDBttKinRJVPctiqwb1jMOMV6cUAp2E6aMfEbNk_iqX_OKFJg";

var jwtEncodedString = tokenString.Substring(7); // trim 'Bearer ' from the start since its just a prefix for the token string

var token = new JwtSecurityToken(jwtEncodedString: jwtEncodedString);
Console.WriteLine("email => " + token.Claims.First(c => c.Type == "email").Value);
            return new OkObjectResult("Test");
}
