using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace NekoPainter.UI
{
    public static class StringTranslations
    {
        public static Dictionary<string, Dictionary<string, string>> EnumTranslations;
        public static Dictionary<string, string> UITranslations;

        public static string current = "";

        public static void Load(string local)
        {
            current = local;
            LoadResource(local, "EnumTransations.json", ref EnumTranslations);
            LoadResource(local, "UITranslations.json", ref UITranslations);
        }
        static void LoadResource<T>(string local, string resourceName, ref T translations)
        {
            string path = Path.Combine(System.Environment.CurrentDirectory, "Strings", local, resourceName);
            translations = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            //JsonSerializer jsonSerializer = new JsonSerializer();
            //translations = jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read))));
        }
    }
}
