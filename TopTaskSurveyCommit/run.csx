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
        // Connect to MongoDB and toptaskqueue collection
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        QueueClient queue = new QueueClient(connectionString, "toptaskqueue");

        // Start stopwatch to count time taken to process x entries
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

                    log.LogInformation("Decoded string: " + decodedString);

                    // Split the decoded string by its delimiter "~!~"
                    string[] topTaskData = decodedString.Split("~!~");
                    log.LogInformation("Data size: " + topTaskData.Length);

                    // Connect to toptasksurvey collection
                    var client = InitClient();
                    log.LogInformation("Client initialized...");
                    var database = client.GetDatabase("pagesuccess");
                    var topTasks = database.GetCollection<TopTask>("toptasksurvey");

                    try
                    {
                        TopTask toptask = new TopTask();
                        if (topTaskData.Length == 24)
                        {
                            log.LogInformation("Data retrieved has length of 24.");

                            toptask.timeStamp = topTaskData[0];
                            toptask.dateTime = topTaskData[0];
                            toptask.surveyReferrer = topTaskData[1];
                            toptask.language = topTaskData[2];
                            toptask.device = topTaskData[3];
                            toptask.screener = topTaskData[4];

                            // Check if Department is not empty for task 1 and is empty for task 2. Set task 1 data.
                            if (!(topTaskData[5].Equals(" / ") || topTaskData[5].Equals("")) && (topTaskData[11].Equals(" / ") || topTaskData[11].Equals("")))
                            {
                                toptask.dept = topTaskData[5];
                                toptask.theme = topTaskData[6];
                                toptask.themeOther = topTaskData[7];
                                log.LogInformation("Theme Other: " + toptask.themeOther);
                                toptask.grouping = topTaskData[8];
                                toptask.task = topTaskData[9];
                                toptask.taskOther = topTaskData[10];
                                log.LogInformation("Entry is Task 1");
                            }
                            // Check if Department is not empty for task 2. Set task 2 data.
                            if (!(topTaskData[11].Equals(" / ") || topTaskData[11].Equals("")))
                            {
                                toptask.dept = topTaskData[11];
                                toptask.theme = topTaskData[12];
                                toptask.themeOther = topTaskData[7];
                                log.LogInformation("Theme Other: " + toptask.themeOther);
                                toptask.grouping = topTaskData[13];
                                toptask.task = topTaskData[14];
                                toptask.taskOther = topTaskData[15];
                                log.LogInformation("Entry is Task 2");
                            }

                            toptask.taskSatisfaction = topTaskData[16];
                            toptask.taskEase = topTaskData[17];
                            toptask.taskCompletion = topTaskData[18];
                            toptask.taskImprove = topTaskData[19];
                            toptask.taskImproveComment = topTaskData[20];
                            toptask.taskWhyNot = topTaskData[21];
                            toptask.taskWhyNotComment = topTaskData[22];
                            toptask.taskSampling = topTaskData[23];
                            string[] topTaskSampling = topTaskData[23].Split(":");

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

                            // Format date & timestamps
                            toptask.dateTime = DateTime.Parse(toptask.dateTime, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                            log.LogInformation("Date converted to: " + toptask.dateTime);

                            toptask.timeStamp = DateTime.Parse(toptask.timeStamp, CultureInfo.InvariantCulture).ToString("HH:mm");
                            log.LogInformation("Timestamp converted to: " + toptask.timeStamp);

                            // Save to DB
                            topTasks.InsertOne(toptask);
                            log.LogInformation("Records saved. TopTask ID: " + toptask.id);
                        }
                        else
                        {
                            log.LogInformation("Data length is not 24");
                        }

                        timesLooped++;

                        // Dequeue the database record. Delete the message
                        queue.DeleteMessage(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                        log.LogInformation("The email data has been dequeued.");
                    }
                    catch (MongoCommandException ex)
                    {
                        log.LogError(ex, "MongoCommandException: An error occurred while interacting with MongoDB.");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Exception: An unexpected error occurred.");
                    }
                }
            }
        }

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        log.LogInformation("-------------------------------");
        log.LogInformation("Time elapsed for " + timesLooped + " entries: " + elapsedMs);
    }
    catch (Exception e)
    {
        log.LogError(e, "Exception: An unexpected error occurred in the Run method.");
    }
}

public static MongoClient InitClient()
{
    MongoClientSettings settings = new MongoClientSettings
    {
        Server = new MongoServerAddress(Environment.GetEnvironmentVariable("mongoDBConnectUrl"), Int32.Parse(Environment.GetEnvironmentVariable("mongoDBConnectPort")))
    };

    if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").Equals("Development"))
    {
        settings.UseTls = true;
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };
        settings.RetryWrites = false;

        MongoIdentity identity = new MongoInternalIdentity(Environment.GetEnvironmentVariable("mongoDBDataBaseName"), Environment.GetEnvironmentVariable("mongoDBUsername"));
        MongoIdentityEvidence evidence = new PasswordEvidence(Environment.GetEnvironmentVariable("mongoDBPassword"));
        settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);
    }

    return new MongoClient(settings);
}
