#load "Problem.csx"
#load "OriginalProblem.csx"

using System;
using System.Globalization;
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

//This widget is used for AEM forms with all fields filled
enum WIDGET_ALL_FIELDS: int {
  TIMESTAMP,
  DATE,
  URL,
  LANG,
  OPPOSITE_LANG,
  TITLE,
  INSTITUTION,
  THEME,
  SECTION,
  PROBLEM,
  PROBLEM_DETAILS,
  YESNO,
  DEVICE,
  BROWSER,
  CONTACT
}

enum WIDGET_EMAIL_VERSION: int {
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

public static async Task Run(TimerInfo myTimer, ILogger log) {
  try {

    string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    QueueClient queue = new QueueClient(connectionString, "problemqueue");

    var watch = System.Diagnostics.Stopwatch.StartNew();
    timesLooped = 0;

    for (int index = 0; index < timesToLoop; index++) {
      QueueProperties properties = await queue.GetPropertiesAsync();
      if (properties.ApproximateMessagesCount > 0) {

        QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync();

        if (retrievedMessage.Length > 0) {

          string theMessage = retrievedMessage[0].MessageText;
          byte[] data = Convert.FromBase64String(theMessage);
          string decodedString = Encoding.UTF8.GetString(data);

          log.LogInformation("Before replace of entity: " + decodedString);

          decodedString = HttpUtility.HtmlDecode(decodedString);

          string[] problemData = decodedString.Split(";");
          log.LogInformation("Data size: " + problemData.Length);

          var client = InitClient();
          log.LogInformation("Client initialized...");

          var database = client.GetDatabase("pagesuccess");
          var problems = database.GetCollection < Problem > ("problem");
          var origProblems = database.GetCollection < Problem > ("originalproblem");
          try {
            Problem problem = new Problem();
            if (problemData.Length == 15) {
              problem.timeStamp = problemData[(int) WIDGET_ALL_FIELDS.TIMESTAMP];
              problem.problemDate = problemData[(int) WIDGET_ALL_FIELDS.DATE];
              problem.url = problemData[(int) WIDGET_ALL_FIELDS.URL];
              problem.language = problemData[(int) WIDGET_ALL_FIELDS.LANG];
              problem.oppositeLang = problemData[(int) WIDGET_ALL_FIELDS.OPPOSITE_LANG];
              problem.title = problemData[(int) WIDGET_ALL_FIELDS.TITLE];
              problem.institution = problemData[(int) WIDGET_ALL_FIELDS.INSTITUTION];
              problem.theme = problemData[(int) WIDGET_ALL_FIELDS.THEME];
              problem.section = problemData[(int) WIDGET_ALL_FIELDS.SECTION];
              problem.problem = problemData[(int) WIDGET_ALL_FIELDS.PROBLEM];
              problem.problemDetails = problemData[(int) WIDGET_ALL_FIELDS.PROBLEM_DETAILS];
              problem.yesno = problemData[(int) WIDGET_ALL_FIELDS.YESNO];
              problem.deviceType = problemData[(int) WIDGET_ALL_FIELDS.DEVICE];
              problem.browser = problemData[(int) WIDGET_ALL_FIELDS.BROWSER];
              problem.contact = problemData[(int) WIDGET_ALL_FIELDS.CONTACT];
              problem.dataOrigin = "POST-REQUEST-WIDGET_ALL_FIELDS";
            } else if (problemData.Length == 9) {
              log.LogInformation("Date before conversion: " + problem.problemDate); 
              problem.institution = problemData[(int) WIDGET_EMAIL_VERSION.INSTITUTION].ToUpper().Trim();
              problem.theme = problemData[(int) WIDGET_EMAIL_VERSION.THEME].ToLower().Trim();
              problem.section = problemData[(int) WIDGET_EMAIL_VERSION.SECTION].ToLower().Trim();
              problem.problemDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
              problem.timeStamp = DateTime.UtcNow.ToString("HH:mm");
              problem.title = problemData[(int) WIDGET_EMAIL_VERSION.TITLE];
              problem.url = problemData[(int) WIDGET_EMAIL_VERSION.URL];
              problem.yesno = problemData[(int) WIDGET_EMAIL_VERSION.YESNO];
              problem.problem = problemData[(int) WIDGET_EMAIL_VERSION.PROBLEM];
              problem.problemDetails = problemData[(int) WIDGET_EMAIL_VERSION.PROBLEM_DETAILS];
              problem.dataOrigin = "EMAIL-VERSION-AEM-(OLD)";
            }

            if (problem.url.ToLower().Contains("/en/") || problem.url.ToLower().Contains("travel.gc.ca")) {
              problem.language = "en";
            }
            if (problem.url.ToLower().Contains("/fr/") || problem.url.ToLower().Contains("voyage.gc.ca")) {
              problem.language = "fr";
            }

            problem.processed = "false";
            problem.airTableSync = "false";
            problem.personalInfoProcessed = "false";
            problem.autoTagProcessed = "false";

            // Not reciving Yes comments anymore.
            // if (problem.yesno.ToUpper().Equals("YES") && !problem.problemDetails.Trim().Equals("")) {
            //   log.LogInformation("Problem is a YES with comments, it is spam, discarding... " + problem.yesno + " - " + problem.problemDetails);
            // }

            //format date & timestamps

            if (problem.problemDetails.Equals("")) {
              log.LogInformation("Problem has no comment. Problem will be disregarded.");
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
          } catch (MongoCommandException ex) {
            string msg = ex.Message;
            log.LogError(ex.Message);
          }

        }
      } else {
        break;
      }
    }
    watch.Stop();
    var elapsedMs = watch.ElapsedMilliseconds;
    log.LogInformation("-------------------------------");
    log.LogInformation("time elapsed for " + timesLooped + " entries: " + elapsedMs);
  } catch (Exception e) {
    log.LogError(e.Message);
  }
}
public static MongoClient InitClient() {
  MongoClientSettings settings = new MongoClientSettings();
  settings.Server = new MongoServerAddress(Environment.GetEnvironmentVariable("mongoDBConnectUrl"), Int32.Parse(Environment.GetEnvironmentVariable("mongoDBConnectPort")));
  if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Development")) {
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