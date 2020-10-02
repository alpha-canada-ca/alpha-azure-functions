#r "Newtonsoft.Json"
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.AspNetCore.Mvc;
using JWT.Algorithms;   
using JWT.Builder;   
using Newtonsoft.Json;


public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,ICollector<string> topTaskQueueItem, ILogger log)
{
var authorizationHeader = req.Headers["Authorization"][0];

    IDictionary<string, object> claims = null;
    try
    {
        if (authorizationHeader.StartsWith("Bearer"))
        {
            authorizationHeader = authorizationHeader.Substring(7);
        }
        // Validate the token and decode the claims.
        claims = new JwtBuilder()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("secretKey")))
            .MustVerifySignature()
            .Decode<IDictionary<string, object>>(authorizationHeader);
            string json = JsonConvert.SerializeObject(claims, Formatting.Indented);
            log.LogInformation(json);
            log.LogInformation("trying to add to queue");
            topTaskQueueItem.Add(json);
    }
    catch (Exception exception)
    {
        log.LogError(exception.StackTrace);
        return new BadRequestObjectResult("Bad key/data....");
    }
return new OkObjectResult("Data received.");
}
