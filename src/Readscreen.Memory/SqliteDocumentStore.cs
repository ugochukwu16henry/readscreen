using Microsoft.Data.Sqlite;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Memory;

public sealed class SqliteDocumentStore : IDocumentStore
{
    private readonly IAppSettings _settings;
    private readonly IEmbeddingService _embeddings;
    private readonly string _dbPath;

    public SqliteDocumentStore(IAppSettings settings, IEmbeddingService embeddings)
    {
        _settings = settings;
        _embeddings = embeddings;
        _dbPath = Path.Combine(GetDataDir(), "documents.db");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GetDataDir());
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS chunks (
                id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                source_file TEXT NOT NULL,
                chunk_index INTEGER NOT NULL,
                content TEXT NOT NULL,
                embedding BLOB,
                FOREIGN KEY(session_id) REFERENCES sessions(id)
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Guid> CreateSessionAsync(string name, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO sessions (id, name, created_at) VALUES ($id, $name, $created)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return id;
    }

    public async Task IngestAsync(Guid sessionId, string filePath, CancellationToken cancellationToken = default)
    {
        var text = DocumentIngestService.ExtractText(filePath);
        var chunks = TextChunker.Chunk(text);
        var fileName = Path.GetFileName(filePath);

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(cancellationToken);
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var embedding = await _embeddings.EmbedAsync(chunk, cancellationToken);
            var blob = EmbeddingToBlob(embedding);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO chunks (id, session_id, source_file, chunk_index, content, embedding)
                VALUES ($id, $sid, $file, $idx, $content, $emb);
                """;
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$sid", sessionId.ToString());
            cmd.Parameters.AddWithValue("$file", fileName);
            cmd.Parameters.AddWithValue("$idx", i);
            cmd.Parameters.AddWithValue("$content", chunk);
            cmd.Parameters.AddWithValue("$emb", blob);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        string query, Guid sessionId, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddings.EmbedAsync(query, cancellationToken);
        var chunks = new List<DocumentChunk>();
        var vectors = new List<float[]>();

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, session_id, source_file, chunk_index, content, embedding
            FROM chunks WHERE session_id = $sid
            """;
        cmd.Parameters.AddWithValue("$sid", sessionId.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            chunks.Add(new DocumentChunk(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(4),
                reader.GetInt32(3)));
            vectors.Add(BlobToEmbedding(reader.IsDBNull(5) ? null : (byte[])reader[5]));
        }

        if (chunks.Count == 0)
            return chunks;

        var indices = VectorSearchService.TopKIndices(vectors.ToArray(), queryEmbedding, topK);
        return indices.Select(i => chunks[i]).ToList();
    }

    public async Task<IReadOnlyList<string>> GetSessionFilesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var files = new List<string>();
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT source_file FROM chunks WHERE session_id = $sid ORDER BY source_file";
        cmd.Parameters.AddWithValue("$sid", sessionId.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            files.Add(reader.GetString(0));
        return files;
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
