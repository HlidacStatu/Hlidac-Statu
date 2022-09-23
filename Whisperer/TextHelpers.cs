namespace Whisperer;

public static class TextHelpers
{
    public static string ReplaceDisallowedCharsWithSpace(this string self, HashSet<char> disallowedCharacters)
    {
        return string.Create(self.Length, self, (buffer, text) =>
        {
            for (int i = 0; i < text.Length; i++)
            {
                buffer[i] = disallowedCharacters.Contains(text[i]) ? ' ' : text[i];
            }
        });
    }
}