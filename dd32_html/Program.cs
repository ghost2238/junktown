using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dd32_html
{
    public class Address
    {
        public bool manual;
        public string module;
        public string address;
        public int offset;
        public string text;
    }

    class Program
    {
        static IEnumerable<string> CreateRows(IEnumerable<Address> adr)
        {
            foreach (var a in adr)
                yield return $"<tr><td>{("0x"+a.offset.ToString("x"))}</td><td>{a.text}</td></tr>";
        }

        static void Main(string[] args)
        {
            var p = JObject.Parse(File.ReadAllText(args[0])).SelectToken("labels").ToString();
            var values = JsonConvert.DeserializeObject<List<Address>>(p);
            foreach (var v in values.Where(x => x.module == "fallout2.exe"))
                v.offset = Convert.ToInt32(v.address, 16)+0x400000;


            var template = File.ReadAllText($"template.html");
            var funcs = values.Where(x => x.module == "fallout2.exe" && x.offset > 0x00410000 && x.offset < 0x00500000);
            var vars = values.Where(x => x.module == "fallout2.exe" && x.offset > 0x00500000);

            File.WriteAllText("func.html", template.Replace("%ROWS%", string.Join("\n", CreateRows(funcs).ToList())));
            File.WriteAllText("vars.html", template.Replace("%ROWS%", string.Join("\n", CreateRows(vars).ToList())));
        }
    }
}
