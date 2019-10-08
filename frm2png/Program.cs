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

            if (!File.Exists(args[0]))
                Console.WriteLine($"{args[0]} is not a valid file.");
            if(args.Length == 1)
                Convert(args[0], null);
            else 
                Convert(args[0], args[1]);
        }

        static void Convert(string input, string outputDir)
        {   
            if (outputDir == null)
                outputDir = Path.GetDirectoryName(input);

            var bmp = FalloutFRMLoader.Load(File.ReadAllBytes(input));
            var c = new Bitmap(bmp[0]);
            var filename = Path.GetFileNameWithoutExtension(input);
            var outpath = outputDir + "\\" + filename + ".png";
            Console.WriteLine($"Saving file to {outpath}");
            c.Save(outpath, ImageFormat.Png);
            Console.WriteLine($"Saved {outpath}");
        }
    }
}
