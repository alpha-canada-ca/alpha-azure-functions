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
    log.LogInformation("Date format:" + DateTime.UtcNow);
    string[] fieldNames = {"institutionopt","themeopt","sectionopt","pageTitle","submissionPage","helpful","problem","details"};
    var postData =  await req.ReadFormAsync();
    var missingFields = new List<string>();
    string queueData = "";
    foreach (String fieldName in fieldNames)  
    { 
        String fieldData = postData[fieldName];
        log.LogInformation("Parsing: "+ fieldName + " value:"+fieldData);
        if (fieldData == null)
        {
            if (fieldName == "details" || fieldName == "problem") 
            {
                queueData += ";";
            } 
            else 
            {
                missingFields.Add(fieldName);
            }
        } 
        else {
            // put in queue data, if there is a semi-colon remove it, bad choice of delimeter but not my choice
            queueData += fieldData.Replace(";"," ")+";";
        }
    }
    queueData = queueData.Substring(0,queueData.Length-1);
    //Thu Aug 13 2020 13:40:56 GMT+0000 (Coordinated Universal Time)
    //Thu Aug 13 2020 19:56:59 GMT+0000 (Coordinated Universal Time)
    string pattern = "ddd MMM dd yyyy HH:mm:ss";
    queueData = DateTime.UtcNow.ToString(pattern)+" GMT+0000 (Coordinated Universal Time)" +";"+queueData;
    if (missingFields.Any())
    {
        var missingFieldsSummary = String.Join(", ", missingFields);
        log.LogInformation("Missing fields..." + missingFieldsSummary);
         new BadRequestObjectResult("Bad data....");
    } else {
        log.LogInformation(queueData);
        problemQueueItem.Add(queueData);
        log.LogInformation("Data queued successfully.");
    }
    return new OkObjectResult("Data recevied...");
}
