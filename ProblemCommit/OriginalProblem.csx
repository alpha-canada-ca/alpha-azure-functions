using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class OriginalProblem : Problem {
	
	public OriginalProblem(Problem problem) {
		this.id = problem.id;
		this.url = problem.url;
		this.problem = problem.problem;
		this.problemDetails = problem.problemDetails;
		this.department = problem.department;
		this.language = problem.language;
		this.resolutionDate = problem.resolutionDate;
		this.resolution = problem.resolution;
		this.topic = problem.topic;
		this.problemDate = problem.problemDate;
		this.title = problem.title;
		this.yesno = problem.yesno;
		this.processed = problem.processed;
		this.airTableSync = problem.airTableSync;
		this.personalInfoProcessed = problem.personalInfoProcessed;
		this.autoTagProcessed = problem.autoTagProcessed;
		this.dataOrigin = problem.dataOrigin;
		this.tags = new List<String>();
        this.institution = problem.institution;
        this.theme = problem.theme;
        this.section  = problem.section;

		
	}
}

 