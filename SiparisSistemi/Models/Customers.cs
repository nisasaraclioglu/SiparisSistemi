using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Customers
    {
        public int CustomerID { get; set; }
        
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        public string CustomerName { get; set; }
        
        public decimal Budget { get; set; }

        [Required]
        public string CustomerType { get; set; } = "Standard";
        
        public decimal TotalSpent { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        public string PasswordHash { get; set; }

        // İlişkiler - nullable olarak işaretle
        public virtual ICollection<Orders>? Orders { get; set; }
        public virtual ICollection<Logs>? Logs { get; set; }

        public Customers()
        {
            CustomerType = "Standard";
        }
    }
}
