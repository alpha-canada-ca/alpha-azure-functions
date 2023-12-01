#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using StrongGrid.Models;
using StrongGrid.Models.Webhooks;
using StrongGrid.Utilities;
using StrongGrid;

public static async Task<IActionResult> Run(HttpRequest req, ICollector<string> problemQueueItem, ILogger log)
{
    log.LogInformation("Email received.");
    var parser = new WebhookParser();
    var inboundMail = parser.ParseInboundEmailWebhook(req.Body);
    log.LogInformation("Email parsed.");

    var text = inboundMail.Text;
    // Split the text up to the 8th semicolon
    string[] parts = text.Split(new char[] { ';' }, 9);

    if (parts.Length >= 9)
    {
        // Sanitize the part after the 8th semicolon
        parts[8] = parts[8].Replace(';', ':');

        // Reconstruct the text
        text = string.Join(";", parts);
    }

    log.LogInformation("Problem Queue Item: " + text);
    problemQueueItem.Add(text);


    return new OkResult();
}