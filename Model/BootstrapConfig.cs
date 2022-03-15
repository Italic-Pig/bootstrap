using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItalicPig.Bootstrap.Model
{
    public class BootstrapConfig
    {
        public ImmutableDictionary<string, ImmutableList<string>> Views { get; }

        public BootstrapConfig()
        {
            Views = ImmutableDictionary<string, ImmutableList<string>>.Empty;
        }

        [JsonConstructor]
        public BootstrapConfig(ImmutableDictionary<string, ImmutableList<string>> views)
        {
            Views = views;
        }

        public static BootstrapConfig Read(string path)
        {
            try
            {
                var Json = File.ReadAllText(MakeConfigPath(path), Encoding.UTF8);
                var Deserialized = JsonSerializer.Deserialize<BootstrapConfig>(Json);
                return Deserialized ?? new BootstrapConfig();
            }
            catch (IOException)
            {
                return new BootstrapConfig();
            }
        }

        public void Write(string path)
        {
            var JsonOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var Json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(MakeConfigPath(path), Json, Encoding.UTF8);
        }

        private static string MakeConfigPath(string path) => Path.Combine(path, ".bootstrap");
    }
}
