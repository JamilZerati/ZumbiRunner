using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Game.Meta.Save.Migrations
{
    public class SaveMigrationV1ToV2 : ISaveMigration
    {
        private readonly string _salt;

        public int SourceVersion => 1;
        public int TargetVersion => 2;

        public SaveMigrationV1ToV2(string salt = null)
        {
            _salt = salt ?? SaveStore.DefaultSalt;
        }

        public string MigrateJson(string sourceJson)
        {
            if (string.IsNullOrWhiteSpace(sourceJson))
            {
                throw new ArgumentException("Source JSON cannot be null or empty.", nameof(sourceJson));
            }

            using var doc = JsonDocument.Parse(sourceJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Source JSON root must be an object.");
            }

            var defaultBarracks = new BarracksState();
            string barracksJson = JsonSerializer.Serialize(defaultBarracks, new JsonSerializerOptions { WriteIndented = false });
            using var barracksDoc = JsonDocument.Parse(barracksJson);

            using var rawMs = new MemoryStream();
            using (var writer = new Utf8JsonWriter(rawMs, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                bool hasBarracks = false;
                bool hasIntegrityHash = false;

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("SaveVersion"))
                    {
                        writer.WriteNumber("SaveVersion", TargetVersion);
                    }
                    else if (prop.NameEquals("IntegrityHash"))
                    {
                        hasIntegrityHash = true;
                        if (!hasBarracks)
                        {
                            writer.WritePropertyName("Barracks");
                            barracksDoc.RootElement.WriteTo(writer);
                            hasBarracks = true;
                        }
                        writer.WriteNull("IntegrityHash");
                    }
                    else
                    {
                        if (prop.NameEquals("Barracks"))
                        {
                            hasBarracks = true;
                        }
                        prop.WriteTo(writer);
                    }
                }

                if (!hasBarracks)
                {
                    writer.WritePropertyName("Barracks");
                    barracksDoc.RootElement.WriteTo(writer);
                }

                if (!hasIntegrityHash)
                {
                    writer.WriteNull("IntegrityHash");
                }

                writer.WriteEndObject();
            }

            string rawJson = Encoding.UTF8.GetString(rawMs.ToArray());
            string newHash = SaveStore.ComputeHash(rawJson, _salt);

            using var fullMs = new MemoryStream();
            using (var writer = new Utf8JsonWriter(fullMs, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                bool hasBarracks = false;
                bool hasIntegrityHash = false;

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("SaveVersion"))
                    {
                        writer.WriteNumber("SaveVersion", TargetVersion);
                    }
                    else if (prop.NameEquals("IntegrityHash"))
                    {
                        hasIntegrityHash = true;
                        if (!hasBarracks)
                        {
                            writer.WritePropertyName("Barracks");
                            barracksDoc.RootElement.WriteTo(writer);
                            hasBarracks = true;
                        }
                        writer.WriteString("IntegrityHash", newHash);
                    }
                    else
                    {
                        if (prop.NameEquals("Barracks"))
                        {
                            hasBarracks = true;
                        }
                        prop.WriteTo(writer);
                    }
                }

                if (!hasBarracks)
                {
                    writer.WritePropertyName("Barracks");
                    barracksDoc.RootElement.WriteTo(writer);
                }

                if (!hasIntegrityHash)
                {
                    writer.WriteString("IntegrityHash", newHash);
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(fullMs.ToArray());
        }
    }
}
