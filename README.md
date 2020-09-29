# alpha-azure-functions
Azure function for receiving data from top task survey and page feedback widget

QueueProblem - Takes email sent to pagesuccess@pagesuccess.com and adds data to queue
QueueProblemForm - Takes data from form and adds it to queue
ProblemCommit - Adds queue data to mongoDB store
QueueTopTaskSurveyForm - Adds data from top task survey to queue 

