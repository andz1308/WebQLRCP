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

                if (ticket.trang_thai_ve == "Đã sử dụng")
                {
                    return Ok(new { success = false, message = "Vé này đã được sử dụng rồi" });
                }

                // Cập nhật trạng thái vé
                ticket.trang_thai_ve = "Đã sử dụng";
                db.SubmitChanges();

                var showtime = ticket.Suat_Chieu;
                var booking = ticket.Dat_Ve;

                LoggingHelper.LogInfo($"✅ Soát vé: {qrCode}");

                return Ok(new
                {
                    success = true,
                    message = "Vé hợp lệ",
                    data = new
                    {
                        movie_title = showtime?.Phim.ten_phim ?? "N/A",
                        customer_name = booking?.Khach_Hang.ho_ten ?? "N/A",
                        seat_number = ticket.Ghe?.so_ghe ?? "N/A",
                        date = showtime?.ngay_chieu.ToString("yyyy-MM-dd") ?? "N/A",
                        time = showtime?.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm") ?? "N/A"
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
