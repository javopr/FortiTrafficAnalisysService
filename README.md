# FortiGate Traffic Analysis Service

**A multi-tenant web application for analyzing FortiGate firewall traffic logs**

**Author:** javier.morales@intwo.cloud  
**Organization:** INTEGRATION TECHNOLOGIES CORP.

---

## ?? Quick Start

### 1. Configure Azure AD Authentication
Follow the detailed instructions in [AZURE_AD_SETUP_GUIDE.md](AZURE_AD_SETUP_GUIDE.md)

### 2. Add Your Admin User to Database
```sql
INSERT INTO AppUsers (AppAccessID, UserUPN, AppGroupID, AppUserName, AppUserEmail)
VALUES (
    NEWID(),
    'your.email@yourdomain.com',
    '22222222-2222-2222-2222-222222222222',
    'Your Name',
    'your.email@yourdomain.com'
)
```

### 3. Run the Application
```powershell
cd src/FortiTrafficAnalysis.WebGui
dotnet run
```

### 4. Navigate to Application
Open your browser and go to: **https://localhost:7225**

---

## 🚀 Features

- 🔐 **Azure AD Authentication** - Enterprise-grade security
- 🏢 **Multi-Tenant Architecture** - Manage multiple customers
- 👥 **Role-Based Access Control** - Users and Admins
- 📊 **Traffic Log Analysis** - Upload and analyze FortiGate logs with advanced filtering
- 🔄 **Server-Side Pagination** - Handle thousands of logs efficiently (100 logs per page)
- ✅ **Multi-Page Selection** - Select logs across different pages for analysis
- 🤖 **AI-Powered Recommendations** - Azure OpenAI GPT-4.1 integration for intelligent policy suggestions
- 💬 **Interactive AI Assistant** - Ask questions about selected logs in natural language (Ready to implement)
- 📝 **Automatic Ticket Generation** - 10-character alphanumeric ticket numbers
- 🎯 **Policy Recommendations** - Generate FortiGate CLI commands
- 📈 **Real-Time Statistics** - Dashboard with traffic analytics
- 🔍 **Advanced Filtering** - Filter by IP, port, protocol, action, date range, and more

---

## ??? Architecture

### Technology Stack
- **Backend:** ASP.NET Core 8.0 (MVC + Web API)
- **Database:** Azure SQL Database with retry logic
- **Authentication:** Microsoft Identity Platform (Azure AD)
- **AI:** Azure OpenAI Service (GPT-4.1)
- **ORM:** Entity Framework Core 8.0
- **Frontend:** Bootstrap 5 + Bootstrap Icons + jQuery

### Projects
1. **Domain** - Entity models
2. **Data** - EF Core, migrations, repositories
3. **Services** - Business logic
4. **WebGui** - MVC web application
5. **WebApi** - REST API endpoints

---

## 📊 Database Schema

- **AppGroups** - Application roles (Users, Admins)
- **AppUsers** - Application user management
- **Customers** - Customer/tenant management
- **FTAServices** - Service contract management
- **FortiGates** - FortiGate device inventory
- **TrafficAnalyses** - Traffic analysis tickets
- **TrafficLogs** - Traffic log storage and analysis (with pagination support)
- **TrafficAnalysisRecommendations** - Policy recommendations
- **AIConversations** - AI chat history (Ready to implement)

---

## ?? Security

- Azure AD authentication with OpenID Connect
- Role-based authorization policies
- Encrypted connections to Azure SQL
- Multi-tenant data isolation

---

## ?? Documentation

- [Azure AD Setup Guide](AZURE_AD_SETUP_GUIDE.md) - Detailed Azure AD configuration
- [Progress Report](PROGRESS.md) - Development progress and next steps

---

## ✅ Development Status

