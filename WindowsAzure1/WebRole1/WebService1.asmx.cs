using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private CloudTable table;
        private CloudTable dash;
        private CloudQueue commandq;

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
        }


        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public void start(string website)
        {
            cloudInitialization();
            if (website.Contains("www.cnn"))
            {
                CloudQueueMessage message = new CloudQueueMessage("http://www.cnn.com/robots.txt");
                commandq.AddMessage(message);
            } else if (website.Contains("sports")) {
                CloudQueueMessage message = new CloudQueueMessage("http://sportsillustrated.cnn.com/robots.txt");
                commandq.AddMessage(message);
            }
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public List<string> getDashboard()
        {
            cloudInitialization();
            TableQuery<DashboardStats> query = new TableQuery<DashboardStats>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
            
            List<string> result = new List<string>();
            foreach (DashboardStats entity in dash.ExecuteQuery(query))
            {
                result.Add(entity.CPU.ToString());
                result.Add(entity.RAM.ToString());
                result.Add(entity.TotalUrl.ToString());
                result.Add(entity.QueueSize.ToString());
                result.Add(entity.IndexSize.ToString());
                if (entity.LastUrls == null)
                {
                    List<string> temp = new List<string>();
                    result.Add(temp.ToString());
                }
                else
                {
                    result.Add(entity.LastUrls.ToString());
                }
                if (entity.ErrorUrl == null)
                {
                    List<string> temp = new List<string>();
                    result.Add(temp.ToString());
                }
                else
                {
                    result.Add(entity.ErrorUrl.ToString());
                }
            }
            return result;
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public void stop()
        {
            cloudInitialization();
            CloudQueueMessage message = new CloudQueueMessage("stop");
            commandq.AddMessage(message);
        }

        // pass in url and find the title
        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public string find(string website)
        {
            try
            {
                string title = "";
                TableQuery<UrlInformation> query = new TableQuery<UrlInformation>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, website));

                foreach (UrlInformation entity in table.ExecuteQuery(query))
                {
                    title = entity.Title;
                }
                return title;
            }
            catch (Exception EX)
            {
                return "website not found: " + EX;
            }
        }

        // clear the table
        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public void clear()
        {
            stop();
            CloudQueueMessage message = new CloudQueueMessage("clear");
            commandq.AddMessage(message);
        }


    }
}
