using Google.Cloud.Firestore;
using INSY7315_TheBteam.Models;
using INSY7315_TheBteam.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Controllers
{
    public class ProjectManagerController : Controller
    {
        private readonly FirestoreDb _firestore;
        private readonly IFirebaseService _firebaseService;

        public ProjectManagerController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            var firebaseField = firebaseService.GetType().GetField("_firestore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _firestore = (FirestoreDb)firebaseField?.GetValue(firebaseService);
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/PMDashboard
        // ----------------------------------------------------------
        public async Task<IActionResult> PMDashboard()
        {
            try
            {
                var userName = HttpContext.Session.GetString("User") ?? "Project Manager";
                var userEmail = HttpContext.Session.GetString("Email") ?? "";
                var userRole = HttpContext.Session.GetString("Role") ?? "Project Manager";

                ViewData["User"] = userName;
                ViewData["Email"] = userEmail;
                ViewData["Role"] = userRole;

                var projects = await GetAllProjectsAsync();

                if (userRole != "Admin" && !string.IsNullOrEmpty(userEmail))
                    projects = projects.Where(p => p.ProjectManager == userEmail).ToList();

                return View(projects);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new List<ProjectModel>());
            }
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/TimelineView
        // ----------------------------------------------------------
        [HttpGet]
        [Route("ProjectManager/TimelineView")]
        public IActionResult TimelineView()
        {
            var userName = HttpContext.Session.GetString("User");
            var userEmail = HttpContext.Session.GetString("Email");
            var userRole = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to access this page.";
                return RedirectToAction("Login", "Home");
            }

            ViewData["User"] = userName ?? "Project Manager";
            ViewData["Email"] = userEmail ?? "";
            ViewData["Role"] = userRole ?? "Project Manager";

            return View("~/Views/ProjectManager/TimelineView.cshtml");
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/GetProjectTimelines
        // API endpoint to fetch project timeline data
        // ----------------------------------------------------------
        [HttpGet]
        [Route("ProjectManager/GetProjectTimelines")]
        public async Task<IActionResult> GetProjectTimelines()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email");
                var userRole = HttpContext.Session.GetString("Role");

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var projects = await GetAllProjectsAsync();

                // Filter by project manager unless admin
                if (userRole != "Admin" && !string.IsNullOrEmpty(userEmail))
                {
                    projects = projects.Where(p => p.ProjectManager == userEmail).ToList();
                }

                var timelines = projects.Select(project => new
                {
                    id = project.Id,
                    projectName = project.Title ?? "Untitled Project",
                    projectType = project.ProjectType ?? "General",
                    startDate = project.StartDate.ToString("yyyy-MM-dd"),
                    endDate = project.EndDate?.ToString("yyyy-MM-dd") ??
                              project.StartDate.AddDays(180).ToString("yyyy-MM-dd"), // Default 6 months duration
                    status = GetProjectStatus(project),
                    progress = project.Progress,
                    budget = project.TotalBudget,
                    manager = project.ProjectManager ?? "Unassigned",
                    location = project.SiteAddress ?? project.Client ?? "",
                    priority = DeterminePriority(project),
                    createdAt = project.StartDate
                }).OrderBy(t => t.startDate).ToList();

                return Json(new { success = true, data = timelines });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetProjectTimelines] ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/GetProjects
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetProjects(string status = "all", string phase = "all",
            string budget = "all", string search = "", bool includeAll = false)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email") ?? "";
                var userRole = HttpContext.Session.GetString("Role") ?? "Project Manager";

                System.Diagnostics.Debug.WriteLine($"[GetProjects] Role: {userRole}, Email: {userEmail}, IncludeAll: {includeAll}");

                var projects = await GetAllProjectsAsync();

                // ✅ NEW: Allow Admin AND Project Manager to see all projects when includeAll=true
                // This is used by Contractor Tracker to show all contractor task assignments
                if (!includeAll && userRole != "Admin" && !string.IsNullOrEmpty(userEmail))
                {
                    projects = projects.Where(p => p.ProjectManager == userEmail).ToList();
                    System.Diagnostics.Debug.WriteLine($"[GetProjects] Filtered to {projects.Count} projects for PM: {userEmail}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetProjects] Returning all {projects.Count} projects (includeAll={includeAll}, role={userRole})");
                }

                if (status != "all")
                    projects = projects.Where(p => GetProjectStatus(p).ToLower() == status.ToLower()).ToList();

                if (phase != "all")
                    projects = projects.Where(p => p.CurrentPhase?.ToLower() == phase.ToLower()).ToList();

                if (budget != "all")
                {
                    projects = budget switch
                    {
                        "low" => projects.Where(p => p.TotalBudget < 100000).ToList(),
                        "medium" => projects.Where(p => p.TotalBudget >= 100000 && p.TotalBudget <= 500000).ToList(),
                        "high" => projects.Where(p => p.TotalBudget > 500000).ToList(),
                        _ => projects
                    };
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    projects = projects.Where(p =>
                        (p.Title?.ToLower().Contains(search) ?? false) ||
                        (p.ProjectCode?.ToLower().Contains(search) ?? false) ||
                        (p.Client?.ToLower().Contains(search) ?? false)
                    ).ToList();
                }

                return Json(new { success = true, data = projects });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetProjects] ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/GetStatistics
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email") ?? "";
                var userRole = HttpContext.Session.GetString("Role") ?? "Project Manager";

                var projects = await GetAllProjectsAsync();

                if (userRole != "Admin" && !string.IsNullOrEmpty(userEmail))
                    projects = projects.Where(p => p.ProjectManager == userEmail).ToList();

                var stats = new
                {
                    activeProjects = projects.Count(p => !p.IsCompleted),
                    onTrack = projects.Count(p => !p.IsCompleted && p.Progress >= GetExpectedProgress(p)),
                    needsAttention = projects.Count(p => !p.IsCompleted && p.Progress < GetExpectedProgress(p) && p.Progress >= GetExpectedProgress(p) - 15),
                    delayed = projects.Count(p => !p.IsCompleted && p.Progress < GetExpectedProgress(p) - 15),
                    totalBudget = projects.Sum(p => p.TotalBudget),
                    pendingApprovals = 15
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/GetContractors
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetContractors()
        {
            try
            {
                var contractors = new List<object>();
                var usersSnapshot = await _firestore.Collection("users").GetSnapshotAsync();

                foreach (var doc in usersSnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    if (data.ContainsKey("Role") && data["Role"].ToString() == "Contractor")
                    {
                        contractors.Add(new
                        {
                            uid = doc.Id,
                            displayName = data.ContainsKey("DisplayName") ? data["DisplayName"].ToString() : "Unknown",
                            email = data.ContainsKey("Email") ? data["Email"].ToString() : ""
                        });
                    }
                }

                return Json(new { success = true, data = contractors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ----------------------------------------------------------
        // GET: /ProjectManager/GetClientRequests
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetClientRequests()
        {
            try
            {
                var snapshot = await _firestore.Collection("projectRequests").GetSnapshotAsync();
                var requests = new List<ClientRequestModel>();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();

                        // Check if already accepted
                        bool isAccepted = data.ContainsKey("isAccepted") &&
                                         data["isAccepted"] is bool accepted && accepted;

                        if (isAccepted) continue; // Skip accepted requests

                        var request = new ClientRequestModel
                        {
                            Id = doc.Id,
                            ClientName = data.ContainsKey("clientName") ? data["clientName"]?.ToString() ?? "" : "",
                            CompanyName = data.ContainsKey("companyName") ? data["companyName"]?.ToString() ?? "" : "",
                            ClientEmail = data.ContainsKey("clientEmail") ? data["clientEmail"]?.ToString() ?? "" : "",
                            ClientPhone = data.ContainsKey("clientPhone") ? data["clientPhone"]?.ToString() ?? "" : "",
                            ProjectTitle = data.ContainsKey("projectTitle") ? data["projectTitle"]?.ToString() ?? "" : "",
                            ProjectType = data.ContainsKey("projectType") ? data["projectType"]?.ToString() ?? "" : "",
                            SiteAddress = data.ContainsKey("siteAddress") ? data["siteAddress"]?.ToString() ?? "" : "",
                            EstimatedBudget = data.ContainsKey("estimatedBudget") ? Convert.ToDouble(data["estimatedBudget"]) : 0,
                            PreferredStart = data.ContainsKey("preferredStart") ? data["preferredStart"]?.ToString() ?? "" : "",
                            ProjectDescription = data.ContainsKey("projectDescription") ? data["projectDescription"]?.ToString() ?? "" : "",
                            ContactMethod = data.ContainsKey("contactMethod") ? data["contactMethod"]?.ToString() ?? "" : "",
                            Urgency = data.ContainsKey("urgency") ? data["urgency"]?.ToString() ?? "" : "",
                            AdditionalNotes = data.ContainsKey("additionalNotes") ? data["additionalNotes"]?.ToString() ?? "" : "",
                            CreatedAt = data.ContainsKey("createdAt") && data["createdAt"] is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow,
                            IsAccepted = false
                        };

                        requests.Add(request);
                    }
                    catch (Exception docEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing document {doc.Id}: {docEx.Message}");
                        continue;
                    }
                }

                return Json(new { success = true, data = requests.OrderByDescending(r => r.CreatedAt).ToList() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/CreateProjectFromRequest
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateProjectFromRequest([FromBody] ProjectFromRequestModel model)
        {
            try
            {
                var pmEmail = HttpContext.Session.GetString("Email");
                if (string.IsNullOrEmpty(pmEmail))
                {
                    return Json(new { success = false, message = "No logged-in user found. Please log in again." });
                }

                var project = new ProjectModel
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectCode = model.ProjectCode,
                    Title = model.Title,
                    Client = model.Client,
                    Contractor = model.Contractor,
                    ProjectManager = pmEmail,
                    TotalBudget = model.TotalBudget,
                    StartDate = string.IsNullOrWhiteSpace(model.StartDate)
                        ? DateTime.UtcNow
                        : DateTime.Parse(model.StartDate).ToUniversalTime(),
                    CurrentPhase = model.CurrentPhase,
                    Phases = new List<string> { model.CurrentPhase },
                    Progress = 0,
                    Spent = 0,
                    DetailedPhases = new List<ProjectPhase>(),
                    IsCompleted = false,
                    SiteAddress = model.SiteAddress,
                    ProjectType = model.ProjectType,
                    Description = model.Description
                };

                await _firestore.Collection("projects").Document(project.Id).SetAsync(project);

                // Mark request as accepted
                if (!string.IsNullOrWhiteSpace(model.RequestId))
                {
                    await _firestore.Collection("projectRequests").Document(model.RequestId).UpdateAsync(new Dictionary<string, object>
                    {
                        { "isAccepted", true },
                        { "acceptedBy", pmEmail },
                        { "acceptedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                    });
                }

                return Json(new { success = true, message = "Project created successfully!", data = project });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creating project: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/AddProject
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> AddProject([FromBody] ProjectModel model)
        {
            try
            {
                var pmEmail = HttpContext.Session.GetString("Email");
                if (string.IsNullOrEmpty(pmEmail))
                {
                    return Json(new { success = false, message = "No logged-in user found. Please log in again." });
                }

                model.Id = Guid.NewGuid().ToString();
                model.StartDate = DateTime.UtcNow;
                model.IsCompleted = false;
                model.ProjectManager = pmEmail;

                await _firestore.Collection("projects").Document(model.Id).SetAsync(model);

                return Json(new { success = true, message = "Project created successfully!", data = model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creating project: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/UpdateProject
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateProject([FromBody] ProjectModel updatedModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(updatedModel.Id))
                    return Json(new { success = false, message = "Invalid project ID." });

                var docRef = _firestore.Collection("projects").Document(updatedModel.Id);
                await docRef.SetAsync(updatedModel, SetOptions.MergeAll);

                return Json(new { success = true, message = "Project updated successfully!", data = updatedModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating project: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/UpdateProgress
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateProgress(string id, int progress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Json(new { success = false, message = "Invalid project ID." });

                var docRef = _firestore.Collection("projects").Document(id);
                var updates = new Dictionary<string, object>
                {
                    { "Progress", progress }
                };

                await docRef.UpdateAsync(updates);

                return Json(new { success = true, message = "Progress updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating progress: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/CompleteProject
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CompleteProject(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Json(new { success = false, message = "Invalid project ID." });

                var docRef = _firestore.Collection("projects").Document(id);
                var updates = new Dictionary<string, object>
                {
                    { "IsCompleted", true },
                    { "Progress", 100 },
                    { "CompletedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                await docRef.UpdateAsync(updates);

                return Json(new { success = true, message = "Project marked as complete!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error completing project: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // POST: /ProjectManager/DeleteProject
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteProject(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Json(new { success = false, message = "Invalid project ID." });

                var docRef = _firestore.Collection("projects").Document(id);
                await docRef.DeleteAsync();

                return Json(new { success = true, message = "Project deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting project: {ex.Message}" });
            }
        }

        // ----------------------------------------------------------
        // Private Helper Methods
        // ----------------------------------------------------------

        private async Task<List<ProjectModel>> GetAllProjectsAsync()
        {
            var snapshot = await _firestore.Collection("projects").GetSnapshotAsync();
            var projects = new List<ProjectModel>();

            foreach (var doc in snapshot.Documents)
            {
                var project = doc.ConvertTo<ProjectModel>();
                project.Id = doc.Id;
                projects.Add(project);
            }

            return projects.OrderByDescending(p => p.StartDate).ToList();
        }

        private string GetProjectStatus(ProjectModel project)
        {
            if (project.IsCompleted) return "Completed";
            var expected = GetExpectedProgress(project);
            var diff = project.Progress - expected;
            if (diff < -15) return "Delayed";
            if (diff < 0) return "Needs Attention";
            return "In Progress";
        }

        private int GetExpectedProgress(ProjectModel project)
        {
            var totalDays = (DateTime.UtcNow - project.StartDate).TotalDays;
            if (totalDays <= 0) return 0;
            var expected = (int)((totalDays / 200.0) * 100);
            return Math.Min(100, Math.Max(0, expected));
        }

        private string DeterminePriority(ProjectModel project)
        {
            // Determine priority based on budget and progress
            if (project.TotalBudget > 1000000 || project.Progress < GetExpectedProgress(project) - 15)
                return "High";
            if (project.TotalBudget > 500000 || project.Progress < GetExpectedProgress(project))
                return "Medium";
            return "Low";
        }
    }

    // ----------------------------------------------------------
    // Supporting Models
    // ----------------------------------------------------------

    public class ProjectFromRequestModel
    {
        public string RequestId { get; set; }
        public string ProjectCode { get; set; }
        public string Title { get; set; }
        public string Client { get; set; }
        public string Contractor { get; set; }
        public double TotalBudget { get; set; }
        public string StartDate { get; set; }
        public string CurrentPhase { get; set; }
        public string SiteAddress { get; set; }
        public string ProjectType { get; set; }
        public string Description { get; set; }
    }
}