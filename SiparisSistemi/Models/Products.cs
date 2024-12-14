using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Products
    {
        [Key]
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public virtual ICollection<Orders> Orders { get; set; }
    }
}
