using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DictController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<DictController> _logger;
        static List<Tuple<string, string, bool>> _dict;
        static List<string> _chars;
        static Dictionary<System.Globalization.UnicodeCategory, HashSet<char>> cat = new Dictionary<System.Globalization.UnicodeCategory, HashSet<char>>();
        static HashSet<System.Globalization.UnicodeCategory> modifiers = new HashSet<System.Globalization.UnicodeCategory> {
            System.Globalization.UnicodeCategory.NonSpacingMark,
            System.Globalization.UnicodeCategory.ModifierLetter,
            System.Globalization.UnicodeCategory.ModifierSymbol
        };
        static HashSet<int> nonModifiers = new HashSet<int> { 0x2c8, 0x2cc };
        public static List<string> GetCharacters(string text)
        {
            char[] ca = text.ToCharArray();
            List<string> characters = new List<string>();
            for (int i = 0; i < ca.Length; i++)
            {
                char c = ca[i];
                var uc = char.GetUnicodeCategory(c);
                bool nextModifier = i < ca.Length - 1 && modifiers.Contains(char.GetUnicodeCategory(ca[i + 1])) && !nonModifiers.Contains(ca[i + 1]);
                bool linkingModifier = nextModifier && i < ca.Length - 2 && (ca[i + 1] >= 860) && (ca[i + 1] <= 866);

                HashSet<char> lc;
                if (!cat.TryGetValue(uc, out lc)) cat.Add(uc, lc = new HashSet<char>());
                lc.Add(c);
                if (c > 65535) continue;
                if (char.IsHighSurrogate(c) || nextModifier)
                {
                    i++;
                    characters.Add(new string(new[] { c, ca[i] }));
                }
                else if (linkingModifier)
                {
                    string s;
                    characters.Add(s = new string(new[] { c, ca[i + 1], ca[i + 2] }));
                    i += 2;
                }
                else
                {
                    characters.Add(new string(new[] { c }));
                }
            }
            return characters;
        }
        static System.Text.RegularExpressions.Regex NoOptional = new System.Text.RegularExpressions.Regex("[(][^)]+[)]");
        static System.Text.RegularExpressions.Regex Optional = new System.Text.RegularExpressions.Regex("[()]+");

        static List<string> DeDup(string str)
        {
            if (str.Contains('('))
            {
                string str1 = NoOptional.Replace(str, string.Empty);
                string str2 = Optional.Replace(str, string.Empty);
                if (str1 != str2) return new List<string> { str1, str2 };
            }
            return new List<string> { str };
        }
        static DictController()
        {
            _dict = new List<Tuple<string, string, bool>>();
            var chars = new System.Collections.Generic.HashSet<string>();
            foreach (string line in System.IO.File.ReadAllLines("../parse/data0.txt"))
            {
                var parts = line.Split("@").Select(_ => _.Trim()).ToList();
                
                foreach (var transcr in parts[1].Split('|').SelectMany(_ => DeDup(_)))
                {
                    if (string.IsNullOrEmpty(transcr) || (transcr[0] != '/' && transcr[0] != '['))
                        continue;
                    foreach (var c in GetCharacters(transcr))
                    {
                        chars.Add(c);
                    }

                    _dict.Add(Tuple.Create(transcr, parts[0], parts.Count == 3));
                }
            }
            _chars = chars.Where(_ => !string.IsNullOrEmpty(_)).ToList();
            _chars.Sort();
        }
        public DictController(ILogger<DictController> logger)
        {
            _logger = logger;
        }
        static List<List<string>> Empty = new List<List<string>>(){};
        const string match = "m";
        const string rest = "r";
        [HttpGet("search")]
        public IEnumerable<List<string>> Get(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return Empty;
            bool lineStart = pattern[0] == '^';
            bool lineEnd = pattern.Last() == '$';
            if (lineStart) pattern = pattern.Substring(1);
            if (lineEnd) pattern = pattern.Substring(0, pattern.Length - 1);

            var result = new List<List<string>>();
            foreach (var pair in _dict)
            {
                string stripped = pair.Item1.Substring(1, pair.Item1.Length - 2);
                char first = pair.Item1.First();
                char last = pair.Item1.Last();
                int index = 0;
                bool mismatch = false;
                var parts = pattern.Split(' ');
                var entry = new List<string>();
                if (parts.Length > 0 && lineStart)
                {
                    if ( stripped.Length < parts[0].Length || stripped.Substring(0, parts[0].Length) != parts[0])
                    {
                        mismatch = true;
                        continue;
                    }
                    entry.Add(match);
                    entry.Add(parts[0]);
                    index += parts[0].Length;
                    parts = parts.Skip(1).ToArray();
                }

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    int pos = stripped.IndexOf(part, index);
                    if (pos == -1)
                    {
                        mismatch = true;
                        break;
                    }
                    if (pos > index)
                    {
                        entry.Add(rest);
                        entry.Add(stripped.Substring(index, pos - index));
                    }
                    entry.Add(match);
                    entry.Add(part);
                    index = pos + part.Length;
                }
                if (index < stripped.Length)
                {
                    entry.Add(rest);
                    entry.Add(stripped.Substring(index));
                }
                if (lineEnd && index != stripped.Length) mismatch = true;
                if (!mismatch)
                {
                    entry.Insert(0, pair.Item3? "a" : "n");
                    entry.Insert(0, pair.Item1);
                    entry.Insert(0, pair.Item2);
                    result.Add(entry);
                }
            }
            result.Sort((a, b) => string.Compare(a[0], b[0]));
            return result;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return _chars;
        }
    }
}
