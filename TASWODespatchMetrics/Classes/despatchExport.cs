using System;

namespace TASWODespatchMetrics
{
    class despatchExport
    {
        public string WorksOrderNumber { get; set; }
        public string CommercialNote { get; set; }
        public string SerialNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public string PartNumber { get; set; }
        public string PartDescription { get; set; }
        public int PaxCount { get; set; }
        public string Batch { get; set; }
        public string BatchLocation { get; set; }
        public string CurrentBatchLocation { get; set; }
        public string Username { get; set; }
        public string MethodType { get; set; }
        public string DefaultLocationCode { get; set; }
        public string ProductGroupCode { get; set; }
        public string RespCode { get; set; }
       
    }
}
