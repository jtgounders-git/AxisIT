package com.example.insy7315_thebteam.ui

import android.app.Activity
import android.content.Context
import android.util.Log
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavHostController
import com.example.insy7315_thebteam.R
import com.example.insy7315_thebteam.auth.FirebaseAuthManager
import com.example.insy7315_thebteam.ui.theme.INSY7315_TheBteamTheme
import com.google.android.gms.auth.api.signin.GoogleSignIn
import com.google.android.gms.common.api.ApiException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlin.random.Random
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background as bg
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import kotlinx.coroutines.delay
import androidx.compose.runtime.remember
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween

@Composable
fun LoginScreen(navController: NavHostController) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(false) }
    var errorMsg by remember { mutableStateOf<String?>(null) }

    val context = LocalContext.current
    val activity = remember { context as Activity }
    val authManager = remember { FirebaseAuthManager(activity) }

    // Google launcher
    val googleLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == Activity.RESULT_OK) {
            val task = GoogleSignIn.getSignedInAccountFromIntent(result.data)
            try {
                val account = task.getResult(ApiException::class.java)
                val idToken = account?.idToken
                if (!idToken.isNullOrEmpty()) {
                    CoroutineScope(Dispatchers.Main).launch {
                        isLoading = true
                        errorMsg = null
                        val res = authManager.firebaseAuthWithGoogle(idToken)
                        isLoading = false
                        if (res.isSuccess) {
                            navController.navigate("dashboard") {
                                popUpTo("login") { inclusive = true }
                            }
                        } else {
                            errorMsg = res.exceptionOrNull()?.localizedMessage ?: "Google auth failed"
                        }
                    }
                } else {
                    Log.w("Login", "Google idToken was null")
                }
            } catch (e: Exception) {
                Log.e("Login", "Google sign-in failed", e)
                errorMsg = e.localizedMessage ?: "Google sign-in error"
            }
        }
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF4A00E0), Color(0xFF8E2DE2)))),
        contentAlignment = Alignment.Center
    ) {
        ParticleBackgroundComposable()

        if (isLoading) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Box(
                    modifier = Modifier
                        .size(220.dp)
                        .clip(CircleShape)
                        .background(Color.White.copy(alpha = 0.2f)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        painter = painterResource(id = R.drawable.axis_logo),
                        contentDescription = "Axis I.T Logo",
                        modifier = Modifier.size(180.dp),
                        tint = Color.White
                    )
                }
                Spacer(modifier = Modifier.height(16.dp))
                Text("Logging in...", color = Color.White, fontSize = 20.sp)
            }
        } else {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(32.dp),
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = "Axis I.T",
                    fontSize = 36.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color.White,
                    letterSpacing = 2.sp
                )

                Spacer(modifier = Modifier.height(24.dp))

                Box(
                    modifier = Modifier
                        .size(220.dp)
                        .clip(CircleShape)
                        .background(Color.White.copy(alpha = 0.2f)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        painter = painterResource(id = R.drawable.axis_logo),
                        contentDescription = "Axis I.T Logo",
                        modifier = Modifier.size(180.dp),
                        tint = Color.White
                    )
                }

                Spacer(modifier = Modifier.height(48.dp))

                // Username
                TextField(
                    value = username,
                    onValueChange = { username = it },
                    placeholder = { Text("Username", color = Color.White.copy(alpha = 0.5f), fontSize = 14.sp) },
                    singleLine = true,
                    textStyle = LocalTextStyle.current.copy(fontSize = 16.sp, color = Color.White),
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(25.dp))
                        .background(Color.White.copy(alpha = 0.1f))
                        .heightIn(min = 60.dp),
                    colors = TextFieldDefaults.colors(
                        focusedTextColor = Color.White,
                        unfocusedTextColor = Color.White,
                        cursorColor = Color.White,
                        focusedContainerColor = Color.Transparent,
                        unfocusedContainerColor = Color.Transparent,
                        focusedIndicatorColor = Color.Transparent,
                        unfocusedIndicatorColor = Color.Transparent
                    )
                )

                Spacer(modifier = Modifier.height(12.dp))

                // Password
                TextField(
                    value = password,
                    onValueChange = { password = it },
                    placeholder = { Text("Password", color = Color.White.copy(alpha = 0.5f), fontSize = 14.sp) },
                    visualTransformation = PasswordVisualTransformation(),
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                    singleLine = true,
                    textStyle = LocalTextStyle.current.copy(fontSize = 16.sp, color = Color.White),
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(25.dp))
                        .background(Color.White.copy(alpha = 0.1f))
                        .heightIn(min = 60.dp),
                    colors = TextFieldDefaults.colors(
                        focusedTextColor = Color.White,
                        unfocusedTextColor = Color.White,
                        cursorColor = Color.White,
                        focusedContainerColor = Color.Transparent,
                        unfocusedContainerColor = Color.Transparent,
                        focusedIndicatorColor = Color.Transparent,
                        unfocusedIndicatorColor = Color.Transparent
                    )
                )

                Spacer(modifier = Modifier.height(16.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        "Forgot Password?",
                        color = Color.White,
                        textDecoration = TextDecoration.Underline,
                        modifier = Modifier.clickable { navController.navigate("forgot_password") }
                    )
                    Text(
                        "Register",
                        color = Color.White,
                        textDecoration = TextDecoration.Underline,
                        modifier = Modifier.clickable { navController.navigate("register") }
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Submit button (email sign-in)
                Button(
                    onClick = {
                        if (username.isNotEmpty() && password.isNotEmpty()) {
                            isLoading = true
                            errorMsg = null
                            CoroutineScope(Dispatchers.Main).launch {
                                val res = authManager.signInWithEmail(username, password)
                                isLoading = false
                                if (res.isSuccess) {
                                    navController.navigate("dashboard") {
                                        popUpTo("login") { inclusive = true }
                                    }
                                } else {
                                    errorMsg = res.exceptionOrNull()?.localizedMessage ?: "Sign-in failed"
                                }
                            }
                        }
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(55.dp)
                        .clip(RoundedCornerShape(25.dp)),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color.White.copy(alpha = 0.2f),
                        contentColor = Color.White
                    )
                ) {
                    Text("Submit", fontSize = 18.sp)
                }

                // Google sign-in trigger (non-intrusive text)
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    "Sign in with Google",
                    modifier = Modifier
                        .clickable {
                            // launch google intent
                            val intent = authManager.getGoogleSignInIntent()
                            googleLauncher.launch(intent)
                        }
                        .padding(top = 12.dp),
                    color = Color.White
                )

                // Optional inline error message (keeps layout intact)
                errorMsg?.let { msg ->
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(text = msg, color = Color(0xFFFFCDD2), fontSize = 13.sp)
                }
            }
        }
    }
}

