using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Customers
    {
        [Key]
        public int CustomerID { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public decimal Budget { get; set; }

        [Required]
        public string CustomerType { get; set; } = "Standard";

        public decimal TotalSpent { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string PasswordHash { get; set; }

        public virtual ICollection<Orders>? Orders { get; set; }
        public virtual ICollection<Logs>? Logs { get; set; }
    }
}
