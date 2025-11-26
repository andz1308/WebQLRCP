using System;
using System.Collections.Generic;

namespace WebCinema.Models
{
    public class InvoiceViewModel
    {
        public BookingDto Booking { get; set; }
        public ShowtimeDto Showtime { get; set; }
        public List<TicketDto> Tickets { get; set; }
        public List<FoodItemDto> FoodItems { get; set; }
        public decimal TicketTotal { get; set; }
        public decimal FoodTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string QRCodeUrl { get; set; }
    }

    public class BookingDto
    {
        public int BookingId { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class ShowtimeDto
    {
        public string MovieTitle { get; set; }
        public string Cinema { get; set; }
        public string Room { get; set; }
        public string Date { get; set; }
        public string StartTime { get; set; }
    }

    public class TicketDto
    {
        public int TicketId { get; set; }
        public string SeatNumber { get; set; }
        public string QRCode { get; set; }
        public decimal Price { get; set; }
    }

    public class FoodItemDto
    {
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
