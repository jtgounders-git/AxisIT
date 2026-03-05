using Google.Cloud.Firestore;
using INSY7315_TheBteam.Models;
using INSY7315_TheBteam.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Controllers
{
    public class ContractorController : Controller
    {
        private readonly IFirebaseService _firebaseService;
        private readonly FirestoreDb _firestore;

        public ContractorController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService ?? throw new ArgumentNullException(nameof(firebaseService));

            // Reflect internal FirestoreDb instance from service
            var firebaseField = firebaseService.GetType().GetField("_firestore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            _firestore = (FirestoreDb)firebaseField?.GetValue(firebaseService)
                         ?? throw new ArgumentNullException("Unable to obtain FirestoreDb from IFirebaseService.");
        }

        // =======================
        // MAIN VIEWS (GET)
        // =======================
        [HttpGet]
        public IActionResult TaskList()
        {
            SetViewDataFromSession();
            return View(); // /Views/Contractor/TaskList.cshtml
        }

        [HttpGet]
        public IActionResult UploadCenter()
        {
            SetViewDataFromSession();
            return View(); // /Views/Contractor/UploadCenter.cshtml
        }

        [HttpGet]
        public IActionResult CompletionReport()
        {
            SetViewDataFromSession();
            return View(); // /Views/Contractor/CompletionReport.cshtml
        }

        // Shared view for Project Managers
        [HttpGet("/ProjectManager/ContractorTracker")]
        public IActionResult ContractorTracker()
        {
            ViewData["User"] = HttpContext.Session.GetString("User") ?? "Project Manager";
            ViewData["Role"] = HttpContext.Session.GetString("Role") ?? "";
            ViewData["Email"] = HttpContext.Session.GetString("Email") ?? "";

            return View("~/Views/ProjectManager/ContractorTracker.cshtml");
        }

        // =======================
        // API ENDPOINTS
        // =======================

        /// <summary>
        /// Retrieves all contractors from Firestore.
        /// </summary>
        [HttpGet]
        [Route("Contractor/GetAllContractors")]
        public async Task<IActionResult> GetAllContractors()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email");
                System.Diagnostics.Debug.WriteLine($"[GetAllContractors] Called by: {userEmail ?? "NO EMAIL"}");

                // Optional: restrict to logged-in users
                // if (string.IsNullOrEmpty(userEmail))
                //     return Json(new { success = false, message = "Unauthorized" });

                var contractors = new List<object>();
                var usersSnapshot = await _firestore.Collection("users").GetSnapshotAsync();

                System.Diagnostics.Debug.WriteLine($"[GetAllContractors] Total users: {usersSnapshot.Documents.Count}");

                foreach (var doc in usersSnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    if (data.TryGetValue("Role", out var roleObj) && roleObj?.ToString() == "Contractor")
                    {
                        var displayName = data.ContainsKey("DisplayName") ? data["DisplayName"].ToString() : "Unknown";
                        var email = data.ContainsKey("Email") ? data["Email"].ToString() : "";

                        contractors.Add(new
                        {
                            id = doc.Id,
                            DisplayName = displayName,
                            Email = email,
                            Role = "Contractor"
                        });
                    }
                }

                return Json(new { success = true, data = contractors });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllContractors] ERROR: {ex}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all tasks assigned to the logged-in contractor.
        /// </summary>
        [HttpGet]
        [Route("Contractor/GetMyTasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email");
                var userName = HttpContext.Session.GetString("User");

                if (string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userName))
                    return Json(new { success = false, message = "User not logged in" });

                var tasksSnapshot = await _firestore.Collection("tasks").GetSnapshotAsync();
                var tasks = new List<ProjectTask>();

                foreach (var doc in tasksSnapshot.Documents)
                {
                    var task = doc.ConvertTo<ProjectTask>();
                    task.Id = doc.Id;

                    var assigned = (task.AssignedContractor ?? "").ToLower();
                    var emailLower = (userEmail ?? "").ToLower();
                    var nameLower = (userName ?? "").ToLower();

                    if (assigned == emailLower ||
                        assigned == nameLower ||
                        assigned.Contains(emailLower) ||
                        assigned.Contains(nameLower))
                    {
                        tasks.Add(task);
                    }
                }

                // Sort by due date
                tasks = tasks.OrderBy(t => t.DueDate).ToList();

                return Json(new { success = true, data = tasks });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error retrieving tasks: {ex.Message}" });
            }
        }

        /// <summary>
        /// Submit a completion report to Firestore.
        /// </summary>
        [HttpPost]
        [Route("Contractor/CompletionReport/SubmitReport")]
        public async Task<IActionResult> SubmitReport([FromBody] CompletionReportModel report)
        {
            try
            {
                if (report == null)
                    return Json(new { success = false, message = "Invalid report data received." });

                // Ensure UTC
                if (report.CompletionDate.Kind != DateTimeKind.Utc)
                    report.CompletionDate = DateTime.SpecifyKind(report.CompletionDate, DateTimeKind.Utc);

                // Metadata
                report.Id = Guid.NewGuid().ToString();
                report.SubmittedBy = HttpContext.Session.GetString("Email") ?? report.SubmittedBy ?? "Unknown User";
                report.SubmissionDate = DateTime.UtcNow;

                // Save to Firestore
                await _firestore.Collection("CompletionReport").Document(report.Id).SetAsync(report);

                return Json(new { success = true, message = "Completion report submitted successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SubmitReport] ERROR: {ex}");
                return Json(new { success = false, message = $"Error submitting report: {ex.Message}" });
            }
        }

        // =======================
        // HELPER
        // =======================
        private void SetViewDataFromSession()
        {
            ViewData["User"] = HttpContext.Session.GetString("User") ?? "Contractor";
            ViewData["Email"] = HttpContext.Session.GetString("Email") ?? "";
            ViewData["Role"] = HttpContext.Session.GetString("Role") ?? "Contractor";
        }
    }
}
