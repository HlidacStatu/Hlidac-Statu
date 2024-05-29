using System.Text;

namespace HlidacStatu.Util;

public static class TextTools
{
    public static bool IsValidCharacter(char c)
    {
        return (c >= 0x20 && c <= 0x7E) || (c >= 0xA0 && c <= 0xFF) || (c >= 0x100 && c <= 0x17F) ||
               (c >= 0x400 && c <= 0x4FF) || (c >= 0x2000 && c <= 0x206F) || (c >= 0x2E00 && c <= 0x2E7F) ||
               char.IsWhiteSpace(c);
    }
    
    public static string CleanBadBytesFromText(this string text)
    {
        StringBuilder cleaned = new StringBuilder();

        foreach (char c in text)
        {
            if (IsValidCharacter(c))
            {
                cleaned.Append(c);
            }
        }

        return cleaned.ToString();
    }
}