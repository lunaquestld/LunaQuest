// Traductions de LunaQuest. Le texte français écrit dans le code sert de clé ; le fichier lang.tsv
// (embarqué dans l'exe) donne pour chaque texte l'anglais, le chinois et le japonais.
// Les messages composés (« Casque détecté : » + modèle…) sont traduits morceau par morceau.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace LunaQuest
{
    static class Lang
    {
        public static readonly string[] Codes = { "fr", "en", "zh", "ja" };
        public static readonly string[] Names = { "Français", "English", "中文", "日本語" };
        public static string Code = "fr";

        static readonly Dictionary<string, string> exact = new Dictionary<string, string>();
        static readonly List<KeyValuePair<string, string>> fragments = new List<KeyValuePair<string, string>>();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static readonly object sync = new object();

        public static void Load(string code)
        {
            Code = Array.IndexOf(Codes, code) >= 0 ? code : "fr";
            exact.Clear();
            fragments.Clear();
            cache.Clear();
            if (Code == "fr") return;
            int col = Array.IndexOf(Codes, Code);
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("lang.tsv"))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '#') continue;
                        var parts = line.Split('\t');
                        if (parts.Length <= col || parts[col].Length == 0) continue;
                        string key = Unescape(parts[0]), value = Unescape(parts[col]);
                        exact[key] = value;
                        if (key.Trim().Length >= 3) fragments.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
                fragments.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            }
            catch { }
        }

        static string Unescape(string s) { return s.Replace("\\n", "\n").Replace("\\t", "\t"); }

        public static string T(string s)
        {
            if (Code == "fr" || string.IsNullOrEmpty(s)) return s;
            lock (sync)
            {
                string done;
                if (cache.TryGetValue(s, out done)) return done;
                done = Translate(s);
                if (cache.Count > 4000) cache.Clear();
                cache[s] = done;
                return done;
            }
        }

        static string Translate(string s)
        {
            string value;
            if (exact.TryGetValue(s, out value)) return value;
            if (s.IndexOf('\n') >= 0)
            {
                var lines = s.Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = Translate(lines[i]);
                return string.Join("\n", lines);
            }
            // Morceau par morceau, les plus longs d'abord, sans couper un mot.
            var parts = new List<string>();
            var sb = new StringBuilder(s);
            foreach (var kv in fragments)
            {
                string text = sb.ToString();
                int at = text.IndexOf(kv.Key, StringComparison.Ordinal);
                while (at >= 0)
                {
                    int end = at + kv.Key.Length;
                    bool startOk = at == 0 || !char.IsLetterOrDigit(kv.Key[0]) || !char.IsLetterOrDigit(text[at - 1]);
                    bool endOk = end >= text.Length || !char.IsLetterOrDigit(kv.Key[kv.Key.Length - 1]) || !char.IsLetterOrDigit(text[end]);
                    if (startOk && endOk)
                    {
                        string mark = "\u0001" + parts.Count + "\u0002";
                        parts.Add(kv.Value);
                        sb.Remove(at, kv.Key.Length).Insert(at, mark);
                        text = sb.ToString();
                        at = text.IndexOf(kv.Key, at + mark.Length, StringComparison.Ordinal);
                    }
                    else at = text.IndexOf(kv.Key, at + 1, StringComparison.Ordinal);
                }
            }
            string result = sb.ToString();
            for (int i = parts.Count - 1; i >= 0; i--) result = result.Replace("\u0001" + i + "\u0002", parts[i]);
            return result;
        }
    }
}
