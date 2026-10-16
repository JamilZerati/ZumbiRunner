using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Game.Meta.Save
{
    public class SaveStore : ISaveStore
    {
        private const string DefaultSalt = "horde_runner_meta_save_salt_v1";
        private readonly List<ISaveMigration> _migrations = new();
        private readonly string _salt;

        public int CurrentVersion { get; }

        public SaveStore(IEnumerable<ISaveMigration> migrations = null, string salt = null, int currentVersion = 1)
        {
            CurrentVersion = currentVersion;
            if (migrations != null)
            {
                _migrations.AddRange(migrations);
            }
            _salt = salt ?? DefaultSalt;
        }

        public void Save(MetaState state, string filePath)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("FilePath is required.", nameof(filePath));

            state.SaveVersion = CurrentVersion;
            state.IntegrityHash = null;

            string rawJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
            string hash = ComputeHash(rawJson);
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
                var state = JsonSerializer.Deserialize<MetaState>(fullJson);
                if (state == null || string.IsNullOrEmpty(state.IntegrityHash))
                {
                    return false;
                }

                string expectedHash = state.IntegrityHash;
                state.IntegrityHash = null;

                string rawJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
                string actualHash = ComputeHash(rawJson);

                return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(content + ":" + _salt);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

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
