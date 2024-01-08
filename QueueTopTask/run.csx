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
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public static async Task<IActionResult> Run(HttpRequest req, ICollector<string> topTaskQueueItem, ILogger log)
{
    log.LogInformation("Email received.");

    string userAgent = req.Headers["User-Agent"].ToString();
    // Initialize default device type as unknown
    string deviceType = "Unknown";

    // If User-Agent string is not empty
    if (!string.IsNullOrEmpty(userAgent))
    {
        // Define regex patterns for device types
        string mobilePattern = @"(iPhone|Android.*Mobile|Windows Phone)";
        string tabletPattern = @"(iPad|Android(?!.*Mobile)|Tablet)";
        string desktopPattern = @"(Windows NT|Macintosh|Linux)";

        // Extract device type using regex
        if (Regex.IsMatch(userAgent, mobilePattern, RegexOptions.IgnoreCase))
        {
            deviceType = "Mobile";
        }
        else if (Regex.IsMatch(userAgent, tabletPattern, RegexOptions.IgnoreCase))
        {
            deviceType = "Tablet";
        }
        else if (Regex.IsMatch(userAgent, desktopPattern, RegexOptions.IgnoreCase))
        {
            deviceType = "Desktop";
        }
    }

    log.LogInformation($"Device Type: {deviceType}");

    // Rest of your Azure Function code...
    var parser = new WebhookParser();
    StreamReader reader = new StreamReader(req.Body);
    string emailText = await reader.ReadToEndAsync();
    emailText = emailText.Replace(";", "; ");

    byte[] byteArray = Encoding.UTF8.GetBytes(emailText);
    MemoryStream stream = new MemoryStream(byteArray);
    var inboundMail = parser.ParseInboundEmailWebhook(stream);

    log.LogInformation("Email parsed.");
    var text = inboundMail.Html;
    log.LogInformation("TopTask Queue Item: " + text);
    topTaskQueueItem.Add(text);

    return new OkResult();
}
