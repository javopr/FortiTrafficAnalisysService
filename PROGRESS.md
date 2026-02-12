# FortiGate Traffic Analysis Service - Development Progress

**Author:** javier.morales@intwo.cloud  
**Organization:** INTEGRATION TECHNOLOGIES CORP.  
**Database:** Azure SQL - intechsql.database.windows.net/fgtas

---

## ? COMPLETED STEPS

### ? STEP 1: Solution Structure Created
- Created Visual Studio solution with 5 projects
- Configured project references and dependencies
- Updated all projects to target .NET 8.0

**Projects:**
1. `FortiTrafficAnalysis.Domain` - Entity models
2. `FortiTrafficAnalysis.Data` - EF Core DbContext and migrations
3. `FortiTrafficAnalysis.Services` - Business logic and services
4. `FortiTrafficAnalysis.WebGui` - ASP.NET Core MVC web application
5. `FortiTrafficAnalysis.WebApi` - ASP.NET Core Web API

---

### ? STEP 2: Entity Models & Database Schema Created

**Entities Created:**
1. ? `AppGroup` - Application roles (Users, Admins)
2. ? `AppUser` - Application user management with Azure AD UPN
3. ? `Customer` - Multi-tenant customer/tenant entities
4. ? `FTAService` - Service contract management
5. ? `FortiGate` - FortiGate device management
6. ? `TrafficLog` - Traffic log storage for analysis

**Database Context:**
- ? `ApplicationDbContext` with full entity configuration
- ? Relationships and foreign keys configured
- ? Indexes for performance optimization
- ? Seed data for AppGroups (Users, Admins)

---

### ? STEP 3: Database Migration & Creation

**Migration Status:**
- ? Initial migration created: `InitialCreate`
- ? Migration applied to Azure SQL Database
- ? All 6 tables created successfully
- ? Seed data inserted (Users and Admins groups)

**Database Tables:**
```
AppGroups          - Contains: Users, Admins
AppUsers           - Empty (needs admin user added)
Customers          - Empty
FTAServices        - Empty
FortiGates         - Empty
TrafficLogs        - Empty
```

---

### ? STEP 4: Azure AD Authentication Implemented

**Authentication Features:**
- ? Azure AD (OpenID Connect) integration configured
- ? Microsoft Identity Web packages installed
- ? Custom authorization service created
- ? Custom authorization policies and handlers
- ? Role-based access control (Admins, Users)

**Authorization Services:**
- ? `IUserAuthorizationService` - Interface for user authorization
- ? `UserAuthorizationService` - Implementation with database lookup
- ? `AppRoleAuthorizationHandler` - Custom authorization handler
- ? `AuthorizationPolicies` - Policy constants (RequireAdminRole, RequireUserRole, RequireAnyRole)

**UI Components:**
- ? Updated layout with modern admin-style design
- ? Bootstrap 5 with Bootstrap Icons
- ? Login/Logout partial view
- ? Home page with welcome screen
- ? Dashboard page (authenticated users)
- ? Access Denied page
- ? Navigation menu with role-based visibility

**Configuration Required:**
- ?? Azure AD app registration needed (see AZURE_AD_SETUP_GUIDE.md)
- ?? appsettings.json needs Azure AD values
- ?? First admin user needs to be added to database

---

## ?? NEXT STEPS

### STEP 5: Admin Module Controllers & Views

**To Be Created:**
1. `AppUsersController` - Manage application users
   - List users
   - Add user (select from Azure AD)
   - Edit user role
   - Delete user

2. `CustomersController` - Manage customers/tenants
   - CRUD operations for customers

3. `FTAServicesController` - Manage service contracts
   - CRUD operations for service contracts
   - Link to customers

4. `FortiGatesController` - Manage FortiGate devices
   - CRUD operations for devices
   - Link to FTA Services
   - Store API keys securely

**Authorization:** All admin controllers require `RequireAdminRole` policy

---

### STEP 6: User Module - Traffic Analysis

**To Be Created:**
1. `TrafficLogsController` - Main traffic analysis interface
   - Upload log files (CSV/text)
   - Display logs in table with paging
   - Filter logs by:
     - Date range
     - Source IP
     - Destination IP
     - Source Port
     - Destination Port
     - Policy action (accept/deny)
   - Select logs for analysis
   - Generate policy recommendations

2. **Log Processing Service:**
   - Parse FortiGate log formats
   - Insert logs into TrafficLogs table
   - Associate with Customer and FortiGate

3. **Recommendation Engine:**
   - Analyze denied traffic patterns
   - Group by common attributes
   - Suggest firewall policies

**Authorization:** Both Users and Admins can access

---

### STEP 7: Web API Implementation

**API Endpoints to Create:**

1. **Log Upload API:**
   - `POST /api/logs/upload` - Upload and process log file
   - `POST /api/logs/parse` - Parse log data

2. **Log Query API:**
   - `GET /api/logs` - Get logs with filters
   - `GET /api/logs/{id}` - Get specific log
   - `DELETE /api/logs/{id}` - Delete log

3. **Analysis API:**
   - `POST /api/analysis/recommendations` - Get policy recommendations
   - `GET /api/analysis/stats` - Get traffic statistics

4. **Admin API (for future FortiGate integration):**
   - `GET /api/fortigate/{id}/policies` - Get FortiGate policies
   - `POST /api/fortigate/{id}/policies` - Create FortiGate policy

**Authorization:** API will use JWT bearer tokens (to be configured)

---

## ?? CURRENT APPLICATION STRUCTURE

