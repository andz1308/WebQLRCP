using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebCinema.Models
{
    // Models/ManagerInventoryViewModel.cs
    public class ManagerInventoryViewModel
    {
        public WebCinema.Models.Do_An DoAn { get; set; }
        public int SoLuongTon { get; set; } // Lấy từ Kho_Do_An của rạp hiện tại
    }
}