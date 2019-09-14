using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TASWODespatchMetrics
{
    class seatRecoveryExport
    {
        public string SalesOrderNumber { get; set; }
        public string CustomerCode { get; set; }
        public string SalesOrderTitle { get; set; }
        public string DeliveryDate { get; set; }
        public string ActualDeliveryDate { get; set; }
        public string PartNumber { get; set; }
        public string PartDescription { get; set; }
        public decimal PaxOrdered { get; set; }
        public decimal PaxDespatched { get; set; }
        public decimal PaxRemaining { get; set; }
        public string SalesOrderStatus { get; set; }
        public string DespatchNote { get; set; }
        public string DespatchStatus { get; set; }
        public string CommercialNotes { get; set; }
    }
}
