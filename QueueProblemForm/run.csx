// Include Newtonsoft.Json package
#r "Newtonsoft.Json"
// Include necessary namespaces
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

// Main function to be run on trigger
public static async Task < IActionResult > Run(
  // Triggering on an HTTP POST request
  [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
  // Queue to add problems to
  ICollector < string > problemQueueItem,
  // Logger to log events
  ILogger log) {
  // Logging the current date and time
  log.LogInformation("Date and time format: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

  // Extract the User-Agent header from the request
  string userAgent = req.Headers["User-Agent"].ToString();
  // Initialize default device type and browser version as unknown
  string deviceType = "Unknown";
  string browserVersion = "Unknown";

  // If User-Agent string is not empty
  if (!string.IsNullOrEmpty(userAgent)) {
    // Define regex patterns for device type and browser version
    string devicePattern = @"(iPad|iPhone|Android|Windows Phone|Windows NT|Linux|Macintosh|Windows)";
    string browserPattern = @"(MSIE|Trident|Edge|Chrome|Firefox|Safari)(?:\/([\d\.]+))?";

    // Extract device type and browser version using regex
    Match deviceMatch = Regex.Match(userAgent, devicePattern, RegexOptions.IgnoreCase);
    Match browserMatch = Regex.Match(userAgent, browserPattern, RegexOptions.IgnoreCase);

    // If matches found, assign values
    if (deviceMatch.Success) {
      deviceType = deviceMatch.Value;
    }
    if (browserMatch.Success) {
      browserVersion = browserMatch.Value;
    }
  }

  // Log the device type and browser version
  log.LogInformation($"Device type: {deviceType}, Browser version: {browserVersion}");

  // Extract form data from the request
  var payload = await req.ReadFormAsync();

  // Define required fields
  List < string > requiredFields = new List < string > {
    "submissionPage",
    "pageTitle",
    "institutionopt",
    "details",
    "helpful"
  };

  // Check if any required fields are missing
  var missingFields = requiredFields.Except(payload.Keys);
  if (missingFields.Any()) {
    // If any required fields are missing, log and return error
    log.LogInformation($"Missing required fields: {string.Join(", ", missingFields)}");
    return new BadRequestObjectResult($"Missing required fields: {string.Join(", ", missingFields)}");
  }

  // Extract relevant data from payload
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

  //given a url that has this format: https://www.canada.ca/en/revenue-agency/services/tax/businesses/topics/gst-hst-businesses.html extract the theme by grabbing the topic from after the /services/
  if (payload != null && payload.ContainsKey("submissionPage") && !StringValues.IsNullOrEmpty(payload["submissionPage"])) {
    string submissionPageValue = payload["submissionPage"].ToString();
    if (submissionPageValue.Contains("/services/")) {
      string[] parts = submissionPageValue.Split("/services/");
      if (parts.Length > 1) {
        string[] themeParts = parts[1].Split("/");
        if (themeParts.Length > 0) {
          string theme = themeParts[0];
          themeopt = theme;
        }
      }
    }
  }

  // Check if details field is empty or pageTitle/submissionPage fields are empty
  if (string.IsNullOrWhiteSpace(details)) {
    // If empty, log the information and return Ok
    log.LogInformation("Entry has no comment and will be disregarded.");
    return new OkObjectResult("Data received...");
  }
  if (string.IsNullOrWhiteSpace(pageTitle) || string.IsNullOrWhiteSpace(submissionPage)) {
    // If empty, log the information and return error
    log.LogInformation("Bad data...");
    return new BadRequestObjectResult("Bad data....");
  }

  // Create a string with the extracted data
  var queueData = $"{timeStamp};{date};{submissionPage};{language};{oppositeLang};{pageTitle};{institutionopt};{themeopt};{sectionopt};{problem};{details};{helpful};{deviceType};{browserVersion};{contact}";

  // Add the data string to the queue
  log.LogInformation(queueData);
  problemQueueItem.Add(queueData);
  // Log the successful queueing of data
  log.LogInformation("Data queued successfully.");

  // Return Ok result with message
  return new OkObjectResult("Data received...");
}