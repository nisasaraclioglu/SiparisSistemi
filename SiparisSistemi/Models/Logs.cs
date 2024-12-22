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

        [Required]
        public DateTime LogDate { get; set; }

        [Required]
        public string LogType { get; set; }

        [Required]
        public string LogDetails { get; set; }

        public virtual Customers Customer { get; set; }
        public virtual Orders Order { get; set; }
    }
}
