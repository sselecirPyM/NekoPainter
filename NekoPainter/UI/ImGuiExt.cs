using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace NekoPainter.UI
{
    public static class ImGuiExt
    {
        public static void LanguageCheck()
        {
            if (currentLanguage != StringTranslations.current)
            {
                currentLanguage = StringTranslations.current;
                enumNamesDictionary.Clear();
            }
        }
        public static bool ComboBox<T>(string label, ref T val) where T : struct, Enum
        {
            string typeName = (typeof(T)).ToString();
            string valName = val.ToString();
            string[] enums = Enum.GetNames<T>();
            string[] enumsTranslation = GetEnumTranslations(typeName, enums);

            int sourceI = Array.FindIndex(enums, u => u == valName);
            int sourceI2 = sourceI;

            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            bool result = ImGui.Combo(string.Format("{1}###{0}", label, hasTranslation ? translation : label), ref sourceI, enumsTranslation, enumsTranslation.Length);
            if (sourceI != sourceI2)
                val = Enum.Parse<T>(enums[sourceI]);


            return result;
        }
        public static bool Begin(string label)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.Begin(string.Format("{1}###{0}", label, hasTranslation ? translation : label));
        }

        public static bool BeginMenu(string label)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.BeginMenu(string.Format("{1}###{0}", label, hasTranslation ? translation : label));
        }
        public static bool Button(string label)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.Button(string.Format("{1}###{0}", label, hasTranslation ? translation : label));
        }
        public static bool Checkbox(string label, ref bool v)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.Checkbox(string.Format("{1}###{0}", label, hasTranslation ? translation : label), ref v);
        }
        public static bool InputInt(string label, ref int v)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.InputInt(string.Format("{1}###{0}", label, hasTranslation ? translation : label), ref v);
        }
        public static bool InputText(string label, ref string v, uint maxLength)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.InputText(string.Format("{1}###{0}", label, hasTranslation ? translation : label), ref v, maxLength);
        }

        public static bool MenuItem(string label)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.MenuItem(string.Format("{1}###{0}", label, hasTranslation ? translation : label));
        }

        public static bool MenuItem(string label, bool enabled)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.MenuItem(string.Format("{1}###{0}", label, hasTranslation ? translation : label), enabled);
        }

        public static bool MenuItem(string label, string shoutcut)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.MenuItem(string.Format("{1}###{0}", label, hasTranslation ? translation : label), shoutcut);
        }

        public static bool MenuItem(string label, string shoutcut, bool selected, bool enabled)
        {
            bool hasTranslation = StringTranslations.UITranslations.TryGetValue(label, out string translation);
            return ImGui.MenuItem(string.Format("{1}###{0}", label, hasTranslation ? translation : label), shoutcut, selected, enabled);
        }

        public static string[] GetEnumTranslations(string typeName, string[] enums)
        {
            if (!enumNamesDictionary.TryGetValue(typeName, out var enumsTranslation))
            {
                enumsTranslation = new string[enums.Length];
                if (StringTranslations.EnumTranslations.TryGetValue(typeName, out var dictionary))
                {
                    for (int i = 0; i < enums.Length; i++)
                    {
                        if (dictionary.TryGetValue(enums[i], out string translation))
                            enumsTranslation[i] = translation;
                        else
                            enumsTranslation[i] = enums[i];
                    }
                }
                else
                {
                    for (int i = 0; i < enums.Length; i++)
                    {
                        enumsTranslation[i] = enums[i];
                    }
                }
                enumNamesDictionary[typeName] = enumsTranslation;
            }
            return enumsTranslation;
        }

        public static Dictionary<string, string[]> enumNamesDictionary = new Dictionary<string, string[]>();

        public static string currentLanguage = "";
    }
}
