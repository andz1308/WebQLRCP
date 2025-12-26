using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebCinema.Infrastructure;
using WebCinema.Models;
using Newtonsoft.Json.Linq;

namespace WebCinema.Controllers.API
{
    /// <summary>
    /// Staff API - Nhân viên có thể quản lý đặt vé offline, xem doanh thu, soát vé
    /// ✅ Yêu cầu xác thực (Staff role)
    /// </summary>
    [RoutePrefix("api/staff")]
    [Authorize(Roles = "Staff")]  // ✅ Chỉ Staff mới truy cập được
    public class StaffApiController : ApiController
    {
        private CSDLDataContext db = new CSDLDataContext();
        private const int MAX_SEAT_LIMIT = 100;

        /// <summary>
        /// GET: api/staff/dashboard/{staffId}
        /// Lấy thống kê dashboard cho nhân viên
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("dashboard/{staffId}")]
        public IHttpActionResult GetDashboard(int staffId)
        {
            try
            {
                // ✅ Validation: staffId phải > 0
                if (staffId <= 0)
                {
                    return BadRequest("Staff ID không hợp lệ");
                }

                // Tổng vé bán
                int totalTickets = db.Ves
                    .Where(v => v.Dat_Ve_id != null && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .Count();

                // Tổng doanh thu
                decimal totalRevenue = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .Sum(b => (decimal?)b.tong_tien) ?? 0m;

                // Doanh thu tháng này
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                decimal monthlyRevenue = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                                b.ngay_tao.HasValue &&
                                b.ngay_tao.Value.Month == currentMonth &&
                                b.ngay_tao.Value.Year == currentYear)
                    .Sum(b => (decimal?)b.tong_tien) ?? 0m;

                // Đơn hàng tháng này
                int monthlyBookings = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                                b.ngay_tao.HasValue &&
                                b.ngay_tao.Value.Month == currentMonth &&
                                b.ngay_tao.Value.Year == currentYear)
                    .Count();

                // Vé đã soát
                int ticketsVerified = db.Ves
                    .Where(v => v.trang_thai_ve == "Đã sử dụng")
                    .Count();

                // Vé chưa soát
                int ticketsPending = db.Ves
                    .Where(v => v.trang_thai_ve == "Chưa sử dụng" && v.Dat_Ve_id != null)
                    .Count();

                var dashboard = new
                {
                    total_tickets = totalTickets,
                    total_revenue = totalRevenue,
                    monthly_revenue = monthlyRevenue,
                    monthly_bookings = monthlyBookings,
                    tickets_verified = ticketsVerified,
                    tickets_pending = ticketsPending
                };

                LoggingHelper.LogInfo($"✅ Staff Dashboard: ID {staffId}");
                return Ok(new
                {
                    success = true,
                    message = "Lấy thống kê thành công",
                    data = dashboard
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/showtimes?date={date}
        /// Lấy danh sách suất chiếu để đặt vé offline
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("showtimes")]
        public IHttpActionResult GetShowtimesForBooking(string date = "")
        {
            try
            {
                var query = db.Suat_Chieus
                    .Where(s => s.ngay_chieu >= DateTime.Now.Date)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime filterDate))
                {
                    query = query.Where(s => s.ngay_chieu.Date == filterDate.Date);
                }

                var showtimes = query
                    .OrderBy(s => s.ngay_chieu)
                    .ThenBy(s => s.Ca_Chieu != null ? s.Ca_Chieu.gio_bat_dau : TimeSpan.Zero)
                    .ToList() // ✅ Materialize to avoid null-nav in LINQ-to-SQL
                    .Select(s => new
                    {
                        showtime_id = s.suat_chieu_id,
                        movie_title = s.Phim != null ? s.Phim.ten_phim : "N/A",
                        cinema = s.Phong_Chieu != null && s.Phong_Chieu.Rap != null ? s.Phong_Chieu.Rap.ten_rap : "N/A",
                        room = s.Phong_Chieu != null ? s.Phong_Chieu.ten_phong : "N/A",
                        date = s.ngay_chieu.ToString("yyyy-MM-dd"),
                        start_time = s.Ca_Chieu != null ? s.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm") : "N/A",
                        price = s.gia_ve,
                        total_seats = s.Phong_Chieu != null ? s.Phong_Chieu.Ghes.Count(g => g.trang_thai == 2) : 0,
                        booked_seats = s.Ves != null ? s.Ves.Count(v => v.Dat_Ve_id != null) : 0,
                        available_seats = (s.Phong_Chieu != null ? s.Phong_Chieu.Ghes.Count(g => g.trang_thai == 2) : 0) -
                                        (s.Ves != null ? s.Ves.Count(v => v.Dat_Ve_id != null && (s.Phong_Chieu == null || s.Phong_Chieu.Ghes.Any(g => g.ghe_id == v.ghe_id && g.trang_thai == 2))) : 0)
                    })
                    .ToList();

                if (!showtimes.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Hiện không có suất chiếu nào",
                        data = new List<object>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách suất chiếu thành công",
                    data = showtimes
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/seats/{showtimeId}
        /// Lấy danh sách ghế của một suất chiếu
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("seats/{showtimeId}")]
        public IHttpActionResult GetSeats(int showtimeId)
        {
            try
            {
                // ✅ Validation: showtimeId phải > 0
                if (showtimeId <= 0)
                {
                    return BadRequest("Showtime ID không hợp lệ");
                }

                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == showtimeId);
                if (showtime == null || showtime.Phong_Chieu == null)
                {
                    return NotFound();
                }

                var bookedSeats = db.Ves
                    .Where(v => v.suat_chieu_id == showtimeId && v.Dat_Ve_id != null)
                    .Select(v => v.ghe_id)
                    .ToList();

                var seats = showtime.Phong_Chieu.Ghes
                    .OrderBy(g => g.hang)
                    .ThenBy(g => g.cot)
                    .Select(g => new
                    {
                        seat_id = g.ghe_id,
                        seat_number = g.so_ghe ?? "N/A",
                        row = g.hang,
                        column = g.cot,
                        status = bookedSeats.Contains(g.ghe_id) ? "booked" : (g.trang_thai == 0 ? "aisle" : "available"),
                        price = showtime.gia_ve
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách ghế thành công",
                    data = new
                    {
                        showtime_id = showtimeId,
                        rows = showtime.Phong_Chieu.so_hang,
                        columns = showtime.Phong_Chieu.so_cot,
                        seats = seats
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/staff/create-booking
        /// Tạo đơn đặt vé offline
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("create-booking")]
        public IHttpActionResult CreateOfflineBooking([FromBody] JObject data)
        {
            try
            {
                // ✅ Check request body
                if (data == null)
                {
                    return BadRequest("Request body is required");
                }

                int showtimeId = data["showtime_id"]?.Value<int>() ?? 0;
                var seatIds = data["seat_ids"]?.ToObject<List<int>>() ?? new List<int>();
                string customerName = data["customer_name"]?.Value<string>();
                string customerPhone = data["customer_phone"]?.Value<string>();
                string paymentMethod = data["payment_method"]?.Value<string>() ?? "cash";

                // ✅ Validation: showtimeId phải > 0
                if (showtimeId <= 0)
                {
                    return BadRequest("Showtime ID không hợp lệ");
                }

                // ✅ Validation: customerName không được rỗng
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    return BadRequest("Tên khách hàng không được rỗng");
                }

                // ✅ Validation: customerPhone không được rỗng & format hợp lệ
                if (string.IsNullOrWhiteSpace(customerPhone) || customerPhone.Length < 10)
                {
                    return BadRequest("Số điện thoại không hợp lệ");
                }

                // ✅ Validation: seatIds phải có ít nhất 1
                if (seatIds.Count == 0)
                {
                    return BadRequest("Phải chọn ít nhất 1 ghế");
                }

                // ✅ Validation: không được vượt quá limit
                if (seatIds.Count > MAX_SEAT_LIMIT)
                {
                    return BadRequest($"Không được đặt quá {MAX_SEAT_LIMIT} ghế");
                }

                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == showtimeId);
                if (showtime == null)
                {
                    return NotFound();
                }

                // Kiểm tra ghế đã đặt
                var bookedSeats = db.Ves
                    .Where(v => v.suat_chieu_id == showtimeId && v.Dat_Ve_id != null)
                    .Select(v => v.ghe_id)
                    .ToList();

                var conflictSeats = seatIds.Where(s => bookedSeats.Contains(s)).ToList();
                if (conflictSeats.Any())
                {
                    return Ok(new { success = false, message = $"Ghế {string.Join(", ", conflictSeats)} đã được đặt" });
                }

                // Tính tổng tiền vé
                decimal ticketTotal = seatIds.Count * showtime.gia_ve;

                // Tìm hoặc tạo khách hàng
                var customer = db.Khach_Hangs.FirstOrDefault(k => k.so_dien_thoai == customerPhone);
                if (customer == null)
                {
                    customer = new Khach_Hang
                    {
                        ho_ten = customerName,
                        so_dien_thoai = customerPhone
                    };
                    db.Khach_Hangs.InsertOnSubmit(customer);
                    db.SubmitChanges();
                }

                // Tạo đơn đặt
                var booking = new Dat_Ve
                {
                    khach_hang_id = customer.khach_hang_id,
                    ngay_tao = DateTime.Now,
                    trang_thai_Dat_Ve = "Đã Thanh toán",
                    tong_tien = ticketTotal,
                    phuong_thuc_thanh_toan = paymentMethod
                };

                db.Dat_Ves.InsertOnSubmit(booking);
                db.SubmitChanges();

                // Tạo vé
                foreach (var seatId in seatIds)
                {
                    var ticket = new Ve
                    {
                        suat_chieu_id = showtimeId,
                        ghe_id = seatId,
                        Dat_Ve_id = booking.Dat_Ve_id,
                        gia_ve = showtime.gia_ve,
                        trang_thai_ve = "Chưa sử dụng",
                        ma_qr_code = Guid.NewGuid().ToString()
                    };
                    db.Ves.InsertOnSubmit(ticket);
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Tạo đơn đặt offline: Booking ID {booking.Dat_Ve_id}, KH: {customerName}");

                return Ok(new
                {
                    success = true,
                    message = "Tạo đơn đặt thành công",
                    data = new
                    {
                        booking_id = booking.Dat_Ve_id,
                        total_amount = booking.tong_tien,
                        status = booking.trang_thai_Dat_Ve
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/bookings?status={status}
        /// Lấy danh sách đơn đặt
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("bookings")]
        public IHttpActionResult GetBookings(string status = "")
        {
            try
            {
                var query = db.Dat_Ves.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.trang_thai_Dat_Ve == status);
                }

                var bookings = query
                    .OrderByDescending(b => b.ngay_tao)
                    .Select(b => new
                    {
                        booking_id = b.Dat_Ve_id,
                        customer_name = b.Khach_Hang.ho_ten,
                        customer_phone = b.Khach_Hang.so_dien_thoai,
                        created_at = b.ngay_tao.HasValue ? b.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm") : "N/A",
                        status = b.trang_thai_Dat_Ve,
                        total_amount = b.tong_tien,
                        tickets_count = b.Ves.Count
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đơn đặt thành công",
                    data = bookings
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/staff/verify-ticket
        /// Soát vé (quét QR code)
        /// ✅ Yêu cầu xác thực
        /// ✅ Kiểm tra quyền rạp
        /// </summary>
        [HttpPost]
        [Route("verify-ticket")]
        public IHttpActionResult VerifyTicket([FromBody] JObject data)
        {
            try
            {
                // ✅ Check request body
                if (data == null)
                {
                    return BadRequest("Request body is required");
                }

                string qrCode = data["qr_code"]?.Value<string>();

                // ✅ Validation: qrCode không được rỗng
                if (string.IsNullOrWhiteSpace(qrCode))
                {
                    return BadRequest("Mã QR không được rỗng");
                }

                var ticket = db.Ves.FirstOrDefault(v => v.ma_qr_code == qrCode.Trim());
                if (ticket == null)
                {
                    return Ok(new { success = false, message = "Mã QR không hợp lệ" });
                }

                var showtime = ticket.Suat_Chieu;

                // ✅ KIỂM TRA QUYỀN: Staff chỉ soát vé của rạp mình
                int staffId = 0;
                // User.Identity.Name có thể là email hoặc ID tùy vào cách tạo ticket
                if (!int.TryParse(User.Identity.Name, out staffId))
                {
                    // Nếu không parse được ID, thử tìm theo email
                    var email = User.Identity.Name;
                    var staffByEmail = db.Nhan_Viens.FirstOrDefault(nv => nv.email == email);
                    if (staffByEmail != null)
                    {
                        staffId = staffByEmail.nhanvien_id;
                    }
                }

                if (staffId > 0)
                {
                    var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == staffId);
                    if (staff != null && staff.rap_id.HasValue)
                    {
                        // Staff có gán rạp → chỉ soát vé của rạp đó
                        if (showtime.Phong_Chieu.rap_id != staff.rap_id.Value)
                        {
                            LoggingHelper.LogInfo($"❌ Staff {staffId} cố soát vé của rạp khác: {qrCode}");
                            return Ok(new { success = false, message = "❌ Bạn không có quyền soát vé này. Vé này thuộc rạp khác." });
                        }
                    }
                    // Nếu staff không có gán rạp (là Admin) → cho soát tất cả
                }

                if (ticket.trang_thai_ve == "Đã sử dụng")
                {
                    return Ok(new { success = false, message = "⚠️ Vé này đã được sử dụng rồi" });
                }

                // Cập nhật trạng thái vé
                ticket.trang_thai_ve = "Đã sử dụng";
                db.SubmitChanges();

                var booking = ticket.Dat_Ve;

                LoggingHelper.LogInfo($"✅ Soát vé (API): {qrCode} | Rạp: {showtime.Phong_Chieu.Rap.ten_rap}");

                return Ok(new
                {
                    success = true,
                    message = "✅ Vé hợp lệ",
                    data = new
                    {
                        movie_title = showtime?.Phim.ten_phim ?? "N/A",
                        customer_name = booking?.Khach_Hang.ho_ten ?? "N/A",
                        seat_number = ticket.Ghe?.so_ghe ?? "N/A",
                        date = showtime?.ngay_chieu.ToString("yyyy-MM-dd") ?? "N/A",
                        time = showtime?.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm") ?? "N/A",
                        cinema = showtime?.Phong_Chieu.Rap.ten_rap ?? "N/A"
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/booking/{bookingId}
        /// Lấy chi tiết đơn đặt
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpGet]
        [Route("booking/{bookingId}")]
        public IHttpActionResult GetBookingDetail(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return BadRequest("Booking ID không hợp lệ");
                }

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                {
                    return NotFound();
                }

                var tickets = booking.Ves.Select(v => new
                {
                    ticket_id = v.ve_id,
                    seat_number = v.Ghe != null ? v.Ghe.so_ghe : "N/A",
                    qr_code = v.ma_qr_code,
                    price = v.gia_ve,
                    status = v.trang_thai_ve
                }).ToList();

                var firstTicket = booking.Ves.FirstOrDefault();
                var showtime = firstTicket?.Suat_Chieu;

                var bookingDetail = new
                {
                    booking_id = booking.Dat_Ve_id,
                    customer_name = booking.Khach_Hang != null ? booking.Khach_Hang.ho_ten : "N/A",
                    customer_phone = booking.Khach_Hang != null ? booking.Khach_Hang.so_dien_thoai : "N/A",
                    created_at = booking.ngay_tao.HasValue ? booking.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm") : "N/A",
                    status = booking.trang_thai_Dat_Ve,
                    total_amount = booking.tong_tien,
                    movie = showtime != null && showtime.Phim != null ? new
                    {
                        movie_id = showtime.Phim.phim_id,
                        title = showtime.Phim.ten_phim
                    } : null,
                    showtime = showtime != null ? new
                    {
                        cinema = showtime.Phong_Chieu != null && showtime.Phong_Chieu.Rap != null ? showtime.Phong_Chieu.Rap.ten_rap : "N/A",
                        room = showtime.Phong_Chieu != null ? showtime.Phong_Chieu.ten_phong : "N/A",
                        date = showtime.ngay_chieu.ToString("yyyy-MM-dd"),
                        time = showtime.Ca_Chieu != null ? showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm") : "N/A"
                    } : null,
                    tickets = tickets
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy chi tiết đơn đặt thành công",
                    data = bookingDetail
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/ticket-statistics
        /// Lấy thống kê vé
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpGet]
        [Route("ticket-statistics")]
        public IHttpActionResult GetTicketStatistics()
        {
            try
            {
                var stats = new
                {
                    total_sold = db.Ves.Where(v => v.Dat_Ve_id != null).Count(),
                    total_used = db.Ves.Where(v => v.trang_thai_ve == "Đã sử dụng").Count(),
                    total_unused = db.Ves.Where(v => v.trang_thai_ve == "Chưa sử dụng" && v.Dat_Ve_id != null).Count(),
                    total_revenue = db.Ves
                        .Where(v => v.Dat_Ve_id != null && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán")
                        .Sum(v => (decimal?)v.gia_ve) ?? 0m
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy thống kê vé thành công",
                    data = stats
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/revenue-by-date?startDate={date}&endDate={date}
        /// Lấy doanh thu theo khoảng thời gian
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpGet]
        [Route("revenue-by-date")]
        public IHttpActionResult GetRevenueByDate(string startDate = "", string endDate = "")
        {
            try
            {
                DateTime start = DateTime.Now.AddDays(-30);
                DateTime end = DateTime.Now;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime parsedStart))
                    start = parsedStart;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime parsedEnd))
                    end = parsedEnd;

                var revenueData = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán" &&
                                b.ngay_tao.HasValue &&
                                b.ngay_tao >= start &&
                                b.ngay_tao <= end)
                    .GroupBy(b => b.ngay_tao.Value.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        revenue = g.Sum(b => b.tong_tien),
                        bookings = g.Count()
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy doanh thu thành công",
                    data = new
                    {
                        start_date = start.ToString("yyyy-MM-dd"),
                        end_date = end.ToString("yyyy-MM-dd"),
                        revenue_data = revenueData,
                        total_revenue = revenueData.Sum(r => r.revenue)
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// PUT: api/staff/booking/{bookingId}/status
        /// Cập nhật trạng thái đơn đặt (Admin only)
        /// ✅ Yêu cầu xác thực (Admin)
        /// </summary>
        [HttpPut]
        [Route("booking/{bookingId}/status")]
        [Authorize(Roles = "Admin")]
        public IHttpActionResult UpdateBookingStatus(int bookingId, [FromBody] JObject data)
        {
            try
            {
                // ✅ Check request body
                if (data == null)
                {
                    return BadRequest("Request body is required");
                }

                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                string status = data["status"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(status))
                    return BadRequest("Status không được rỗng");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                booking.trang_thai_Dat_Ve = status;
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Cập nhật trạng thái đơn {bookingId}: {status}");

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thành công"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/movies-by-showtime/{showtimeId}
        /// Lấy chi tiết phim của suất chiếu
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpGet]
        [Route("movies-by-showtime/{showtimeId}")]
        public IHttpActionResult GetMovieByShowtime(int showtimeId)
        {
            try
            {
                if (showtimeId <= 0)
                    return BadRequest("Showtime ID không hợp lệ");

                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == showtimeId);
                if (showtime == null)
                    return NotFound();

                var movieData = new
                {
                    movie_id = showtime.Phim.phim_id,
                    title = showtime.Phim.ten_phim,
                    description = showtime.Phim.mo_ta,
                    duration = showtime.Phim.thoi_luong,
                    image = showtime.Phim.hinh_anh,
                    cinema = showtime.Phong_Chieu.Rap.ten_rap,
                    room = showtime.Phong_Chieu.ten_phong,
                    date = showtime.ngay_chieu.ToString("yyyy-MM-dd"),
                    time = showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm"),
                    price = showtime.gia_ve
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin phim thành công",
                    data = movieData
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/profile/{staffId}
        /// Lấy thông tin chi tiết Staff (Nhân viên)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("profile/{staffId}")]
        public IHttpActionResult GetStaffProfile(int staffId)
        {
            try
            {
                if (staffId <= 0)
                    return BadRequest("Staff ID không hợp lệ");

                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == staffId);
                if (staff == null)
                    return NotFound();

                var profile = new
                {
                    staff_id = staff.nhanvien_id,
                    full_name = staff.ho_ten,
                    email = staff.email,
                    phone = staff.so_dien_thoai,
                    date_of_birth = staff.ngay_sinh.HasValue ? staff.ngay_sinh.Value.ToString("yyyy-MM-dd") : null,
                    gender = staff.gioi_tinh ?? "N/A",
                    address = staff.dia_chi ?? "N/A",
                    role = staff.Role != null ? staff.Role.ten_role : "N/A",
                    cinema = staff.Rap != null ? new
                    {
                        cinema_id = staff.Rap.rap_id,
                        name = staff.Rap.ten_rap
                    } : null,
                    status = staff.trang_thai ?? "N/A",
                    join_date = staff.ngay_vao_lam.HasValue ? staff.ngay_vao_lam.Value.ToString("yyyy-MM-dd") : "N/A"
                };

                LoggingHelper.LogInfo($"✅ Lấy thông tin Staff: ID {staffId}");

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin nhân viên thành công",
                    data = profile
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// PUT: api/staff/profile/{staffId}
        /// Cập nhật thông tin Staff (Nhân viên)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPut]
        [Route("profile/{staffId}")]
        public IHttpActionResult UpdateStaffProfile(int staffId, [FromBody] JObject data)
        {
            try
            {
                if (staffId <= 0)
                    return BadRequest("Staff ID không hợp lệ");

                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == staffId);
                if (staff == null)
                    return NotFound();

                // ✅ Cập nhật các field được phép
                if (data["full_name"] != null)
                    staff.ho_ten = data["full_name"].Value<string>();

                if (data["phone"] != null)
                    staff.so_dien_thoai = data["phone"].Value<string>();

                if (data["date_of_birth"] != null && DateTime.TryParse(data["date_of_birth"].Value<string>(), out var dob))
                    staff.ngay_sinh = dob;

                if (data["gender"] != null)
                    staff.gioi_tinh = data["gender"].Value<string>();

                if (data["address"] != null)
                    staff.dia_chi = data["address"].Value<string>();

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Cập nhật thông tin Staff: ID {staffId}");

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin nhân viên thành công"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/staff/verify-ticket-web
        /// Soát vé theo Web (Giống web version ở TicketRefundController)
        /// ✅ Yêu cầu xác thực (Staff)
        /// ✅ Kiểm tra quyền rạp
        /// ✅ Hỗ trợ soát vé qua ticket_id hoặc qr_code
        /// </summary>
        [HttpPost]
        [Route("verify-ticket-web")]
        public IHttpActionResult VerifyTicketWeb([FromBody] JObject data)
        {
            try
            {
                if (data == null)
                    return BadRequest("Request body is required");

                // ✅ Chấp nhận cả ticket_id hoặc qr_code
                int? ticketId = data["ticket_id"]?.Value<int>();
                string qrCode = data["qr_code"]?.Value<string>();

                Ve ticket = null;

                // Tìm ticket
                if (ticketId.HasValue && ticketId > 0)
                {
                    ticket = db.Ves.FirstOrDefault(v => v.ve_id == ticketId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(qrCode))
                {
                    ticket = db.Ves.FirstOrDefault(v => v.ma_qr_code == qrCode.Trim());
                }
                else
                {
                    return BadRequest("Phải cung cấp ticket_id hoặc qr_code");
                }

                if (ticket == null)
                    return Ok(new { success = false, message = "❌ Vé không tồn tại hoặc mã QR không hợp lệ" });

                var showtime = ticket.Suat_Chieu;

                // ✅ KIỂM TRA QUYỀN: Staff chỉ soát vé của rạp mình
                var staffId = int.Parse(User.Identity.Name ?? "0");
                if (staffId > 0)
                {
                    var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == staffId);
                    if (staff != null && staff.rap_id.HasValue)
                    {
                        // Staff có gán rạp → chỉ soát vé của rạp đó
                        if (showtime.Phong_Chieu.rap_id != staff.rap_id.Value)
                        {
                            LoggingHelper.LogInfo($"❌ Staff {staffId} cố soát vé của rạp khác: Ticket {ticketId}");
                            return Ok(new { success = false, message = "❌ Bạn không có quyền soát vé này. Vé này thuộc rạp khác." });
                        }
                    }
                }

                // ✅ KIỂM TRA TRẠNG THÁI VÉ
                if (ticket.trang_thai_ve == "Đã sử dụng")
                {
                    return Ok(new { success = false, message = "⚠️ Vé này đã được sử dụng rồi" });
                }

                // ✅ KIỂM TRA VÉ CÓ THUỘC ĐƠN ĐÃ THANH TOÁN KHÔNG
                var booking = ticket.Dat_Ve;
                if (booking == null || booking.trang_thai_Dat_Ve != "Đã Thanh toán")
                {
                    return Ok(new { success = false, message = "⚠️ Đơn đặt chưa được thanh toán hoặc vé không hợp lệ" });
                }

                // ✅ CẬP NHẬT TRẠNG THÁI VÉ
                ticket.trang_thai_ve = "Đã sử dụng";
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Soát vé (Web): Ticket {ticket.ve_id} | QR: {ticket.ma_qr_code} | Rạp: {showtime.Phong_Chieu.Rap.ten_rap}");

                return Ok(new
                {
                    success = true,
                    message = "✅ Vé hợp lệ - Đã soát vé thành công",
                    data = new
                    {
                        ticket_id = ticket.ve_id,
                        movie_title = showtime.Phim.ten_phim,
                        customer_name = booking.Khach_Hang.ho_ten,
                        seat_number = ticket.Ghe?.so_ghe ?? "N/A",
                        seat_location = (ticket.Ghe != null) ? $"Hàng {(char)('A' + ticket.Ghe.hang)} - Cột {ticket.Ghe.cot}" : "N/A",
                        date = showtime.ngay_chieu.ToString("yyyy-MM-dd"),
                        time = showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm"),
                        cinema = showtime.Phong_Chieu.Rap.ten_rap,
                        room = showtime.Phong_Chieu.ten_phong,
                        price = ticket.gia_ve,
                        verified_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/verified-tickets?date={date}
        /// Lấy danh sách vé đã soát trong ngày
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpGet]
        [Route("verified-tickets")]
        public IHttpActionResult GetVerifiedTickets(string date = "")
        {
            try
            {
                DateTime filterDate = DateTime.Today;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
                    filterDate = parsedDate.Date;

                var staffId = int.Parse(User.Identity.Name ?? "0");
                var staff = db.Nhan_Viens.FirstOrDefault(nv => nv.nhanvien_id == staffId);

                var query = db.Ves
                    .Where(v => v.trang_thai_ve == "Đã sử dụng" &&
                                v.Dat_Ve != null &&
                                v.Suat_Chieu.ngay_chieu == filterDate)
                    .AsQueryable();

                // ✅ Nếu Staff có gán rạp → chỉ lấy vé của rạp đó
                if (staff != null && staff.rap_id.HasValue)
                {
                    query = query.Where(v => v.Suat_Chieu.Phong_Chieu.rap_id == staff.rap_id.Value);
                }

                var verifiedTickets = query
                    .OrderByDescending(v => v.ve_id)
                    .Select(v => new
                    {
                        ticket_id = v.ve_id,
                        movie_title = v.Suat_Chieu.Phim.ten_phim,
                        customer_name = v.Dat_Ve.Khach_Hang.ho_ten,
                        seat_number = v.Ghe != null ? v.Ghe.so_ghe : "N/A",
                        date = v.Suat_Chieu.ngay_chieu.ToString("yyyy-MM-dd"),
                        time = v.Suat_Chieu.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm"),
                        cinema = v.Suat_Chieu.Phong_Chieu.Rap.ten_rap,
                        price = v.gia_ve,
                        qr_code = v.ma_qr_code
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách vé đã soát thành công",
                    data = new
                    {
                        date = filterDate.ToString("yyyy-MM-dd"),
                        total_verified = verifiedTickets.Count,
                        total_revenue = verifiedTickets.Sum(t => t.price),
                        tickets = verifiedTickets
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/staff/verify-ticket-info
        /// Xác thực thông tin vé (chỉ định danh bằng QR code)
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpPost]
        [Route("verify-ticket-info")]
        public IHttpActionResult VerifyTicketInfo([FromBody] JObject data)
        {
            try
            {
                string ticketCode = data?["ticket_code"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(ticketCode))
                {
                    return BadRequest("Mã vé không được để trống");
                }

                var ticket = db.Ves.FirstOrDefault(v => v.ma_qr_code == ticketCode);

                if (ticket == null)
                {
                    return Ok(new { success = false, message = "Mã vé không hợp lệ" });
                }

                var showtime = ticket.Suat_Chieu;
                var movie = showtime.Phim;
                var booking = ticket.Dat_Ve;
                var customer = booking.Khach_Hang;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        ticket_id = ticket.ve_id,
                        movie_title = movie.ten_phim,
                        show_date = showtime.ngay_chieu.ToString("yyyy-MM-dd"),
                        show_time = showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm"),
                        cinema_name = showtime.Phong_Chieu.Rap.ten_rap,
                        room_name = showtime.Phong_Chieu.ten_phong,
                        seat_number = ticket.Ghe.so_ghe,
                        customer_name = customer.ho_ten,
                        ticket_status = ticket.trang_thai_ve,
                        booking_status = booking.trang_thai_Dat_Ve
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "VerifyTicketInfo");
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/staff/confirm-ticket-usage
        /// Xác nhận sử dụng vé (Đánh dấu vé là đã sử dụng)
        /// ✅ Yêu cầu xác thực (Staff)
        /// </summary>
        [HttpPost]
        [Route("confirm-ticket-usage")]
        public IHttpActionResult ConfirmTicketUsage([FromBody] JObject data)
        {
            try
            {
                string ticketCode = data?["ticket_code"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(ticketCode))
                {
                    return BadRequest("Mã vé không được để trống");
                }

                var ticket = db.Ves.FirstOrDefault(v => v.ma_qr_code == ticketCode);

                if (ticket == null)
                {
                    return Ok(new { success = false, message = "Mã vé không hợp lệ" });
                }

                if (ticket.trang_thai_ve == "Đã sử dụng")
                {
                    return Ok(new { success = false, message = "Vé này đã được sử dụng" });
                }

                if (ticket.Dat_Ve.trang_thai_Dat_Ve != "Đã Thanh toán")
                {
                    return Ok(new { success = false, message = "Vé chưa được thanh toán" });
                }

                ticket.trang_thai_ve = "Đã sử dụng";
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Soát vé thành công: {ticketCode}");

                return Ok(new { success = true, message = "Xác nhận sử dụng vé thành công" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "ConfirmTicketUsage");
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/staff/bookings
        /// Lấy danh sách đơn đặt vé (cho Staff)
        /// </summary>
        [HttpGet]
        [Route("bookings")]
        public IHttpActionResult GetBookings(int page = 1, int pageSize = 20, string status = null)
        {
            try
            {
                var query = db.Dat_Ves.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.trang_thai_Dat_Ve == status);
                }

                int total = query.Count();
                var bookings = query
                    .OrderByDescending(b => b.ngay_tao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        booking_id = b.Dat_Ve_id,
                        customer_name = b.Khach_Hang != null ? b.Khach_Hang.ho_ten : "Khách vãng lai",
                        created_at = b.ngay_tao.HasValue ? b.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm") : "N/A",
                        status = b.trang_thai_Dat_Ve,
                        total_amount = b.tong_tien,
                        tickets_count = b.Ves.Count
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        bookings = bookings,
                        total = total,
                        page = page,
                        page_size = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "GetBookings");
                return InternalServerError(ex);
            }
        }
        /// <summary>
        /// POST: api/staff/booking/cancel
        /// Hủy đơn đặt vé (giống StaffBookingManagementController)
        /// </summary>
        [HttpPost]
        [Route("booking/cancel")]
        public IHttpActionResult CancelBooking([FromBody] JObject data)
        {
            try
            {
                int bookingId = data["booking_id"]?.Value<int>() ?? 0;
                if (bookingId <= 0) return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null) return NotFound();

                // Logic hủy giống MVC Controller
                booking.trang_thai_Dat_Ve = "Đã Hủy";

                // 1. Giải phóng vé
                var tickets = db.Ves.Where(v => v.Dat_Ve_id == bookingId).ToList();
                foreach (var t in tickets)
                {
                    t.Dat_Ve_id = null;
                    t.trang_thai_ve = "Chưa sử dụng";
                    t.ma_qr_code = null;
                }

                // 2. Xóa đồ ăn
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == bookingId).ToList();
                db.DonHang_DoAns.DeleteAllOnSubmit(foodOrders);

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Staff hủy booking {bookingId}");
                return Ok(new { success = true, message = "Hủy đơn đặt thành công" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "CancelBooking");
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
