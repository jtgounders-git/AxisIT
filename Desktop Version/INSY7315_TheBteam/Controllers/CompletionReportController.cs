using Google.Cloud.Firestore;
using INSY7315_TheBteam.Models;
using INSY7315_TheBteam.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Controllers
{
    public class CompletionReportController : Controller
    {
        private readonly FirestoreDb _firestore;

        public CompletionReportController(IFirebaseService firebaseService)
        {
            var field = firebaseService.GetType().GetField("_firestore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _firestore = (FirestoreDb)field?.GetValue(firebaseService)
                         ?? throw new ArgumentNullException("Firestore not initialized.");
        }

        /// <summary>
        /// Accepts JSON data from the frontend and stores it in Firestore.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitReport([FromBody] CompletionReportModel report)
        {
            try
            {
                if (report == null)
                    return Json(new { success = false, message = "Invalid report data received." });

                // ✅ Ensure CompletionDate is in UTC
                if (report.CompletionDate.Kind != DateTimeKind.Utc)
                    report.CompletionDate = DateTime.SpecifyKind(report.CompletionDate, DateTimeKind.Utc);

                // Assign metadata
                report.Id = Guid.NewGuid().ToString();
                report.SubmittedBy = HttpContext.Session.GetString("Email") ?? report.SubmittedBy ?? "Unknown User";
                report.SubmissionDate = DateTime.UtcNow;

                // ✅ Save to Firestore
                await _firestore.Collection("CompletionReport").Document(report.Id).SetAsync(report);

                return Json(new { success = true, message = "Completion report submitted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error submitting report: {ex.Message}" });
            }
        }
    }
}
