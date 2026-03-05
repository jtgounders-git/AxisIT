using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Google.Cloud.Firestore;
using INSY7315_TheBteam.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Controllers
{
    public class ClientController : Controller
    {
        private readonly FirestoreDb _firestore;

        public ClientController(IFirebaseService firebaseService)
        {
            // Extract FirestoreDb instance from IFirebaseService
            var firebaseField = firebaseService.GetType().GetField("_firestore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            _firestore = (FirestoreDb)firebaseField?.GetValue(firebaseService)
                         ?? throw new ArgumentNullException("Unable to obtain FirestoreDb from IFirebaseService.");
        }

        // ----------------------------------------------------------
        // GET: /Clients/ClientRequest
        // Routes: /Clients/ClientRequest, /ClientRequest
        // ----------------------------------------------------------
        [HttpGet]
        [Route("Clients/ClientRequest")]
        [Route("ClientRequest")]
        public IActionResult ClientRequest()
        {
            var userName = HttpContext.Session.GetString("User");
            var userEmail = HttpContext.Session.GetString("Email");
            var userRole = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to access client pages.";
                return RedirectToAction("Login", "Home");
            }

            ViewData["User"] = userName ?? "Client";
            ViewData["Email"] = userEmail ?? "";
            ViewData["Role"] = userRole ?? "Client";

            return View("~/Views/Clients/ClientRequest.cshtml");
        }

        // ----------------------------------------------------------
        // POST: /Client/SubmitRequest
        // ----------------------------------------------------------
        [HttpPost]
        [Route("Client/SubmitRequest")]
        public async Task<IActionResult> SubmitRequest(
            string ClientName,
            string CompanyName,
            string ClientEmail,
            string ClientPhone,
            string ProjectTitle,
            string ProjectType,
            string SiteAddress,
            double? EstimatedBudget,
            DateTime? PreferredStart,
            string ProjectDescription,
            string ContactMethod,
            string Urgency,
            string AdditionalNotes)
        {
            try
            {
                var sessionEmail = HttpContext.Session.GetString("Email");
                var sessionName = HttpContext.Session.GetString("User");

                if (string.IsNullOrEmpty(sessionEmail))
                {
                    TempData["ErrorMessage"] = "Please log in before submitting a request.";
                    return RedirectToAction("Login", "Home");
                }

                var docRef = _firestore.Collection("projectRequests").Document();

                var requestData = new Dictionary<string, object>
                {
                    { "clientName", ClientName },
                    { "companyName", CompanyName ?? "" },
                    { "clientEmail", ClientEmail },
                    { "clientPhone", ClientPhone },
                    { "projectTitle", ProjectTitle },
                    { "projectType", ProjectType },
                    { "siteAddress", SiteAddress },
                    { "estimatedBudget", EstimatedBudget.HasValue ? EstimatedBudget.Value : 0.0 },
                    { "preferredStart", PreferredStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "projectDescription", ProjectDescription },
                    { "contactMethod", ContactMethod ?? "Email" },
                    { "urgency", Urgency ?? "Normal" },
                    { "additionalNotes", AdditionalNotes ?? "" },
                    { "submittedBy", sessionEmail },
                    { "submittedByName", sessionName },
                    { "createdAt", Timestamp.GetCurrentTimestamp() },
                    { "isAccepted", false }
                };

                await docRef.SetAsync(requestData);

                TempData["SuccessMessage"] = $"Your project request has been submitted successfully! Reference ID: {docRef.Id}";
                return RedirectToAction("ClientRequest", "Client");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"There was a problem submitting your request: {ex.Message}";
                return RedirectToAction("ClientRequest", "Client");
            }
        }

        // ----------------------------------------------------------
        // GET: /Client/Notifications
        // Routes: /Client/Notifications, /Clients/Notifications
        // ----------------------------------------------------------
        [HttpGet]
        [Route("Client/Notifications")]
        [Route("Clients/Notifications")]
        public IActionResult Notifications()
        {
            var userName = HttpContext.Session.GetString("User");
            var userEmail = HttpContext.Session.GetString("Email");
            var userRole = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to view notifications.";
                return RedirectToAction("Login", "Home");
            }

            ViewData["User"] = userName ?? "Client";
            ViewData["Email"] = userEmail ?? "";
            ViewData["Role"] = userRole ?? "Client";

            return View("~/Views/Clients/ClientNotificationPanel.cshtml");
        }

        // ----------------------------------------------------------
        // GET: /Client/GetMyRequests
        // API endpoint to fetch all project requests for logged-in user
        // ----------------------------------------------------------
        [HttpGet]
        [Route("Client/GetMyRequests")]
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("Email");
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var snapshot = await _firestore.Collection("projectRequests").GetSnapshotAsync();
                var requests = new List<object>();

                foreach (var doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();

                    // Check both submittedBy and clientEmail for backwards compatibility
                    var submittedBy = data.ContainsKey("submittedBy") ? data["submittedBy"]?.ToString() : "";
                    var clientEmail = data.ContainsKey("clientEmail") ? data["clientEmail"]?.ToString() : "";

                    // Match if either field matches the logged-in user's email
                    if (submittedBy == userEmail || clientEmail == userEmail)
                    {
                        requests.Add(new
                        {
                            id = doc.Id,
                            clientName = data.ContainsKey("clientName") ? data["clientName"] : "",
                            companyName = data.ContainsKey("companyName") ? data["companyName"] : "",
                            clientEmail = clientEmail,
                            clientPhone = data.ContainsKey("clientPhone") ? data["clientPhone"] : "",
                            projectTitle = data.ContainsKey("projectTitle") ? data["projectTitle"] : "",
                            projectType = data.ContainsKey("projectType") ? data["projectType"] : "",
                            siteAddress = data.ContainsKey("siteAddress") ? data["siteAddress"] : "",
                            estimatedBudget = data.ContainsKey("estimatedBudget") ? data["estimatedBudget"] : 0,
                            preferredStart = data.ContainsKey("preferredStart") ? data["preferredStart"] : "",
                            projectDescription = data.ContainsKey("projectDescription") ? data["projectDescription"] : "",
                            contactMethod = data.ContainsKey("contactMethod") ? data["contactMethod"] : "Email",
                            urgency = data.ContainsKey("urgency") ? data["urgency"] : "Normal",
                            additionalNotes = data.ContainsKey("additionalNotes") ? data["additionalNotes"] : "",
                            isAccepted = data.ContainsKey("isAccepted") ? data["isAccepted"] : false,
                            createdAt = data.ContainsKey("createdAt") && data["createdAt"] is Timestamp ts
                                ? ts.ToDateTime()
                                : DateTime.UtcNow
                        });
                    }
                }

                // Order by creation date (newest first)
                var ordered = requests.OrderByDescending(r => ((dynamic)r).createdAt).ToList();

                return Json(new { success = true, data = ordered });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}