```
FortiTrafficAnalysisService/
?
??? src/
?   ??? FortiTrafficAnalysis.Domain/
?   ?   ??? Entities/
?   ?       ??? AppGroup.cs
?   ?       ??? AppUser.cs
?   ?       ??? Customer.cs
?   ?       ??? FTAService.cs
?   ?       ??? FortiGate.cs
?   ?       ??? TrafficLog.cs
?   ?
?   ??? FortiTrafficAnalysis.Data/
?   ?   ??? ApplicationDbContext.cs
?   ?   ??? ApplicationDbContextFactory.cs
?   ?   ??? Migrations/
?   ?       ??? 20260212025432_InitialCreate.cs
?   ?
?   ??? FortiTrafficAnalysis.Services/
?   ?   ??? Authorization/
?   ?       ??? IUserAuthorizationService.cs
?   ?       ??? UserAuthorizationService.cs
?   ?       ??? AppRoleAuthorizationHandler.cs
?   ?       ??? AuthorizationPolicies.cs
?   ?
?   ??? FortiTrafficAnalysis.WebGui/
?   ?   ??? Controllers/
?   ?   ?   ??? HomeController.cs
?   ?   ??? Views/
?   ?   ?   ??? Home/
?   ?   ?   ?   ??? Index.cshtml
?   ?   ?   ?   ??? Dashboard.cshtml
?   ?   ?   ?   ??? AccessDenied.cshtml
?   ?   ?   ??? Shared/
?   ?   ?       ??? _Layout.cshtml
?   ?   ?       ??? _LoginPartial.cshtml
?   ?   ??? Program.cs
?   ?   ??? appsettings.json
?   ?
?   ??? FortiTrafficAnalysis.WebApi/
?       ??? Program.cs
?       ??? appsettings.json
?
??? AZURE_AD_SETUP_GUIDE.md
??? PROGRESS.md (this file)
??? FortiTrafficAnalysisService.sln
```

---

## ?? HOW TO RUN THE APPLICATION

### Prerequisites:
1. .NET 8.0 SDK installed
2. Azure SQL Database accessible
3. Azure AD app registration completed

### Steps:

1. **Configure Azure AD** (see AZURE_AD_SETUP_GUIDE.md)

2. **Add your admin user to database:**
   ```sql
   INSERT INTO AppUsers (AppAccessID, UserUPN, AppGroupID, AppUserName, AppUserEmail)
   VALUES (
       NEWID(),
       'your.email@domain.com',
       '22222222-2222-2222-2222-222222222222',
       'Your Name',
       'your.email@domain.com'
   )
   ```

3. **Run the WebGui:**
   ```powershell
   cd src/FortiTrafficAnalysis.WebGui
   dotnet run
   ```

4. **Navigate to:** https://localhost:7225

5. **Sign in with your Azure AD account**

---

## ?? DATABASE SCHEMA

### AppGroups
```
AppGroupID (PK)    | AppGroupName
-------------------|-------------
11111111-...       | Users
22222222-...       | Admins
```

### AppUsers
```
AppAccessID (PK) | UserUPN              | AppGroupID (FK) | AppUserName    | AppUserEmail
-----------------|----------------------|-----------------|----------------|------------------
GUID             | user@domain.com      | GUID            | User Name      | user@domain.com
```

### Customers
```
CustomerID (PK)  | CustomerName
-----------------|------------------
GUID             | Customer Name
```

### FTAServices
```
FTAID (PK) | JobID  | CustomerID (FK) | ServiceStart | ServiceEnd | ServiceStatus
-----------|--------|-----------------|--------------|------------|---------------
GUID       | JOB001 | GUID            | DateTime     | DateTime   | Active/Inactive
```

### FortiGates
```
FGID (PK) | FTAID (FK) | FGHostname | FGHost    | FGSerial | FGvDOM | FGapiKey | FGStatus
----------|------------|------------|-----------|----------|--------|----------|----------
GUID      | GUID       | FW-01      | 10.0.0.1  | FG123    | root   | xxxxx    | Active
```

### TrafficLogs
```
LogTempID (PK) | CustomerID | FGID  | LogTimestamp | SourceIP  | DestinationIP | SourcePort | DestinationPort | Protocol | PolicyAction | RawLogLine
---------------|------------|-------|--------------|-----------|---------------|------------|-----------------|----------|--------------|------------
GUID           | GUID       | GUID  | DateTime     | 10.0.0.1  | 20.0.0.1      | 443        | 80              | TCP      | deny         | full log...
```

---

## ?? KNOWN ISSUES & WARNINGS

1. **Microsoft.Identity.Web 3.3.0 Vulnerability:**
   - Package has known moderate severity vulnerability
   - Will upgrade to patched version after basic functionality is verified

2. **Security Improvements Needed:**
   - Move credentials to Azure Key Vault
   - Implement User Secrets for development
   - Add proper logging and monitoring
   - Implement rate limiting
   - Add CORS configuration for API

3. **Missing Features:**
   - Email notifications
   - Audit logging
   - Report generation
   - Backup/restore functionality
   - API authentication (JWT tokens)

---

## ?? DOCUMENTATION REFERENCES

- [Azure AD App Registration](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Microsoft Identity Web](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/)
- [Bootstrap 5](https://getbootstrap.com/)

---

## ?? CONTACT

**Author:** javier.morales@intwo.cloud  
**Organization:** INTEGRATION TECHNOLOGIES CORP.

---

**Last Updated:** 2025-01-XX
