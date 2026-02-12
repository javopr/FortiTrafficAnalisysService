# Azure AD App Registration Guide
## FortiGate Traffic Analysis Service

**Author:** javier.morales@intwo.cloud  
**Organization:** INTEGRATION TECHNOLOGIES CORP.

---

## Step 1: Register Application in Azure AD

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** ? **App registrations** ? **New registration**

### App Registration Details:

- **Name:** `FortiGate Traffic Analysis WebGui`
- **Supported account types:** `Accounts in this organizational directory only (Single tenant)`
- **Redirect URI:** 
  - Platform: `Web`
  - URI: `https://localhost:7225/signin-oidc`

Click **Register**

---

## Step 2: Create Client Secret

1. In your newly registered app, go to **Certificates & secrets**
2. Click **New client secret**
3. **Description:** `WebGui Secret`
4. **Expires:** `24 months` (or as per your security policy)
5. Click **Add**
6. **IMPORTANT:** Copy the secret **Value** immediately (you won't be able to see it again)

---

## Step 3: Configure API Permissions (Optional but Recommended)

1. Go to **API permissions**
2. You should already have `Microsoft Graph` ? `User.Read` (delegated)
3. This is sufficient for basic authentication

---

## Step 4: Gather Required Information

From the **Overview** page of your app registration, copy:

1. **Application (client) ID** - Example: `12345678-1234-1234-1234-123456789abc`
2. **Directory (tenant) ID** - Example: `87654321-4321-4321-4321-cba987654321`
3. **Client Secret Value** - From Step 2 above (copy and save securely)

Also note your **Azure AD Domain**:
- If your organization is `contoso.onmicrosoft.com`, then your domain is `contoso.onmicrosoft.com`
- Or use your custom domain if configured

---

## Step 5: Update appsettings.json

Open `src/FortiTrafficAnalysis.WebGui/appsettings.json` and update the following values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_DOMAIN.onmicrosoft.com",          ? Replace with your domain
    "TenantId": "YOUR_TENANT_ID",                     ? Replace with Directory (tenant) ID
    "ClientId": "YOUR_CLIENT_ID",                     ? Replace with Application (client) ID
    "ClientSecret": "YOUR_CLIENT_SECRET",             ? Replace with Client Secret Value
    "CallbackPath": "/signin-oidc"
  }
}
```

### Example (with fake values):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "intwo.onmicrosoft.com",
    "TenantId": "87654321-4321-4321-4321-cba987654321",
    "ClientId": "12345678-1234-1234-1234-123456789abc",
    "ClientSecret": "AbC~1234567890abcdefghijklmnopqrstuvwxyz",
    "CallbackPath": "/signin-oidc"
  }
}
```

---

## Step 6: Add Your First Admin User to the Database

Before you can log in and use the application, you need to add at least one user with Admin privileges to the database.

### Option A: Using SQL Server Management Studio or Azure Data Studio

Connect to your Azure SQL Database and run:

```sql
-- Get the Admin Group ID (should be the seeded value)
DECLARE @AdminGroupID UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222'

-- Add your user (replace with your actual UPN and details)
INSERT INTO AppUsers (AppAccessID, UserUPN, AppGroupID, AppUserName, AppUserEmail)
VALUES (
    NEWID(),
    'javier.morales@intwo.cloud',           -- Your Azure AD UPN
    @AdminGroupID,                           -- Admin group
    'Javier Morales',                        -- Your display name
    'javier.morales@intwo.cloud'            -- Your email
)
```

### Option B: Using a Migration or Seed Data

Add this to your `ApplicationDbContext.cs` in the `SeedData` method (after the AppGroups seed):

```csharp
// Seed initial admin user
var adminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
modelBuilder.Entity<AppUser>().HasData(
    new AppUser
    {
        AppAccessID = adminUserId,
        UserUPN = "javier.morales@intwo.cloud",
        AppGroupID = adminsGroupId,
        AppUserName = "Javier Morales",
        AppUserEmail = "javier.morales@intwo.cloud"
    }
);
```

Then create and apply a new migration.

---

## Step 7: Test the Application

1. Run the application:
   ```powershell
   cd src/FortiTrafficAnalysis.WebGui
   dotnet run
   ```

2. Navigate to: `https://localhost:7225`

3. Click **"Sign In with Azure AD"**

4. You should be redirected to Microsoft login

5. After successful authentication:
   - If your user exists in the `AppUsers` table ? You'll be authorized
   - If your user doesn't exist ? You'll see "Access Denied"

---

## Troubleshooting

### Error: "AADSTS50011: The redirect URI specified in the request does not match"
- Make sure the redirect URI in Azure AD matches exactly: `https://localhost:7225/signin-oidc`

### Error: "Access Denied" after login
- Your user is not in the `AppUsers` table. Add your user using Step 6 above.

### Error: "An error occurred while processing your request"
- Check the application logs
- Verify all Azure AD settings in `appsettings.json`
- Ensure the client secret is correct and not expired

### Database Connection Issues
- Verify your Azure SQL firewall allows your IP
- Check the connection string in `appsettings.json`

---

## Next Steps After Azure AD is Working

Once authentication is working, we'll proceed to:

1. ? **STEP 5:** Create Admin Controllers and Views
   - AppUsers management
   - Customers management
   - FTAServices management
   - FortiGates management

2. ? **STEP 6:** Create User/Traffic Analysis Module
   - Log upload functionality
   - Traffic log viewer with filters
   - Policy recommendation engine

3. ? **STEP 7:** Create Web API Controllers
   - Log processing endpoints
   - Query/filter endpoints
   - Recommendation endpoints

---

## Security Notes

?? **IMPORTANT:** 
- Never commit `appsettings.json` with real credentials to source control
- Use **Azure Key Vault** or **User Secrets** for production
- The current Microsoft.Identity.Web package has a known vulnerability - we'll upgrade it later
- Always use HTTPS in production
- Implement proper logging and monitoring

---

## Application URLs

- **WebGui Development:** https://localhost:7225
- **WebApi Development:** https://localhost:7XXX (we'll configure this next)

---

## Support

For issues or questions, contact: javier.morales@intwo.cloud
