using System.Text.Json;
using System.IO;

namespace TBAntiCheat.Core
{

    internal class BaseConfig<T> where T : new()
    {
        internal T Config { get; private set; }
        protected readonly string configPath = string.Empty;

        internal BaseConfig(string path)
        {
            string folderPath = $"{ACCore.GetCore().ModuleDirectory}/Configs/";
            if (Directory.Exists(folderPath) == false)
            {
                Directory.CreateDirectory(folderPath);
            }

            configPath = $"{folderPath}{path}.json";
            Config = new T();

            Load();
        }

        internal bool Save()
        {
            string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            return true;
        }

        internal bool Load()
        {
            if (File.Exists(configPath) == false)
            {
                Save();
                return false;
            }

            string json = File.ReadAllText(configPath);
            Config = JsonSerializer.Deserialize<T>(json);

            return true;
        }
    }
}
