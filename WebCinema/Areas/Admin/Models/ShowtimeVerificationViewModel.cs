using System;
using WebCinema.Models;

namespace WebCinema.Areas.Admin.Models
{
    public class ShowtimeVerificationViewModel
    {
        public Suat_Chieu Showtime { get; set; }
        public int TotalTickets { get; set; }
        public int VerifiedTickets { get; set; }
    }
}
