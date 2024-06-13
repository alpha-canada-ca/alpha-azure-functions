#r "Newtonsoft.Json"
#r "Microsoft.Extensions.Primitives"
#r "Microsoft.AspNetCore.Http.Features"

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

public static async Task<IActionResult> Run(
  [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
  ICollector<string> problemQueueItem,
  ILogger log)
{
  log.LogInformation("Date and time format: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

  string userAgent = req.Headers["User-Agent"].ToString();
  string deviceType = "Unknown";
  string browserVersion = "Unknown";

  if (!string.IsNullOrEmpty(userAgent))
  {
    string devicePattern = @"(iPad|iPhone|Android|Windows Phone|Windows NT|Linux|Macintosh|Windows)";
    string browserPattern = @"(MSIE|Trident|Edge|Chrome|Firefox|Safari)(?:\/([\d\.]+))?";

    Match deviceMatch = Regex.Match(userAgent, devicePattern, RegexOptions.IgnoreCase);
    Match browserMatch = Regex.Match(userAgent, browserPattern, RegexOptions.IgnoreCase);

    if (deviceMatch.Success)
    {
      deviceType = deviceMatch.Value;
    }
    if (browserMatch.Success)
    {
      browserVersion = browserMatch.Value;
    }
  }

  log.LogInformation($"Device type: {deviceType}, Browser version: {browserVersion}");

  var payload = await req.ReadFormAsync();

  List<string> requiredFields = new List<string> {
    "submissionPage",
    "pageTitle",
    "institutionopt",
    "details",
    "helpful"
  };

  var missingFields = requiredFields.Except(payload.Keys);
  if (missingFields.Any())
  {
    log.LogWarning($"Missing required fields: {string.Join(", ", missingFields)}");
    return new BadRequestObjectResult($"Missing required fields: {string.Join(", ", missingFields)}");
  }

  var timeStamp = DateTime.UtcNow.ToString("HH:mm");
  var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

  var submissionPage = payload["submissionPage"].ToString().Replace(";", " ");
  var language = payload["language"];
  var pageTitle = payload["pageTitle"];
  var institutionopt = payload["institutionopt"].ToString().ToUpper().Trim();
  var themeopt = payload["themeopt"].ToString().ToLower().Trim();
  var sectionopt = payload["sectionopt"].ToString().ToLower().Trim();
  var problem = payload["problem"];
  var details = payload["details"].ToString().Replace(";", "");
  var helpful = payload["helpful"];
  var oppositeLang = payload["oppositelang"];
  var contact = payload["contact"];

  if (payload != null && payload.ContainsKey("submissionPage") && !StringValues.IsNullOrEmpty(payload["submissionPage"]))
  {
    string submissionPageValue = payload["submissionPage"].ToString();
    if (submissionPageValue.Contains("/services/"))
    {
      string[] parts = submissionPageValue.Split("/services/");
      if (parts.Length > 1)
      {
        string[] themeParts = parts[1].Split("/");
        if (themeParts.Length > 0)
        {
          string theme = themeParts[0];
          themeopt = theme;
        }
      }
    }
  }

  if (string.IsNullOrWhiteSpace(details))
  {
    log.LogWarning("Entry has no comment and will be disregarded.");
    return new OkObjectResult("Data received...");
  }
  if (string.IsNullOrWhiteSpace(pageTitle) || string.IsNullOrWhiteSpace(submissionPage))
  {
    log.LogWarning("Bad data...");
    return new BadRequestObjectResult("Bad data....");
  }

  var queueData = $"{timeStamp};{date};{submissionPage};{language};{oppositeLang};{pageTitle};{institutionopt};{themeopt};{sectionopt};{problem};{details};{helpful};{deviceType};{browserVersion};{contact}";

  var queueDataLength = queueData.Split(';').Length;

  log.LogInformation("Number of items in queueData: " + queueDataLength);
  log.LogInformation(queueData);
  problemQueueItem.Add(queueData);
  log.LogInformation("Data queued successfully.");

  return new OkObjectResult("Data received...");
}
