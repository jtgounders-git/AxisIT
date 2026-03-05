using Google.Cloud.Firestore;
using INSY7315_TheBteam.Models;
using INSY7315_TheBteam.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Controllers
{
    public class TaskManagementController : Controller
    {
        private readonly FirestoreDb _firestore;

        public TaskManagementController(IFirebaseService firebaseService)
        {
            var firebaseField = firebaseService.GetType().GetField("_firestore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _firestore = (FirestoreDb)firebaseField?.GetValue(firebaseService)
                         ?? throw new ArgumentNullException("Unable to obtain FirestoreDb from IFirebaseService.");
        }

        // Get all tasks for a project
        [HttpGet]
        public async Task<IActionResult> GetProjectTasks(string projectId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectId))
                    return Json(new { success = false, message = "projectId is required" });

                var snapshot = await _firestore.Collection("tasks")
                    .WhereEqualTo("ProjectId", projectId)
                    .GetSnapshotAsync();

                var tasks = new List<ProjectTask>();
                foreach (var doc in snapshot.Documents)
                {
                    var task = doc.ConvertTo<ProjectTask>();
                    task.Id = doc.Id;
                    tasks.Add(task);
                }

                return Json(new { success = true, data = tasks.OrderBy(t => t.DueDate).ToList() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // NEW: Get tasks assigned to currently signed-in user (by session "Email")
        [HttpGet]
        public async Task<IActionResult> GetAssignedTasks()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email") ?? "";
                if (string.IsNullOrWhiteSpace(userEmail))
                    return Json(new { success = false, message = "No logged-in user found." });

                var snapshot = await _firestore.Collection("tasks")
                    .WhereEqualTo("AssignedContractor", userEmail)
                    .GetSnapshotAsync();

                var tasks = new List<ProjectTask>();
                foreach (var doc in snapshot.Documents)
                {
                    var task = doc.ConvertTo<ProjectTask>();
                    task.Id = doc.Id;
                    tasks.Add(task);
                }

                return Json(new { success = true, data = tasks.OrderBy(t => t.DueDate).ToList() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Create new task
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ProjectId))
                    return Json(new { success = false, message = "Invalid request" });

                var projectDoc = await _firestore.Collection("projects").Document(request.ProjectId).GetSnapshotAsync();
                if (!projectDoc.Exists)
                    return Json(new { success = false, message = "Project not found" });

                var project = projectDoc.ConvertTo<ProjectModel>();

                var progressQuery = await _firestore.Collection("projectProgress")
                    .WhereEqualTo("ProjectId", request.ProjectId)
                    .GetSnapshotAsync();

                ProjectProgress progress;
                string progressId;

                if (progressQuery.Documents.Count > 0)
                {
                    var progressDoc = progressQuery.Documents[0];
                    progress = progressDoc.ConvertTo<ProjectProgress>();
                    progressId = progressDoc.Id;

                    if (progress.TotalAllocated + request.AllocatedBudget > progress.BudgetLimit)
                    {
                        return Json(new { success = false, message = $"Budget limit exceeded. Available: R{(progress.BudgetLimit - progress.TotalAllocated):N2}" });
                    }
                }
                else
                {
                    progress = new ProjectProgress
                    {
                        ProjectId = request.ProjectId,
                        BudgetLimit = project.TotalBudget,
                        OverallProgress = 0,
                        TotalTasks = 0,
                        CompletedTasks = 0,
                        TotalAllocated = 0,
                        TotalSpent = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    progressId = progress.Id;
                    await _firestore.Collection("projectProgress").Document(progressId).SetAsync(progress);
                }

                var task = new ProjectTask
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = request.ProjectId,
                    TaskName = request.TaskName ?? "",
                    Description = request.Description ?? "",
                    AssignedContractor = request.AssignedContractor ?? "",
                    DueDate = !string.IsNullOrWhiteSpace(request.DueDate) ? DateTime.Parse(request.DueDate).ToUniversalTime() : DateTime.UtcNow,
                    IsCompleted = false,
                    Status = "pending",
                    AllocatedBudget = request.AllocatedBudget,
                    CreatedAt = DateTime.UtcNow
                };

                await _firestore.Collection("tasks").Document(task.Id).SetAsync(task);

                progress.TotalTasks++;
                progress.TotalAllocated += request.AllocatedBudget;
                progress.LastUpdated = DateTime.UtcNow;
                await _firestore.Collection("projectProgress").Document(progressId).SetAsync(progress, SetOptions.MergeAll);

                await _firestore.Collection("projects").Document(request.ProjectId).UpdateAsync(new Dictionary<string, object>
                {
                    { "Spent", progress.TotalAllocated }
                });

                // ✅ CRITICAL: Update project progress after creating task
                await UpdateProjectProgress(request.ProjectId);

                return Json(new { success = true, message = "Task created successfully", data = task });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Complete task
        [HttpPost]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.TaskId))
                    return Json(new { success = false, message = "taskId is required" });

                var taskDoc = await _firestore.Collection("tasks").Document(request.TaskId).GetSnapshotAsync();
                if (!taskDoc.Exists)
                    return Json(new { success = false, message = "Task not found" });

                var task = taskDoc.ConvertTo<ProjectTask>();
                if (task.IsCompleted)
                    return Json(new { success = false, message = "Task already completed" });

                await _firestore.Collection("tasks").Document(request.TaskId).UpdateAsync(new Dictionary<string, object>
                {
                    { "IsCompleted", true },
                    { "Status", "completed" },
                    { "CompletedDate", Timestamp.FromDateTime(DateTime.UtcNow) }
                });

                await UpdateProjectProgress(task.ProjectId);

                return Json(new { success = true, message = "Task completed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ UPDATED METHOD: update project progress accurately by checking both IsCompleted and Status
        private async Task UpdateProjectProgress(string projectId)
        {
            try
            {
                var projectRef = _firestore.Collection("projects").Document(projectId);
                var projectDoc = await projectRef.GetSnapshotAsync();

                if (!projectDoc.Exists)
                    return;

                var tasksSnapshot = await _firestore.Collection("tasks")
                    .WhereEqualTo("ProjectId", projectId)
                    .GetSnapshotAsync();

                if (tasksSnapshot.Count == 0)
                {
                    await projectRef.UpdateAsync("Progress", 0);
                    return;
                }

                int completedTasks = 0;
                foreach (var taskDoc in tasksSnapshot.Documents)
                {
                    var taskData = taskDoc.ToDictionary();
                    bool isCompleted = false;

                    // Check IsCompleted field first
                    if (taskData.ContainsKey("IsCompleted") && taskData["IsCompleted"] is bool completed)
                    {
                        isCompleted = completed;
                    }

                    // Also check Status field as fallback
                    if (!isCompleted && taskData.ContainsKey("Status"))
                    {
                        var status = taskData["Status"]?.ToString()?.ToLower() ?? "";
                        isCompleted = status == "completed";
                    }

                    if (isCompleted)
                        completedTasks++;
                }

                int totalTasks = tasksSnapshot.Count;
                int progressPercentage = (int)Math.Round((completedTasks / (double)totalTasks) * 100);

                // Update the project's Progress field
                await projectRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "Progress", progressPercentage }
                });

                System.Diagnostics.Debug.WriteLine($"✅ Updated project {projectId} progress to {progressPercentage}% ({completedTasks}/{totalTasks} tasks completed)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating project progress: {ex.Message}");
            }
        }

        // Get project progress
        [HttpGet]
        public async Task<IActionResult> GetProjectProgress(string projectId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectId))
                    return Json(new { success = false, message = "projectId is required" });

                var progressQuery = await _firestore.Collection("projectProgress")
                    .WhereEqualTo("ProjectId", projectId)
                    .GetSnapshotAsync();

                if (progressQuery.Documents.Count > 0)
                {
                    var progressDoc = progressQuery.Documents[0];
                    var progress = progressDoc.ConvertTo<ProjectProgress>();
                    return Json(new { success = true, data = progress });
                }

                var computedProgress = await ComputeProgressFromTasks(projectId);
                return Json(new { success = true, data = computedProgress });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Allocate resources
        [HttpPost]
        public async Task<IActionResult> AllocateResource([FromBody] AllocateResourceRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ProjectId))
                    return Json(new { success = false, message = "Invalid request" });

                var projectDoc = await _firestore.Collection("projects").Document(request.ProjectId).GetSnapshotAsync();
                if (!projectDoc.Exists)
                    return Json(new { success = false, message = "Project not found" });

                var project = projectDoc.ConvertTo<ProjectModel>();
                var budgetLimit = project.TotalBudget;

                var progressQuery = await _firestore.Collection("projectProgress")
                    .WhereEqualTo("ProjectId", request.ProjectId)
                    .GetSnapshotAsync();

                if (progressQuery.Documents.Count == 0)
                {
                    var newProgress = new ProjectProgress
                    {
                        ProjectId = request.ProjectId,
                        BudgetLimit = budgetLimit,
                        OverallProgress = 0,
                        TotalTasks = 0,
                        CompletedTasks = 0,
                        TotalAllocated = 0,
                        TotalSpent = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _firestore.Collection("projectProgress").Document(newProgress.Id).SetAsync(newProgress);
                    progressQuery = await _firestore.Collection("projectProgress").WhereEqualTo("ProjectId", request.ProjectId).GetSnapshotAsync();
                }

                var progressDoc = progressQuery.Documents[0];
                var progress = progressDoc.ConvertTo<ProjectProgress>();

                double totalCost = request.Quantity * request.CostPerUnit;

                if (progress.TotalAllocated + totalCost > progress.BudgetLimit)
                    return Json(new { success = false, message = $"Budget limit exceeded. Available: R{(progress.BudgetLimit - progress.TotalAllocated):N2}" });

                double percentOfBudget = progress.BudgetLimit > 0 ? (totalCost / progress.BudgetLimit) * 100.0 : 0;

                var resourceData = new Dictionary<string, object>
                {
                    { "ProjectId", request.ProjectId },
                    { "TaskId", request.TaskId ?? "" },
                    { "ResourceName", request.ResourceName ?? "" },
                    { "Phase", request.Phase ?? "" },
                    { "Quantity", request.Quantity },
                    { "AllocatedQuantity", request.Quantity },
                    { "CostPerUnit", request.CostPerUnit },
                    { "TotalCost", totalCost },
                    { "PercentOfBudget", percentOfBudget },
                    { "Status", "allocated" },
                    { "CreatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                var resourceId = Guid.NewGuid().ToString();
                await _firestore.Collection("resources").Document(resourceId).SetAsync(resourceData);

                progress.TotalAllocated += totalCost;
                progress.LastUpdated = DateTime.UtcNow;
                await _firestore.Collection("projectProgress").Document(progressDoc.Id).SetAsync(progress, SetOptions.MergeAll);

                await _firestore.Collection("projects").Document(request.ProjectId).UpdateAsync(new Dictionary<string, object>
                {
                    { "Spent", progress.TotalAllocated }
                });

                resourceData["Id"] = resourceId;
                return Json(new { success = true, message = "Resource allocated successfully", data = resourceData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get project resources
        [HttpGet]
        public async Task<IActionResult> GetProjectResources(string projectId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectId))
                    return Json(new { success = false, message = "projectId is required" });

                var snapshot = await _firestore.Collection("resources")
                    .WhereEqualTo("ProjectId", projectId)
                    .GetSnapshotAsync();

                var resources = new List<ResourceAllocation>();
                foreach (var doc in snapshot.Documents)
                {
                    var resource = doc.ConvertTo<ResourceAllocation>();
                    resource.Id = doc.Id;
                    resources.Add(resource);
                }

                return Json(new { success = true, data = resources });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Compute progress
        private async Task<ProjectProgress> ComputeProgressFromTasks(string projectId)
        {
            var progress = new ProjectProgress
            {
                ProjectId = projectId,
                OverallProgress = 0,
                TotalTasks = 0,
                CompletedTasks = 0,
                BudgetLimit = 0,
                TotalAllocated = 0,
                TotalSpent = 0,
                LastUpdated = DateTime.UtcNow
            };

            var projectDoc = await _firestore.Collection("projects").Document(projectId).GetSnapshotAsync();
            if (projectDoc.Exists)
            {
                var project = projectDoc.ConvertTo<ProjectModel>();
                progress.BudgetLimit = project.TotalBudget;
            }

            var tasksSnap = await _firestore.Collection("tasks").WhereEqualTo("ProjectId", projectId).GetSnapshotAsync();
            foreach (var doc in tasksSnap.Documents)
            {
                var t = doc.ConvertTo<ProjectTask>();
                progress.TotalTasks++;
                progress.TotalAllocated += t.AllocatedBudget;
                if (t.IsCompleted)
                {
                    progress.CompletedTasks++;
                    progress.TotalSpent += t.AllocatedBudget;
                }
            }

            progress.OverallProgress = progress.TotalTasks > 0 ? (int)((double)progress.CompletedTasks / progress.TotalTasks * 100) : 0;
            progress.LastUpdated = DateTime.UtcNow;
            return progress;
        }
    }

    // Request Models
    public class CreateTaskRequest
    {
        public string ProjectId { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string AssignedContractor { get; set; }
        public string DueDate { get; set; }
        public double AllocatedBudget { get; set; }
    }

    public class AllocateResourceRequest
    {
        public string ProjectId { get; set; }
        public string TaskId { get; set; }
        public string ResourceName { get; set; }
        public string Phase { get; set; }
        public int Quantity { get; set; }
        public double CostPerUnit { get; set; }
    }

    public class CompleteTaskRequest
    {
        public string TaskId { get; set; }
    }
}
