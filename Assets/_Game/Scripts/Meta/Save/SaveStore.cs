using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Meta.Save.Migrations;

namespace Game.Meta.Save
{
    public class SaveStore : ISaveStore
    {
        public const string DefaultSalt = "horde_runner_meta_save_salt_v1";
        public const int V1 = 1;
        public const int V2 = 2;

        private readonly List<ISaveMigration> _migrations = new();
        private readonly string _salt;

        public int CurrentVersion { get; }

        public SaveStore(IEnumerable<ISaveMigration> migrations = null, string salt = null, int currentVersion = 1)
        {
            CurrentVersion = currentVersion;
            _salt = salt ?? DefaultSalt;
            if (migrations != null)
            {
                _migrations.AddRange(migrations);
            }
            EnsureDefaultMigrations();
        }

        private void EnsureDefaultMigrations()
        {
            if (!_migrations.Exists(m => m.SourceVersion == 1 && m.TargetVersion == 2))
            {
                _migrations.Add(new SaveMigrationV1ToV2(_salt));
            }
        }

        public void Save(MetaState state, string filePath)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("FilePath is required.", nameof(filePath));

            state.SaveVersion = CurrentVersion;
            state.IntegrityHash = null;

            string rawJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
            string hash = ComputeHash(rawJson, _salt);
            state.IntegrityHash = hash;

            string fullJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });

            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, fullJson, Encoding.UTF8);
        }

        public MetaState Load(string filePath)
        {
            if (!ValidateIntegrity(filePath))
            {
                throw new InvalidOperationException("Save file integrity compromised or file not found.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);

            int version = ExtractVersion(json);
            while (version < CurrentVersion)
            {
                var migration = _migrations.Find(m => m.SourceVersion == version);
                if (migration == null)
                {
                    throw new InvalidOperationException($"No save migration available from version {version} to {CurrentVersion}.");
                }
                json = migration.MigrateJson(json);
                version = migration.TargetVersion;
            }

            return JsonSerializer.Deserialize<MetaState>(json);
        }

        public bool ValidateIntegrity(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                string fullJson = File.ReadAllText(filePath, Encoding.UTF8);
                using var doc = JsonDocument.Parse(fullJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (!doc.RootElement.TryGetProperty("IntegrityHash", out var hashProp) ||
                    hashProp.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                string expectedHash = hashProp.GetString();
                if (string.IsNullOrEmpty(expectedHash))
                {
                    return false;
                }

                using var ms = new MemoryStream();
                using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
                {
                    writer.WriteStartObject();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.NameEquals("IntegrityHash"))
                        {
                            writer.WriteNull("IntegrityHash");
                        }
                        else
                        {
                            prop.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                }

                string rawJson = Encoding.UTF8.GetString(ms.ToArray());
                string actualHash = ComputeHash(rawJson, _salt);

                return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string ComputeHash(string content, string salt = DefaultSalt)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(content + ":" + salt);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private string ComputeHash(string content) => ComputeHash(content, _salt);

        private static int ExtractVersion(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("SaveVersion", out var prop) && prop.TryGetInt32(out int ver))
                {
                    return ver;
                }
            }
            catch { }
            return 0;
        }
    }
}
