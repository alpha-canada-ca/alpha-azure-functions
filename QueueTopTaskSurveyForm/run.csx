#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.AspNetCore.Mvc;

public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,ICollector<string> problemQueueItem, ILogger log)
{
    return new OkObjectResult("Data recevied...");
}
