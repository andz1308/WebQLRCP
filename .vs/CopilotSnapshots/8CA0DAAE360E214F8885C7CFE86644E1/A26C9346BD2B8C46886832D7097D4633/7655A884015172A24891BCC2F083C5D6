using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;
using WebCinema.Services;

namespace WebCinema.Controllers
{
    public class TicketRefundController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: TicketRefund/MyTickets - Xem danh sách vé của customer
        [HttpGet]
        public ActionResult MyTickets()
        {
            if (Session["CustomerId"] == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyTickets") });

            int customerId = (int)Session["CustomerId"];
            var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);
            
            if (customer == null)
                return RedirectToAction("Logout", "Account");

            // ✅ Lấy TẤT CẢ đơn đặt đã thanh toán của customer (bao gồm cả đã hủy và đã xem)
            var bookings = db.Dat_Ves
                .Where(b => b.khach_hang_id == customerId && 
                           (b.trang_thai_Dat_Ve == "Đã Thanh toán" || b.trang_thai_Dat_Ve == "Đã Hủy"))
                .OrderByDescending(b => b.ngay_tao)
                .ToList();

            ViewBag.Customer = customer;
            return View(bookings);
        }

        // GET: TicketRefund/Details/{bookingId} - Xem chi tiết vé (kèm QR code)
        [HttpGet]
        public ActionResult Details(int id)
        {
            if (Session["CustomerId"] == null)
                return RedirectToAction("Login", "Account");

            int customerId = (int)Session["CustomerId"];
            var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == id && b.khach_hang_id == customerId);

            if (booking == null)
                return HttpNotFound();

            var tickets = booking.Ves.ToList();
            var showtime = tickets.FirstOrDefault()?.Suat_Chieu;

            // ✅ Kiểm tra xem suất chiếu có phải là quá khứ không
            bool isShowtimePassed = showtime?.ngay_chieu.Date < DateTime.Now.Date;

            ViewBag.Tickets = tickets;
            ViewBag.Showtime = showtime;
            ViewBag.IsShowtimePassed = isShowtimePassed;

            // Lấy yêu cầu hủy nếu có
            var cancelRequest = db.HuyVes.FirstOrDefault(y => y.dat_ve_id == id);
            ViewBag.CancelRequest = cancelRequest;

