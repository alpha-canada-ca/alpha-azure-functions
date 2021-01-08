#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using StrongGrid.Models;
using StrongGrid.Models.Webhooks;
using StrongGrid.Utilities;
using StrongGrid; 

public static async Task<IActionResult> Run(HttpRequest req,ICollector<string> topTaskQueueItem, ILogger log)
{
    log.LogInformation("Email received.");
    var parser = new WebhookParser();

    StreamReader reader = new StreamReader(req.Body);
    string emailText = reader.ReadToEnd();
    emailText = emailText.Replace(";", "; ");
    // log.LogInformation(emailText);

    byte[] byteArray = Encoding.ASCII.GetBytes(emailText);
    MemoryStream stream = new MemoryStream(byteArray);



    var inboundMail = parser.ParseInboundEmailWebhook(stream);
    log.LogInformation("Email parsed.");
    var text = inboundMail.Html;
    log.LogInformation("TopTask Queue Item: " + text);
    topTaskQueueItem.Add(text);

    return new OkResult();
}