### 🎉 Completed (Version 1.0)
- [x] Solution structure and projects
- [x] Entity models and database schema
- [x] EF Core migrations
- [x] Azure SQL Database setup with retry logic
- [x] Azure AD authentication
- [x] Authorization services
- [x] Modern UI layout with Bootstrap 5
- [x] Admin module (Users, Customers, Services, Devices)
- [x] Traffic log upload and parsing
- [x] Log filtering and querying with server-side pagination
- [x] Advanced filtering (IP, port, protocol, action, date range)
- [x] Multi-page log selection
- [x] Policy recommendation engine
- [x] Real-time dashboard statistics
- [x] Azure OpenAI integration (GPT-4.1)
- [x] AI test successful

### 🚀 Ready to Implement (Version 2.0)
- [ ] AI chat interface in ticket details page
- [ ] Interactive Q&A with selected logs
- [ ] AI conversation history storage
- [ ] Enhanced recommendations with AI-generated CLI
- [ ] Export recommendations to file

### 🔮 Future Enhancements (Version 3.0)
- [ ] Web API implementation
- [ ] FortiGate REST API integration
- [ ] Automated log collection
- [ ] Email notifications
- [ ] Advanced analytics dashboard
- [ ] Report generation (PDF/Excel)

---

## 🗺️ Roadmap

**Version 1.0 - Core Functionality** ✅ **(COMPLETED)**
- ✅ Admin module for managing users, customers, services, and devices
- ✅ Traffic log upload and parsing
- ✅ Server-side pagination and filtering
- ✅ Policy recommendation engine
- ✅ Azure OpenAI integration setup

**Version 2.0 - AI Integration** 🚀 **(NEXT - Estimated: 2-3 weeks)**
- 🔄 AI chat interface in ticket details
- 🔄 Interactive Q&A with natural language
- 🔄 AI conversation history
- 🔄 Enhanced CLI command generation
- 🔄 Context-aware recommendations

**Version 3.0 - API & Automation** 🔮 **(Q2 2025)**
- Web API implementation
- FortiGate REST API integration
- Automated log collection
- Scheduled reports
- Email notifications

**Version 4.0 - Analytics & Reporting** 🔮 **(Q3 2025)**
- Advanced analytics dashboard
- Custom report builder
- Export to PDF/Excel
- Trend analysis
- Compliance reports

---

## ?? Contributing

This is a proprietary application for INTEGRATION TECHNOLOGIES CORP.

---

## ?? Contact

For questions or support:
- **Email:** javier.morales@intwo.cloud
- **Organization:** INTEGRATION TECHNOLOGIES CORP.

---

## ?? License

Proprietary - © 2025 INTEGRATION TECHNOLOGIES CORP.
All rights reserved.

## 🚀 Current Status

**Version:** 1.0 (Production Ready)  
**Last Updated:** February 13, 2026

### ✅ Recent Changes
- ✅ Implemented auto-generated 10-character alphanumeric ticket numbers
- ✅ Added interactive log table with real-time filtering
- ✅ Completed log file upload and parsing for FortiGate logs
- ✅ Server-side pagination for large datasets (3,800+ logs)
- ✅ Multi-page log selection with persistent state
- ✅ Real-time dashboard statistics
- ✅ Azure OpenAI integration (GPT-4.1) tested and working
- ✅ Database connection retry logic implemented
- ✅ Advanced filtering (IP, port, protocol, action, date range)

### 🎯 Next Steps (Version 2.0)
1. **Implement AI Chat Interface** (Priority: High)
   - Create service layer (IAIRecommendationService)
   - Add database migration for AIConversations
   - Build chat UI in ticket details page
   - Implement conversation history

2. **Enhanced Recommendations**
   - AI-generated FortiGate CLI commands
   - Security risk assessment
   - Alternative policy suggestions

3. **User Experience**
   - Export recommendations to file
   - Copy CLI commands to clipboard
   - Keyboard shortcuts for filters

### 📊 Statistics
- **Total Traffic Logs Analyzed:** 3,828 (from test data)
- **AI Integration:** Azure OpenAI GPT-4.1
- **Performance:** <100ms for 100 logs per page
- **Cost:** ~$20-50/month for AI (estimated)

### 🗄️ Database Migrations Applied
- Initial
- AddTrafficAnalysisModule
- AddTicketNumberToTrafficAnalysis
- **Next:** AddAIConversations (ready to apply)