using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class TopTask {
	
	[BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
	public ObjectId id { get; set; }
	public String dateTime { get; set; }
	public String timeStamp{ get; set; }
	public String surveyReferrer { get; set; }
	public String language { get; set; }
	public String device { get; set; }
	public String screener { get; set; }
	public String dept { get; set; }
	public String theme { get; set; }
	public String themeOther { get; set; }
	public String grouping { get; set; }
	public String task { get; set; }
	public String taskOther { get; set; }
	public String taskSatisfaction { get; set; }
	public String taskEase { get; set; }
	public String taskCompletion { get; set; }
	public String taskImprove { get; set; }
	public String taskImproveComment { get; set; }
	public String taskWhyNot { get; set; }
	public String taskWhyNotComment { get; set; }
	public String taskSampling { get; set; }
	public String samplingInvitation { get; set; }
	public String samplingGC { get; set; }
	public String samplingCanada { get; set; }
	public String samplingTheme { get; set; }
	public String samplingInstitution { get; set; }
	public String samplingGrouping { get; set; }
	public String samplingTask { get; set; }
	public String processed { get; set; }
	public String topTaskAirTableSynce { get; set; }
	public String personalInfoProcessed { get; set; }
	public String autoTagProcessed { get; set; }
	
	

	public TopTask() {

	}
}

 