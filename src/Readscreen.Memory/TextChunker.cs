namespace Readscreen.Memory;

public static class TextChunker
{
    public static IReadOnlyList<string> Chunk(string text, int maxChars = 1500, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var chunks = new List<string>();
        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(maxChars, text.Length - start);
            var chunk = text.Substring(start, length).Trim();
            if (chunk.Length > 0)
                chunks.Add(chunk);

            if (start + length >= text.Length)
                break;

            start += maxChars - overlap;
        }

        return chunks;
    }
}
