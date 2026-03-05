using Google.Cloud.Firestore;
using System;

namespace INSY7315_TheBteam.Models
{
    [FirestoreData]
    public class CompletionReportModel
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty] public string TaskName { get; set; } = "";
        [FirestoreProperty] public string ProjectName { get; set; } = "";
        [FirestoreProperty] public string WorkDescription { get; set; } = "";
        [FirestoreProperty] public string Challenges { get; set; } = "";
        [FirestoreProperty] public string MaterialsUsed { get; set; } = "";
        [FirestoreProperty] public string AdditionalNotes { get; set; } = "";

        [FirestoreProperty] public bool SafetyCheck { get; set; }
        [FirestoreProperty] public bool QualityCheck { get; set; }
        [FirestoreProperty] public bool InspectionCheck { get; set; }

        [FirestoreProperty] public double HoursWorked { get; set; }

        [FirestoreProperty] public DateTime CompletionDate { get; set; }
        [FirestoreProperty] public DateTime SubmissionDate { get; set; }

        [FirestoreProperty] public string SubmittedBy { get; set; } = "";
    }
}
