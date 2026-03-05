# 🏗️ Axis IT – Integrated Property, Construction, and Maintenance Management System

**🌐 Live Demo:** [Axis IT Web App](https://axisitconstruction.onrender.com/)  

---

# 📝 Project Overview
Axis IT is an **AI-assisted, integrated platform** for property, construction, and maintenance management. It centralizes workflows for administrators, project managers, contractors, and clients into a single intelligent system. The platform provides **real-time monitoring**, **automated task assignment**, **secure document management**, and **AI-driven insights**, reducing inefficiencies in construction and property management projects.

**🎯 Key Objectives:**
- Streamline operations and communication across projects
- Provide transparency for clients and stakeholders
- Deliver AI-assisted estimations and reporting
- Centralize documentation and task tracking

---

# ✨ Features
- 👤 Digital Onboarding for all user roles
- 📊 Role-Based Dashboards (Admin, Manager, Contractor, Client)
- 🛠️ Maintenance Issue Logging with photos, videos, and attachments
- ✅ Automated Task Assignment & Tracking
- 💰 Budget Management with AI-driven cost estimation
- 🧾 Quotation & Invoice Management
- 🔔 Real-Time Notifications via app and email
- 📁 Document Library with secure upload/download
- 📈 Interactive Reports on project progress, finances, and contractor performance
- 🤖 AI Features:
  - Blueprint Cost Estimation (Google Gemini API)
  - AxiBot Intelligent Assistant for role-specific guidance

---

# 👥 User Roles & Personas

| Role | Responsibilities & Benefits |
|------|-----------------------------|
| Administrator | Oversee projects, budgets, and documentation. Centralized dashboard, automated notifications, AI-generated summaries. |
| Project Manager | Manage multiple sites, assign tasks, prevent budget overruns. AI-assisted task assignment and live progress tracking. |
| Contractor | Complete assigned tasks, upload proof of work. Task tracking, file uploads, notifications, performance metrics. |
| Client/Tenant | Report maintenance issues, track requests. Mobile-friendly interface, real-time updates, automated progress notifications. |
| Financial Officer | Monitor budgets and invoices. Automated approvals, consolidated financial summaries, alerts on exceeded budgets. |
| System Administrator | Maintain backend, user permissions, uptime. Firebase integration, role-based access, diagnostic tools. |

---

# 🖥️ Web Application Components
- 🔐 Authentication: Login, Forgot Password, Profile management
- 📊 Dashboards: Admin, Manager, Contractor, Tenant
- 🏗️ Project Management: Project list, details, creation, task management
- 🛠️ Maintenance Management: Request list, submission, tracking
- 👤 User Management: User list, details, role management
- 💰 Financial Management: Budget tracking, quotations, invoices, financial reports
- 📁 Documents & Files: Library, viewer, upload/download functionality
- 💬 Communications: Notifications, messaging, alerts
- 🤖 AI Components: Cost estimation, contractor recommendation, risk assessment

---

# 📱 Mobile Application Screens
- 🔐 Authentication: Splash, Login, Forgot Password
- 🧭 Navigation: Bottom tab and drawer navigators
- 📊 Dashboards: Tenant, Contractor, Manager
- 🛠️ Maintenance: Submit request, history, details, tracking
- ✅ Task Management: Task list, details, update, time tracking
- ⚙️ Profile & Settings: Profile management, notification settings
- 📸 Camera & Media: Camera, gallery, video recording
- 🔔 Notifications: List, details

---

# 💻 Technology Stack
- Frontend: React (Web), React Native (Mobile), Figma for UI/UX
- Backend: ASP.NET Core, C#, Entity Framework
- Database: Firebase Realtime Database / Firestore
- Authentication: Firebase Authentication, Single Sign-On (SSO)
- AI Integration: Google Gemini API
- Version Control: GitHub
- Development Tools: Visual Studio, Android Studio

---

# 🔒 Security & Compliance
- 🔑 Secure authentication with hashed passwords and SSO
- 🛡️ Role-Based Access Control (RBAC) to limit permissions
- 🛡️ CSRF protection, HTTPS/TLS for secure communication
- 🔧 Environment variables for sensitive configurations
- 📝 Audit logs and daily data backups
- ✅ POPIA & GDPR compliant

---

# ⚙️ Installation & Setup
1. Clone the repository: git clone https://github.com/VCSTDN2024/insy7315-poe-the_b_team.git
2. Install dependencies for both web and mobile using npm install
3. Configure Firebase project and set up environment variables
4. Run the web app using npm start
5. Run the mobile app using npx react-native run-android for Android or npx react-native run-ios for iOS
6. Access the live demo via the provided link

---

# 🚀 Usage
- Login according to role (Admin, Project Manager, Contractor, Client)
- Navigate dashboards to manage projects, tasks, and maintenance requests
- Submit and track maintenance issues via web or mobile
- Use AI features for cost estimation and intelligent guidance
- Upload and manage documents securely
