using System;
using System.Linq;
using System.Threading;
using url_lib;

namespace cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var URLs = URLParser.ParseFile(@"");
            //foreach(var url in URLs.Where(x => x.IsImage && !x.IsImageHoster).OrderBy(x => x.ToString()))
            foreach (var url in URLs.Where(x => !x.IsVideoURL && !x.IsImageHoster).OrderBy(x => x.ToString()))
                Console.WriteLine(url);
            Console.ReadKey();
        }
    }
}
