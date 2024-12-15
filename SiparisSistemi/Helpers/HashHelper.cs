using System.Security.Cryptography;
using System.Text;

namespace SiparisSistemi.Helpers
{
    public static class HashHelper
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Şifreyi byte dizisine çevir
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                
                // Hash'i hesapla
                byte[] hash = sha256.ComputeHash(bytes);
                
                // Hash'i hexadecimal string'e çevir
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInput = HashPassword(password);
            return hashedInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
} 