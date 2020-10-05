#load "Problem.csx"
#load "OriginalProblem.csx"

using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
using MongoDB;
using MongoDB.Driver;
using System.Security.Authentication;
using System.Web;

 enum WIDGET_VERSION_1 : int
{
    DATE,
    TITLE,
    URL,
    YESNO,
    PROBLEM,
    PROBLEM_DETAILS
}

enum WIDGET_VERSION_2 : int
{
    DATE,
    INSTITUTION,
    THEME,
    SECTION,
    TITLE,
    URL,
    YESNO,
    PROBLEM,
    PROBLEM_DETAILS
}

public static async Task Run(TimerInfo myTimer, ILogger log)
{
    try {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        QueueClient queue = new QueueClient(connectionString, "problemqueue");
        QueueProperties properties = await queue.GetPropertiesAsync();
        if (properties.ApproximateMessagesCount > 0)
        {
            QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1);
            if (retrievedMessage.Length > 0) {
                string theMessage = retrievedMessage[0].MessageText;
                byte[] data = Convert.FromBase64String(theMessage);
                string decodedString = Encoding.UTF8.GetString(data);
                log.LogInformation("Before replace of entity: "+decodedString);
                decodedString = HttpUtility.HtmlDecode(decodedString);
                log.LogInformation(decodedString);

                string[] problemData = decodedString.Split(";");
                log.LogInformation("Data size:"+problemData.Length);
                var client = InitClient();
                log.LogInformation("Client initialized...");
                var database = client.GetDatabase("pagesuccess");
                var problems = database.GetCollection<Problem>("problem");
                var origProblems = database.GetCollection<Problem>("originalproblem");
                try
                {
                    Problem problem = new Problem();
                    if (problemData.Length < 9) 
                    {
                        log.LogInformation("Data retrieved has less than 9 fields so it must be version 1 of the widget.");
                        //Mon Jun 01 2020 05:41:57 GMT+0000 (Coordinated Universal Time);Coronavirus
                        //disease (COVID-19): Symptoms and
                        //treatment;/content/canadasite/en/public-health/services/diseases/2019-novel-coronavirus-infection/symptoms.html;No;The
                        //answer I need is missing;where do you go to get tested
                        problem.problemDate = problemData[(int)WIDGET_VERSION_1.DATE];
                        problem.title = problemData[(int)WIDGET_VERSION_1.TITLE];
                        problem.url = problemData[(int)WIDGET_VERSION_1.URL];
                        problem.yesno = problemData[(int)WIDGET_VERSION_1.YESNO];
                        problem.problem = problemData[(int)WIDGET_VERSION_1.PROBLEM];
                        for (int i = ((int)WIDGET_VERSION_1.PROBLEM_DETAILS); i < problemData.Length; i++) 
                        {                   
                            problem.problemDetails += problemData[i];
                        }
                        problem.institution="";
                        problem.theme="";
                        problem.section="";
                        problem.dataOrigin = "ProblemCommit";
                    } 
                    else  
                    {
                        log.LogInformation("Data retrieved has 9 or more fields so it must be version 2 of the widget.");
                        problem.institution=problemData[(int)WIDGET_VERSION_2.INSTITUTION];
                        problem.theme=problemData[(int)WIDGET_VERSION_2.THEME];
                        problem.section=problemData[(int)WIDGET_VERSION_2.SECTION];
                        problem.problemDate = problemData[(int)WIDGET_VERSION_2.DATE];
                        problem.title = problemData[(int)WIDGET_VERSION_2.TITLE];
                        problem.url = problemData[(int)WIDGET_VERSION_2.URL];
                        problem.yesno = problemData[(int)WIDGET_VERSION_2.YESNO];
                        problem.problem = problemData[(int)WIDGET_VERSION_2.PROBLEM];
                        for (int i = ((int)WIDGET_VERSION_2.PROBLEM_DETAILS); i < problemData.Length; i++) 
                        {                   
                            problem.problemDetails += problemData[i];
                        }
                        problem.dataOrigin = "ProblemCommit-WidgetVersion2";
                    }
                    
                    if (problem.url.ToLower().Contains("/en/") || problem.url.ToLower().Contains("travel.gc.ca")) {
                        problem.language = "en";
                    } else {
                        problem.language = "fr";
                    }
                    problem.resolutionDate = "";
                    problem.resolution = "";
                    problem.department = "N/A";
                    
                    problem.processed = "false";
                    problem.airTableSync = "false";
                    problem.personalInfoProcessed = "false";
                    problem.autoTagProcessed = "false";
                   
                    if (problem.yesno.ToUpper().Equals("YES") && !problem.problemDetails.Trim().Equals("") )  {
                        log.LogInformation("Problem is a YES with comments, it is spam, discarding... " + problem.yesno + " - " + problem.problemDetails);
                    } else {
                        problems.InsertOne(problem);
                        log.LogInformation("Records saved. Problem ID:" + problem.id);
                        OriginalProblem origProblem = new OriginalProblem(problem);
                        origProblems.InsertOne(origProblem);
                        log.LogInformation("Original record has been saved.");
                    }
                    
                    // dequeue the database record.
                    // Delete the message
                    queue.DeleteMessage(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                    log.LogInformation("The email data has been dequeued.");
                }
                catch (MongoCommandException ex)
                {
                    string msg = ex.Message;
                    log.LogError(ex.Message);
                }
            }
        }
    } catch (Exception e) {
        log.LogError(e.Message);
    }
}
public static MongoClient InitClient() {
    MongoClientSettings settings = new MongoClientSettings();
    settings.Server = new MongoServerAddress(Environment.GetEnvironmentVariable("mongoDBConnectUrl"), Int32.Parse(Environment.GetEnvironmentVariable("mongoDBConnectPort")));
    if(!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Development")){
        settings.UseTls = true;
        settings.SslSettings = new SslSettings();
        settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;
        settings.RetryWrites = false;
        MongoIdentity identity = new MongoInternalIdentity("pagesuccess", Environment.GetEnvironmentVariable("mongoDBUsername"));
        MongoIdentityEvidence evidence = new PasswordEvidence(Environment.GetEnvironmentVariable("mongoDBPassword"));
        settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence); 
    }
    MongoClient client = new MongoClient(settings);
    return client;
}

