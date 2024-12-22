using System.ComponentModel.DataAnnotations;

namespace SiparisSistemi.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
