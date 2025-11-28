using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebCinema.Models
{
    public class StaffInventoryViewModel
    {
        public int Do_An_id { get; set; }
        public string TenSanPham { get; set; }
        public string MoTa { get; set; }
        public string Loai { get; set; }
        public decimal? Gia { get; set; }
        public string TrangThai { get; set; }
        public int SoLuongTon { get; set; } // Lấy từ bảng Kho_Do_An
    }
}