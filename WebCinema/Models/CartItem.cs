using System;
using System.Collections.Generic;

namespace WebCinema.Models
{
    /// <summary>
    /// Represents a booking item in the shopping cart
    /// </summary>
    [Serializable]
    public class CartItem
    {
        public string CartItemId { get; set; }
        public int ShowtimeId { get; set; }
        public string MovieName { get; set; }
        public string CinemaName { get; set; }
        public string RoomName { get; set; }
        public DateTime ShowDate { get; set; }
        public string ShowTime { get; set; }
        public string Language { get; set; }
        
        // Seat information
        public List<CartSeatInfo> Seats { get; set; }
        public decimal TicketTotal { get; set; }
        
        // Food information
        public List<CartFoodInfo> Foods { get; set; }
        public decimal FoodTotal { get; set; }
        
        public decimal TotalPrice => TicketTotal + FoodTotal;
        
        public DateTime AddedAt { get; set; }

        public CartItem()
        {
            CartItemId = Guid.NewGuid().ToString();
            Seats = new List<CartSeatInfo>();
            Foods = new List<CartFoodInfo>();
            AddedAt = DateTime.Now;
        }
    }

    [Serializable]
    public class CartSeatInfo
    {
        public int SeatId { get; set; }
        public int TicketId { get; set; }
        public string SeatNumber { get; set; }
        public string SeatType { get; set; }
        public decimal Price { get; set; }
    }

    [Serializable]
    public class CartFoodInfo
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
