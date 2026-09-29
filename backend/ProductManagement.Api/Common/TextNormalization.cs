using System.Text;

namespace ProductManagement.Api.Common;

public static class TextNormalization
{
    public static string NormalizeNfc(string value)
        => value.Trim().Normalize(NormalizationForm.FormC);

    public static string? NormalizeNfcOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return NormalizeNfc(value);
    }
}
