using ProductManagement.Api.Common;

namespace ProductManagement.Api.Security;

public static class RecordAccessPolicy
{
    // A record is protected from STAFF unless it is explicitly STAFF-created and
    // has never been modified by a non-STAFF role. This also protects legacy rows
    // whose ownership metadata is missing.
    public static bool IsAdminProtected(string? createdByRole, string? lastModifiedByRole)
    {
        if (!string.Equals(createdByRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase))
            return true;

        return !string.IsNullOrWhiteSpace(lastModifiedByRole)
               && !string.Equals(lastModifiedByRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanModify(string? currentRole, string? createdByRole, string? lastModifiedByRole)
    {
        if (string.Equals(currentRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(currentRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase)
               && !IsAdminProtected(createdByRole, lastModifiedByRole);
    }

    public static void EnsureCanModify(
        string? currentRole,
        string? createdByRole,
        string? lastModifiedByRole,
        string entityLabel)
    {
        if (CanModify(currentRole, createdByRole, lastModifiedByRole))
            return;

        if (string.Equals(currentRole, AppRoles.Staff, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                $"{entityLabel} nay do ADMIN tao/da chinh sua hoac khong xac dinh duoc nguon tao. STAFF chi duoc xem va xem lich su.");
        }

        throw AppException.Forbidden("Tai khoan khong co quyen thuc hien thao tac nay.");
    }
}
