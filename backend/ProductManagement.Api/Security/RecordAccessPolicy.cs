using ProductManagement.Api.Common;

namespace ProductManagement.Api.Security;

public static class RecordAccessPolicy
{
    public static bool ShouldProtectAfterAction(string? currentRole, bool currentValue)
        => currentValue || string.Equals(currentRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    public static bool CanModify(string? currentRole, bool isAdminProtected)
    {
        if (string.Equals(currentRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(currentRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase)
               && !isAdminProtected;
    }

    public static void EnsureCanModify(string? currentRole, bool isAdminProtected, string entityLabel)
    {
        if (CanModify(currentRole, isAdminProtected))
            return;

        if (string.Equals(currentRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase))
            throw AppException.Forbidden($"Ban khong co quyen chinh sua {entityLabel.ToLowerInvariant()} nay.");

        throw AppException.Forbidden("Tai khoan khong co quyen thuc hien thao tac nay.");
    }
}
