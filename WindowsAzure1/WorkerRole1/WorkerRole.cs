using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Xml;
using System.Globalization;
using HtmlAgilityPack;
using System.Web;

namespace WorkerRole1
{

    public class WorkerRole : RoleEntryPoint
    {
        private Boolean stop = true;
        private HashSet<string> disallow = new HashSet<string>();
        private HashSet<string> visited = new HashSet<string>();
        private Queue<string> lastTenUrl = new Queue<string>();
        private List<string> errorUrl = new List<string>();
        private DateTime today = DateTime.Now;
        private CloudTable table;
        private CloudTable dash;
        private CloudQueue urlq;
        private CloudQueue commandq;
        private int totalUrlCrawl = 0;
        private int qSize = 0;
        private int indexSize = 0;
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole1 entry point called");
            cloudInitialization();
            
            while (true)
            {
                urlq.FetchAttributes();
                qSize = (int)urlq.ApproximateMessageCount;
                getCommandMsg();
                if (!stop)
                {
                    getUrlMsg();
                }
                updateDash();
                Thread.Sleep(500);
                Trace.TraceInformation("Working");
            }
        }

        private void cloudInitialization()
        {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(ConfigurationManager.AppSettings["lecture8"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("pa3Table");
            table.CreateIfNotExists();
            dash = tableClient.GetTableReference("dashboard");
            dash.CreateIfNotExists();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            commandq = queueClient.GetQueueReference("command");
            commandq.CreateIfNotExists();
            urlq = queueClient.GetQueueReference("url");
            urlq.CreateIfNotExists();
            cpuCounter.NextValue();

            updateDash();
        }

        private double getCPU()
        {
            return cpuCounter.NextValue();
        }

        private double getRAM()
        {
            return ramCounter.NextValue();
        }

        private void updateDash()
        {
            DashboardStats dashInfo = new DashboardStats("worker1", getCPU(), getRAM(), 
                    lastTenUrl.ToArray().ToString(), totalUrlCrawl,
                    qSize, indexSize, errorUrl.ToArray().ToString());
            TableOperation insertOperation = TableOperation.InsertOrReplace(dashInfo);
            dash.Execute(insertOperation);
        }

        private void getUrlMsg()
        {
            CloudQueueMessage url = urlq.GetMessage();
            if (url != null)
            {
                urlq.DeleteMessage(url);
                try
                {
                    totalUrlCrawl++;
                    WebClient wClient = new WebClient();
                    string htmlString = wClient.DownloadString(url.AsString);
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlString);
                    getTitle(htmlDoc, url.AsString);
                    downloadSubUrl(htmlDoc, url.AsString);
                }
                catch (Exception EX)
                {
                    errorUrl.Add(EX + ": " + url.AsString);
                }
            }  
            
        }

        private void getCommandMsg()
        {
            CloudQueueMessage message = commandq.GetMessage();
            if (message != null)
            {
                commandq.DeleteMessage(message);
                string m = message.AsString;
                if (m.Equals("stop"))
                {
                    stop = true;
                }
                else if (m.Equals("clear"))
                {
                    urlq.Clear();
                    table.Delete();

                    disallow = new HashSet<string>();
                    visited = new HashSet<string>();
                    lastTenUrl = new Queue<string>();
                    errorUrl = new List<string>();
                    totalUrlCrawl = 0;
                    qSize = 0;
                    indexSize = 0;
                    ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    updateDash();
                }
                else
                {
                    stop = false;
                    getRobots(m);
                }
            }
        }

        private void updateLastTenUrl(string url)
        {
            if (lastTenUrl.Count > 9)
            {
                lastTenUrl.Dequeue();
            }
            lastTenUrl.Enqueue(url);
        }

        private void getTitle(HtmlDocument htmlDoc, string url)
        {
            var metas = htmlDoc.DocumentNode.Descendants("meta");
            string date = "";
            foreach (var metaTag in metas)
            {
                if (metaTag.Attributes.Contains("http-equiv") && 
                    metaTag.Attributes["http-equiv"].Value == "last-modified")
                {
                    date = metaTag.Attributes["content"].Value;
                    break;
                }
            }  
            
            if (date != "" && compareDate(date))
            {
                var title = htmlDoc.DocumentNode.SelectSingleNode("//title").InnerText;
                Uri uriAddress = new Uri(url);
                string category = uriAddress.Segments[1].Trim(new char[] {'/'}).ToLower();

                var urlencode = HttpUtility.UrlEncode(url);
                UrlInformation urlInfo = new UrlInformation(category, urlencode, title, date);
                TableOperation insertOperation = TableOperation.Insert(urlInfo);
                table.Execute(insertOperation);
                indexSize++;
                updateLastTenUrl(url);

                updateDash();
            }
        }

        // if valid, add to urlq
        private void downloadSubUrl(HtmlDocument htmlDoc, string url)
        {
            var hrefs = htmlDoc.DocumentNode.Descendants("a");
            string href = "";
            foreach (var aTag in hrefs)
            {
                href = aTag.Attributes["href"].Value;
                if (href.EndsWith("html") || href.EndsWith("htm"))
                {
                    if (!href.StartsWith("http://"))
                    {
                        if (url.Contains("www.cnn"))
                        {
                            href = "http://www.cnn.com" + href;
                        }
                        else
                        {
                            href = "http://sportsillustrated.cnn.com" + href;
                        }
                        string[] tokens = url.Split(new string[] { "/" }, StringSplitOptions.None);
                        if (!disallow.Contains(tokens[3]) && !visited.Contains(href))
                        {
                            visited.Add(href);
                            CloudQueueMessage m = new CloudQueueMessage(href);
                            urlq.AddMessage(m);
                        }
                    }
                    else
                    {
                        if (href.StartsWith("http://www.cnn.com") || href.StartsWith("http://www.sportsillustrated.cnn.com"))
                        {
                            string[] tokens = href.Split(new string[] { "/" }, StringSplitOptions.None);
                            if (!disallow.Contains(tokens[3]) && !visited.Contains(href))
                            {
                                visited.Add(href);
                                CloudQueueMessage m = new CloudQueueMessage(href);
                                urlq.AddMessage(m);
                            }
                        }
                    }
                }
            }
        }

        // compare the date between last-modified date and today
        // return true if difference <= 60
        private bool compareDate(string day)
        {
            day = day.Substring(0, 10);
            DateTime lastModified = DateTime.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            double difference = today.Subtract(lastModified).TotalDays;
            return difference <= 60;
        }

        private XmlNodeList createList(string xml, string tagName)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xml);
            return xDoc.GetElementsByTagName(tagName);
        }

        //private void getRobots(CloudQueueMessage site)
        public void getRobots(string robot)
        {
            WebClient wClient = new WebClient();
            string htmlString = wClient.DownloadString(robot);
            string line;
            using (StringReader reader = new StringReader(htmlString))
            {
                line = reader.ReadLine();
                if (line.StartsWith("Sitemap: "))
                {
                    string xml1 = line.Substring(9);
                    XmlNodeList sitemapList = createList(xml1,"sitemap");
                    foreach (XmlNode sitemap in sitemapList)
                    {
                        string day1 = sitemap["lastmod"].InnerText;
                        if (compareDate(day1))
                        {
                            string xml2 = sitemap["loc"].InnerText;
                            XmlNodeList urlList = createList(xml2, "url");
                            foreach(XmlNode url in urlList){
                                string day2 = url["lastmod"].InnerText;
                                if (compareDate(day2))
                                {
                                    string loc = url["loc"].InnerText;
                                    if (!visited.Contains(loc))
                                    {
                                        visited.Add(loc);
                                        CloudQueueMessage m = new CloudQueueMessage(loc);
                                        urlq.AddMessage(m);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (line.StartsWith("Disallow: "))
                {
                    disallow.Add(line.Substring(10));
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
