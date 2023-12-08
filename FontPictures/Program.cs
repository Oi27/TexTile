﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using System.IO;
using CommandLine;

namespace FontPictures
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('b', "border", Required = false, HelpText = "Add a border of font/border.png to the generated image.")]
            public bool Bordered { get; set; }
            [Option('f', "font", Required = false, HelpText = "Font to use from directory list in ./Fonts")] 
            public string Font { get; set; }
            [Option('s', "scale", Required = false, HelpText = "Integer value at which to scale tiles.")]
            public int Scale { get; set; }
            [Option('l', "list", Required = false, HelpText = "Display a list of available fonts.")]
            public bool ListMode { get; set; }
            [Option('d', "destination", Required = false, HelpText = "Path to save image. If not provided, defaults to application directory.")]
            public string DestinationPath { get; set; }
            [Option('w', "word-wrap", Required = false, HelpText = "Word wrap at specified number of pixels on X coordinate.")]
            public int WordWrap { get; set; }
        }

        //Note: Main has to be static because it's the main entry point to the program! It needs to initialize non-static things to call non-static things.
        //Main is not non-static because there's nothing that runs before to initialize it.
        //So you could indeed make an instance of the Program Class in here and then do all the heavy lifting in the non-static thing.
        public bool Verbose { get; set; }
        public int Scale { set; get;}
        public bool Bordered { set; get; }
        public string FontDirectory { get; set; }
        public string FontConfigPath { get; set; }
        public string DestinationPath { get; set; }
        public int WordWrap { set; get; }
        public string Text { set; get; }
        readonly static string fontsPath = AppDomain.CurrentDomain.BaseDirectory + "Fonts\\";
        static void Main(string[] args)
        {
            //initialize font lists & update config
#if DEBUG
            //args = new [] { "-f", "test", "-v" };
#endif
            MainConfig conf = new MainConfig();
            Program textile = new Program();

            if (args.Length == 0) { Console.WriteLine("No text specified."); return; }
            textile.Text = args[0];

        Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.ListMode)
                       {
                           //list contents of Font directory.
                           ListFontsToConsole();
                           return;
                       }


                       if (o.Font == null)
                       {
                           Console.WriteLine("No font specified.");
                           return;
                       }
                       //validate that the font exists first
                       if (!IsValidFont(o.Font.Trim()))
                       {
                           Console.WriteLine("Invalid font specified.");
                           //could go into detail on failure reasons here, esp. due to invalid config IF config validation is added.
                           return;
                       }
                       if (o.Verbose) { Console.WriteLine("Valid font specified."); }
                       textile.FontDirectory = fontsPath + o.Font.Trim();
                       textile.FontConfigPath = textile.FontDirectory + "\\font.xml";

                       if(o.Scale == 0)
                       {
                           textile.Scale = 1;
                       }
                       else
                       {
                           textile.Scale = o.Scale;
                       }

                       if(o.DestinationPath == null)
                       {
                           textile.DestinationPath = AppDomain.CurrentDomain.BaseDirectory + "output.png";
                       }
                       else
                       {
                           Console.WriteLine("Destination path not yet handled!");
                           return;
                       }

                       textile.WordWrap = o.WordWrap;   //defaults to 0 if not provided.
                       textile.Bordered = o.Bordered;
                       textile.Verbose = o.Verbose;
                   });
            //after settings of the program are populated, it can be called to do real work.
            //at this point if no image text was provided, it will be the first option arg... -v seems apt. OK!
            textile.GenerateImage();
        }
        public void GenerateImage()
        {
            //search out pictures in the font folder for each character in the string and align them on a new PNG.
            FontConfig theFont = new FontConfig(FontConfigPath);
            if (theFont.UpperCaseOnly) { this.Text = this.Text.ToUpper(); }
            for(int i = 0; i < this.Text.Length; i++)
            {
                System.Drawing.Image;
            }

        }

        #region CLI
        public static void ListFontsToConsole()
        { 
            List<string> validFonts = ListFonts();
            foreach (string item in validFonts)
            {
                Console.WriteLine(item.Substring(item.LastIndexOf('\\')+1));
            }
            return;
        }

        public static bool IsValidFont(string fontName)
        {
            string checkDirectory = Program.fontsPath + fontName;
            List<string> validFonts = ListFonts();
            foreach (string dir in validFonts)
            {
                if (checkDirectory == dir)
                {
                    return true;
                }
            }
            //could validate font config in here...
            return false;
        }
        public static List<string> ListFonts()
        {
            //return list of paths to each font directory
            if (!Directory.Exists(fontsPath))
            {
                Directory.CreateDirectory(fontsPath);
            }
            string[] subDirs = Directory.GetDirectories(fontsPath);
            List<string> validFonts = new List<string>();
            foreach (string dir in subDirs)
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (Path.GetFileName(file) == "font.xml")
                    {
                        validFonts.Add(dir);
                    }
                }
            }
            return validFonts;
        }
        #endregion
        public class FontConfig
        {
            public bool UpperCaseOnly { set; get; }
            public string ConfigPath { get; set; }
            public FontConfig(string configPath)
            {
                ConfigPath = configPath;
                InitializeConfig();
            }
            public void InitializeConfig()
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateNewConfig();
                }
                XmlDocument config = new XmlDocument();
                config.Load(ConfigPath);
                XmlNode root = config.LastChild;

                UpperCaseOnly = bool.Parse(root.SelectSingleNode(Contents.UpperCaseOnly).InnerText);

                return;
            }
            private void CreateNewConfig()
            {
                ConfigPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
                XmlWriterSettings set = new XmlWriterSettings
                {
                    Indent = true
                };
                XmlWriter foo = XmlWriter.Create(ConfigPath, set);
                foo.WriteStartDocument();
                foo.WriteStartElement("config");    //root element
                foo.WriteElementString(Contents.Comment, Contents.HeaderComment);
                foo.WriteElementString(Contents.UpperCaseOnly, "true");
                foo.Close();
            }

            public static class Contents
            {
                //contents of this class control the config generation and all references to the nodes.
                public const string UpperCaseOnly = "UpperCaseOnly";
                public const string Comment = "Comment";

                public const string DefaultFont = "";

                public const string HeaderComment =
                    "\n" +
                    "Font Config" +
                    "\n";
            }
        }
        public class MainConfig
        {
            private string MostRecentFont { get; set; }
            public string ConfigPath { get; set; }
            public MainConfig()
            {
                ConfigPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
                InitializeConfig();
            }
            public void InitializeConfig()
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateNewConfig();
                }
                XmlDocument config = new XmlDocument();
                config.Load(ConfigPath);
                XmlNode root = config.LastChild;

                MostRecentFont = root.SelectSingleNode(Contents.MostRecentlyUsedFont).InnerText;

                return;
            }
            private void CreateNewConfig()
            {
                ConfigPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
                XmlWriterSettings set = new XmlWriterSettings
                {
                    Indent = true
                };
                XmlWriter foo = XmlWriter.Create(ConfigPath, set);
                foo.WriteStartDocument();
                foo.WriteStartElement("config");    //root element
                foo.WriteElementString(Contents.Comment, Contents.HeaderComment);
                foo.WriteElementString(Contents.MostRecentlyUsedFont, Contents.DefaultFont);
                foo.Close();
            }
            public static class Contents
            {
                //contents of this class control the config generation and all references to the nodes.
                public const string MostRecentlyUsedFont = "MostRecentFont";
                public const string Comment = "Comment";

                public const string DefaultFont = "";

                public const string HeaderComment =
                    "\n" +
                    "Stores the most recently used font." +
                    "\n";
            }
        }
    }
}
