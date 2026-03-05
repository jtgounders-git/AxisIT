using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using INSY7315_TheBteam.Services;

namespace INSY7315_TheBteam.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFirebaseService _firebaseService;
        private readonly IConfiguration _configuration;

        private const string SessionKeyUser = "User";
        private const string SessionKeyRole = "Role";
        private const string SessionKeyUid = "Uid";
        private const string SessionKeyEmail = "Email";

        public HomeController(
            ILogger<HomeController> logger,
            IFirebaseService firebaseService,
            IConfiguration configuration)
        {
            _logger = logger;
            _firebaseService = firebaseService;
            _configuration = configuration;
        }

        // GET: Login page
        [HttpGet]
        public IActionResult Index()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyUser)))
                return RedirectToAction(nameof(Dashboard));

            // Pass Firebase config to view
            ViewBag.FirebaseConfig = JsonSerializer.Serialize(_configuration.GetSection("FirebaseWeb").Get<Dictionary<string, string>>());
            return View("Index");
        }

        // POST: Verify Firebase Token and Login
        [HttpPost]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                {
                    return Json(new { success = false, message = "Invalid token" });
                }

                // Verify the Firebase ID token
                var decodedToken = await _firebaseService.VerifyIdTokenAsync(request.IdToken);
                var uid = decodedToken.Uid;

                // Get user from Firebase Auth
                var userRecord = await _firebaseService.GetUserAsync(uid);

                // Get or create user data in Firestore
                var userData = await _firebaseService.GetUserDataAsync(uid);

                string role = "Client"; // Default role
                string displayName = userRecord.DisplayName ?? userRecord.Email ?? "User";

                if (userData == null)
                {
                    // First time user - create profile with default role
                    userData = new Dictionary<string, object>
                    {
                        { "Email", userRecord.Email },
                        { "DisplayName", displayName },
                        { "Role", role },
                        { "CreatedAt", DateTime.UtcNow },
                        { "Provider", userRecord.ProviderData[0]?.ProviderId ?? "password" }
                    };
                    await _firebaseService.SetUserDataAsync(uid, userData);
                }
                else
                {
                    // Existing user - get their role
                    role = userData.ContainsKey("Role") ? userData["Role"].ToString() : "Client";
                    displayName = userData.ContainsKey("DisplayName") ? userData["DisplayName"].ToString() : displayName;
                }

                // Set session
                HttpContext.Session.Clear();
                HttpContext.Session.SetString(SessionKeyUid, uid);
                HttpContext.Session.SetString(SessionKeyUser, displayName);
                HttpContext.Session.SetString(SessionKeyEmail, userRecord.Email ?? "");
                HttpContext.Session.SetString(SessionKeyRole, role);

                _logger.LogInformation($"User logged in via Firebase: {displayName} ({role})");

                return Json(new
                {
                    success = true,
                    message = $"Welcome, {displayName}!",
                    redirectUrl = Url.Action(nameof(Dashboard))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Firebase login error: {ex.Message}");
                return Json(new { success = false, message = "Authentication failed. Please try again." });
            }
        }

        // GET: Signup page (Firebase handles actual signup, this is for role selection)
        [HttpGet]
        public IActionResult Signup()
        {
            ViewBag.FirebaseConfig = JsonSerializer.Serialize(_configuration.GetSection("FirebaseWeb").Get<Dictionary<string, string>>());
            return View("Signup");
        }

        // POST: Update user role after Firebase signup
        [HttpPost]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken) || string.IsNullOrWhiteSpace(request.Role))
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var allowedRoles = new[] { "Project Manager", "Contractor", "Client" };
                if (!Array.Exists(allowedRoles, r => r == request.Role))
                {
                    return Json(new { success = false, message = "Invalid role selection" });
                }

                // Verify token
                var decodedToken = await _firebaseService.VerifyIdTokenAsync(request.IdToken);
                var uid = decodedToken.Uid;
                var userRecord = await _firebaseService.GetUserAsync(uid);

                // Update role in Firestore
                var userData = await _firebaseService.GetUserDataAsync(uid);

                if (userData == null)
                {
                    // Create new user profile
                    userData = new Dictionary<string, object>
                    {
                        { "Email", userRecord.Email },
                        { "DisplayName", userRecord.DisplayName ?? userRecord.Email ?? "User" },
                        { "Role", request.Role },
                        { "CreatedAt", DateTime.UtcNow },
                        { "Provider", userRecord.ProviderData[0]?.ProviderId ?? "password" }
                    };
                    await _firebaseService.SetUserDataAsync(uid, userData);
                }
                else
                {
                    // Update existing user role
                    await _firebaseService.UpdateUserRoleAsync(uid, request.Role);
                }

                // Set session
                HttpContext.Session.Clear();
                HttpContext.Session.SetString(SessionKeyUid, uid);
                HttpContext.Session.SetString(SessionKeyUser, userRecord.DisplayName ?? userRecord.Email ?? "User");
                HttpContext.Session.SetString(SessionKeyEmail, userRecord.Email ?? "");
                HttpContext.Session.SetString(SessionKeyRole, request.Role);

                return Json(new
                {
                    success = true,
                    message = $"Account created with role: {request.Role}",
                    redirectUrl = Url.Action(nameof(Dashboard))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Role update error: {ex.Message}");
                return Json(new { success = false, message = "Failed to update role. Please try again." });
            }
        }

        // Dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            var user = HttpContext.Session.GetString(SessionKeyUser);
            var role = HttpContext.Session.GetString(SessionKeyRole);
            var email = HttpContext.Session.GetString(SessionKeyEmail);

            if (string.IsNullOrEmpty(user))
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["User"] = user;
            ViewData["Role"] = role;
            ViewData["Email"] = email;
            return View("Dashboard");
        }

        // Contractor actions
        [HttpGet]
        public IActionResult TaskList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyUser)))
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["User"] = HttpContext.Session.GetString(SessionKeyUser);
            ViewData["Role"] = HttpContext.Session.GetString(SessionKeyRole);
            return View("TaskList");
        }

        [HttpGet]
        public IActionResult UploadCenter()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyUser)))
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["User"] = HttpContext.Session.GetString(SessionKeyUser);
            ViewData["Role"] = HttpContext.Session.GetString(SessionKeyRole);
            return View("UploadCenter");
        }

        [HttpGet]
        public IActionResult CompletionReport()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeyUser)))
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["User"] = HttpContext.Session.GetString(SessionKeyUser);
            ViewData["Role"] = HttpContext.Session.GetString(SessionKeyRole);
            return View("CompletionReport");
        }

        // Logout
        [HttpGet]
        public IActionResult Logout()
        {
            var user = HttpContext.Session.GetString(SessionKeyUser);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out.";
            if (!string.IsNullOrEmpty(user))
                _logger.LogInformation($"User logged out: {user}");
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Request Models
    public class FirebaseLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class UpdateRoleRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}