using System;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Orders
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public string OrderStatus { get; set; } // Yeni eklenen özellik

        public virtual Customers Customer { get; set; }
        public virtual Products Product { get; set; }
    }
}
