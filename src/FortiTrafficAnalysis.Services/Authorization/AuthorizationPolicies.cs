namespace FortiTrafficAnalysis.Services.Authorization
{
    /// <summary>
    /// Authorization policy names
    /// </summary>
    public static class AuthorizationPolicies
    {
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireUserRole = "RequireUserRole";
        public const string RequireAnyRole = "RequireAnyRole";
    }

    /// <summary>
    /// Application role names
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admins";
        public const string User = "Users";
    }
}