// Reuse your ParticleBackground and AnimatedParticle logic but separated into a composable
@Composable
private fun ParticleBackgroundComposable() {
    val particles = remember {
        List(80) {
            AnimatedParticle(
                x = Random.nextFloat(),
                startY = Random.nextFloat(),
                radius = Random.nextFloat() * 4 + 2,
                speed = Random.nextFloat() * 1f + 0.5f
            )
        }
    }

    LaunchedEffect(particles) {
        particles.forEach { particle ->
            launch {
                particle.animateY()
            }
        }
    }

    Canvas(modifier = Modifier.fillMaxSize()) {
        val width = size.width
        val height = size.height

        particles.forEach { particle ->
            drawCircle(
                color = Color.White.copy(alpha = 0.3f),
                radius = particle.radius,
                center = Offset(particle.x * width, particle.yAnimatable.value * height)
            )
        }
    }
}

private class AnimatedParticle(
    val x: Float,
    startY: Float,
    val radius: Float,
    private val speed: Float
) {
    val yAnimatable = Animatable(startY)

    suspend fun animateY() {
        while (true) {
            yAnimatable.animateTo(
                targetValue = 1.1f,
                animationSpec = tween(
                    durationMillis = (20000 / speed).toInt(),
                    easing = LinearEasing
                )
            )
            yAnimatable.snapTo(-0.1f)
        }
    }
}