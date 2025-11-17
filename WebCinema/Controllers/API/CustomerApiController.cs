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
    /// Customer API - Khách hàng có thể xem phim, đặt vé, xem hóa đơn
    /// </summary>
    [RoutePrefix("api/customer")]
    public class CustomerApiController : ApiController
    {
        private CSDLDataContext db = new CSDLDataContext();
        private const int DEFAULT_PAGE_SIZE = 10;
        private const int MAX_PAGE_SIZE = 100;

        /// <summary>
        /// GET: api/customer/movies?page=1&pageSize=10
        /// Lấy danh sách phim đang chiếu (có suất chiếu từ hôm nay trở đi) - GIố WEB
        /// ✅ Chỉ lấy phim có trạng thái "Đang chiếu"
        /// </summary>
        [HttpGet]
        [Route("movies")]
        [AllowAnonymous]
        public IHttpActionResult GetMovies(int page = 1, int pageSize = 10)
        {
            try
            {
                // ✅ Validation: page và pageSize phải hợp lệ
                if (page < 1)
                {
                    return BadRequest("Page phải >= 1");
                }

                if (pageSize < 1 || pageSize > MAX_PAGE_SIZE)
                {
                    pageSize = DEFAULT_PAGE_SIZE;
                }

                var today = DateTime.Today;

                // ✅ FIX: Lấy CHỈ phim đang chiếu (có suất chiếu >= hôm nay VÀ trạng thái "Đang chiếu") - GIỐNG WEB
                int total = db.Phims
                    .Where(p => p.trang_thai == "Đang chiếu" && p.Suat_Chieus.Any(sc => sc.ngay_chieu >= today))
                    .Count();

                int totalPages = (int)Math.Ceiling(total / (double)pageSize);

                var movies = db.Phims
                    .Where(p => p.trang_thai == "Đang chiếu" && p.Suat_Chieus.Any(sc => sc.ngay_chieu >= today)) // ✅ Filter: chỉ phim đang chiếu
                    .OrderByDescending(p => p.ngay_khoi_chieu)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        movie_id = p.phim_id,
                        title = p.ten_phim,
                        description = p.mo_ta,
                        duration = p.thoi_luong,
                        release_date = p.ngay_khoi_chieu != null ? p.ngay_khoi_chieu.Value.Date : (DateTime?)null,
                        image = p.hinh_anh
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách phim đang chiếu thành công",
                    data = new
                    {
                        movies = movies,
                        total = total,
                        current_page = page,
                        total_pages = totalPages,
                        page_size = pageSize
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
        /// GET: api/customer/showtimes/{movieId}
        /// Lấy danh sách suất chiếu của một phim
        /// ✅ Chỉ lấy suất chiếu "Sắp Diễn Ra" hoặc "Đang Chiếu"
        /// </summary>
        [HttpGet]
        [Route("showtimes/{movieId}")]
        [AllowAnonymous]
        public IHttpActionResult GetShowtimes(int movieId, string date = null)
        {
            try
            {
                // ✅ Validation: movieId phải > 0
                if (movieId <= 0)
                {
                    return BadRequest("Movie ID không hợp lệ");
                }

                // ✅ Kiểm tra phim tồn tại
                var movieExists = db.Phims.Any(p => p.phim_id == movieId);
                if (!movieExists)
                {
                    return Ok(new { success = false, message = "Phim không tồn tại" });
                }

                // ✅ Parse date nếu được truyền
                DateTime? filterDate = null;
                if (!string.IsNullOrEmpty(date))
                {
                    if (DateTime.TryParse(date, out var parsedDate))
                    {
                        filterDate = parsedDate.Date;
                    }
                    else
                    {
                        return BadRequest("Định dạng ngày không hợp lệ. Vui lòng dùng yyyy-MM-dd");
                    }
                }

                var query = db.Suat_Chieus
                    .Where(s => s.phim_id == movieId && s.ngay_chieu >= DateTime.Now.Date);

                // ✅ Lọc theo ngày nếu được truyền
                if (filterDate.HasValue)
                {
                    query = query.Where(s => s.ngay_chieu == filterDate.Value);
                }

                var showtimes = query
                    .OrderBy(s => s.ngay_chieu)
                    .ThenBy(s => s.Ca_Chieu != null ? s.Ca_Chieu.gio_bat_dau : TimeSpan.Zero)
                    .ToList() // ✅ Materialize trước khi select để tránh null nav properties
                    .Where(s => s.Ca_Chieu != null) // ✅ Lọc chỉ những suất có ca chiếu hợp lệ
                    .Select(s => new
                    {
                        showtime_id = s.suat_chieu_id,
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
                        message = filterDate.HasValue ? $"Không có suất chiếu nào vào ngày {filterDate:yyyy-MM-dd}" : "Hiện không có suất chiếu nào",
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
        /// GET: api/customer/bookings/{customerId}
        /// Lấy lịch sử đặt vé của khách hàng
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("bookings/{customerId}")]
        [Authorize]  // ✅ Thêm Authorization
        public IHttpActionResult GetBookings(int customerId)
        {
            try
            {
                // ✅ Validation: customerId phải > 0
                if (customerId <= 0)
                {
                    return BadRequest("Customer ID không hợp lệ");
                }

                // ✅ Kiểm tra khách hàng tồn tại
                var customerExists = db.Khach_Hangs.Any(k => k.khach_hang_id == customerId);
                if (!customerExists)
                {
                    return Ok(new { success = false, message = "Khách hàng không tồn tại" });
                }

                // ✅ FIX N+1 Query: Dùng .Include() hoặc Select chính xác để tránh lazy loading
                var bookings = db.Dat_Ves
                    .Where(b => b.khach_hang_id == customerId)
                    .OrderByDescending(b => b.ngay_tao)
                    .Select(b => new
                    {
                        booking_id = b.Dat_Ve_id,
                        created_at = b.ngay_tao.HasValue ? b.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm") : "N/A",
                        status = b.trang_thai_Dat_Ve,
                        total_amount = b.tong_tien,
                        tickets_count = b.Ves.Count,
                        // ✅ FIX: Tránh gọi FirstOrDefault() lần 2
                        movie_title = b.Ves.Select(v => v.Suat_Chieu.Phim.ten_phim).FirstOrDefault() ?? "N/A"
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch sử đặt vé thành công",
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
        /// GET: api/customer/booking/{bookingId}
        /// Lấy chi tiết một đơn đặt vé
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("booking/{bookingId}")]
        [Authorize]  // ✅ Thêm Authorization
        public IHttpActionResult GetBookingDetail(int bookingId)
        {
            try
            {
                // ✅ Validation: bookingId phải > 0
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
                    customer_email = booking.Khach_Hang != null ? booking.Khach_Hang.email : "N/A",
                    customer_phone = booking.Khach_Hang != null ? booking.Khach_Hang.so_dien_thoai : "N/A",
                    // ✅ Thêm các cột mới
                    customer_dob = booking.Khach_Hang != null && booking.Khach_Hang.ngay_sinh.HasValue 
                        ? booking.Khach_Hang.ngay_sinh.Value.ToString("yyyy-MM-dd") 
                        : "N/A",
                    customer_gender = booking.Khach_Hang != null ? booking.Khach_Hang.gioi_tinh ?? "N/A" : "N/A",
                    customer_address = booking.Khach_Hang != null ? booking.Khach_Hang.dia_chi ?? "N/A" : "N/A",
                    created_at = booking.ngay_tao.HasValue ? booking.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm") : "N/A",
                    status = booking.trang_thai_Dat_Ve,
                    total_amount = booking.tong_tien,
                    payment_method = "N/A",
                    movie = showtime != null && showtime.Phim != null ? new
                    {
                        movie_id = showtime.Phim.phim_id,
                        title = showtime.Phim.ten_phim
                    } : null,
                    showtime = showtime != null ? new
                    {
                        showtime_id = showtime.suat_chieu_id,
                        cinema = showtime.Phong_Chieu != null && showtime.Phong_Chieu.Rap != null ? showtime.Phong_Chieu.Rap.ten_rap : "N/A",
                        room = showtime.Phong_Chieu != null ? showtime.Phong_Chieu.ten_phong : "N/A",
                        date = showtime.ngay_chieu.Date.ToString("yyyy-MM-dd"),
                        time = showtime.Ca_Chieu != null ? showtime.Ca_Chieu.gio_bat_dau.ToString() : "N/A"
                    } : null,
                    tickets = tickets,
                    food_items = booking.DonHang_DoAns != null ? booking.DonHang_DoAns.Select(f => new
                    {
                        food_name = f.Do_An != null ? f.Do_An.ten_san_pham : "N/A",
                        quantity = f.so_luong,
                        price = f.Do_An != null ? f.Do_An.gia : (decimal?)0
                    }).ToList().Cast<object>().ToList() : new List<object>()
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
        /// GET: api/customer/foods
        /// Lấy danh sách đồ ăn
        /// </summary>
        [HttpGet]
        [Route("foods")]
        [AllowAnonymous]
        public IHttpActionResult GetFoods()
        {
            try
            {
                var foods = db.Do_Ans
                    // ✅ Lọc: chỉ lấy đồ ăn có giá > 0 (hoạt động)
                    .Where(d => d.gia.HasValue && d.gia > 0)
                    .OrderBy(d => d.ten_san_pham)
                    .Select(d => new
                    {
                        food_id = d.Do_An_id,
                        name = d.ten_san_pham,
                        price = d.gia,
                        description = d.mo_ta
                    })
                    .ToList();

                if (!foods.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Hiện không có đồ ăn nào",
                        data = new List<object>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đồ ăn thành công",
                    data = foods
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/cinemas
        /// Lấy danh sách rạp chiếu
        /// </summary>
        [HttpGet]
        [Route("cinemas")]
        [AllowAnonymous]
        public IHttpActionResult GetCinemas()
        {
            try
            {
                var cinemas = db.Raps
                    .OrderBy(r => r.ten_rap)
                    .Select(r => new
                    {
                        cinema_id = r.rap_id,
                        name = r.ten_rap,
                        address = r.dia_chi
                    })
                    .ToList();

                if (!cinemas.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Hiện không có rạp nào",
                        data = new List<object>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách rạp thành công",
                    data = cinemas
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/movie/{movieId}
        /// Lấy chi tiết phim - GIỐNG WEB (genres, cast, showtimes, rating)
        /// ✅ Chỉ lấy phim có trạng thái "Đang chiếu"
        /// </summary>
        [HttpGet]
        [Route("movie/{movieId}")]
        [AllowAnonymous]
        public IHttpActionResult GetMovieDetail(int movieId)
        {
            try
            {
                if (movieId <= 0)
                {
                    return BadRequest("Movie ID không hợp lệ");
                }

                var movie = db.Phims.FirstOrDefault(p => p.phim_id == movieId);
                if (movie == null)
                {
                    return NotFound();
                }

                // ✅ Kiểm tra trạng thái phim
                if (movie.trang_thai != "Đang chiếu")
                {
                    return Ok(new { success = false, message = "Phim này hiện không có sẵn" });
                }

                var today = DateTime.Today;

                // ✅ Lấy thể loại
                var genres = movie.Phim_LoaiPhims != null
                    ? movie.Phim_LoaiPhims
                        .Where(pl => pl.Loai_Phim != null)
                        .Select(pl => (object)new
                        {
                            genre_id = pl.Loai_Phim.loaiphim_id,
                            name = pl.Loai_Phim.ten_loai
                        })
                        .ToList()
                    : new List<object>();

                // ✅ Lấy đạo diễn
                var director = movie.Dao_Dien;

                // ✅ Lấy danh sách diễn viên
                var actors = movie.Vai_Diens != null
                    ? movie.Vai_Diens
                        .Where(v => v.Dien_Vien != null)
                        .Select(v => new
                        {
                            actor_id = v.Dien_Vien.dienvien_id,
                            name = v.Dien_Vien.ho_ten,
                            role = v.ten_vai_dien
                        })
                        .ToList()
                        .Cast<object>()
                        .ToList()
                    : new List<object>();

                // ✅ Lấy rating trung bình
                var avgRating = movie.Danh_Gias != null && movie.Danh_Gias.Any()
                    ? movie.Danh_Gias.Average(d => d.diem_rating ?? 0)
                    : 0;

                // ✅ Lấy suất chiếu từ hôm nay trở đi
                var showtimes = movie.Suat_Chieus != null
                    ? movie.Suat_Chieus
                        .Where(sc => sc.ngay_chieu >= today && sc.Phong_Chieu != null && sc.Ca_Chieu != null)
                        .OrderBy(sc => sc.ngay_chieu)
                        .ThenBy(sc => sc.Ca_Chieu.gio_bat_dau)
                        .Select(sc => (object)new
                        {
                            showtime_id = sc.suat_chieu_id,
                            cinema = sc.Phong_Chieu.Rap != null ? sc.Phong_Chieu.Rap.ten_rap : "N/A",
                            room = sc.Phong_Chieu.ten_phong,
                            date = sc.ngay_chieu.ToString("yyyy-MM-dd"),
                            start_time = sc.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm"),
                            price = sc.gia_ve
                        })
                        .ToList()
                    : new List<object>();

                var movieDetail = new
                {
                    movie_id = movie.phim_id,
                    title = movie.ten_phim,
                    description = movie.mo_ta,
                    duration = movie.thoi_luong,
                    release_date = movie.ngay_khoi_chieu,
                    image = movie.hinh_anh,
                    video = !string.IsNullOrEmpty(movie.video) ? movie.video : null, // ✅ Trả về video URL từ DB
                    director = director != null ? new
                    {
                        director_id = director.daodien_id,
                        name = director.ho_ten
                    } : null,
                    avg_rating = avgRating,
                    review_count = movie.Danh_Gias != null ? movie.Danh_Gias.Count : 0,
                    genres = genres,
                    actors = actors,
                    showtimes = showtimes
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy chi tiết phim thành công",
                    data = movieDetail
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/trending
        /// Lây danh sách phim trending/nổi bật (chỉ phim "Đang chiếu")
        /// </summary>
        [HttpGet]
        [Route("trending")]
        [AllowAnonymous]
        public IHttpActionResult GetTrendingMovies()
        {
            try
            {
                // ✅ Lấy phim có trạng thái "Đang chiếu" với nhiều đánh giá nhất hoặc rating cao
                var trendingMovies = db.Phims
                    .Where(p => p.trang_thai == "Đang chiếu" && p.ngay_khoi_chieu <= DateTime.Now)
                    .AsEnumerable()
                    .Where(p => p.Danh_Gias != null) // ✅ Filter null collections
                    .OrderByDescending(p => p.Danh_Gias.Count)
                    .ThenByDescending(p => p.Danh_Gias.Any() ? p.Danh_Gias.Average(d => (double)(d.diem_rating ?? 0)) : 0)
                    .Take(10)
                    .Select(p => new
                    {
                        movie_id = p.phim_id,
                        title = p.ten_phim,
                        image = p.hinh_anh,
                        rating = p.Danh_Gias != null && p.Danh_Gias.Any()
                            ? p.Danh_Gias.Average(d => (double)(d.diem_rating ?? 0))
                            : 0,
                        review_count = p.Danh_Gias != null ? p.Danh_Gias.Count : 0
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách phim nổi bật thành công",
                    data = trendingMovies
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/reviews/{movieId}
        /// Lấy danh sách đánh giá của phim
        /// </summary>
        [HttpGet]
        [Route("reviews/{movieId}")]
        [AllowAnonymous]
        public IHttpActionResult GetMovieReviews(int movieId)
        {
            try
            {
                if (movieId <= 0)
                {
                    return BadRequest("Movie ID không hợp lệ");
                }

                var movieExists = db.Phims.Any(p => p.phim_id == movieId);
                if (!movieExists)
                {
                    return NotFound();
                }

                var reviews = db.Danh_Gias
                    .Where(d => d.phim_id == movieId)
                    .OrderByDescending(d => d.ngay_Danh_Gia)
                    .Select(d => new
                    {
                        review_id = d.Danh_Gia_id,
                        customer_name = d.Khach_Hang != null ? d.Khach_Hang.ho_ten : "N/A",
                        rating = d.diem_rating,
                        content = d.noi_dung,
                        date = d.ngay_Danh_Gia.HasValue ? d.ngay_Danh_Gia.Value.ToString("yyyy-MM-dd") : "N/A"
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đánh giá thành công",
                    data = reviews
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/customer/create-booking
        /// Tạo đơn đặt vé (khách hàng online) - GIỐNG WEB
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("create-booking")]
        [Authorize]
        public IHttpActionResult CreateOnlineBooking([FromBody] JObject data)
        {
            try
            {
                // ✅ Check request body
                if (data == null)
                {
                    return BadRequest("Request body is required");
                }

                int customerId = data["customer_id"]?.Value<int>() ?? 0;
                int showtimeId = data["showtime_id"]?.Value<int>() ?? 0;
                var seatIds = data["seat_ids"]?.ToObject<List<int>>() ?? new List<int>();
                var foodItems = data["food_items"]?.ToObject<List<JObject>>() ?? new List<JObject>();

                // ✅ Validation
                if (customerId <= 0)
                    return BadRequest("Customer ID không hợp lệ");

                if (showtimeId <= 0)
                    return BadRequest("Showtime ID không hợp lệ");

                if (seatIds.Count == 0)
                    return BadRequest("Phải chọn ít nhất 1 ghế");

                // ✅ Kiểm tra suất chiếu
                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == showtimeId);
                if (showtime == null)
                    return NotFound();

                // ✅ Kiểm tra ghế - CHỈ block nếu liên kết với booking "Đã Thanh toán" (GIỐNG WEB)
                var paidBookingIds = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .Select(b => b.Dat_Ve_id)
                    .ToList();

                var bookedSeats = db.Ves
                    .Where(v => v.suat_chieu_id == showtimeId
                        && v.Dat_Ve_id != null
                        && paidBookingIds.Contains(v.Dat_Ve_id.Value))
                    .Select(v => v.ghe_id)
                    .ToList();

                var conflictSeats = seatIds.Where(s => bookedSeats.Contains(s)).ToList();
                if (conflictSeats.Any())
                    return Ok(new { success = false, message = $"Ghế đã được đặt: {string.Join(", ", conflictSeats)}" });

                // ✅ Tính tổng tiền vé
                var selectedTickets = db.Ves
                    .Where(v => seatIds.Contains(v.ghe_id) && v.suat_chieu_id == showtimeId)
                    .ToList();

                decimal ticketTotal = selectedTickets.Sum(t => t.gia_ve);

                // ✅ Tính tiền đồ ăn
                decimal foodTotal = 0;

                foreach (var item in foodItems)
                {
                    if (item == null) continue;

                    int foodId = item["food_id"]?.Value<int>() ?? 0;
                    int quantity = item["quantity"]?.Value<int>() ?? 0;

                    if (foodId > 0 && quantity > 0)
                    {
                        var food = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == foodId);
                        if (food != null && food.gia.HasValue)
                        {
                            foodTotal += (food.gia.Value * quantity);
                        }
                    }
                }

                // ✅ TẠOBOOKING VỚI TRẠNG THÁI "CHƯA THANH TOÁN" NGAY (GIỐNG WEB)
                decimal totalAmount = ticketTotal + foodTotal;
                var booking = new Dat_Ve
                {
                    khach_hang_id = customerId,
                    ngay_tao = DateTime.Now,
                    trang_thai_Dat_Ve = "Chưa thanh toán", // ✅ NGAY LẬP TỨC
                    tong_tien = totalAmount,
                    phuong_thuc_thanh_toan = "vnpay"
                };

                db.Dat_Ves.InsertOnSubmit(booking);
                db.SubmitChanges();

                // ✅ Cập nhật vé
                foreach (var ticket in selectedTickets)
                {
                    ticket.Dat_Ve_id = booking.Dat_Ve_id;
                    ticket.trang_thai_ve = "Chưa sử dụng";
                    ticket.ma_qr_code = Guid.NewGuid().ToString();
                }

                // ✅ Thêm đồ ăn
                foreach (var item in foodItems)
                {
                    if (item == null) continue;

                    int foodId = item["food_id"]?.Value<int>() ?? 0;
                    int quantity = item["quantity"]?.Value<int>() ?? 0;

                    if (foodId > 0 && quantity > 0)
                    {
                        var food = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == foodId);
                        if (food != null)
                        {
                            var foodOrder = new DonHang_DoAn
                            {
                                Dat_Ve_id = booking.Dat_Ve_id,
                                Do_An_id = food.Do_An_id,
                                so_luong = quantity
                            };
                            db.DonHang_DoAns.InsertOnSubmit(foodOrder);
                        }
                    }
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Tạo đơn đặt online: Booking ID {booking.Dat_Ve_id}, Trạng thái: Chưa thanh toán, Vé: {selectedTickets.Count}");

                return Ok(new
                {
                    success = true,
                    message = "Tạo đơn đặt thành công",
                    data = new
                    {
                        booking_id = booking.Dat_Ve_id,
                        total_amount = booking.tong_tien,
                        ticket_total = ticketTotal,
                        food_total = foodTotal,
                        status = booking.trang_thai_Dat_Ve,
                        created_at = booking.ngay_tao?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
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
        /// POST: api/customer/cancel-booking
        /// Huỷ đơn đặt (nếu chưa thanh toán) - GIỐNG WEB
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("cancel-booking")]
        [Authorize]
        public IHttpActionResult CancelBooking([FromBody] JObject data)
        {
            try
            {
                int bookingId = data["booking_id"]?.Value<int>() ?? 0;

                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                // ✅ Chỉ huỷ được nếu chưa thanh toán (GIỐNG WEB)
                if (booking.trang_thai_Dat_Ve == "Đã Thanh toán")
                    return Ok(new { success = false, message = "Không thể huỷ đơn đã thanh toán" });

                // ✅ GIẢI PHÓNG TẤT CẢ VÉ (xóa Dat_Ve_id, clear QR code)
                var allVesInBooking = db.Ves.Where(v => v.Dat_Ve_id == bookingId).ToList();
                foreach (var ticket in allVesInBooking)
                {
                    ticket.Dat_Ve_id = null;
                    ticket.trang_thai_ve = "Chưa sử dụng";
                    ticket.ma_qr_code = null;
                }

                // ✅ XÓA CÁC ĐỒ ĂN LIÊN QUAN
                var foodOrders = db.DonHang_DoAns.Where(f => f.Dat_Ve_id == bookingId).ToList();
                foreach (var food in foodOrders)
                {
                    db.DonHang_DoAns.DeleteOnSubmit(food);
                }

                // ✅ CẬP NHẬT TRẠNG THÁI BOOKING THÀNH "ĐÃ HỦY" hoặc XÓA
                booking.trang_thai_Dat_Ve = "Đã Hủy";

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Huỷ đơn đặt: Booking {bookingId}, giải phóng {allVesInBooking.Count} vé, xóa {foodOrders.Count} đồ ăn");

                return Ok(new
                {
                    success = true,
                    message = "Huỷ đơn đặt thành công"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/customer/create-review
        /// Tạo đánh giá phim
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("create-review")]
        [Authorize]
        public IHttpActionResult CreateReview([FromBody] JObject data)
        {
            try
            {
                int customerId = data["customer_id"]?.Value<int>() ?? 0;
                int movieId = data["movie_id"]?.Value<int>() ?? 0;
                int rating = data["rating"]?.Value<int>() ?? 0;
                string content = data["content"]?.Value<string>();
                int? veId = data["ticket_id"]?.Value<int>();

                // ✅ Validation
                if (customerId <= 0)
                    return BadRequest("Customer ID không hợp lệ");

                if (movieId <= 0)
                    return BadRequest("Movie ID không hợp lệ");

                if (rating < 1 || rating > 5)
                    return BadRequest("Rating phải từ 1-5");

                if (string.IsNullOrWhiteSpace(content))
                    return BadRequest("Nội dung đánh giá không được rỗng");

                // ✅ Tạo đánh giá
                var review = new Danh_Gia
                {
                    khach_hang_id = customerId,
                    phim_id = movieId,
                    diem_rating = rating,
                    noi_dung = content,
                    ngay_Danh_Gia = DateTime.Now
                };

                // ✅ Nếu có ticket_id, liên kết vé
                if (veId.HasValue && veId > 0)
                {
                    var ticket = db.Ves.FirstOrDefault(v => v.ve_id == veId);
                    if (ticket != null)
                    {
                        review.ve_id = veId.Value;
                    }
                }

                db.Danh_Gias.InsertOnSubmit(review);
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Tạo đánh giá: Movie {movieId}, Rating {rating}");

                return Ok(new
                {
                    success = true,
                    message = "Tạo đánh giá thành công"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/seats/{showtimeId}
        /// Lấy danh sách ghế của một suất chiếu - GIỐNG WEB (với loại ghế + tính giá động)
        /// </summary>
        [HttpGet]
        [Route("seats/{showtimeId}")]
        [AllowAnonymous]
        public IHttpActionResult GetSeats(int showtimeId)
        {
            try
            {
                if (showtimeId <= 0)
                {
                    return BadRequest("Showtime ID không hợp lệ");
                }

                var showtime = db.Suat_Chieus.FirstOrDefault(s => s.suat_chieu_id == showtimeId);
                if (showtime == null || showtime.Phong_Chieu == null)
                {
                    return NotFound();
                }

                // ✅ Lấy hệ số giá ngày
                var loaiNgay = db.Loai_Ngays.FirstOrDefault(ln => ln.loai_ngay_id == showtime.loai_ngay_id);
                decimal hesoNgay = loaiNgay?.phu_phi ?? 0m;

                // ✅ Lấy danh sách ghế đã đặt (chỉ những ghế có booking "Đã Thanh toán")
                var paidBookingIds = db.Dat_Ves
                    .Where(b => b.trang_thai_Dat_Ve == "Đã Thanh toán")
                    .Select(b => b.Dat_Ve_id)
                    .ToList();

                var bookedSeats = db.Ves
                    .Where(v => v.suat_chieu_id == showtimeId
                        && v.Dat_Ve_id != null
                        && paidBookingIds.Contains(v.Dat_Ve_id.Value))
                    .Select(v => v.ghe_id)
                    .ToList();

                // ✅ Tính giá động cho từng ghế: Giá = GiáGốc + (GiáGốc × HesoNgay%) + (GiáGốc × PhuPhiGhe%)
                var seats = showtime.Phong_Chieu.Ghes
                    .OrderBy(g => g.hang)
                    .ThenBy(g => g.cot)
                    .Select(g => new
                    {
                        seat_id = g.ghe_id,
                        seat_number = g.so_ghe ?? "N/A",
                        row = ((char)('A' + g.hang)).ToString(), // ✅ Convert int (0,1,2...) to String (A,B,C...)
                        column = g.cot,
                        seat_type = g.Loai_Ghe != null ? new
                        {
                            type_id = g.Loai_Ghe.loaighe_id,
                            name = g.Loai_Ghe.ten_loai,
                            surcharge = g.Loai_Ghe.phu_phi ?? 0
                        } : null,
                        status = bookedSeats.Contains(g.ghe_id) ? "booked" : (g.trang_thai == 0 ? "aisle" : "available"),
                        // ✅ GIÁ ĐỘNG: Giá gốc + (Giá gốc × Hệ số ngày %) + (Giá gốc × Phí ghế %)
                        price = showtime.gia_ve +
                                (showtime.gia_ve * hesoNgay / 100) +
                                (showtime.gia_ve * (g.Loai_Ghe != null ? (g.Loai_Ghe.phu_phi ?? 0) : 0) / 100)
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
                        base_price = showtime.gia_ve,
                        day_surcharge_percent = hesoNgay,
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// GET: api/customer/invoice/{bookingId}
        /// Lấy thông tin hóa đơn (in hóa đơn)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("invoice/{bookingId}")]
        [Authorize]
        public IHttpActionResult GetInvoice(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                // ✅ Kiểm tra quyền (khách hàng chỉ xem được hóa đơn của mình)
                var customerId = System.Web.HttpContext.Current?.Session["CustomerId"] as int?;
                if (customerId.HasValue && booking.khach_hang_id != customerId.Value)
                {
                    // Cho phép nếu là admin hoặc staff (để staff xem được hóa đơn khách hàng)
                    var userRole = System.Web.HttpContext.Current?.Session["UserRole"] as string;
                    if (userRole != "Admin" && userRole != "Staff")
                    {
                        return Unauthorized();
                    }
                }

                var tickets = booking.Ves.ToList();
                var firstTicket = tickets.FirstOrDefault();
                var showtime = firstTicket?.Suat_Chieu;

                // ✅ Tính toán thông tin hóa đơn
                decimal ticketTotal = tickets.Sum(t => t.gia_ve);
                decimal foodTotal = 0;
                var foodItems = new List<object>();

                foreach (var foodOrder in booking.DonHang_DoAns != null ? booking.DonHang_DoAns.ToList() : new List<DonHang_DoAn>())
                {
                    var food = foodOrder.Do_An;
                    if (food == null) continue;

                    decimal unitPrice = food.gia ?? 0m;
                    decimal itemTotal = unitPrice * foodOrder.so_luong;
                    foodTotal += itemTotal;

                    foodItems.Add(new
                    {
                        food_id = food.Do_An_id,
                        food_name = food.ten_san_pham,
                        price = unitPrice,
                        quantity = foodOrder.so_luong,
                        total_price = itemTotal
                    });
                }

                var invoice = new
                {
                    booking_id = booking.Dat_Ve_id,
                    customer_name = booking.Khach_Hang != null ? booking.Khach_Hang.ho_ten : "N/A",
                    customer_email = booking.Khach_Hang != null ? booking.Khach_Hang.email : "N/A",
                    customer_phone = booking.Khach_Hang != null ? booking.Khach_Hang.so_dien_thoai : "N/A",
                    // ✅ Thêm các cột mới
                    customer_dob = booking.Khach_Hang != null && booking.Khach_Hang.ngay_sinh.HasValue 
                        ? booking.Khach_Hang.ngay_sinh.Value.ToString("yyyy-MM-dd") 
                        : "N/A",
                    customer_gender = booking.Khach_Hang != null ? booking.Khach_Hang.gioi_tinh ?? "N/A" : "N/A",
                    customer_address = booking.Khach_Hang != null ? booking.Khach_Hang.dia_chi ?? "N/A" : "N/A",
                    created_at = booking.ngay_tao.HasValue ? booking.ngay_tao.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A",
                    status = booking.trang_thai_Dat_Ve,
                    movie = showtime != null && showtime.Phim != null ? new
                    {
                        movie_id = showtime.Phim.phim_id,
                        title = showtime.Phim.ten_phim
                    } : null,
                    showtime = showtime != null ? new
                    {
                        cinema = showtime.Phong_Chieu != null && showtime.Phong_Chieu.Rap != null ? showtime.Phong_Chieu.Rap.ten_rap : "N/A",
                        room = showtime.Phong_Chieu != null ? showtime.Phong_Chieu.ten_phong : "N/A",
                        date = showtime.ngay_chieu.Date.ToString("yyyy-MM-dd"),
                        time = showtime.Ca_Chieu != null ? showtime.Ca_Chieu.gio_bat_dau.ToString() : "N/A"
                    } : null,
                    tickets = tickets.Select(t => new
                    {
                        ticket_id = t.ve_id,
                        seat_number = t.Ghe != null ? t.Ghe.so_ghe : "N/A",
                        qr_code = t.ma_qr_code,
                        price = t.gia_ve,
                        status = t.trang_thai_ve
                    }).ToList(),
                    food_items = foodItems,
                    ticket_total = ticketTotal,
                    food_total = foodTotal,
                    grand_total = ticketTotal + foodTotal
                };

                LoggingHelper.LogInfo($"✅ Lấy hóa đơn: Booking {bookingId}");

                return Ok(new
                {
                    success = true,
                    message = "Lấy hóa đơn thành công",
                    data = invoice
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// GET: api/customer/invoice/{bookingId}/qr-code
        /// Lấy QR code thanh toán của hóa đơn
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("invoice/{bookingId}/qr-code")]
        [Authorize]
        public IHttpActionResult GetInvoiceQRCode(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                // ✅ Kiểm tra quyền
                var customerId = System.Web.HttpContext.Current?.Session["CustomerId"] as int?;
                if (customerId.HasValue && booking.khach_hang_id != customerId.Value)
                {
                    var userRole = System.Web.HttpContext.Current?.Session["UserRole"] as string;
                    if (userRole != "Admin" && userRole != "Staff")
                    {
                        return Unauthorized();
                    }
                }

                // ✅ Tạo QR code thanh toán
                var qrService = new QRCodePaymentService();
                string qrDescription = qrService.GenerateTransactionDescription(bookingId);
                string qrCodeUrl = qrService.GenerateQRCodeUrl(booking.tong_tien, qrDescription);

                LoggingHelper.LogInfo($"✅ Lấy QR code: Booking {bookingId}, Tổng tiền: {booking.tong_tien}");

                return Ok(new
                {
                    success = true,
                    message = "Lấy QR code thành công",
                    data = new
                    {
                        booking_id = bookingId,
                        qr_code_url = qrCodeUrl,
                        description = qrDescription,
                        amount = booking.tong_tien,
                        currency = "VNĐ"
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
        /// GET: api/customer/profile/{customerId}
        /// Lấy thông tin profile khách hàng (đầy đủ bao gồm các cột mới)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpGet]
        [Route("profile/{customerId}")]
        [Authorize]
        public IHttpActionResult GetCustomerProfile(int customerId)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest("Customer ID không hợp lệ");

                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);
                if (customer == null)
                    return NotFound();

                var profile = new
                {
                    customer_id = customer.khach_hang_id,
                    full_name = customer.ho_ten,
                    email = customer.email,
                    phone = customer.so_dien_thoai,
                    // ✅ Thêm các cột mới
                    date_of_birth = customer.ngay_sinh.HasValue ? customer.ngay_sinh.Value.ToString("yyyy-MM-dd") : null,
                    gender = customer.gioi_tinh ?? "N/A",
                    address = customer.dia_chi ?? "N/A",
                    registration_date = customer.ngay_dang_ky.HasValue ? customer.ngay_dang_ky.Value.ToString("yyyy-MM-dd") : "N/A",
                    total_bookings = customer.Dat_Ves.Count,
                    total_spent = customer.Dat_Ves.Sum(d => (decimal?)d.tong_tien) ?? 0m
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin profile thành công",
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
        /// PUT: api/customer/profile/{customerId}
        /// Cập nhật thông tin profile khách hàng
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPut]
        [Route("profile/{customerId}")]
        [Authorize]
        public IHttpActionResult UpdateCustomerProfile(int customerId, [FromBody] JObject data)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest("Customer ID không hợp lệ");

                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);
                if (customer == null)
                    return NotFound();

                // ✅ Cập nhật các field
                if (data["full_name"] != null)
                    customer.ho_ten = data["full_name"].Value<string>();

                if (data["phone"] != null)
                    customer.so_dien_thoai = data["phone"].Value<string>();

                // ✅ Cập nhật các cột mới
                if (data["date_of_birth"] != null && DateTime.TryParse(data["date_of_birth"].Value<string>(), out var dob))
                    customer.ngay_sinh = dob;

                if (data["gender"] != null)
                    customer.gioi_tinh = data["gender"].Value<string>();

                if (data["address"] != null)
                    customer.dia_chi = data["address"].Value<string>();

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Cập nhật profile: Customer {customerId}");

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật profile thành công"
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }
    }
}
