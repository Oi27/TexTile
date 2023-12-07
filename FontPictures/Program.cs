using System;
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
        }

        //Note: Main has to be static because it's the main entry point to the program! It needs to initialize non-static things to call non-static things.
        //Main is not non-static because there's nothing that runs before to initialize it.
        //So you could indeed make an instance of the Program Class in here and then do all the heavy lifting in the non-static thing.
        public bool Verbose { get; set; }
        static void Main(string[] args)
        {
            //initialize font lists & update config
#if DEBUG
            args = new [] { "-l" };
#endif
            Config conf = new Config();
            Program textile = new Program();

        Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.ListMode)
                       {
                           //list contents of Font directory.
                           ListFontsToConsole();
                       }
                       textile.Verbose = o.Verbose;
                   });
            //after settings of the program are populated, it can be called to do real work.
            textile.RealWork();
        }
        public void RealWork()
        {

        }

        public static void ListFontsToConsole()
        {
            string fontsPath = AppDomain.CurrentDomain.BaseDirectory + "\\Fonts";
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
                    if(Path.GetFileName(file) == "font.xml")
                    {
                        validFonts.Add(dir);
                    }
                }
            }
            foreach (string item in subDirs)
            {
                Console.WriteLine(item.Substring(item.LastIndexOf('\\')+1));
            }
            return;
        }
        public class Config
        {
            private string MostRecentFont { get; set; }
            public string ConfigPath { get; set; }
            public Config()
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

                MostRecentFont = root.SelectSingleNode(ConfigContents.MostRecentlyUsedFont).InnerText;

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
                foo.WriteElementString(ConfigContents.Comment, ConfigContents.HeaderComment);
                foo.WriteElementString(ConfigContents.MostRecentlyUsedFont, ConfigContents.DefaultFont);
                foo.Close();
            }
        }
        public static class ConfigContents
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
