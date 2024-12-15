using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiparisSistemi.Models
{
    public class Customers
    {
        [Key]
        public int CustomerID { get; set; }
        
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        public string CustomerName { get; set; }
        
        public decimal Budget { get; set; }

        [Required]
        public string CustomerType { get; set; } = "Standard";
        
        public decimal TotalSpent { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        public string PasswordHash { get; set; }

        [NotMapped]
        [Compare("PasswordHash", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? ConfirmPassword { get; set; }

        public virtual ICollection<Orders>? Orders { get; set; }
        public virtual ICollection<Logs>? Logs { get; set; }

        public Customers()
        {
            CustomerType = "Standard";
        }
    }
}
