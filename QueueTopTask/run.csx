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

public static async Task<IActionResult> Run(HttpRequest req, ICollector<string> topTaskQueueItem, ILogger log)
{
    log.LogInformation("Email received.");
    var parser = new WebhookParser();

    StreamReader reader = new StreamReader(req.Body);
    string emailText = reader.ReadToEnd();
    //incoming emails had incorrect semicolon format and adding a space
    //after the semicolon was neeeded in order to do any further processing.
    emailText = emailText.Replace(";", "; ");

    //convert the emailText into a stream in order to parse.
    byte[] byteArray = Encoding.UTF8.GetBytes(emailText);
    MemoryStream stream = new MemoryStream(byteArray);
    var inboundMail = parser.ParseInboundEmailWebhook(stream);

    log.LogInformation("Email parsed.");
    //inboudMail.Html instead of inboundMail.Text in queueProblem was required. 
    //Principal Publisher is sending the entries from a different server which does not contain a .Text attribute
    var text = inboundMail.Html;
    log.LogInformation("TopTask Queue Item: " + text);
    //Add to  Queue
    topTaskQueueItem.Add(text);

    return new OkResult();
}