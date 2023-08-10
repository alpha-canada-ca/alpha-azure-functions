using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class OriginalProblem : Problem
{
    public OriginalProblem(Problem problem)
    {
        this.id = problem.id;
        this.timeStamp = problem.timeStamp;
        this.problemDate = problem.problemDate;
        this.url = problem.url;
        this.language = problem.language;
        this.oppositeLang = problem.oppositeLang;
        this.title = problem.title;
        this.institution = problem.institution;
        this.theme = problem.theme;
        this.section = problem.section;
        this.problem = problem.problem;
        this.problemDetails = problem.problemDetails;
        this.yesno = problem.yesno;
        this.deviceType = problem.deviceType;
        this.browser = problem.browser;
        this.contact = problem.contact;
        this.processed = problem.processed;
        this.airTableSync = problem.airTableSync;
        this.personalInfoProcessed = problem.personalInfoProcessed;
        this.autoTagProcessed = problem.autoTagProcessed;
        this.dataOrigin = problem.dataOrigin;
        this.tags = new List<String>();
    }
}

