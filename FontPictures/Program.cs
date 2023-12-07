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

        static void Main(string[] args)
        {
            //initialize font lists & update config

        Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.ListMode)
                       {
                           //list contents of 
                       }
                       if (o.Verbose)
                       {
                           Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Verbose}");
                           Console.WriteLine("Quick Start Example! App is in Verbose mode!" + o.Font);
                       }
                       else
                       {
                           Console.WriteLine($"Current Arguments: -v {o.Verbose}");
                           Console.WriteLine("Quick Start Example!");
                       }
                   });
        }
        public class Config
        {
            public string MostRecentFont { get; set; }
            public string ConfigPath { get; set; }
            public void InitializeConfig()
            {
                string configPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
                if (!File.Exists(configPath))
                {
                    CreateNewConfig();
                }
                XmlDocument config = new XmlDocument();
                config.Load(configPath);
                XmlNode root = config.LastChild;

                MostRecentFont = root.SelectSingleNode(ConfigContents.MostRecentlyUsedFont).InnerText;

                return;
            }
            public void CreateNewConfig()
            {
                string configPath = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
                XmlWriterSettings set = new XmlWriterSettings
                {
                    Indent = true
                };
                XmlWriter foo = XmlWriter.Create(configPath, set);
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
