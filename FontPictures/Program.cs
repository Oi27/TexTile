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
using SkiaSharp;

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
            args = new [] { "@BAD DAB ", "-f", "test", "-s", "8", "-l" };
#endif
            if (!Directory.Exists(fontsPath)) { Directory.CreateDirectory(fontsPath); }
            Program.CreateFontConfig(fontsPath + "font.xml");
            MainConfig conf = new MainConfig();
            Program textile = new Program();

            if (args.Length == 0) { Console.WriteLine("No text specified."); return; }
            textile.Text = args[0];

            bool terminateDueToCommandArgs = false;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.ListMode)
                       {
                           //list contents of Font directory.
                           ListFontsToConsole();
                           ListSpecialCharacters();
                           terminateDueToCommandArgs = true;
                           return;
                       }


                       if (o.Font == null)
                       {
                           Console.WriteLine("No font specified. Use -f or --font as an argument.");
                           terminateDueToCommandArgs = true;
                           return;
                       }
                       //validate that the font exists first
                       if (!IsValidFont(o.Font.Trim()))
                       {
                           Console.WriteLine("Invalid font specified.");
                           //could go into detail on failure reasons here, esp. due to invalid config IF config validation is added.
                           terminateDueToCommandArgs = true;
                           return;
                       }
                       if (o.Verbose) { Console.WriteLine("Valid font specified."); }

                       textile.FontDirectory = fontsPath + o.Font.Trim();
                       textile.FontConfigPath = textile.FontDirectory + "\\font.xml";

                       if(o.Scale < 0)
                       {
                           Console.WriteLine("Scale cannot be negative!");
                           terminateDueToCommandArgs = true;
                           return;
                       }
                       if (o.Scale == 0)
                       {
                           textile.Scale = 1;
                       }
                       else
                       {
                           textile.Scale = o.Scale;
                       }

                       if(o.DestinationPath == null)
                       {
                           textile.DestinationPath = AppDomain.CurrentDomain.BaseDirectory + textile.Text + ".png";
                       }
                       else
                       {
                           Console.WriteLine("Destination path not yet handled!");
                           terminateDueToCommandArgs = true;
                           return;
                       }

                       textile.WordWrap = o.WordWrap;   //defaults to 0 if not provided.
                       textile.Bordered = o.Bordered;
                       textile.Verbose = o.Verbose;
                   });
            //after settings of the program are populated, it can be called to do real work.
            //at this point if no image text was provided, it will be the first option arg... -v seems apt. OK!
            if (terminateDueToCommandArgs) { return; }
            textile.GenerateImage();
        }
        public void GenerateImage()
        {
            //search out pictures in the font folder for each character in the string and align them on a new PNG.
            //all the images need to be the same vertical height or it will mess things up...
            //maybe word wrap will chop it up and rearrange a very wide normal image.
            FontConfig theFont = new FontConfig(FontConfigPath);
            if (theFont.UpperCaseOnly) { this.Text = this.Text.ToUpper(); }
            int maxWidth = 0;
            int runningWidth = 0;
            int runningHeight = 0;
            List<SKImage> overlays = new List<SKImage>();
            bool containsSpecialCharacters = false;
            for (int i = 0; i < this.Text.Length; i++)
            {
                string lookfor = this.Text[i].ToString();
                string cmp = lookfor;
                lookfor = AdjustSpecialCharacters(lookfor);
                if(lookfor != cmp) { containsSpecialCharacters = true; }
                string match = null;
                foreach (string item in Directory.GetFiles(this.FontDirectory))
                {
                    if (Path.GetFileNameWithoutExtension(item) == lookfor)
                    {
                        match = item;
                        break;
                    }
                }
                if (match == null)
                {
                    Console.WriteLine("Character not found! Missing \"" + lookfor + "\" in fonts folder.");
                    Console.WriteLine("Aborting image generation.");
                    return;
                }
                SKImage letter = SKImage.FromEncodedData(match);
                maxWidth += letter.Width + theFont.FontOffset;
                runningWidth += letter.Width + theFont.FontOffset;
                if (runningHeight == 0) { runningHeight = letter.Height; }
                overlays.Add((letter));
            }
            int maxHeight = runningHeight;
            //add a positive font offset to the end if the parameter was negative (does this always apply?)
            if (theFont.FontOffset < 0) { maxWidth += Math.Abs(theFont.FontOffset); }
            //we have the final dimensions of the bitmap and a list of the characters ready to go.
            //draw loop does not need to do word wrapping yet. draw them in a single row.
            SKBitmap finalImage = new SKBitmap(maxWidth, runningHeight);
            SKSurface surface = SKSurface.Create(finalImage.Info);
            SKCanvas canvas = surface.Canvas;

            //it would be nice to fill the canvas with a background color that suits the letters.
            //Color pick the first pixel of the first tile?
            SKColor fillColor = SKBitmap.FromImage(overlays[0]).GetPixel(0, 0);
            using (SKPaint paint = new SKPaint(){Color = fillColor})
            {
                canvas.DrawRect(0, 0, finalImage.Width, finalImage.Height, paint);
            };
            runningWidth = 0;
            runningHeight = 0;
            foreach (SKImage letter in overlays)
            {
                SKBitmap bitmapLetter = SKBitmap.FromImage(letter);
                canvas.DrawBitmap(bitmapLetter, runningWidth, runningHeight);
                runningWidth += letter.Width + theFont.FontOffset;
            }
            //if bordered, the new size can be calculated & we can drop this whole assembly on top

            //code here

            //Scaling is the last thing to take care of:
            string fallbackPath = AppDomain.CurrentDomain.BaseDirectory + "output.png";
            SKImageInfo resizeInfo = new SKImageInfo(maxWidth*this.Scale, maxHeight*this.Scale);
            using (SKBitmap srcBitmap = SKBitmap.FromImage(surface.Snapshot()))
            using (SKBitmap resizedSKBitmap = srcBitmap.Resize(resizeInfo, SKFilterQuality.None))
            using (SKImage newImg = SKImage.FromBitmap(resizedSKBitmap))
            using (SKData data = newImg.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream backupStream = File.OpenWrite(fallbackPath))
            {
                //IF TO HANDLE IF THE DESTINATION PATH HAS INVALID CHARACTERS IN IT (?,$, ETC)
                if (containsSpecialCharacters)
                {
                    Console.WriteLine("Query text contains invalid file path characters. Defaulting to \".\\output.png\"");
                    data.SaveTo(backupStream);
                    Console.WriteLine("Result saved to " + fallbackPath);
                }
                else
                {
                    FileStream stream = File.OpenWrite(DestinationPath);
                    data.SaveTo(stream);
                    stream.Dispose();
                    Console.WriteLine("Result saved to " + DestinationPath);
                }
                
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
        public static void CreateFontConfig(string path)
        {
            //initialize a default config that can be copied into new font folders by the user.
            FontConfig A = new FontConfig(path);
            return;
        }
        public static string AdjustSpecialCharacters(string queryCharacter)
        {
            string lookfor = queryCharacter;
            switch (lookfor)
            {
                case " ":
                    lookfor = "space";
                    break;
                case ".":
                    lookfor = "period";
                    break;
                case ",":
                    lookfor = "comma";
                    break;
                case "!":
                    lookfor = "exclamation";
                    break;
                case "?":
                    lookfor = "question";
                    break;
                case "<":
                    lookfor = "less";
                    break;
                case ">":
                    lookfor = "greater";
                    break;
                case ":":
                    lookfor = "colon";
                    break;
                case ";":
                    lookfor = "semicolon";
                    break;
                case "\"":
                    lookfor = "doublequote";
                    break;
                case "\'":
                    lookfor = "singlequote";
                    break;
                case "*":
                    lookfor = "asterisk";
                    break;
                case "/":
                    lookfor = "fwdslash";
                    break;
                case "\\":
                    lookfor = "backslash";
                    break;
                case "|":
                    lookfor = "vertical";
                    break;
                default:
                    break;
            }
            return lookfor;
        }
        public static void ListSpecialCharacters()
        {
            const string specials =
                " .,!?<>:;\"\'*/\\|";
            Console.WriteLine("----------------\nImage Names for Special Characters:");
            foreach (char item in specials)
            {
                Console.WriteLine(item.ToString() + " --- " + AdjustSpecialCharacters(item.ToString()));
            }
            Console.WriteLine("----------------");
            return;
        }

        #endregion
        public class FontConfig
        {
            //i think this could be a static class and it would work out...
            public bool UpperCaseOnly { set; get; }
            public string ConfigPath { get; set; }
            public int FontOffset { set; get; }
            public FontConfig(string configPath)
            {
                ConfigPath = configPath;
                InitializeConfig();
            }
            private void InitializeConfig()
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateNewConfig();
                }
                XmlDocument config = new XmlDocument();
                config.Load(ConfigPath);
                XmlNode root = config.LastChild;

                UpperCaseOnly = bool.Parse(root.SelectSingleNode(nameof(Contents.UpperCaseOnly)).InnerText);
                FontOffset = int.Parse(root.SelectSingleNode(Contents.FontOffset).InnerText);

                return;
            }
            private void CreateNewConfig()
            {
                XmlWriterSettings set = new XmlWriterSettings
                {
                    Indent = true
                };
                XmlWriter foo = XmlWriter.Create(ConfigPath, set);
                foo.WriteStartDocument();
                foo.WriteStartElement("config");    //root element
                foo.WriteElementString(Contents.Comment, Contents.HeaderComment);
                foo.WriteElementString(nameof(Contents.UpperCaseOnly), Contents.UpperCaseOnly);
                foo.WriteElementString(Contents.FontOffset, Contents.DefaultFontOffset.ToString());
                foo.Close();
            }

            public static class Contents
            {
                //contents of this class control the config generation and all references to the nodes.
                //not sure if it's better to have nameof() or just contents.uppercaseonly when referencing nodes of the config...
                //nameof() improves readability of this class, while having duplicate strings in the config makes it more readable elsewhere.
                public const string UpperCaseOnly = "true";
                public const string FontOffset = "FontOffset";
                public const int DefaultFontOffset = -1;
                public const string Comment = "Comment";

                public const string DefaultFont = "";

                public const string HeaderComment =
                    "\n" +
                    "Copy this file into any font folders and customize it.\n" +
                    "UppercaseOnly will convert queries to uppercase before checking: tiles are case sensitive.\n" +
                    "FontOffset is an X offset to apply as tiles are drawn to the output file. Useful to have single-px character separation." +
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
