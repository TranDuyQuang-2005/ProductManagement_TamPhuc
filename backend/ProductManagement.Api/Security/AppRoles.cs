namespace ProductManagement.Api.Security;

public static class AppRoles
{
    public const string Admin = "ADMIN";
    public const string Staff = "STAFF";

    public static readonly string[] All = [Admin, Staff];
}
