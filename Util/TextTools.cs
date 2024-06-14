using System.Text;

namespace HlidacStatu.Util;

public static class TextTools
{
    public static bool IsValidCharacter(char c)
    {
        return (c <= 0x2D67) &&  //odfiltrovat čínštinu,... 
               (char.IsWhiteSpace(c) || 
                char.IsSeparator(c) || 
                char.IsPunctuation(c) || 
                char.IsSymbol(c) ||
                char.IsLetterOrDigit(c));
    }
    
    public static string CleanBadBytesFromText(this string text)
    {
        text = text.Normalize(NormalizationForm.FormKC);
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