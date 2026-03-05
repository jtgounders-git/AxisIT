using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace INSY7315_TheBteam.Models
{
    [FirestoreData]
    public class ProjectPhase
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string Name { get; set; } = "";
        [FirestoreProperty] public double Budget { get; set; }
        [FirestoreProperty] public DateTime Deadline { get; set; } = DateTime.UtcNow;
        [FirestoreProperty] public int ProjectedPercent { get; set; } = 0;
        [FirestoreProperty] public int ActualPercent { get; set; } = 0;
        [FirestoreProperty] public Dictionary<string, double> Resources { get; set; } = new();
    }

    [FirestoreData]
    public class ProjectModel
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string ProjectCode { get; set; } = "";
        [FirestoreProperty] public string Title { get; set; } = "";
        [FirestoreProperty] public string Client { get; set; } = "";
        [FirestoreProperty] public string Contractor { get; set; } = "";
        [FirestoreProperty] public string ProjectManager { get; set; } = "";
        [FirestoreProperty] public double TotalBudget { get; set; } = 0;
        [FirestoreProperty] public double Spent { get; set; } = 0;
        [FirestoreProperty] public int Progress { get; set; } = 0;
        [FirestoreProperty] public string CurrentPhase { get; set; } = "";
        [FirestoreProperty] public List<string> Phases { get; set; } = new();
        [FirestoreProperty] public DateTime StartDate { get; set; } = DateTime.UtcNow;
        [FirestoreProperty] public DateTime? EndDate { get; set; } = null; // ✅ NEW PROPERTY
        [FirestoreProperty] public List<ProjectPhase> DetailedPhases { get; set; } = new();
        [FirestoreProperty] public bool IsCompleted { get; set; } = false;
        [FirestoreProperty] public DateTime? CompletedAt { get; set; } = null;
        [FirestoreProperty] public string SiteAddress { get; set; } = "";
        [FirestoreProperty] public string ProjectType { get; set; } = "";
        [FirestoreProperty] public string Description { get; set; } = "";
    }

    // NEW: Task Model
    [FirestoreData]
    public class ProjectTask
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string ProjectId { get; set; } = "";
        [FirestoreProperty] public string TaskName { get; set; } = "";
        [FirestoreProperty] public string Description { get; set; } = "";
        [FirestoreProperty] public string AssignedContractor { get; set; } = "";
        [FirestoreProperty] public DateTime DueDate { get; set; } = DateTime.UtcNow;
        [FirestoreProperty] public bool IsCompleted { get; set; } = false;
        [FirestoreProperty] public DateTime? CompletedDate { get; set; } = null;
        [FirestoreProperty] public string Status { get; set; } = "pending"; // pending, active, completed
        [FirestoreProperty] public double AllocatedBudget { get; set; } = 0;
        [FirestoreProperty] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // NEW: Resource Allocation Model
    [FirestoreData]
    public class ResourceAllocation
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string ProjectId { get; set; } = "";
        [FirestoreProperty] public string TaskId { get; set; } = "";
        [FirestoreProperty] public string ResourceName { get; set; } = "";
        [FirestoreProperty] public string Phase { get; set; } = "";
        [FirestoreProperty] public int Quantity { get; set; } = 0;
        [FirestoreProperty] public int AllocatedQuantity { get; set; } = 0;
        [FirestoreProperty] public double CostPerUnit { get; set; } = 0;
        [FirestoreProperty] public double TotalCost { get; set; } = 0;
        [FirestoreProperty] public string Status { get; set; } = "pending"; // pending, partial, allocated
        [FirestoreProperty] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [FirestoreProperty] public double PercentOfBudget { get; set; } = 0;
    }

    // NEW: Project Progress Model
    [FirestoreData]
    public class ProjectProgress
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string ProjectId { get; set; } = "";
        [FirestoreProperty] public int OverallProgress { get; set; } = 0;
        [FirestoreProperty] public int TotalTasks { get; set; } = 0;
        [FirestoreProperty] public int CompletedTasks { get; set; } = 0;
        [FirestoreProperty] public double BudgetLimit { get; set; } = 0;
        [FirestoreProperty] public double TotalAllocated { get; set; } = 0;
        [FirestoreProperty] public double TotalSpent { get; set; } = 0;
        [FirestoreProperty] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    [FirestoreData]
    public class ClientRequestModel
    {
        [FirestoreProperty] public string Id { get; set; } = "";
        [FirestoreProperty("clientName")] public string ClientName { get; set; } = "";
        [FirestoreProperty("companyName")] public string CompanyName { get; set; } = "";
        [FirestoreProperty("clientEmail")] public string ClientEmail { get; set; } = "";
        [FirestoreProperty("clientPhone")] public string ClientPhone { get; set; } = "";
        [FirestoreProperty("projectTitle")] public string ProjectTitle { get; set; } = "";
        [FirestoreProperty("projectType")] public string ProjectType { get; set; } = "";
        [FirestoreProperty("siteAddress")] public string SiteAddress { get; set; } = "";
        [FirestoreProperty("estimatedBudget")] public double EstimatedBudget { get; set; } = 0;
        [FirestoreProperty("preferredStart")] public string PreferredStart { get; set; } = "";
        [FirestoreProperty("projectDescription")] public string ProjectDescription { get; set; } = "";
        [FirestoreProperty("contactMethod")] public string ContactMethod { get; set; } = "";
        [FirestoreProperty("urgency")] public string Urgency { get; set; } = "";
        [FirestoreProperty("additionalNotes")] public string AdditionalNotes { get; set; } = "";
        [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [FirestoreProperty("isAccepted")] public bool IsAccepted { get; set; } = false;
        [FirestoreProperty("acceptedBy")] public string AcceptedBy { get; set; } = "";
        [FirestoreProperty("acceptedAt")] public DateTime? AcceptedAt { get; set; } = null;
    }

    public enum RequestStatus { Pending, InProgress, Complete }

    [FirestoreData]
    public class MaintenanceRequestModel
    {
        [FirestoreProperty] public string Id { get; set; } = Guid.NewGuid().ToString();
        [FirestoreProperty] public string Title { get; set; } = "";
        [FirestoreProperty] public string Description { get; set; } = "";
        [FirestoreProperty] public string ClientUsername { get; set; } = "";
        [FirestoreProperty] public string AssignedContractor { get; set; } = "";
        [FirestoreProperty] public RequestStatus Status { get; set; } = RequestStatus.Pending;
        [FirestoreProperty] public List<string> UploadedFiles { get; set; } = new();
        [FirestoreProperty] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}