using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Problem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId id { get; set; }
    public String timeStamp { get; set; }
    public String problemDate { get; set; }
    public String url { get; set; }
    public String language { get; set; }
    public String oppositeLang { get; set; }
    public String title { get; set; }
    public String institution { get; set; }
    public String theme { get; set; }
    public String section { get; set; }
    public String problem { get; set; }
    public String problemDetails { get; set; }
    public String yesno { get; set; }
    public String deviceType { get; set; }
    public String browser { get; set; }
    public String contact { get; set; }
    public String processed { get; set; }
    public String airTableSync { get; set; }
    public String personalInfoProcessed { get; set; }
    public String autoTagProcessed { get; set; }
    public String dataOrigin { get; set; }
    public List<String> tags = new List<String>();


    public Problem()
    {

    }
}

