namespace WebCinema.Models
{
    public class CustomerBookingViewModel
    {
        public int booking_id { get; set; }
        public string movie_name { get; set; }
        public string show_date { get; set; }
        public string cinema_name { get; set; }
        public int ticket_count { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; }
    }
}
