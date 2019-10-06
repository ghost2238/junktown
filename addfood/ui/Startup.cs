using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;

namespace Addfood_ui
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        string html;
        byte[] font;
        string mysql;

        class Menymat
        {
            public int Ar;
            public int Vecka;
            public string Mat;
            public int Veckodag;
        }

        private IEnumerable<Menymat> HamtaMat() {
            var conn = new MySqlConnection(mysql);
            conn.Open();
            var cmd = new MySqlCommand(@"SELECT Ar, Vecka, Mat, Veckodag FROM mat
                                         LEFT JOIN menyer ON mat.MenyFK = menyer.MenyId
                                         ORDER BY Ar, Vecka DESC, Veckodag ASC", conn);
            var r = cmd.ExecuteReader();
            while (r.Read())
            {
                yield return new Menymat()
                {
                    Ar = r.GetInt16(0),
                    Vecka = r.GetInt32(1),
                    Mat = r.GetString(2),
                    Veckodag = r.GetInt16(3)
                };
            }
            r.Close();
        }


        string[] dagar = new string[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag" };
        public string Html()
        {
            var sb = new StringBuilder();
            var sb2 = new StringBuilder();
            sb2.Append($"<table id='t' style='display: none;'>");
            foreach (var veckomeny in HamtaMat().ToList().GroupBy(x => x.Ar+"_"+x.Vecka))
            {
                var s = veckomeny.ToList();
                var spl = veckomeny.Key.Split("_");
                sb.Append($"<h2>VECKA {spl[1]}</h2>");

                
                foreach (var dag in veckomeny.ToList().GroupBy(x => x.Veckodag))
                {
                    var idag = ((int)DateTime.Now.DayOfWeek-1 == dag.Key) ? " idag" :"";
                    var vecka = dag.Key == 0 ? $"{spl[0]}, v{spl[1]}" : "";
                    sb2.Append($"<tr class='{idag}'><td>{vecka}</td><td>{dagar[dag.Key]}</td><td>{string.Join("<br/>", dag.ToList().OrderBy(x => x.Mat).ToList().Select(x => x.Mat))}</td></tr>");
                    sb.Append($@"<div class='box{idag}'>
                                <h1>{dagar[dag.Key]}</h1>
                                <ul>");
                   foreach(var ratt in dag.ToList().OrderBy(x => x.Mat).ToList())
                        sb.Append($"<li>{ratt.Mat}</li>");
                    sb.Append("</ul></div>");
                }
                sb.Append($"<div style='clear: left;'></div>");
                sb.Append("<hr />");
                
            }
            sb2.Append("</table>");
            html = File.ReadAllText("./index.html");
            return html.Replace("[BOXAR]", sb.ToString()).Replace("[TABELL]", sb2.ToString());
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            foreach (var l in File.ReadAllLines("./config.cfg"))
            {
                var lt = l.Trim();
                if (lt.Substring(0, 4) == "sql=")
                    mysql = lt.Substring(4).Replace("\"", "");
            }

            font = File.ReadAllBytes("./ibm_vga8.woff2");
            html = File.ReadAllText("./index.html");

            app.Use(async (context, next) =>
            {
                try
                {
                    if(context.Request.Path == "/")
                    {
                        var html = Html();
                        var b = Encoding.UTF8.GetBytes(html);
                        context.Response.ContentType = "text/html; charset=utf-8";
                        context.Response.ContentLength = b.Length;
                        await context.Response.Body.WriteAsync(b, 0, b.Length);
                        return;
                    }

                    if(context.Request.Path == "/ibm_vga8.woff2")
                    {
                        context.Response.ContentType = "font/woff2";
                        context.Response.ContentLength = font.Length;
                        await context.Response.Body.WriteAsync(font, 0, font.Length);
                        return;
                    }

                    await next.Invoke();
                }
                catch (Exception){ }
            });
        }
    }
}
