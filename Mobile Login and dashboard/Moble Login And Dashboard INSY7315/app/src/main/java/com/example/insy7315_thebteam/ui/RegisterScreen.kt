package com.example.insy7315_thebteam

import android.annotation.SuppressLint
import android.app.Activity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.google.android.gms.auth.api.signin.GoogleSignIn
import com.google.android.gms.auth.api.signin.GoogleSignInOptions
import com.google.android.gms.common.api.ApiException
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.GoogleAuthProvider
import com.google.firebase.firestore.FirebaseFirestore

@SuppressLint("ContextCastToActivity")
@Composable
fun RegisterScreen(
    onRegisterSuccess: () -> Unit,
    onBackToLogin: () -> Unit
) {
    val auth = FirebaseAuth.getInstance()
    val firestore = FirebaseFirestore.getInstance()

    // UI State
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var confirmPassword by remember { mutableStateOf("") }
    var role by remember { mutableStateOf("") }
    var currentStep by remember { mutableStateOf(1) }
    var loading by remember { mutableStateOf(false) }
    var message by remember { mutableStateOf<String?>(null) }

    val gradientBackground = Brush.linearGradient(
        listOf(Color(0xFF6a4c93), Color(0xFF2a5298))
    )

    val activity = LocalContext.current as Activity

    // Google Sign-in launcher
    val launcher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.StartActivityForResult()
    ) { result ->
        val task = GoogleSignIn.getSignedInAccountFromIntent(result.data)
        try {
            val account = task.getResult(ApiException::class.java)
            val credential = GoogleAuthProvider.getCredential(account.idToken, null)
            loading = true
            auth.signInWithCredential(credential)
                .addOnSuccessListener {
                    loading = false
                    currentStep = 2
                }
                .addOnFailureListener {
                    loading = false
                    message = "Google Sign-In failed: ${it.localizedMessage}"
                }
        } catch (e: ApiException) {
            message = "Google Sign-In canceled."
        }
    }

    // Register UI
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(gradientBackground)
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            "✨ Create Account",
            color = Color.White,
            fontSize = 28.sp,
            fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.Center
        )
        Spacer(Modifier.height(16.dp))

        message?.let {
            Text(
                text = it,
                color = if (it.contains("failed", true)) Color.Red else Color.Green,
                modifier = Modifier
                    .fillMaxWidth()
                    .background(Color.White.copy(alpha = 0.15f), RoundedCornerShape(8.dp))
                    .padding(8.dp),
                textAlign = TextAlign.Center
            )
            Spacer(Modifier.height(8.dp))
        }

        if (currentStep == 1) {
            // Step 1: Email or Google Sign-Up
            OutlinedTextField(
                value = email,
                onValueChange = { email = it },
                label = { Text("Email", color = Color.White) },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    focusedBorderColor = Color.White,
                    unfocusedBorderColor = Color.White.copy(0.5f),
                    focusedLabelColor = Color.White,
                    unfocusedLabelColor = Color.White.copy(0.7f),
                    cursorColor = Color.White
                )
            )
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                value = password,
                onValueChange = { password = it },
                label = { Text("Password (min 8 chars)", color = Color.White) },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    focusedBorderColor = Color.White,
                    unfocusedBorderColor = Color.White.copy(0.5f),
                    focusedLabelColor = Color.White,
                    unfocusedLabelColor = Color.White.copy(0.7f),
                    cursorColor = Color.White
                )
            )
            Spacer(Modifier.height(8.dp))
            OutlinedTextField(
                value = confirmPassword,
                onValueChange = { confirmPassword = it },
                label = { Text("Confirm Password", color = Color.White) },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    focusedBorderColor = Color.White,
                    unfocusedBorderColor = Color.White.copy(0.5f),
                    focusedLabelColor = Color.White,
                    unfocusedLabelColor = Color.White.copy(0.7f),
                    cursorColor = Color.White
                )
            )
            Spacer(Modifier.height(16.dp))

            Button(
                onClick = {
                    if (password != confirmPassword) {
                        message = "Passwords do not match."
                        return@Button
                    }
                    if (password.length < 8) {
                        message = "Password must be at least 8 characters."
                        return@Button
                    }

                    loading = true
                    auth.createUserWithEmailAndPassword(email, password)
                        .addOnSuccessListener {
                            loading = false
                            currentStep = 2
                        }
                        .addOnFailureListener {
                            loading = false
                            message = "Sign-up failed: ${it.localizedMessage}"
                        }
                },
                enabled = !loading,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF764ba2))
            ) {
                if (loading)
                    CircularProgressIndicator(
                        color = Color.White,
                        modifier = Modifier.size(22.dp)
                    )
                else
                    Text("Sign Up with Email", color = Color.White)
            }

            Spacer(Modifier.height(8.dp))

            Button(
                onClick = {
                    val gso = GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                        .requestIdToken(activity.getString(R.string.default_web_client_id))
                        .requestEmail()
                        .build()
                    val client = GoogleSignIn.getClient(activity, gso)
                    launcher.launch(client.signInIntent)
                },
                enabled = !loading,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White)
            ) {
                Text("Sign Up with Google", color = Color.Black)
            }

            TextButton(onClick = onBackToLogin) {
                Text("Already have an account? Login", color = Color.White)
            }
        } else {
            // Step 2: Role Selection
            Text("Select your role", color = Color.White, fontSize = 18.sp)
            Spacer(Modifier.height(16.dp))

            val roles = listOf("Project Manager", "Contractor", "Client")
            roles.forEach {
                Button(
                    onClick = {
                        loading = true
                        role = it
                        val uid = auth.currentUser?.uid ?: return@Button
                        val userDoc = firestore.collection("users").document(uid)
                        userDoc.set(
                            mapOf(
                                "email" to auth.currentUser?.email,
                                "role" to role,
                                "createdAt" to System.currentTimeMillis()
                            )
                        ).addOnSuccessListener {
                            loading = false
                            message = "Registration complete!"
                            onRegisterSuccess()
                        }.addOnFailureListener { e ->
                            loading = false
                            message = "Error saving role: ${e.localizedMessage}"
                        }
                    },
                    enabled = !loading,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 6.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF667eea))
                ) {
                    Text(it, color = Color.White)
                }
            }
        }
    }
}