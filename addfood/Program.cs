using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace addfood
{
    enum Veckodag
    {
        Mandag,
        Tisdag,
        Onsdag,
        Torsdag,
        Fredag
    }

    class Meny
    {
        public int Vecka;
        public Dictionary<Veckodag, List<string>> Mat = new Dictionary<Veckodag, List<string>>();
    }

    class Program
    {
        public static string sql;
        static void Main(string[] args)
        {
            AsyncMain().Wait();
        }

        static async Task AsyncMain()
        {
            foreach (var l in File.ReadAllLines("./config.cfg"))
            {
                var lt = l.Trim();
                if (lt.Substring(0, 6) == "mysql=")
                {
                    sql = lt.Substring(6).Replace("\"", "");
                }
            }
            var meny = await ParsaHTML();
            if(meny != null)
                Spara(meny);
        }

        static async Task<string> HamtaHTML()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:69.0) Gecko/20100101 Firefox/69.0");
            var t = await client.GetAsync("http://www.addfood.se/lunch/", HttpCompletionOption.ResponseContentRead);
            var html = await t.Content.ReadAsStringAsync();
            return html;
        }

        static void Spara(Meny meny)
        {
            var conn = new MySqlConnection(sql);
            conn.Open();
            var cmd = new MySqlCommand($"SELECT COUNT(*) FROM menyer WHERE Ar = {DateTime.Now.Year} AND Vecka = {meny.Vecka}", conn);
            var r = cmd.ExecuteReader();
            r.Read();
            if (r.GetInt32(0) != 0)
                return;
            r.Close();
            using (var trans = conn.BeginTransaction())
            {
                cmd = new MySqlCommand($"INSERT INTO menyer (MenyId, Ar, Vecka) VALUES (null, @y, @v)", conn);
                cmd.Parameters.Add(new MySqlParameter("y", DateTime.Now.Year));
                cmd.Parameters.Add(new MySqlParameter("v", meny.Vecka));
                cmd.Transaction = trans;
                cmd.ExecuteNonQuery();
                var id = cmd.LastInsertedId;

                foreach (var mat in meny.Mat)
                {
                    foreach (var matval in mat.Value)
                    {
                        cmd = new MySqlCommand($"INSERT INTO mat (MatId, MenyFK, Veckodag, Mat) VALUES (null, @id, @veckodag, @mat)", conn);
                        cmd.Parameters.Add(new MySqlParameter("id", id));
                        cmd.Parameters.Add(new MySqlParameter("mat", matval));
                        cmd.Parameters.Add(new MySqlParameter("veckodag", (int)mat.Key));
                        cmd.Transaction = trans;
                        cmd.ExecuteNonQuery();
                    }
                }

                trans.Commit();
            }
            conn.Close();
        }

        static async Task<Meny> ParsaHTML()
        {
            var html = await HamtaHTML();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            int vecka;
            try
            {
                var n = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/section/div/div/div[1]/div/div[1]/div[1]/div/div/div[1]/h4");
                vecka = int.Parse(string.Join("", n.InnerText.Where(x => char.IsDigit(x))));
            }
            catch (Exception ex)
            {
                File.AppendAllText("./error.log", "Kunde inte parsa vecka: " + ex.StackTrace);
                File.WriteAllText("./addfood.html", html);
                return null;
            }
            var wrappers = ClassQuery(doc.DocumentNode, "wpb_column vc_column_container vc_col-sm-4");
            var meny = new Meny() { Vecka = vecka };
            try
            {
                meny.Mat[Veckodag.Mandag] = ParsaMat(wrappers[1], "MÅNDAG").ToList();
                meny.Mat[Veckodag.Tisdag] = ParsaMat(wrappers[2], "TISDAG").ToList();
                meny.Mat[Veckodag.Onsdag] = ParsaMat(wrappers[3], "ONSDAG").ToList();
                meny.Mat[Veckodag.Torsdag] = ParsaMat(wrappers[4], "TORSDAG").ToList();
                meny.Mat[Veckodag.Fredag] = ParsaMat(wrappers[5], "FREDAG").ToList();
            }
            catch (Exception ex)
            {
                File.AppendAllText("./error.log", "Kunde inte parsa mat: " + ex.StackTrace);
                File.WriteAllText("./addfood.html", html);
                return null;
            }

            return meny;
        }

        static IEnumerable<string> ParsaMat(HtmlNode n, string dag)
        {
            var text = InnerTextKoll(n, dag);
            var rader = text.Split('\n');
            foreach (var r in rader)
            {
                var t = WebUtility.HtmlDecode(r).Trim();
                if (t == dag)
                    continue;
                if (t.Length < 5)
                    continue;
                yield return t;
            }
        }

        static string InnerTextKoll(HtmlNode node, string text)
        {
            var inner = node.InnerText;
            if (!inner.Contains(text))
                new Exception("Noden innehåller inte " + text);
            return inner;
        }

        static List<HtmlNode> ClassQuery(HtmlNode n, string cls)
            => n.Descendants().Where(x => x.Attributes.Contains("class") && cls == x.Attributes["class"].Value).ToList();
    }
}
