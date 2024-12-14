using System;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Orders
    {
        [Key]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // "Pending", "Completed", "Cancelled"

        // İlişkiler
        public virtual Customers Customer { get; set; }
        public virtual Products Product { get; set; }
    }
}
