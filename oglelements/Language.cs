﻿using OGL.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace OGL
{
    public class Language : IComparable<Language>, IHTML, IOGLElement<Language>
    {
        [XmlIgnore]
        public string filename;
        [XmlIgnore]
        public static XmlSerializer Serializer = new XmlSerializer(typeof(Language));
        [XmlIgnore]
        private static XslCompiledTransform transform = new XslCompiledTransform();
        [XmlIgnore]
        static public Dictionary<String, Language> languages = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);
        [XmlIgnore]
        static public Dictionary<String, Language> simple = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);
        public String Name { get; set; }
        public String Description { get; set; }
        public String Skript { get; set; }
        public String TypicalSpeakers { get; set; }
        public String Source { get; set; }
        [XmlIgnore]
        public bool ShowSource { get; set; } = false;
        [XmlIgnore]
        public Bitmap Image
        {
            set
            { // serialize
                if (value == null) ImageData = null;
                else using (MemoryStream ms = new MemoryStream())
                {
                    value.Save(ms, ImageFormat.Png);
                    ImageData = ms.ToArray();
                }
            }
            get
            { // deserialize
                if (ImageData == null)
                {
                    return null;
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(ImageData))
                    {
                        return new Bitmap(ms);
                    }
                }
            }
        }

        public byte[] ImageData { get; set; }
        public void register(string file)
        {
            filename = file;
            string full = Name + " " + ConfigManager.SourceSeperator + " " + Source;
            if (languages.ContainsKey(full)) throw new Exception("Duplicate Language: " + full);
            languages.Add(full, this);
            if (simple.ContainsKey(Name))
            {
                simple[Name].ShowSource = true;
                ShowSource = true;
            }
            else simple.Add(Name, this);
        }
        public Language() 
        {
            Skript = "";
            TypicalSpeakers = "";
            Source = ConfigManager.DefaultSource;
        }
        public Language(String name, String description, String skript, String speakers)
        {
            Name = name;
            Description = description;
            Skript = skript;
            TypicalSpeakers = speakers;
            Source = ConfigManager.DefaultSource;
            register(null);
        }
        public static Language Get(String name, string sourcehint)
        {
            if (name.Contains(ConfigManager.SourceSeperator))
            {
                if (languages.ContainsKey(name)) return languages[name];
                name = SourceInvariantComparer.NoSource(name);
            }
            if (sourcehint != null && languages.ContainsKey(name + " " + ConfigManager.SourceSeperator + " " + sourcehint)) return languages[name + " " + ConfigManager.SourceSeperator + " " + sourcehint];
            if (simple.ContainsKey(name)) return simple[name];
            ConfigManager.LogError("Unknown Language: " + name);
            return new Language(name, "Missing Entry", "", "");
        }
        //public static void ExportAll()
        //{
        //    foreach (Language s in languages.Values)
        //    {
        //        FileInfo file = SourceManager.getFileName(s.Name, s.Source, ConfigManager.Directory_Languages);
        //        using (TextWriter writer = new StreamWriter(file.FullName)) Serializer.Serialize(writer, s);
        //    }
        //}
        
        public String ToHTML()
        {
            try
            {
                if (transform.OutputSettings == null) transform.Load(ConfigManager.Transform_Languages.FullName);
                using (MemoryStream mem = new MemoryStream())
                {
                    Serializer.Serialize(mem, this);
                    ConfigManager.RemoveDescription(mem);
                    mem.Seek(0, SeekOrigin.Begin);
                    XmlReader xr = XmlReader.Create(mem);
                    using (StringWriter textWriter = new StringWriter())
                    {
                        using (XmlWriter xw = XmlWriter.Create(textWriter))
                        {
                            transform.Transform(xr, xw);
                            return textWriter.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "<html><body><b>Error generating output:</b><br>" + ex.Message + "<br>" + ex.InnerException + "<br>" + ex.StackTrace + "</body></html>";
            }
        }
        public override string ToString()
        {
            if (ShowSource || ConfigManager.AlwaysShowSource) return Name + " " + ConfigManager.SourceSeperator + " " + Source;
            return Name;
        }
        public int CompareTo(Language other)
        {
            return Name.CompareTo(other.Name);
        }
        public Language Clone()
        {
            using (MemoryStream mem = new MemoryStream())
            {
                Serializer.Serialize(mem, this);
                mem.Seek(0, SeekOrigin.Begin);
                Language r = (Language)Serializer.Deserialize(mem);
                r.filename = filename;
                return r;
            }
        }
    }
}
