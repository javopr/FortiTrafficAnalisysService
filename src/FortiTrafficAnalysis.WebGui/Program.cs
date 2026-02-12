using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Services.Authorization;
using FortiTrafficAnalysis.Services.Authentication;
using FortiTrafficAnalysis.Services.LogParsing;
using FortiTrafficAnalysis.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Cookie Authentication (local authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Require authentication by default for all controllers
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register authentication and authorization services
builder.Services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
builder.Services.AddScoped<IAuthorizationHandler, AppRoleAuthorizationHandler>();

// Register log parsing services
builder.Services.AddScoped<IFortiGateLogParserService, FortiGateLogParserService>();

// Register ticket number generator
builder.Services.AddSingleton<ITicketNumberGenerator, TicketNumberGenerator>();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
        policy.Requirements.Add(new AppRoleRequirement(AppRoles.Admin)));
    
    options.AddPolicy(AuthorizationPolicies.RequireUserRole, policy =>
        policy.Requirements.Add(new AppRoleRequirement(AppRoles.User)));
    
    options.AddPolicy(AuthorizationPolicies.RequireAnyRole, policy =>
        policy.Requirements.Add(new AppRoleRequirement(AppRoles.Admin, AppRoles.User)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
