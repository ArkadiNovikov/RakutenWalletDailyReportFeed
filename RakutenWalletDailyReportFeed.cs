using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using HtmlAgilityPack;
using System.Text;
using System.Xml;
using Microsoft.SyndicationFeed.Rss;
using System.Globalization;
using Microsoft.SyndicationFeed;
using System.Linq;

namespace RakutenMatsudaFeed
{
    public static class RakutenWalletDailyReportFeed
    {
        [FunctionName("RakutenMatsudaFeed")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            //log.LogInformation("C# HTTP trigger function processed a request.");

            //string name = req.Query["name"];
            //https://www.rakuten-wallet.co.jp/market/market-list/
            Uri source = new ("https://www.rakuten-wallet.co.jp/market/daily_report/");

            var web = new HtmlWeb();
            web.OverrideEncoding = System.Text.Encoding.UTF8;
            var htmlDoc = web.Load(source);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//article/ul/li");
            
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var feedWriter = new RssFeedWriter(xmlWriter);
                
                await feedWriter.WriteTitle("楽天ウォレット Daily Report");
                await feedWriter.WriteLanguage(CultureInfo.GetCultureInfo("ja-JP"));

                foreach (var n in nodes)
                {
                    HtmlNode htmlNode = n.SelectSingleNode(".//span");
                    var linkNode = n.SelectSingleNode("./a");
                    var titleNode = linkNode.SelectSingleNode("./div[@class='text']");
                    var dateNode = titleNode.SelectSingleNode("./span");

                    var absoluteLink = new Uri(source, linkNode.Attributes["href"].Value);

                    var item = new SyndicationItem()
                    {
                        Id = absoluteLink.ToString(),
                        Title = titleNode.InnerText.Trim(),
                        Published = DateTime.Parse(dateNode.InnerText)
                    };
                    item.AddLink(new SyndicationLink(absoluteLink));

                    await feedWriter.Write(item);
                }
                xmlWriter.Flush();
            }
            

            var result = new ContentResult();
            result.StatusCode = 200;
            result.ContentType = "application/xml";
            result.Content = sw.ToString();

            return result;
        }
    }

    class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this._encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}
