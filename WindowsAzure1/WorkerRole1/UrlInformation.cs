using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class UrlInformation : TableEntity
    {
        public UrlInformation(string category, string url, string title, string date)
        {
            this.PartitionKey = category;
            this.RowKey = url;
            this.Url = url;
            this.Title = title;
            this.Date = date;
        }

        public UrlInformation() { }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
    }
}
