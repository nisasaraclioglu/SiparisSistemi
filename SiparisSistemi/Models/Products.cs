using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Products
    {
        [Key]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Ürün türü zorunludur")]
        public string ProductType { get; set; }  // Laptop, Telefon, Aksesuar vb.

        public string? ImagePath { get; set; }  // Ürün resminin yolu

        public virtual ICollection<Orders> Orders { get; set; }
    }
}
