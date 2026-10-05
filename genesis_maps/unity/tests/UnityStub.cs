// Minimal stand-in for the parts of UnityEngine used by BuckRogersMaps.cs, so the loader can be compiled and tested
// outside Unity (mono mcs). Not part of the Unity project.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace UnityEngine
{
    public class TextAsset { public string text; }
    public static class Debug { public static void LogError(string s) { Console.Error.WriteLine("LogError: " + s); } public static void Log(object s) { Console.WriteLine(s); } }
    public static class Resources
    {
        public static string Root = ".";
        public static T Load<T>(string path) where T : class
        {
            var f = Path.Combine(Root, path + ".json");
            if (!File.Exists(f)) return null;
            return new TextAsset { text = File.ReadAllText(f) } as T;
        }
    }

    // reflection based JSON reader that fills public fields like JsonUtility does (arrays, nested classes, no dictionaries)
    public static class JsonUtility
    {
        public static T FromJson<T>(string json) { var p = new P(json); return (T)Fill(typeof(T), p.Value()); }

        static object Fill(Type t, object j)
        {
            if (j == null) return null;
            if (t == typeof(int)) return Convert.ToInt32(j, CultureInfo.InvariantCulture);
            if (t == typeof(string)) return (string)j;
            if (t == typeof(bool)) return (bool)j;
            if (t == typeof(float)) return Convert.ToSingle(j, CultureInfo.InvariantCulture);
            if (t.IsArray)
            {
                var l = (List<object>)j; var et = t.GetElementType(); var a = Array.CreateInstance(et, l.Count);
                for (int i = 0; i < l.Count; i++) a.SetValue(Fill(et, l[i]), i);
                return a;
            }
            var d = (Dictionary<string, object>)j; var o = Activator.CreateInstance(t);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (d.ContainsKey(f.Name)) f.SetValue(o, Fill(f.FieldType, d[f.Name]));
            return o;
        }

        class P
        {
            string s; int i;
            public P(string s) { this.s = s; }
            void Ws() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            public object Value()
            {
                Ws(); char c = s[i];
                if (c == '{') { i++; var d = new Dictionary<string, object>(); Ws(); if (s[i] == '}') { i++; return d; }
                    while (true) { Ws(); var k = Str(); Ws(); i++; d[k] = Value(); Ws(); if (s[i++] == '}') return d; } }
                if (c == '[') { i++; var l = new List<object>(); Ws(); if (s[i] == ']') { i++; return l; }
                    while (true) { l.Add(Value()); Ws(); if (s[i++] == ']') return l; } }
                if (c == '"') return Str();
                if (s.Substring(i, 4) == "true") { i += 4; return true; }
                if (s.Substring(i, 5) == "false") { i += 5; return false; }
                if (s.Substring(i, 4) == "null") { i += 4; return null; }
                int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return s.Substring(st, i - st);
            }
            string Str()
            {
                var sb = new StringBuilder(); i++;
                while (s[i] != '"')
                {
                    if (s[i] == '\\') { i++; char e = s[i++]; if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; } else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e); }
                    else sb.Append(s[i++]);
                }
                i++; return sb.ToString();
            }
        }
    }
}
