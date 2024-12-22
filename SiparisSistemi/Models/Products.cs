using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Products
    {
        [Key]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(255, ErrorMessage = "Ürün adı en fazla 255 karakter olabilir.")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Stok bilgisi zorunludur.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı negatif olamaz.")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Fiyat bilgisi zorunludur.")]
        [Range(0, double.MaxValue, ErrorMessage = "Fiyat negatif olamaz.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Ürün türü zorunludur.")]
        [StringLength(100, ErrorMessage = "Ürün türü en fazla 100 karakter olabilir.")]
        public string ProductType { get; set; }

        [StringLength(255, ErrorMessage = "Resim yolu en fazla 255 karakter olabilir.")]
        public string? ImagePath { get; set; }

        public virtual ICollection<Orders>? Orders { get; set; }
    }
}
