#load "TopTask.csx"

using System;
using System.Globalization;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Security.Authentication;
using System.Web;

public static int timesToLoop = 100;

public static int timesLooped = 0;
public static async Task Run(TimerInfo myTimer, ILogger log)
{
    try
    {
        //connect to mongodb and toptaskqueue collection
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        QueueClient queue = new QueueClient(connectionString, "toptaskqueue");

        //start stop watch to count time taken to process x entries
        var watch = System.Diagnostics.Stopwatch.StartNew();
        timesLooped = 0;
        for (int index = 0; index < timesToLoop; index++)
        {
            QueueProperties properties = await queue.GetPropertiesAsync();
            if (properties.ApproximateMessagesCount > 0)
            {
                QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1);

                if (retrievedMessage.Length > 0)
                {

                    string theMessage = retrievedMessage[0].MessageText;
                    byte[] data = Convert.FromBase64String(theMessage);
                    string decodedString = Encoding.UTF8.GetString(data);


                    decodedString = decodedString.Replace("<html><body><pre>", "");
                    decodedString = decodedString.Replace("</pre></body></html>", "");

                    decodedString = HttpUtility.HtmlDecode(decodedString);

                    log.LogInformation("decoded string: " + decodedString);
                    // split the decoded string by it's delimiter "~!~"
                    string[] topTaskdata = decodedString.Split("~!~");
                    // print the data size (number of fields)
                    log.LogInformation("Data size:" + topTaskdata.Length);

                    //connect to toptasksurvey collection
                    var client = InitClient();
                    log.LogInformation("Client initialized...");
                    var database = client.GetDatabase("pagesuccess");
                    var topTasks = database.GetCollection<TopTask>("toptasksurvey");

                    try
                    {
                        TopTask toptask = new TopTask();
                        if (topTaskdata.Length == 24)
                        {
                            log.LogInformation("Data retrieved has length of 24.");

                            toptask.timeStamp = topTaskdata[0];
                            toptask.dateTime = topTaskdata[0];
                            toptask.surveyReferrer = topTaskdata[1];
                            toptask.language = topTaskdata[2];
                            toptask.device = topTaskdata[3];
                            toptask.screener = topTaskdata[4];

                            //check if Department is not empty for task 1 and is empty for task 2. Set task 1 data.
                            //the reason to check for " / " is because PP is sending bilingual values seperated by a slash
                            //if there is no data it still comes in as " / "
                            if (!(topTaskdata[5].Equals(" / ") || topTaskdata[5].Equals("")) && (topTaskdata[11].Equals(" / ") || topTaskdata[11].Equals("")))
                            {
                                toptask.dept = topTaskdata[5];
                                toptask.theme = topTaskdata[6];
                                toptask.themeOther = topTaskdata[7];
                                log.LogInformation("theme Other: " + toptask.themeOther);
                                toptask.grouping = topTaskdata[8];
                                toptask.task = topTaskdata[9];
                                toptask.taskOther = topTaskdata[10];
                                log.LogInformation("Entry is Task 1");
                            }
                            //check if Department is not empty for task 2. Set task 2 data.
                            if (!(topTaskdata[11].Equals(" / ") || topTaskdata[11].Equals("")))
                            {
                                toptask.dept = topTaskdata[11];
                                toptask.theme = topTaskdata[12];
                                toptask.themeOther = topTaskdata[7];
                                log.LogInformation("theme Other: " + toptask.themeOther);
                                toptask.grouping = topTaskdata[13];
                                toptask.task = topTaskdata[14];
                                toptask.taskOther = topTaskdata[15];
                                log.LogInformation("Entry is task 2");
                            }
                            toptask.themeOther = topTaskdata[7];
                            log.LogInformation("theme Other: " + toptask.themeOther);
                            toptask.taskSatisfaction = topTaskdata[16];
                            toptask.taskEase = topTaskdata[17];
                            toptask.taskCompletion = topTaskdata[18];
                            toptask.taskImprove = topTaskdata[19];
                            toptask.taskImproveComment = topTaskdata[20];
                            toptask.taskWhyNot = topTaskdata[21];
                            toptask.taskWhyNotComment = topTaskdata[22];
                            toptask.taskSampling = topTaskdata[23];
                            string[] topTaskSampling = topTaskdata[23].Split(":");

                            if (topTaskSampling.Length == 7)
                            {
                                toptask.samplingInvitation = topTaskSampling[0];
                                toptask.samplingGC = topTaskSampling[1];
                                toptask.samplingCanada = topTaskSampling[2];
                                toptask.samplingTheme = topTaskSampling[3];
                                toptask.samplingInstitution = topTaskSampling[4];
                                toptask.samplingGrouping = topTaskSampling[5];
                                toptask.samplingTask = topTaskSampling[6];
                            }
                            else
                            {
                                toptask.samplingInvitation = "";
                                toptask.samplingGC = "";
                                toptask.samplingCanada = "";
                                toptask.samplingTheme = "";
                                toptask.samplingInstitution = "";
                                toptask.samplingGrouping = "";
                                toptask.samplingTask = "";
                            }

                            toptask.processed = "false";
                            toptask.topTaskAirTableSynce = "false";
                            toptask.personalInfoProcessed = "false";
                            toptask.autoTagProcessed = "false";

                            //format date & timestamps
                            toptask.dateTime = DateTime.Parse(toptask.dateTime, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                            log.LogInformation("Date converted to: " + toptask.dateTime);

                            toptask.timeStamp = DateTime.Parse(toptask.timeStamp, CultureInfo.InvariantCulture).ToString("HH:mm");
                            log.LogInformation("Timestamp converted to: " + toptask.timeStamp);

                            //save to DB
                            topTasks.InsertOne(toptask);
                            log.LogInformation("Records saved. TopTask ID:" + toptask.id);

                        }
                        else
                        {
                            log.LogInformation("data length is not 24");
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
            }
        }

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        log.LogInformation("-------------------------------");
        log.LogInformation("time elapsed for " + timesLooped + " entries: " + elapsedMs);
    }
    catch (Exception e)
    {
        log.LogError(e.Message);
    }
}
public static MongoClient InitClient()
{
    MongoClientSettings settings = new MongoClientSettings();
    settings.Server = new MongoServerAddress(Environment.GetEnvironmentVariable("mongoDBConnectUrl"), Int32.Parse(Environment.GetEnvironmentVariable("mongoDBConnectPort")));
    if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Development"))
    {
        settings.UseTls = true;
        settings.SslSettings = new SslSettings();
        settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;
        settings.RetryWrites = false;
        MongoIdentity identity = new MongoInternalIdentity(Environment.GetEnvironmentVariable("mongoDBDataBaseName"), Environment.GetEnvironmentVariable("mongoDBUsername"));
        MongoIdentityEvidence evidence = new PasswordEvidence(Environment.GetEnvironmentVariable("mongoDBPassword"));
        settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);
    }
    MongoClient client = new MongoClient(settings);
    return client;
}

