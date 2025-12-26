using System;

namespace WebCinema.Models
{
    public class AuthResult
    {
        public bool IsAuthenticated { get; set; }
        // Role: "Customer", "Admin", "Staff"
        public string Role { get; set; }
        public Khach_Hang Customer { get; set; }
        public Nhan_Vien Employee { get; set; }
    }
}