            return View(booking);
        }

        // ✅ POST: TicketRefund/RequestCancel - GỬI YÊU CẦU HỦY VÉ + TỰ ĐỘNG HỦY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestCancel(int bookingId, string reason)
        {
            if (Session["CustomerId"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            int customerId = (int)Session["CustomerId"];
            var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId && b.khach_hang_id == customerId);

            if (booking == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });

            // ✅ KIỂM TRA: Vé phải chưa qua ngày chiếu
            var firstTicket = booking.Ves.FirstOrDefault();
            if (firstTicket == null)
                return Json(new { success = false, message = "Không tìm thấy vé" });

            var showtime = firstTicket.Suat_Chieu;
            if (showtime.ngay_chieu.Date < DateTime.Now.Date)
                return Json(new { success = false, message = "❌ Không thể hủy vé đã qua ngày chiếu" });

            // ✅ KIỂM TRA MỚI: Phải trước 2 giờ so với giờ bắt đầu suất chiếu
            var now = DateTime.Now;
            var showtimeStart = showtime.ngay_chieu.Date.Add(showtime.Ca_Chieu.gio_bat_dau);
            var minimumCancelTime = showtimeStart.AddHours(-2);
            
            if (now >= minimumCancelTime)
            {
                var timeUntilShowtime = showtimeStart - now;
                var hoursLeft = Math.Floor(timeUntilShowtime.TotalHours);
                var minutesLeft = timeUntilShowtime.Minutes;
                
                return Json(new { 
                    success = false, 
                    message = $"❌ Không thể hủy vé!\n\n⏰ Chỉ được hủy trước 2 giờ so với giờ chiếu.\n\n" +
                             $"📅 Suất chiếu: {showtimeStart:dd/MM/yyyy HH:mm}\n" +
                             $"⏳ Thời gian còn lại: {hoursLeft} giờ {minutesLeft} phút\n\n" +
                             $"💡 Bạn cần hủy trước {minimumCancelTime:dd/MM/yyyy HH:mm}"
                });
            }

            // ✅ KIỂM TRA: Đơn không được phép là "Đã Hủy"
            if (booking.trang_thai_Dat_Ve == "Đã Hủy")
                return Json(new { success = false, message = "❌ Đơn này đã hủy rồi" });

            // ✅ KIỂM TRA: Vé không được phải "Đã sử dụng" hoặc "Đã Hủy"
            var usedOrCancelledTickets = booking.Ves.Where(v => v.trang_thai_ve == "Đã sử dụng" || v.trang_thai_ve == "Đã Hủy").ToList();
            if (usedOrCancelledTickets.Any())
                return Json(new { success = false, message = "❌ Vé này đã sử dụng hoặc đã hủy, không thể hủy" });

            // ✅ KIỂM TRA: Nếu đã có yêu cầu hủy chưa xác nhận
            var existingRequest = db.HuyVes.FirstOrDefault(y => y.dat_ve_id == bookingId && y.trang_thai == "Chờ xác nhận");
            if (existingRequest != null)
                return Json(new { success = false, message = "⚠️ Đã có yêu cầu hủy vé đang chờ Admin xác nhận chuyển tiền" });

            // ✅ TÍNH: Số tiền hoàn lại = 70% (trừ 30%)
            decimal soTienHoanLai = booking.tong_tien * 0.7m;  // 70%

            try
            {
                // ✅ TẠO: Yêu cầu hủy vé với trạng thái "Chờ xác nhận" (Admin sẽ xác nhận chuyển tiền)
                var cancelRequest = new HuyVe
                {
                    dat_ve_id = bookingId,
                    ly_do_huy = reason ?? "Không cung cấp lý do",
                    so_tien_hoan_lai = soTienHoanLai,
                    phan_tram_hoan = 30m,
                    trang_thai = "Chờ xác nhận",  // ✅ CHỜ ADMIN XÁC NHẬN CHUYỂN TIỀN
                    ngay_tao = DateTime.Now
                };

                db.HuyVes.InsertOnSubmit(cancelRequest);

                // ✅ HỦY NGAY: Trạng thái đơn → "Đã Hủy"
                booking.trang_thai_Dat_Ve = "Đã Hủy";

                // ✅ GIẢI PHÓNG: Tất cả vé
                foreach (var ticket in booking.Ves)
                {
                    ticket.trang_thai_ve = "Đã Hủy";
                    ticket.Dat_Ve_id = null;
                }

                // ✅ XÓA: Tất cả đồ ăn
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == bookingId).ToList();
                foreach (var food in foodOrders)
                {
                    db.DonHang_DoAns.DeleteOnSubmit(food);
                }

                // ✅ HOÀN LẠI: Điểm tích lũy
                var customer = booking.Khach_Hang;
                if (customer != null)
                {
                    int ticketCount = booking.Ves.Count;
                    if (customer.diem_tich_luy.HasValue && customer.diem_tich_luy >= ticketCount)
                    {
                        customer.diem_tich_luy -= ticketCount;
                        LoggingHelper.LogInfo($"✅ Hoàn lại {ticketCount} điểm cho khách {customer.khach_hang_id}");
                    }
                }

                db.SubmitChanges();

                // ✅ GỬI EMAIL THÔNG BÁO HỦY VÉ
                try
                {
                    var customer2 = booking.Khach_Hang;
                    if (customer2 != null && !string.IsNullOrEmpty(customer2.email))
                    {
                        var emailService = new EmailServiceMailKit();
                        string emailMessage = $@"
                        <p>Xin chào <strong>{customer2.ho_ten}</strong>,</p>

                        <p>Chúng tôi xác nhận rằng bạn đã yêu cầu <strong>hủy vé</strong> thành công.</p>

                        <h3>📋 Chi tiết hủy vé:</h3>
                        <ul>
                            <li><strong>Mã đơn:</strong> #{bookingId}</li>
                            <li><strong>Lý do:</strong> {reason}</li>
                            <li><strong>Số tiền gốc:</strong> {booking.tong_tien:N0} ₫</li>
                            <li><strong>Số tiền hoàn lại (70%):</strong> <span style='color: #27ae60; font-weight: bold;'>{soTienHoanLai:N0} ₫</span></li>
                            <li><strong>Phí hủy (30%):</strong> {(booking.tong_tien * 0.3m):N0} ₫</li>
                            <li><strong>Trạng thái:</strong> ✅ Đã hủy</li>
                        </ul>

                        <h3>💳 Thanh toán hoàn lại:</h3>
                        <p>Admin sẽ xác nhận chuyển khoản hoàn lại cho bạn trong vòng 1-3 ngày làm việc. Vui lòng kiểm tra email để nhận thông báo xác nhận chuyển tiền.</p>

                        <h3>📝 Lưu ý:</h3>
                        <ul>
                            <li>Vé của bạn đã được hủy hoàn toàn</li>
                            <li>Điểm tích lũy đã được hoàn lại</li>
                            <li>Bạn có thể tiếp tục đặt vé xem phim khác</li>
                        </ul>

                        <p style='margin-top: 20px; color: #666;'>Cảm ơn bạn đã sử dụng dịch vụ của DAV Cinema!</p>
                        ";

                        bool sent = emailService.SendInvoiceEmail(
                            customer2.email,
                            customer2.ho_ten,
                            $"cancel_request_{bookingId}_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                            null,  // Không cần file path
                            emailMessage
                        );

                        if (sent)
                            LoggingHelper.LogInfo($"✅ Gửi email thông báo hủy vé tới: {customer2.email}");
                    }
                }
                catch (Exception emailEx)
                {
                    LoggingHelper.LogError(emailEx, "Lỗi gửi email hủy vé");
                }

                LoggingHelper.LogInfo($"✅ Hủy vé TỰ ĐỘNG: Booking {bookingId}, Customer {customerId}, Hoàn: {soTienHoanLai}");

                return Json(new 
                { 
                    success = true, 
                    message = "✅ Yêu cầu hủy vé được gửi thành công!\n\n💰 Số tiền hoàn lại: " + soTienHoanLai.ToString("N0") + " ₫ (70%)\n📧 Email xác nhận đã gửi\n⏳ Admin sẽ xác nhận chuyển tiền trong 1-3 ngày",
                    refundAmount = soTienHoanLai,
                    cancelled = true  // ✅ MARK AS CANCELLED
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "Lỗi hủy vé");
                return Json(new { success = false, message = "❌ Lỗi: " + ex.Message });
            }
        }

        // GET: TicketRefund/CancelStatus/{bookingId} - Xem trạng thái hủy
        [HttpGet]
        public ActionResult CancelStatus(int id)
        {
            if (Session["CustomerId"] == null)
                return RedirectToAction("Login", "Account");

            var cancelRequest = db.HuyVes.FirstOrDefault(y => y.dat_ve_id == id);

            if (cancelRequest == null)
                return HttpNotFound();

            return View(cancelRequest);
        }

        // ✅ POST: TicketRefund/SubmitReview - Gửi đánh giá phim từ trang vé
        [HttpPost]
        public ActionResult SubmitReview(int movieId, int ticketId, int rating, string content)
        {
            try
            {
                if (Session["CustomerId"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá!" });

                int customerId = (int)Session["CustomerId"];

                // Validate
                if (rating < 1 || rating > 5)
                    return Json(new { success = false, message = "Điểm đánh giá phải từ 1-5 sao!" });

                if (string.IsNullOrWhiteSpace(content))
                    return Json(new { success = false, message = "Vui lòng nhập nội dung đánh giá!" });

                if (content.Length > 500)
                    return Json(new { success = false, message = "Nội dung đánh giá không được quá 500 ký tự!" });

                // ✅ Kiểm tra vé có thuộc về khách hàng không
                var ticket = db.Ves.FirstOrDefault(v => v.ve_id == ticketId);
                if (ticket == null || ticket.Dat_Ve == null || ticket.Dat_Ve.khach_hang_id != customerId)
                    return Json(new { success = false, message = "Vé không hợp lệ hoặc không thuộc về bạn!" });

                // ✅ Kiểm tra vé đã sử dụng chưa
                if (ticket.trang_thai_ve != "Đã sử dụng")
                    return Json(new { success = false, message = "Chỉ có thể đánh giá sau khi xem phim!" });

                // ✅ Kiểm tra đã đánh giá chưa (dựa trên ve_id)
                var existingReview = db.Danh_Gias.FirstOrDefault(dg =>
                    dg.ve_id == ticketId &&
                    dg.khach_hang_id == customerId);

                if (existingReview != null)
                    return Json(new { success = false, message = "Bạn đã đánh giá phim này với vé này rồi!" });

                // ✅ Tạo đánh giá mới
                var review = new Danh_Gia
                {
                    phim_id = movieId,
                    khach_hang_id = customerId,
                    ve_id = ticketId,
                    diem_rating = rating,
                    noi_dung = content.Trim(),
                    ngay_Danh_Gia = DateTime.Now
                };

                db.Danh_Gias.InsertOnSubmit(review);
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Customer {customerId} reviewed movie {movieId} with ticket {ticketId}, rating: {rating}");

                return Json(new
                {
                    success = true,
                    message = "Cảm ơn bạn đã đánh giá! Ý kiến của bạn rất quan trọng với chúng tôi."
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "Lỗi khi gửi đánh giá");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
