using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace url_lib
{
    interface IURLValidator
    {
        bool IsValid(string url);
        bool CanHandle(string host);
    }

   
    public static class HTTPChecker
    {
        private static HttpClient Client(bool allowRedirect)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.AllowAutoRedirect = allowRedirect;

            HttpClient client = new HttpClient(clientHandler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:69.0) Gecko/20100101 Firefox/69.0");
            return client;
        }
        public static async Task<HttpResponseHeaders> CheckResponseHeaders(string url, bool allowRedirect)
        {
            var t = await Client(allowRedirect).GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return t.Headers;
        }
    }

    public class ImgurValidator : IURLValidator
    {
        public bool CanHandle(string host)
            => host.EndsWith("imgur.com");

        public bool IsValid(string url)
        {
            var headers = HTTPChecker.CheckResponseHeaders(url, false).GetAwaiter().GetResult();
            if (headers.Location == null)
                return true;
            if (headers.Location.ToString().EndsWith("/removed.png"))
                return false;

            return true;
        }
    }
    public class URL
    {
        readonly string[] ImageExtensions = new string[] { "png", "jpg", "jpeg", "gif" };
        readonly string[] ImageHosters = new string[] { "imgur.com", "i.cubeupload.com", "imageshack.us", "imagebam.com",
            "photobucket.com", "photoshack.com", "imagebanana.com", "tinypic.com", "abload.de" };
        readonly string[] VideoHosters = new string[] { "youtube.com", "vimeo.com", "liveleak.com" };

        readonly IURLValidator[] validators = new IURLValidator[] { new ImgurValidator() };
        public URL(string Uri, string filename)
        {
            Filename = filename;
            Uri = Uri.Trim();
            if(Uri.Last() == ',')
            {
                try
                {
                    this.Uri = new Uri(Uri.Substring(0, Uri.Length-1));
                    return;
                }
                catch (Exception) {  }
            }
            try
            {
                this.Uri = new Uri(Uri);
            }
            catch (Exception) { }
            {
                this.ErrorUri = Uri;
            }
        }

        public bool IsVideoURL => VideoHosters.Any(x => this.Uri != null && this.Uri.Host.ToLower().EndsWith(x));
        public bool IsImage => ImageExtensions.Any(x => this.Uri != null && this.Uri.LocalPath.ToLower().EndsWith(x));
        public bool IsImageHoster => ImageHosters.Any(x => this.Uri != null && this.Uri.Host.ToLower().EndsWith(x));
        public bool IsFileExtension(string extension) => this.Uri != null && this.Uri.LocalPath.ToLower().EndsWith("." + extension);
        public bool IsValid()
        {
            var val = validators.FirstOrDefault(x => x.CanHandle(this.Uri.Host));
            return val.IsValid(this.Uri.ToString());
        }
        public bool OnDomain(string domain) => this.Uri.Host.ToLower().EndsWith(domain);

        public Uri Uri;
        private string ErrorUri;
        public string Filename;
        
        
        public override string ToString() => Uri == null ? $"INVALID_URL[{ErrorUri}]" : Uri.ToString();
    }

    public static class URLParser
    {
        public static List<URL> ParseFile(string filename)
            => Parse(filename, File.ReadAllText(filename));

        public static List<URL> ParseDirectory(string path, string pattern, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            var foiles = Directory.GetFiles(path, pattern, option);
            return foiles.SelectMany(x => ParseFile(x)).ToList();
        }

        public static List<URL> ParseStream(string filename, FileStream stream)
        {
            var urls = new List<URL>();
            Memory<byte> buffer=null;
            stream.ReadAsync(buffer);
            return urls;
        }
        public static List<URL> Parse(string filename, string data)
        {
            var urls = new List<URL>();
            foreach(Match m in new Regex("http://(.+?)[ \r\n]").Matches(data))
                urls.Add(new URL(m.Captures[0].ToString(), filename));
            foreach (Match m in new Regex("https://(.+?)[ \r\n]").Matches(data))
                urls.Add(new URL(m.Captures[0].ToString(), filename));

            return urls.DistinctBy(x => x.ToString()).ToList();
        }
    }
}

static public class Extensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
    {
        return source.DistinctBy(keySelector, null);
    }
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
                Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return _(); IEnumerable<TSource> _()
        {
            var knownKeys = new HashSet<TKey>(comparer);
            foreach (var element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
            }
        }
    }
}