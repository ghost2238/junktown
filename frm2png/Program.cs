using FOCommon.Graphic;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace frm2png
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("frm2png.exe <src> <dst>");
                return;
            }

            string dst = null;

            if (args.Length == 2)
                dst = args[1];


            if (Directory.Exists(args[0]))
            {
                foreach(var c in Directory.GetFiles(args[0]))
                {
                    if (Path.GetExtension(c.ToLower()) == ".frm")
                        Convert(c, dst);
                }
                Environment.Exit(0);
            }

            if (!File.Exists(args[0]))
                Console.WriteLine($"{args[0]} is not a valid file.");
            Convert(args[0], dst);
        }

        static void Convert(string input, string outputDir)
        {   
            if (outputDir == null)
                outputDir = Path.GetDirectoryName(input);


            var bmp = FalloutFRMLoader.Load(File.ReadAllBytes(input));
            var c = new Bitmap(bmp[0]);
            c.MakeTransparent(Color.FromArgb(11, 0, 11));
            var filename = Path.GetFileNameWithoutExtension(input);
            var outpath = outputDir + "\\" + filename + ".png";
            Console.WriteLine($"Saving file to {outpath}");
            c.Save(outpath, ImageFormat.Png);
            Console.WriteLine($"Saved {outpath}");
        }
    }
}
