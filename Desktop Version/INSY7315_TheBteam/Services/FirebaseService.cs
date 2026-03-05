using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace INSY7315_TheBteam.Services
{
    public interface IFirebaseService
    {
        Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
        Task<UserRecord> GetUserAsync(string uid);
        Task<Dictionary<string, object>> GetUserDataAsync(string uid);
        Task SetUserDataAsync(string uid, Dictionary<string, object> data);
        Task UpdateUserRoleAsync(string uid, string role);
        Task<List<Dictionary<string, object>>> GetContractorsAsync(); // ADD THIS LINE
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirestoreDb _firestore;
        private readonly FirebaseAuth _auth;

        public FirebaseService(IConfiguration configuration)
        {
            var projectId = configuration["Firebase:ProjectId"];
            var serviceAccountPath = configuration["Firebase:ServiceAccountPath"];

            // Resolve the full path to the service account file
            string fullPath;
            if (Path.IsPathRooted(serviceAccountPath))
            {
                fullPath = serviceAccountPath;
            }
            else
            {
                // Look in multiple locations
                var possiblePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, serviceAccountPath),
                    Path.Combine(Directory.GetCurrentDirectory(), serviceAccountPath),
                    serviceAccountPath
                };

                fullPath = possiblePaths.FirstOrDefault(File.Exists);

                if (fullPath == null)
                {
                    throw new FileNotFoundException(
                        $"Firebase service account file not found. Searched in: {string.Join(", ", possiblePaths)}");
                }
            }

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(fullPath),
                    ProjectId = projectId
                });
            }

            _auth = FirebaseAuth.DefaultInstance;

            // Create Firestore with the same credentials
            var credential = GoogleCredential.FromFile(fullPath);
            var firestoreClientBuilder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            };
            _firestore = firestoreClientBuilder.Build();
        }

        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                return await _auth.VerifyIdTokenAsync(idToken);
            }
            catch (FirebaseAuthException ex)
            {
                throw new UnauthorizedAccessException($"Invalid token: {ex.Message}");
            }
        }

        public async Task<UserRecord> GetUserAsync(string uid)
        {
            return await _auth.GetUserAsync(uid);
        }

        public async Task<Dictionary<string, object>> GetUserDataAsync(string uid)
        {
            var docRef = _firestore.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                return snapshot.ToDictionary();
            }

            return null;
        }

        public async Task SetUserDataAsync(string uid, Dictionary<string, object> data)
        {
            var docRef = _firestore.Collection("users").Document(uid);
            await docRef.SetAsync(data);
        }

        public async Task UpdateUserRoleAsync(string uid, string role)
        {
            var docRef = _firestore.Collection("users").Document(uid);
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "Role", role },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            });
        }

        // ADD THIS NEW METHOD
        public async Task<List<Dictionary<string, object>>> GetContractorsAsync()
        {
            try
            {
                var contractorsQuery = _firestore.Collection("users").WhereEqualTo("Role", "Contractor");
                var snapshot = await contractorsQuery.GetSnapshotAsync();

                var contractors = new List<Dictionary<string, object>>();

                foreach (var document in snapshot.Documents)
                {
                    var contractorData = document.ToDictionary();
                    contractorData["id"] = document.Id;
                    contractors.Add(contractorData);
                }

                return contractors;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching contractors: {ex.Message}", ex);
            }
        }
    }
}