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

## ?? Features

- ? **Azure AD Authentication** - Enterprise-grade security
- ? **Multi-Tenant Architecture** - Manage multiple customers
- ? **Role-Based Access Control** - Users and Admins
- ?? **Traffic Log Analysis** - Upload and analyze FortiGate logs (Coming Soon)
- ?? **Policy Recommendations** - AI-powered firewall policy suggestions (Coming Soon)
- ?? **FortiGate Integration** - Direct API integration (Coming Soon)

---

## ??? Architecture

### Technology Stack
- **Backend:** ASP.NET Core 8.0 (MVC + Web API)
- **Database:** Azure SQL Database
- **Authentication:** Microsoft Identity Platform (Azure AD)
- **ORM:** Entity Framework Core 8.0
- **Frontend:** Bootstrap 5 + Bootstrap Icons

### Projects
1. **Domain** - Entity models
2. **Data** - EF Core, migrations, repositories
3. **Services** - Business logic
4. **WebGui** - MVC web application
5. **WebApi** - REST API endpoints

---

## ?? Database Schema

- **AppGroups** - Application roles (Users, Admins)
- **AppUsers** - Application user management
- **Customers** - Customer/tenant management
- **FTAServices** - Service contract management
- **FortiGates** - FortiGate device inventory
- **TrafficLogs** - Traffic log storage and analysis

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

## ??? Development Status

### ? Completed
- [x] Solution structure and projects
- [x] Entity models and database schema
- [x] EF Core migrations
- [x] Azure SQL Database setup
- [x] Azure AD authentication
- [x] Authorization services
- [x] Modern UI layout

### ?? In Progress
- [ ] Admin module (Users, Customers, Services, Devices)
- [ ] Traffic log upload and parsing
- [ ] Log filtering and querying
- [ ] Policy recommendation engine
- [ ] Web API implementation

---

## ?? Roadmap

**Phase 1: Core Functionality** (Current)
- Complete Admin module
- Implement Traffic log analysis
- Create recommendation engine

**Phase 2: API Integration**
- Web API implementation
- FortiGate REST API integration
- Automated log collection

**Phase 3: Advanced Features**
- Dashboard analytics
- Report generation
- Email notifications
- Audit logging

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

**Last Updated:** January XX, 2024

### Recent Changes
- ✅ Implemented auto-generated 10-character alphanumeric ticket numbers
- ✅ Added interactive log table with real-time filtering
- ✅ Completed log file upload and parsing for FortiGate logs

### Next Steps
1. Implement Policy Recommendation Engine
2. Add ticket edit functionality
3. Enhanced log filtering on all columns

### Database Migrations Applied
- Initial
- AddTrafficAnalysisModule
- AddTicketNumberToTrafficAnalysis