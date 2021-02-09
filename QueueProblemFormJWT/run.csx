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
using Newtonsoft.Json.Linq;


public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,ICollector<string> problemQueueItem, ILogger log)
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
            string valuesOnly = JsonConvert.ToString(json);

            Dictionary<string, string> postData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            string[] fieldNames = {"institutionopt","themeopt","sectionopt","pageTitle","submissionPage","helpful","problem","details"};
            var missingFields = new List<string>();
            string queueData = "";
            bool noComment = false;
            foreach (String fieldName in fieldNames)  
            { 
                String fieldData = postData[fieldName];
                log.LogInformation("Parsing: "+ fieldName + " value:"+fieldData);
                if(fieldName == "details" && fieldData.Equals("")){
                    noComment = true;
                }
                //If the data is null and there is no data for the fieldname then append a ; (; signifies the next field)
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
                    // put in queue data, if there is a semi-colon add a space, bad choice of delimeter but not my choice
                    queueData += fieldData.Replace(";"," ")+";";
                }
            }
            //append date to the beginning of the data.
            queueData = queueData.Substring(0,queueData.Length-1);
            queueData = DateTime.Now.ToString("yyyy-MM-dd") +";"+queueData;
            if (missingFields.Any()){
                var missingFieldsSummary = String.Join(", ", missingFields);
                log.LogInformation("Missing fields..." + missingFieldsSummary);
                new BadRequestObjectResult("Bad data....");
            }
            if(noComment){
                log.LogInformation("Entry has no comment and will be disregarded.");
            }
            else {
                log.LogInformation(queueData);
                problemQueueItem.Add(queueData);
                log.LogInformation("Data queued successfully.");
            }
    }
    catch (Exception exception)
    {
        log.LogError(exception.StackTrace);
        return new BadRequestObjectResult("Bad key/data....");
    }
return new OkObjectResult("Data received.");
}
