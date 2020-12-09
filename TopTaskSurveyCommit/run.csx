
using System;
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

public static async Task Run(TimerInfo myTimer, ILogger log)
{
    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
    var client = InitClient();
    log.LogInformation("Connection succesful");
     try {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        QueueClient queue = new QueueClient(connectionString, "toptaskqueue");
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
               
                string[] topTaskData = decodedString.Split(";");
                log.LogInformation("Data size:" + topTaskData.Length);

                log.LogInformation("Client initialized...");
                var database = client.GetDatabase("pagesuccess");
                var document = BsonSerializer.Deserialize<BsonDocument>(decodedString);
                var collection = database.GetCollection<BsonDocument>("toptasksurvey");
                await collection.InsertOneAsync(document);
                log.LogInformation("Successfully added to DB.");
                queue.DeleteMessage(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                log.LogInformation("The JSON data has been dequeued.");
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
        MongoIdentity identity = new MongoInternalIdentity(Environment.GetEnvironmentVariable("mongoDBDataBaseName"), Environment.GetEnvironmentVariable("mongoDBUsername"));
        MongoIdentityEvidence evidence = new PasswordEvidence(Environment.GetEnvironmentVariable("mongoDBPassword"));
        settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence); 
    }
    MongoClient client = new MongoClient(settings);
    return client;
}

