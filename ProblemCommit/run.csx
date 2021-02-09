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

public static int timesToLoop = 100;
public static int timesLooped = 0;

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
//Widget v3 is used for NON-AEM feedback forms with the language field set.
//Many of the NON-AEM pages do not have a language in their url.
enum WIDGET_VERSION_3 : int
{
    DATE,
    INSTITUTION,
    THEME,
    SECTION,
    TITLE,
    URL,
    YESNO,
    PROBLEM,
    LANGUAGE,
    PROBLEM_DETAILS
}

public static async Task Run(TimerInfo myTimer, ILogger log)
{
    try {

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        QueueClient queue       = new QueueClient(connectionString, "problemqueue");

        var watch   = System.Diagnostics.Stopwatch.StartNew();
        timesLooped = 0;

        for(int index = 0; index < timesToLoop; index++){
            QueueProperties properties = await queue.GetPropertiesAsync();
            if (properties.ApproximateMessagesCount > 0){

                QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync();
                
                if (retrievedMessage.Length > 0) {

                        string theMessage       = retrievedMessage[0].MessageText;
                        byte[] data             = Convert.FromBase64String(theMessage);
                        string decodedString    = Encoding.UTF8.GetString(data);

                        log.LogInformation("Before replace of entity: "+decodedString);

                        decodedString           = HttpUtility.HtmlDecode(decodedString);

                        string[] problemData    = decodedString.Split(";");
                        log.LogInformation("Data size:"+problemData.Length);

                        var client          = InitClient();
                        log.LogInformation("Client initialized...");

                        var database        = client.GetDatabase("pagesuccess");
                        var problems        = database.GetCollection<Problem>("problem");
                        var origProblems    = database.GetCollection<Problem>("originalproblem");
                        try
                        {
                            Problem problem = new Problem();
                            if (problemData.Length == 10){
                                log.LogInformation("Data retrieved has 10 fields so it must be version 3 of the widget.");
                                problem.institution         = problemData[(int)WIDGET_VERSION_3.INSTITUTION];
                                problem.theme               = problemData[(int)WIDGET_VERSION_3.THEME];
                                problem.section             = problemData[(int)WIDGET_VERSION_3.SECTION];
                                problem.problemDate         = problemData[(int)WIDGET_VERSION_3.DATE];
                                problem.title               = problemData[(int)WIDGET_VERSION_3.TITLE];
                                problem.url                 = problemData[(int)WIDGET_VERSION_3.URL];
                                problem.yesno               = problemData[(int)WIDGET_VERSION_3.YESNO];
                                problem.problem             = problemData[(int)WIDGET_VERSION_3.PROBLEM];
                                problem.language            = problemData[(int)WIDGET_VERSION_3.LANGUAGE];
                                for (int i = ((int)WIDGET_VERSION_3.PROBLEM_DETAILS); i < problemData.Length; i++) 
                                {                   
                                    problem.problemDetails += problemData[i];
                                }
                                problem.dataOrigin = "ProblemCommit-WidgetVersion3";
                            } 
                            if (problemData.Length < 9) {
                                log.LogInformation("Data retrieved has less than 9 fields so it must be version 1 of the widget.");
                                problem.problemDate     = problemData[(int)WIDGET_VERSION_1.DATE];
                                problem.title           = problemData[(int)WIDGET_VERSION_1.TITLE];
                                problem.url             = problemData[(int)WIDGET_VERSION_1.URL];
                                problem.yesno           = problemData[(int)WIDGET_VERSION_1.YESNO];
                                problem.problem         = problemData[(int)WIDGET_VERSION_1.PROBLEM];
                                for (int i = ((int)WIDGET_VERSION_1.PROBLEM_DETAILS); i < problemData.Length; i++){                   
                                    problem.problemDetails += problemData[i];
                                }
                                problem.institution     ="";
                                problem.theme           ="";
                                problem.section         ="";
                                problem.dataOrigin      = "ProblemCommit";
                            }
                           
                            if(problemData.Length == 9 || problemData.Length > 10) {
                                log.LogInformation("Data retrieved has 9 or more fields so it must be version 2 of the widget.");
                                problem.institution     =problemData[(int)WIDGET_VERSION_2.INSTITUTION];
                                problem.theme           =problemData[(int)WIDGET_VERSION_2.THEME];
                                problem.section         =problemData[(int)WIDGET_VERSION_2.SECTION];
                                problem.problemDate     = problemData[(int)WIDGET_VERSION_2.DATE];
                                problem.title           = problemData[(int)WIDGET_VERSION_2.TITLE];
                                problem.url             = problemData[(int)WIDGET_VERSION_2.URL];
                                problem.yesno           = problemData[(int)WIDGET_VERSION_2.YESNO];
                                problem.problem         = problemData[(int)WIDGET_VERSION_2.PROBLEM];
                                for (int i = ((int)WIDGET_VERSION_2.PROBLEM_DETAILS); i < problemData.Length; i++){                   
                                    problem.problemDetails += problemData[i];
                                }
                                problem.dataOrigin      = "ProblemCommit-WidgetVersion2";
                            }
                            
                            if (problem.url.ToLower().Contains("/en/") || problem.url.ToLower().Contains("travel.gc.ca")) {
                                problem.language    = "en";
                            } 
                            if (problem.url.ToLower().Contains("/fr/") || problem.url.ToLower().Contains("voyage.gc.ca")){
                                problem.language    = "fr";
                            }
                            problem.resolutionDate  = "";
                            problem.resolution      = "";
                            problem.department      = "N/A";
                            
                            problem.processed               = "false";
                            problem.airTableSync            = "false";
                            problem.personalInfoProcessed   = "false";
                            problem.autoTagProcessed        = "false";
                        
                            if (problem.yesno.ToUpper().Equals("YES") && !problem.problemDetails.Trim().Equals("") )  {
                                log.LogInformation("Problem is a YES with comments, it is spam, discarding... " + problem.yesno + " - " + problem.problemDetails);
                            }
                         
                            log.LogInformation("Date converted from: " + problem.problemDate + " to: " + DateTime.Now.ToString("yyyy-MM-dd"));
                            problem.problemDate = DateTime.Now.ToString("yyyy-MM-dd");
                            
                            if (problem.problemDetails.Equals("")) {
                                log.LogInformation("Problem has no comment and will be disregarded.");
                            } else {
                                problems.InsertOne(problem);
                                log.LogInformation("Records saved. Problem ID:" + problem.id);
                                OriginalProblem origProblem = new OriginalProblem(problem);
                                origProblems.InsertOne(origProblem);
                                log.LogInformation("Original record has been saved.");
                            }
                            timesLooped++;
                            
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
            } else{
                break;
            }
        }
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        log.LogInformation("-------------------------------");
        log.LogInformation("time elapsed for "+ timesLooped + " entries: " + elapsedMs); 
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

