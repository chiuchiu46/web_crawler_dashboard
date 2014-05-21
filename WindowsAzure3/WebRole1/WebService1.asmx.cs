using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
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
        static TrieTree tree;
        private PerformanceCounter memProcess;

        private void Load()
        {
            try
            {
                TrieNode test = tree.getRoot();
            }
            catch (NullReferenceException)
            {
                this.memProcess = new PerformanceCounter("Memory", "Available MBytes");
                string filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString()
                            + "\\wiki.txt";
                Download(filePath);
                tree = ReadAndBuildTree(filePath);
            }
        }

        private float AvailableMemory()
        {
            return memProcess.NextValue();
        }

        private void Download(string filePath)
        {
 	        string connstr = ConfigurationManager.AppSettings["lecture8"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connstr);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();    // create a client to work on the drive storage
            CloudBlobContainer container = blobClient.GetContainerReference("page-titles-namespaces");        // retrieve blob container
//            CloudBlobContainer container = blobClient.GetContainerReference("test");        // retrieve blob container

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            blob.DownloadToStream(fs);
                        }
                    }
                }
            }
        }

        private TrieTree ReadAndBuildTree(string filePath)
        {
            string line;
            int counter = 0;
            TrieTree tree = new TrieTree();
            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                tree.addTitle(line);
                counter++;
                if (counter > 100)
                {
                    counter = 0;
                    float f = AvailableMemory();
                    if (f < 20)
                    {
                        return tree;
                    }
                }
            }
            file.Close();

            return tree;
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public List<string> SearchTitles(string word)
        {
            Load();
            List<string> result = tree.searchTitle(word.Trim());
            return result;
        }
    }
}
