using Microsoft.Data.Sqlite;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Memory;

public sealed class SqliteMemoryStore : IMemoryStore
{
    private readonly IAppSettings _settings;
    private readonly IEmbeddingService _embeddings;
    private readonly string _dbPath;

    public SqliteMemoryStore(IAppSettings settings, IEmbeddingService embeddings)
    {
        _settings = settings;
        _embeddings = embeddings;
        _dbPath = Path.Combine(GetDataDir(), "memory.db");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GetDataDir());
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS memories (
                id TEXT PRIMARY KEY,
                category TEXT NOT NULL,
                title TEXT NOT NULL,
                content TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                embedding BLOB
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertAsync(MemoryEntry entry, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddings.EmbedAsync($"{entry.Title}\n{entry.Content}", cancellationToken);
        var blob = EmbeddingToBlob(embedding);

        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO memories (id, category, title, content, updated_at, embedding)
            VALUES ($id, $cat, $title, $content, $updated, $emb)
            ON CONFLICT(id) DO UPDATE SET
                category=$cat, title=$title, content=$content, updated_at=$updated, embedding=$emb;
            """;
        cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("$cat", entry.Category);
        cmd.Parameters.AddWithValue("$title", entry.Title);
        cmd.Parameters.AddWithValue("$content", entry.Content);
        cmd.Parameters.AddWithValue("$updated", entry.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$emb", blob);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM memories WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<MemoryEntry>();
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, category, title, content, updated_at FROM memories ORDER BY updated_at DESC";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new MemoryEntry(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4))));
        }
        return list;
    }

    public async Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddings.EmbedAsync(query, cancellationToken);
        var entries = new List<MemoryEntry>();
        var vectors = new List<float[]>();

        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, category, title, content, updated_at, embedding FROM memories";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new MemoryEntry(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4))));
            vectors.Add(BlobToEmbedding(reader.IsDBNull(5) ? null : (byte[])reader[5]));
        }

        if (vectors.Count == 0 || queryEmbedding.Length == 0)
            return entries.Take(topK).ToList();

        var indices = VectorSearchService.TopKIndices(vectors.ToArray(), queryEmbedding, topK);
        return indices.Select(i => entries[i]).ToList();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private string GetDataDir()
    {
        var dir = _settings.Current.DataDirectory;
        return string.IsNullOrWhiteSpace(dir)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Readscreen")
            : dir;
    }

    private static byte[] EmbeddingToBlob(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BlobToEmbedding(byte[]? blob)
    {
        if (blob == null || blob.Length == 0)
            return Array.Empty<float>();

        var floats = new float[blob.Length / sizeof(float)];
        Buffer.BlockCopy(blob, 0, floats, 0, blob.Length);
        return floats;
    }
}
