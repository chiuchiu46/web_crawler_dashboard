using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class DashboardStats : TableEntity
    {
        public DashboardStats(string worker, double CPU, double RAM, string LastUrls, int TotalUrl,
            int QueueSize, int IndexSize, string ErrorUrl)
        {
            this.PartitionKey = "pa";
            this.RowKey = worker;
            this.CPU = CPU;
            this.RAM = RAM;
            this.LastUrls = LastUrls;
            this.TotalUrl = TotalUrl;
            this.QueueSize = QueueSize;
            this.IndexSize = IndexSize;
            this.ErrorUrl = ErrorUrl;
        }

        public DashboardStats() { }
        public double CPU { get; set; }
        public double RAM { get; set; }
        public string LastUrls { get; set; }
        public int TotalUrl { get; set; }
        public int QueueSize { get; set; }
        public int IndexSize { get; set; }
        public string ErrorUrl { get; set; }
    }
}