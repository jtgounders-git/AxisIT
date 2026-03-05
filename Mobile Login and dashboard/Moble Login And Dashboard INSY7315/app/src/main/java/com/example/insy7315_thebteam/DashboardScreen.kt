package com.example.insy7315_thebteam

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.firestore.FirebaseFirestore

@Composable
fun DashboardScreen(onLogout: () -> Unit) {
    val auth = FirebaseAuth.getInstance()
    val firestore = FirebaseFirestore.getInstance()

    var username by remember { mutableStateOf("User") }
    var userEmail by remember { mutableStateOf("") }
    var userRole by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(true) }

    // Fetch user data from Firestore
    LaunchedEffect(Unit) {
        val currentUser = auth.currentUser
        if (currentUser != null) {
            userEmail = currentUser.email ?: ""

            // Fetch role from Firestore FIRST, then set username based on role
            firestore.collection("users").document(currentUser.uid).get()
                .addOnSuccessListener { document ->
                    // Try both lowercase "role" and capitalized "Role" to account for field name differences
                    userRole = document.getString("role")
                        ?: document.getString("Role")
                                ?: "User"

                    // Use a normalized role for logic (case-insensitive)
                    val normalizedRole = userRole.lowercase().trim()

                    // Set username based on role or display name
                    username = when {
                        normalizedRole == "admin" -> "Admin"
                        currentUser.displayName?.isNotEmpty() == true -> currentUser.displayName!!
                        userEmail.isNotEmpty() -> userEmail.split("@").first()
                        else -> "User"
                    }
                    isLoading = false
                }
                .addOnFailureListener {
                    userRole = "User"
                    username = "User"
                    isLoading = false
                }
        } else {
            isLoading = false
        }
    }

    val gradientBackground = Brush.linearGradient(
        colors = listOf(Color(0xFF1e3c72), Color(0xFF2a5298), Color(0xFF6a4c93))
    )

    if (isLoading) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(gradientBackground),
            contentAlignment = Alignment.Center
        ) {
            CircularProgressIndicator(color = Color.White)
        }
    } else {
        // Normalize role once for rendering decisions (case-insensitive)
        val roleForRender = userRole.lowercase().trim()

        Column(
            modifier = Modifier
                .fillMaxSize()
                .background(gradientBackground)
        ) {
            // Top section with user info
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 48.dp, start = 16.dp, end = 16.dp, bottom = 16.dp)
            ) {
                // --- Top Navbar ---
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 8.dp),
                    horizontalArrangement = Arrangement.Start,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Box(
                        modifier = Modifier
                            .size(100.dp)
                            .clip(CircleShape)
                            .background(Color.White.copy(alpha = 0.2f)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            painter = painterResource(id = R.drawable.axis_logo),
                            contentDescription = "Axis I.T Logo",
                            tint = Color.White,
                            modifier = Modifier.size(80.dp)
                        )
                    }
                    Spacer(modifier = Modifier.width(16.dp))
                    Column {
                        Text("Axis I.T", fontSize = 22.sp, fontWeight = FontWeight.Bold, color = Color.White)
                        Text(
                            "Construction Management",
                            fontSize = 14.sp,
                            color = Color.White.copy(alpha = 0.7f)
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text("Welcome, $username", fontSize = 16.sp, color = Color.White)
                        if (userEmail.isNotEmpty()) {
                            Text(userEmail, fontSize = 12.sp, color = Color.White.copy(alpha = 0.7f))
                        }
                        Text(
                            userRole,
                            fontSize = 12.sp,
                            color = Color(0xFFb4a0ff),
                            modifier = Modifier
                                .clip(RoundedCornerShape(12.dp))
                                .background(Color(0xFFb4a0ff).copy(alpha = 0.3f))
                                .padding(horizontal = 8.dp, vertical = 4.dp)
                        )
                    }
                    Spacer(modifier = Modifier.weight(1f))
                    IconButton(
                        onClick = onLogout,
                        modifier = Modifier
                            .size(50.dp)
                            .clip(RoundedCornerShape(25.dp))
                            .background(Color(0xFF667eea))
                    ) {
                        Icon(
                            painter = painterResource(id = R.drawable.ic_logout),
                            contentDescription = "Logout",
                            tint = Color.White,
                            modifier = Modifier.size(60.dp)
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // --- Welcome Banner ---
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = Color(0xFF428679).copy(alpha = 0.4f)
                    ),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                ) {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            "🎉 Welcome back, $username!",
                            fontSize = 18.sp,
                            fontWeight = FontWeight.Medium,
                            color = Color.White
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))

                // --- Fixed Dashboard Header ---
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        "Construction Management Dashboard",
                        fontSize = 28.sp,
                        fontWeight = FontWeight.Light,
                        color = Color.White
                    )
                    Text(
                        "Dr Majoko Projects (Pty) Ltd - Integrated Property Construction and Maintenance Platform",
                        fontSize = 14.sp,
                        color = Color.White.copy(alpha = 0.8f),
                        modifier = Modifier.padding(top = 4.dp),
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))
            }

            // Scrollable content area based on role
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f)
            ) {
                when (roleForRender) {
                    "admin" -> AdminDashboardContent()
                    "project manager", "project_manager", "pm" -> ProjectManagerDashboardContent()
                    "contractor" -> ContractorDashboardContent()
                    "client" -> ClientDashboardContent()
                    else -> DefaultDashboardContent()
                }
            }
        }
    }
}

