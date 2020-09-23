#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using StrongGrid.Models;
using StrongGrid.Models.Webhooks;
using StrongGrid.Utilities;
using StrongGrid; 

public static async Task<IActionResult> Run(HttpRequest req,ICollector<string> problemQueueItem, ILogger log)
{
    log.LogInformation("Email received.");
    var parser = new WebhookParser();
    var inboundMail = parser.ParseInboundEmailWebhook(req.Body);
    log.LogInformation("Email parsed.");
    var text = inboundMail.Text;
    log.LogInformation("Problem Queue Item: " + text);
    problemQueueItem.Add(text);
    
    
    return new OkResult();
}