using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Services
{
    /// <summary>
    /// Service để tạo file PDF hóa đơn (HTML-based PDF)
    /// </summary>
    public class InvoicePdfService
    {
        private CSDLDataContext db;

        public InvoicePdfService()
        {
            db = new CSDLDataContext();
        }

        /// <summary>
        /// Tạo PDF hóa đơn từ Dat_Ve (lưu dưới dạng HTML + CSS)
        /// </summary>
        public string GenerateInvoicePdf(int bookingId)
        {
            try
            {
                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    throw new Exception("Không tìm thấy đơn đặt");

                var customer = booking.Khach_Hang;
                var tickets = booking.Ves.ToList();
                var firstTicket = tickets.FirstOrDefault();
                var showtime = firstTicket?.Suat_Chieu;

                // ✅ Tính toán giá
                decimal ticketTotal = tickets.Sum(t => t.gia_ve);
                decimal foodTotal = 0;
                List<DonHang_DoAn> foodOrders = booking.DonHang_DoAns?.ToList() ?? new List<DonHang_DoAn>();
                
                foreach (var food in foodOrders)
                {
                    foodTotal += (food.Do_An.gia ?? 0m) * food.so_luong;
                }

                decimal originalTotal = ticketTotal + foodTotal;
                decimal discount = originalTotal - booking.tong_tien;

                // ✅ Tạo thư mục nếu chưa tồn tại
                string invoiceDir = System.Web.HttpContext.Current?.Server.MapPath("~/Content/hoadon") 
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/hoadon");
                
                if (!Directory.Exists(invoiceDir))
                    Directory.CreateDirectory(invoiceDir);

                // ✅ Tên file: invoice_[bookingId]_[datetime].html (hoặc PDF)
                string fileName = $"invoice_{bookingId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string filePath = Path.Combine(invoiceDir, fileName);

                // ✅ Tạo HTML hóa đơn
                string htmlContent = GenerateInvoiceHtml(booking, customer, tickets, showtime, foodOrders, ticketTotal, foodTotal, originalTotal, discount);

                // ✅ Lưu file HTML
                File.WriteAllText(filePath, htmlContent, Encoding.UTF8);

                LoggingHelper.LogInfo($"✅ Tạo hóa đơn HTML: {fileName}");

                return fileName;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "Lỗi tạo hóa đơn");
                throw;
            }
        }

        /// <summary>
        /// Tạo HTML nội dung hóa đơn
        /// </summary>
        private string GenerateInvoiceHtml(Dat_Ve booking, Khach_Hang customer, List<Ve> tickets, Suat_Chieu showtime, List<DonHang_DoAn> foodOrders, decimal ticketTotal, decimal foodTotal, decimal originalTotal, decimal discount)
        {
            StringBuilder html = new StringBuilder();

            html.Append(@"<!DOCTYPE html>");
            html.Append(@"<html>");
            html.Append(@"<head>");
            html.Append(@"<meta charset='UTF-8'>");
            html.Append(@"<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.Append(@"<style>");
            html.Append(@"body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background: #f9f9f9; }");
            html.Append(@".container { max-width: 900px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            html.Append(@".header { text-align: center; border-bottom: 3px solid #ff6b35; padding-bottom: 20px; margin-bottom: 20px; }");
            html.Append(@".header h1 { margin: 0; color: #ff6b35; font-size: 2em; }");
            html.Append(@".header p { margin: 5px 0; color: #666; }");
            html.Append(@".info-section { margin: 20px 0; }");
            html.Append(@".info-section h3 { background: #f5f5f5; padding: 10px; margin: 0 0 10px 0; color: #ff6b35; border-left: 4px solid #ff6b35; }");
            html.Append(@".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }");
            html.Append(@".info-item { padding: 10px 0; border-bottom: 1px solid #e0e0e0; }");
            html.Append(@".info-label { font-weight: bold; color: #ff6b35; }");
            html.Append(@".info-value { color: #333; }");
            html.Append(@"table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
            html.Append(@"th { background: #f5f5f5; padding: 12px; text-align: left; border-bottom: 2px solid #ff6b35; font-weight: bold; }");
            html.Append(@"td { padding: 10px 12px; border-bottom: 1px solid #e0e0e0; }");
            html.Append(@".text-right { text-align: right; }");
            html.Append(@".total-row { font-weight: bold; background: #fff5f2; }");
            html.Append(@".total-row td { border-top: 2px solid #ff6b35; }");
            html.Append(@".grand-total { font-size: 1.5em; color: #ff6b35; background: #fff5f2; }");
            html.Append(@".footer { text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; color: #666; font-size: 0.9em; }");
            html.Append(@".note-box { background: #fffbea; border: 1px solid #ffc107; padding: 15px; margin: 15px 0; border-radius: 5px; }");
            html.Append(@"@media print { body { background: white; } .container { box-shadow: none; } }");
            html.Append(@"</style>");
            html.Append(@"</head>");
            html.Append(@"<body>");

            html.Append(@"<div class='container'>");

            // Header
            html.Append(@"<div class='header'>");
            html.Append(@"<h1>🎬 DAV CINEMA</h1>");
            html.Append(@"<p>HÓA ĐƠN THANH TOÁN</p>");
            html.Append($@"<p style='color: #ff6b35; font-weight: bold;'>Mã đơn: #{booking.Dat_Ve_id}</p>");
            html.Append($@"<p>Ngày: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.Append(@"</div>");

            // Thông tin khách hàng
            html.Append(@"<div class='info-section'>");
            html.Append(@"<h3>👤 THÔNG TIN KHÁCH HÀNG</h3>");
            html.Append(@"<div class='info-grid'>");
            html.Append($@"<div class='info-item'><span class='info-label'>Họ tên:</span><span class='info-value'>{customer?.ho_ten ?? "N/A"}</span></div>");
            html.Append($@"<div class='info-item'><span class='info-label'>Email:</span><span class='info-value'>{customer?.email ?? "N/A"}</span></div>");
            html.Append($@"<div class='info-item'><span class='info-label'>Điện thoại:</span><span class='info-value'>{customer?.so_dien_thoai ?? "N/A"}</span></div>");
            html.Append($@"<div class='info-item'><span class='info-label'>Trạng thái:</span><span class='info-value'>{booking.trang_thai_Dat_Ve}</span></div>");
            html.Append(@"</div>");
            html.Append(@"</div>");

            // Thông tin phim & suất chiếu
            if (showtime != null)
            {
                html.Append(@"<div class='info-section'>");
                html.Append(@"<h3>🎬 THÔNG TIN PHIM & SUẤT CHIẾU</h3>");
                html.Append(@"<div class='info-grid'>");
                html.Append($@"<div class='info-item'><span class='info-label'>Phim:</span><span class='info-value'>{showtime.Phim.ten_phim}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Rạp:</span><span class='info-value'>{showtime.Phong_Chieu.Rap.ten_rap} - Phòng {showtime.Phong_Chieu.ten_phong}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Ngày chiếu:</span><span class='info-value'>{showtime.ngay_chieu:dd/MM/yyyy}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Giờ chiếu:</span><span class='info-value'>{showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm")}</span></div>");
                html.Append($@"<div class='info-item' colspan='2'><span class='info-label'>Ghế:</span><span class='info-value'>{string.Join(", ", tickets.Select(t => t.Ghe?.so_ghe ?? "N/A"))}</span></div>");
                html.Append(@"</div>");
                html.Append(@"</div>");
            }

            // Danh sách vé
            html.Append(@"<div class='info-section'>");
            html.Append(@"<h3>🎫 DANH SÁCH VÉ</h3>");
            html.Append(@"<table>");
            html.Append(@"<tr><th>Ghế</th><th>Loại Ghế</th><th>Mã QR</th><th class='text-right'>Giá</th></tr>");
            
            foreach (var ticket in tickets)
            {
                string seatType = ticket.Ghe?.Loai_Ghe?.ten_loai ?? "N/A";
                html.Append($@"<tr><td>{ticket.Ghe?.so_ghe ?? "N/A"}</td><td>{seatType}</td><td style='font-size: 0.8em; word-break: break-all;'>{ticket.ma_qr_code}</td><td class='text-right'>{ticket.gia_ve:N0} ₫</td></tr>");
            }
            
            html.Append(@"</table>");
            html.Append(@"</div>");

            // Danh sách đồ ăn (nếu có)
            if (foodOrders.Any())
            {
                html.Append(@"<div class='info-section'>");
                html.Append(@"<h3>🍿 ĐỒ ĂN & THỨC UỐNG</h3>");
                html.Append(@"<table>");
                html.Append(@"<tr><th>Tên sản phẩm</th><th>SL</th><th class='text-right'>Đơn giá</th><th class='text-right'>Thành tiền</th></tr>");
                
                foreach (var food in foodOrders)
                {
                    decimal unitPrice = food.Do_An.gia ?? 0m;
                    decimal itemTotal = unitPrice * food.so_luong;
                    html.Append($@"<tr><td>{food.Do_An.ten_san_pham}</td><td>{food.so_luong}</td><td class='text-right'>{unitPrice:N0} ₫</td><td class='text-right'>{itemTotal:N0} ₫</td></tr>");
                }
                
                html.Append(@"</table>");
                html.Append(@"</div>");
            }

            // Chi tiết thanh toán
            html.Append(@"<div class='info-section'>");
            html.Append(@"<h3>💳 CHI TIẾT THANH TOÁN</h3>");
            html.Append(@"<table>");
            html.Append($@"<tr><td>Tiền vé ({tickets.Count} vé)</td><td class='text-right'>{ticketTotal:N0} ₫</td></tr>");
            
            if (foodTotal > 0)
            {
                html.Append($@"<tr><td>Tiền đồ ăn</td><td class='text-right'>{foodTotal:N0} ₫</td></tr>");
            }
            
            html.Append($@"<tr class='total-row'><td>Tổng tiền gốc</td><td class='text-right'>{originalTotal:N0} ₫</td></tr>");
            
            if (discount > 0)
            {
                html.Append($@"<tr style='color: #27ae60;><td>Giảm giá</td><td class='text-right'>-{discount:N0} ₫</td></tr>");
            }
            
            html.Append($@"<tr class='total-row grand-total'><td>TỔNG THANH TOÁN</td><td class='text-right'>{booking.tong_tien:N0} ₫</td></tr>");
            html.Append(@"</table>");
            html.Append(@"</div>");

            // Lưu ý
            html.Append(@"<div class='note-box'>");
            html.Append(@"<strong>📝 LƯU Ý QUAN TRỌNG:</strong>");
            html.Append(@"<ul>");
            html.Append(@"<li>Vui lòng đến rạp <strong>15 phút</strong> trước giờ chiếu</li>");
            html.Append(@"<li>Mang theo hóa đơn này hoặc mã QR để kiểm tra tại quầy</li>");
            html.Append(@"<li>Giữ email này để có thể xem lại thông tin vé</li>");
            html.Append(@"<li>Trường hợp có vấn đề, liên hệ: Hotline 1900-1234-5678</li>");
            html.Append(@"</ul>");
            html.Append(@"</div>");

            // Footer
            html.Append(@"<div class='footer'>");
            html.Append(@"<p>&copy; 2024 DAV Cinema. All rights reserved.</p>");
            html.Append(@"<p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>");
            html.Append(@"<p>📞 Hotline: 1900-1234-5678 | 📧 Email: support@davcinema.vn | 🌐 Website: www.davcinema.vn</p>");
            html.Append(@"</div>");

            html.Append(@"</div>");
            html.Append(@"</body>");
            html.Append(@"</html>");

            return html.ToString();
        }

        ~InvoicePdfService()
        {
            if (db != null)
                db.Dispose();
        }
    }
}
