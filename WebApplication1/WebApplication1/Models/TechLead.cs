using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class TechLead
    {
        [Key]
        public int ID { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime ResetPasswordExpiry { get; set; }
    }
}