@Composable
fun AdminDashboardContent() {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(horizontal = 16.dp)
    ) {
        // --- Fixed Quick Stats Section ---
        val stats = listOf(
            "Active Projects" to "23",
            "Contractors" to "156",
            "Pending Tasks" to "89",
            "Total Budgets" to "R2.4M",
            "Active Tenants" to "342",
            "Maintenance Issues" to "12"
        )

        LazyRow(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier
                .fillMaxWidth()
                .height(110.dp)
        ) {
            items(stats) { (label, value) ->
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = Color.White.copy(alpha = 0.1f)
                    ),
                    shape = RoundedCornerShape(15.dp),
                    modifier = Modifier
                        .width(140.dp)
                        .height(100.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(12.dp),
                        verticalArrangement = Arrangement.Center,
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(value, fontSize = 20.sp, fontWeight = FontWeight.Bold, color = Color(0xFF667eea))
                        Text(label, fontSize = 12.sp, color = Color.White.copy(alpha = 0.8f))
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        // --- ADMIN SPECIFIC TABS ---
        RoleSectionHeader(title = "👑 Admin Functions")
        AdminTabs()

        Spacer(modifier = Modifier.height(24.dp))

        // --- All Role Sections for Admin (Admin sees all tiles) ---
        Column(verticalArrangement = Arrangement.spacedBy(24.dp)) {
            // Project Manager Section
            RoleSectionHeader(title = "📊 Project Manager Functions")
            ProjectManagerTabs()

            Spacer(modifier = Modifier.height(16.dp))

            // Contractor Section
            RoleSectionHeader(title = "🔧 Contractor Functions")
            ContractorTabs()

            Spacer(modifier = Modifier.height(16.dp))

            // Client Section
            RoleSectionHeader(title = "🏠 Client Functions")
            ClientTabs()
        }

        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
fun AdminTabs() {
    val adminTabs = listOf(
        "👥 User Management",
        "⚙️ System Settings",
        "📈 Analytics",
        "🔐 Role Management",
        "📊 Reports",
        "🚨 Audit Log"
    )

    // Grid layout for Admin tabs
    Column(
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // First row
        Row(
            horizontalArrangement = Arrangement.spacedBy(16.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            adminTabs.take(3).forEach { tab ->
                Card(
                    onClick = { /* Handle admin tab click */ },
                    colors = CardDefaults.cardColors(
                        containerColor = Color(0xFF667eea).copy(alpha = 0.3f)
                    ),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .weight(1f)
                        .height(100.dp)
                ) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            tab,
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Medium,
                            color = Color.White,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }

        // Second row
        Row(
            horizontalArrangement = Arrangement.spacedBy(16.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            adminTabs.drop(3).forEach { tab ->
                Card(
                    onClick = { /* Handle admin tab click */ },
                    colors = CardDefaults.cardColors(
                        containerColor = Color(0xFF667eea).copy(alpha = 0.3f)
                    ),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .weight(1f)
                        .height(100.dp)
                ) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            tab,
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Medium,
                            color = Color.White,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun RoleSectionHeader(title: String) {
    Card(
        colors = CardDefaults.cardColors(
            containerColor = Color(0xFFc8b4ff).copy(alpha = 0.2f)
        ),
        shape = RoundedCornerShape(8.dp),
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 8.dp, vertical = 4.dp)
    ) {
        Text(
            title,
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = Color(0xFFc8b4ff),
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 12.dp)
        )
    }
}

@Composable
fun ProjectManagerDashboardContent() {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(horizontal = 16.dp)
    ) {
        // --- Fixed Quick Stats Section ---
        val stats = listOf(
            "Active Projects" to "23",
            "Contractors" to "156",
            "Pending Tasks" to "89",
            "Total Budgets" to "R2.4M"
        )

        LazyRow(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier
                .fillMaxWidth()
                .height(110.dp)
        ) {
            items(stats) { (label, value) ->
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = Color.White.copy(alpha = 0.1f)
                    ),
                    shape = RoundedCornerShape(15.dp),
                    modifier = Modifier
                        .width(140.dp)
                        .height(100.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(12.dp),
                        verticalArrangement = Arrangement.Center,
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(value, fontSize = 20.sp, fontWeight = FontWeight.Bold, color = Color(0xFF667eea))
                        Text(label, fontSize = 12.sp, color = Color.White.copy(alpha = 0.8f))
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        // --- Project Manager Tabs Only ---
        ProjectManagerTabs()

        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
fun ProjectManagerTabs() {
    val pmTabs = listOf(
        "📊 PM Dashboard",
        "📅 Timeline View",
        "👷 Contractor Tracker",
        "📁 File Review Screen"
    )

    // Grid layout for PM tabs
    Column(
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Two rows of cards
        Row(
            horizontalArrangement = Arrangement.spacedBy(16.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            pmTabs.take(2).forEach { tab ->
                Card(
                    onClick = { /* Handle tab click */ },
                    colors = CardDefaults.cardColors(
                        containerColor = Color.White.copy(alpha = 0.08f)
                    ),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .weight(1f)
                        .height(120.dp)
                ) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            tab,
                            fontSize = 18.sp,
                            fontWeight = FontWeight.Medium,
                            color = Color.White,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }
        Row(
            horizontalArrangement = Arrangement.spacedBy(16.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            pmTabs.drop(2).forEach { tab ->
                Card(
                    onClick = { /* Handle tab click */ },
                    colors = CardDefaults.cardColors(
                        containerColor = Color.White.copy(alpha = 0.08f)
                    ),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .weight(1f)
                        .height(120.dp)
                ) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            tab,
                            fontSize = 18.sp,
                            fontWeight = FontWeight.Medium,
                            color = Color.White,
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun ContractorDashboardContent() {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(horizontal = 16.dp)
    ) {
        Text(
            "Contractor Dashboard",
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            color = Color.White,
            modifier = Modifier.padding(bottom = 24.dp)
        )

        ContractorTabs()

        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
fun ContractorTabs() {
    val contractorTabs = listOf(
        "✅ Task List",
        "📤 Upload Center",
        "📋 Completion Report Form"
    )

    // Single column for contractor tabs
    Column(
        verticalArrangement = Arrangement.spacedBy(16.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        contractorTabs.forEach { tab ->
            Card(
                onClick = { /* Handle tab click */ },
                colors = CardDefaults.cardColors(
                    containerColor = Color.White.copy(alpha = 0.08f)
                ),
                shape = RoundedCornerShape(12.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .height(120.dp)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        tab,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Medium,
                        color = Color.White,
                        textAlign = TextAlign.Center
                    )
                }
            }
        }
    }
}

@Composable
fun ClientDashboardContent() {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(horizontal = 16.dp)
    ) {
        Text(
            "Client Dashboard",
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            color = Color.White,
            modifier = Modifier.padding(bottom = 24.dp)
        )

        ClientTabs()

        Spacer(modifier = Modifier.height(32.dp))
    }
}

@Composable
fun ClientTabs() {
    val clientTabs = listOf(
        "🏠 Client Dashboard",
        "📝 Request Form",
        "🔔 Notification Panel"
    )

    // Single column for client tabs
    Column(
        verticalArrangement = Arrangement.spacedBy(16.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        clientTabs.forEach { tab ->
            Card(
                onClick = { /* Handle tab click */ },
                colors = CardDefaults.cardColors(
                    containerColor = Color.White.copy(alpha = 0.08f)
                ),
                shape = RoundedCornerShape(12.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .height(120.dp)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        tab,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Medium,
                        color = Color.White,
                        textAlign = TextAlign.Center
                    )
                }
            }
        }
    }
}

@Composable
fun DefaultDashboardContent() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            "Welcome to Axis I.T",
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            color = Color.White,
            modifier = Modifier.padding(bottom = 16.dp)
        )
        Text(
            "Your role is being configured. Please contact administrator.",
            fontSize = 16.sp,
            color = Color.White.copy(alpha = 0.8f),
            textAlign = TextAlign.Center
        )
    }
}
