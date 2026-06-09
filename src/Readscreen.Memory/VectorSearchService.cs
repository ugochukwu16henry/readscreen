namespace Readscreen.Memory;

public static class VectorSearchService
{
    public static IReadOnlyList<int> TopKIndices(float[][] vectors, float[] query, int k)
    {
        if (vectors.Length == 0 || query.Length == 0)
            return Array.Empty<int>();

        return vectors
            .Select((v, i) => (i, Score: CosineSimilarity(v, query)))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .Select(x => x.i)
            .ToList();
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0f;

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0f;

        return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB)));
    }
}
