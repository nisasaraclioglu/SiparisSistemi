using System;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Logs
    {
        [Key]
        public int LogID { get; set; }
        public int CustomerID { get; set; }
        public int? OrderID { get; set; }

        public DateTime LogDate { get; set; }
        public string LogType { get; set; }
        public string LogDetails { get; set; }

        // İlişkiler
        public virtual Customers Customer { get; set; }
        public virtual Orders Order { get; set; }
    }
}
