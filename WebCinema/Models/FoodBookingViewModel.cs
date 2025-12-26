using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebCinema.Models
{
    public class FoodBookingViewModel
    {
        public WebCinema.Models.Do_An Food { get; set; }
        public int MaxStock { get; set; } // Đây là biến chứa số lượng tồn kho
    }
}