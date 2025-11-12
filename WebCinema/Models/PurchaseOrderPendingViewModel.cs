using System;
using System.Collections.Generic;

namespace WebCinema.Models
{
    public class PurchaseOrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Notes { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class PurchaseOrderPendingViewModel
    {
        public int SupplierId { get; set; }
        public string Supplier { get; set; }
        public DateTime ExpectedDate { get; set; }
        public string CreatedByStaff { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<PurchaseOrderItemViewModel> Items { get; set; } = new List<PurchaseOrderItemViewModel>();
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
    }
}