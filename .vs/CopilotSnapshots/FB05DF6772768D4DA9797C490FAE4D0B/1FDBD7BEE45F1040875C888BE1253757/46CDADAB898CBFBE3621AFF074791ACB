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
        public IHttpActionResult GetMovies(int page = 1, int pageSize = 10, int? cinemaId = null, int? genreId = null, string language = null, string date = null)
        {
            try
            {
                if (page < 1) return BadRequest("Page phải >= 1");
                if (pageSize < 1 || pageSize > MAX_PAGE_SIZE) pageSize = DEFAULT_PAGE_SIZE;

                // Determine date filter: if provided use it, otherwise default to today
                DateTime today = DateTime.Today;
                DateTime filterDate;
                bool useSpecificDate = false;
                if (!string.IsNullOrEmpty(date))
                {
                    if (!DateTime.TryParse(date, out filterDate))
                        return BadRequest("Định dạng ngày không hợp lệ. Vui lòng dùng yyyy-MM-dd");
                    useSpecificDate = true;
                }
                else
                {
                    filterDate = today;
                }

                // Build showtime query similar to HomeController
                var showtimeQuery = db.Suat_Chieus.AsQueryable();

                if (useSpecificDate)
                {
                    showtimeQuery = showtimeQuery.Where(sc => sc.ngay_chieu == filterDate);
                }
                else
                {
                    showtimeQuery = showtimeQuery.Where(sc => sc.ngay_chieu >= today);
                }

                if (cinemaId.HasValue && cinemaId.Value > 0)
                {
                    var roomIds = db.Phong_Chieus
                        .Where(pc => pc.rap_id == cinemaId.Value)
                        .Select(pc => pc.phong_chieu_id)
                        .ToList();

                    showtimeQuery = showtimeQuery.Where(sc => roomIds.Contains(sc.phong_chieu_id));
                }

                if (!string.IsNullOrEmpty(language))
                {
                    showtimeQuery = showtimeQuery.Where(sc => sc.ngon_ngu == language);
                }

                // Now get movie IDs that have matching showtimes
                var movieIdsWithShowtimes = showtimeQuery
                    .Select(sc => sc.phim_id)
                    .Distinct()
                    .ToList();

                // If genre filter is present, intersect with genre list
                List<Phim> moviesList;
                if (genreId.HasValue && genreId.Value > 0)
                {
                    var movieIdsWithGenre = db.Phim_LoaiPhims
                        .Where(plp => plp.loaiphim_id == genreId.Value)
                        .Select(plp => plp.phim_id)
                        .ToList();

                    moviesList = db.Phims
                        .Where(p => movieIdsWithShowtimes.Contains(p.phim_id) && movieIdsWithGenre.Contains(p.phim_id))
                        .ToList();
                }
                else
                {
                    moviesList = db.Phims
                        .Where(p => movieIdsWithShowtimes.Contains(p.phim_id))
                        .ToList();
                }

                // Build view models similar to HomeController
                var movieService = new Services.MovieService();
                var movieViewModels = moviesList.Select(m => new
                {
                    movie_id = m.phim_id,
                    title = m.ten_phim,
                    description = m.mo_ta,
                    duration = m.thoi_luong,
                    release_date = m.ngay_khoi_chieu,
                    image = m.hinh_anh,
                    genres = m.Phim_LoaiPhims != null ? m.Phim_LoaiPhims.Select(pl => pl.Loai_Phim != null ? pl.Loai_Phim.ten_loai : null).Where(x => x != null).ToList() : new List<string>(),
                    avg_rating = movieService.GetAverageRating(m.phim_id),
                    rating_count = movieService.GetRatingCount(m.phim_id)
                }).ToList();

                int total = movieViewModels.Count;
                int totalPages = (int)Math.Ceiling(total / (double)pageSize);

                var paged = movieViewModels
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách phim thành công",
                    data = new
                    {
                        movies = paged,
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
        public IHttpActionResult GetMovieDetail(int movieId, bool includeInactive = false)
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

                // ✅ Kiểm tra trạng thái phim (allow override via query param includeInactive)
                if (!includeInactive && movie.trang_thai != "Đang chiếu")
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
        /// ✅ Support mã khuyến mãi KM004
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
                string promoCode = data["promo_code"]?.Value<string>() ?? "";

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

                // ✅ KIỂM TRA VÀ ÁP DỤNG MÃ KHUYẾN MÃI KM004
                decimal discountAmount = 0m;
                string appliedPromoCode = "";

                if (!string.IsNullOrEmpty(promoCode))
                {
                    // ✅ KIỂM TRA MÃ KHUYẾN MÃI CÓ TỒN TẠI KHÔNG
                    var promo = db.Khuyen_Mais.FirstOrDefault(k => k.ma_khuyen_mai == promoCode);
                    if (promo == null)
                    {
                        return Ok(new { success = false, message = "Mã khuyến mãi không tồn tại" });
                    }

                    // ✅ KIỂM TRA MÃ KHUYẾN MÃI HOẠT ĐỘNG KHÔNG
                    if (promo.trang_thai != "Hoạt động")
                    {
                        return Ok(new { success = false, message = "Mã khuyến mãi không hoạt động" });
                    }

                    if (promo.so_luong_con_lai <= 0)
                    {
                        return Ok(new { success = false, message = "Mã khuyến mãi đã hết" });
                    }

                    // ✅ KM004: KIỂM TRA CÓ COMBO TRONG ĐỒ ĂN KHÔNG
                    if (promoCode == "KM004")
                    {
                        bool hasCombo = false;
                        try
                        {
                            foreach (var item in foodItems)
                            {
                                if (item == null) continue;
                                int foodId = item["food_id"]?.Value<int>() ?? 0;

                                if (foodId > 0)
                                {
                                    var food = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == foodId);
                                    if (food != null)
                                    {
                                        // ✅ KIỂM TRA 1: Cột loai (FoodType) - CHÍNH
                                        if ((food.loai ?? "").IndexOf("combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            hasCombo = true;
                                            LoggingHelper.LogInfo($"✅ KM004: Found Combo in loai field for food {foodId}");
                                            break;
                                        }

                                        // ✅ KIỂM TRA 2: Cột ten_san_pham (FoodName) - PHỤ
                                        if ((food.ten_san_pham ?? "").IndexOf("combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            hasCombo = true;
                                            LoggingHelper.LogInfo($"✅ KM004: Found Combo in ten_san_pham field for food {foodId}");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingHelper.LogError(ex, "Error checking KM004 combo");
                        }

                        if (!hasCombo)
                        {
                            return Ok(new { success = false, message = "Mã KM004 cần mua Combo để áp dụng" });
                        }
                    }

                    // ✅ TÍNH TOÁN DISCOUNT
                    decimal baseTotal = ticketTotal + foodTotal;
                    if (promo.loai_giam_gia == "%" || (promo.loai_giam_gia ?? "").Contains("%"))
                    {
                        discountAmount = (baseTotal * promo.gia_tri_giam) / 100m;
                    }
                    else
                    {
                        discountAmount = promo.gia_tri_giam;
                    }

                    appliedPromoCode = promoCode;
                    LoggingHelper.LogInfo($"✅ API Applied {promoCode}: Base={baseTotal}, Discount={discountAmount}");
                }

                // ✅ TẠOBOOKING VỚI TRẠNG THÁI "CHƯA THANH TOÁN" NGAY (GIỐNG WEB)
                decimal totalBeforeDiscount = ticketTotal + foodTotal;
                decimal totalAfterDiscount = totalBeforeDiscount - discountAmount;
                if (totalAfterDiscount < 0) totalAfterDiscount = 0;

                var booking = new Dat_Ve
                {
                    khach_hang_id = customerId,
                    ngay_tao = DateTime.Now,
                    trang_thai_Dat_Ve = "Chưa thanh toán", // ✅ NGAY LẬP TỨC
                    tong_tien = totalAfterDiscount, // ✅ Giá sau discount
                    phuong_thuc_thanh_toan = "vnpay"
                };

                db.Dat_Ves.InsertOnSubmit(booking);
                db.SubmitChanges();

                // ✅ LƯU ORIGINAL TOTAL VÀO SESSION - TRƯỚC KHI ÁP DỤNG PROMO
                // Đây là tổng tiền gốc (vé + đồ ăn) chưa có discount
                // Nếu user chọn promo sau, sẽ tính discount dựa trên original này
                try
                {
                    var session = System.Web.HttpContext.Current?.Session;
                    if (session != null)
                    {
                        // Lưu original total vào session để ApplyPromoToBooking sử dụng
                        session[$"Booking_{booking.Dat_Ve_id}_OriginalTotal"] = totalBeforeDiscount;
                        LoggingHelper.LogInfo($"✅ Lưu Session Booking_{booking.Dat_Ve_id}_OriginalTotal = {totalBeforeDiscount}");
                    }
                }
                catch (Exception sessEx)
                {
                    LoggingHelper.LogError(sessEx, $"Failed to save original total to session for booking {booking.Dat_Ve_id}");
                }

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

                // ✅ Giảm số lượng mã khuyến mãi nếu đã áp dụng
                if (!string.IsNullOrEmpty(appliedPromoCode))
                {
                    var promo = db.Khuyen_Mais.FirstOrDefault(k => k.ma_khuyen_mai == appliedPromoCode);
                    if (promo != null && promo.so_luong_con_lai > 0)
                    {
                        promo.so_luong_con_lai--;
                        db.SubmitChanges();
                        LoggingHelper.LogInfo($"✅ Decrement KM004: Remaining {promo.so_luong_con_lai}");
                    }
                }

                LoggingHelper.LogInfo($"✅ Tạo đơn đặt online: Booking ID {booking.Dat_Ve_id}, Trạng thái: Chưa thanh toán, Vé: {selectedTickets.Count}, PromoCode: {appliedPromoCode}");

                return Ok(new
                {
                    success = true,
                    message = "Tạo đơn đặt thành công",
                    data = new
                    {
                        booking_id = booking.Dat_Ve_id,
                        ticket_total = ticketTotal,
                        food_total = foodTotal,
                        discount_amount = discountAmount,
                        applied_promo_code = appliedPromoCode,
                        total_amount = booking.tong_tien,
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
                {
                    return NotFound();
                }

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
        public IHttpActionResult GetSeats(int showtimeId, bool flat = false)
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
                // Use explicit query to ensure we load all seats from DB and avoid lazy-loading/nav property issues
                int roomId = showtime.Phong_Chieu.phong_chieu_id;
                var allSeats = db.Ghes
                    .Where(g => g.phong_chieu_id == roomId)
                    .OrderBy(g => g.hang)
                    .ThenBy(g => g.cot)
                    .ToList();

                var seats = allSeats
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

                // Build seat summary for diagnostics
                int totalSeatsCount = allSeats.Count;
                int bookedCount = seats.Count(s => s.status == "booked");
                int availableCount = seats.Count(s => s.status == "available");
                var typeSummary = allSeats
                    .GroupBy(g => g.Loai_Ghe != null ? g.Loai_Ghe.ten_loai : "Unknown")
                    .Select(gr => new { type = gr.Key, count = gr.Count() })
                    .ToList();

                // Use explicit room values for rows/columns
                int rowsCount = showtime.Phong_Chieu?.so_hang ?? (allSeats.Any() ? allSeats.Max(s => s.hang) + 1 : 0);
                int columnsCount = showtime.Phong_Chieu?.so_cot ?? (allSeats.Any() ? allSeats.Max(s => s.cot) : 0);

                // If client requests a flat layout, return layout marker and zero rows/columns
                if (flat)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Lấy danh sách ghế thành công (flat layout)",
                        data = new
                        {
                            showtime_id = showtimeId,
                            layout = "flat",
                            rows = 0,
                            columns = 0,
                            base_price = showtime.gia_ve,
                            day_surcharge_percent = hesoNgay,
                            total_seats = totalSeatsCount,
                            booked_seats = bookedCount,
                            available_seats = availableCount,
                            seat_type_summary = typeSummary,
                            seats = seats
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách ghế thành công",
                    data = new
                    {
                        showtime_id = showtimeId,
                        rows = rowsCount,
                        columns = columnsCount,
                        base_price = showtime.gia_ve,
                        day_surcharge_percent = hesoNgay,
                        total_seats = totalSeatsCount,
                        booked_seats = bookedCount,
                        available_seats = availableCount,
                        seat_type_summary = typeSummary,
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
        /// POST: api/customer/confirm-qr-payment
        /// Xác nhận thanh toán QR (cập nhật booking thành "Chờ Duyệt", admin sẽ duyệt)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("confirm-qr-payment")]
        [Authorize]
        public IHttpActionResult ConfirmQRPayment([FromBody] JObject data)
        {
            try
            {
                int bookingId = data["booking_id"]?.Value<int>() ?? 0;
                string promoCode = data["promo_code"]?.Value<string>() ?? "";

                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                // ✅ Kiểm tra trạng thái - phải là "Chưa thanh toán"
                if (booking.trang_thai_Dat_Ve != "Chưa thanh toán")
                {
                    return Ok(new { success = false, message = "Đơn hàng đã được xử lý hoặc bị hủy" });
                }

                // ✅ NẾU CÓ PROMO CODE, GIẢM SỐ LƯỢNG (nếu chưa giảm lúc apply)
                if (!string.IsNullOrEmpty(promoCode))
                {
                    var promoCodeRecord = db.Khuyen_Mais.FirstOrDefault(km => km.ma_khuyen_mai == promoCode);
                    
                    if (promoCodeRecord != null && (promoCodeRecord.so_luong_con_lai ?? 0) > 0)
                    {
                        // ✅ Giảm số lượng mã
                        promoCodeRecord.so_luong_con_lai--;
                        db.SubmitChanges();
                        
                        LoggingHelper.LogInfo($"✅ Giảm mã khuyến mãi: {promoCode}, Còn lại: {promoCodeRecord.so_luong_con_lai}");
                    }
                }

                // ✅ Cập nhật trạng thái thành "Chờ Duyệt" (chờ Admin duyệt)
                booking.trang_thai_Dat_Ve = "Chờ Duyệt";
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Xác nhận QR Payment: Booking ID {bookingId}, Trạng thái: Chờ Duyệt, PromoCode: {promoCode ?? "None"}");

                return Ok(new
                {
                    success = true,
                    message = "Đơn hàng đã gửi, vui lòng chờ Admin duyệt",
                    booking_id = booking.Dat_Ve_id,
                    status = booking.trang_thai_Dat_Ve
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/customer/check-booking-status
        /// Kiểm tra trạng thái booking (dùng để polling xem có được Admin duyệt không)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("check-booking-status")]
        [Authorize]
        public IHttpActionResult CheckBookingStatus([FromBody] JObject data)
        {
            try
            {
                int bookingId = data["booking_id"]?.Value<int>() ?? 0;

                if (bookingId <= 0)
                    return BadRequest("Booking ID không hợp lệ");

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null)
                    return NotFound();

                return Ok(new
                {
                    success = true,
                    booking_id = booking.Dat_Ve_id,
                    status = booking.trang_thai_Dat_Ve,
                    total_amount = booking.tong_tien
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
                int? customerId = null;
                try
                {
                    var session = System.Web.HttpContext.Current?.Session;
                    if (session != null && session["CustomerId"] != null)
                    {
                        // session value might be stored as string or int
                        var raw = session["CustomerId"];
                        if (raw is int) customerId = (int)raw;
                        else if (raw is string) {
                            if (int.TryParse((string)raw, out var tmp)) customerId = tmp;
                        } else {
                            try { customerId = Convert.ToInt32(raw); } catch { customerId = null; }
                        }
                    }
                }
                catch (Exception sessEx)
                {
                    LoggingHelper.LogError(sessEx, "Session read failed");
                    customerId = null;
                }

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

                // ✅ THÊM CHI TIẾT TỪNG VÉ KÈM QR CODE (QUAN TRỌNG!)
                var ticketDetails = tickets.Select(t => new
                {
                    ticket_id = t.ve_id,
                    seat_id = t.ghe_id,
                    seat_number = t.Ghe != null ? t.Ghe.so_ghe : "N/A",
                    row = t.Ghe != null ? ((char)('A' + t.Ghe.hang)).ToString() : "N/A",
                    column = t.Ghe != null ? t.Ghe.cot : 0,
                    seat_type = t.Ghe != null && t.Ghe.Loai_Ghe != null ? t.Ghe.Loai_Ghe.ten_loai : "N/A",
                    price = t.gia_ve,
                    status = t.trang_thai_ve,
                    qr_code = t.ma_qr_code  // ✅ QR CODE CHO TỪNG VÉ
                }).ToList();

                var invoice = new
                {
                    booking_id = booking.Dat_Ve_id,
                    customer_name = booking.Khach_Hang != null ? booking.Khach_Hang.ho_ten : "N/A",
                    customer_email = booking.Khach_Hang != null ? booking.Khach_Hang.email : "N/A",
                    customer_phone = booking.Khach_Hang != null ? booking.Khach_Hang.so_dien_thoai : "N/A",
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
                        room = showtime.Phong_Chieu.ten_phong,
                        date = showtime.ngay_chieu.Date.ToString("yyyy-MM-dd"),
                        time = showtime.Ca_Chieu != null ? showtime.Ca_Chieu.gio_bat_dau.ToString(@"hh\:mm") : "N/A"
                    } : null,
                    tickets = ticketDetails,  // ✅ CHI TIẾT TỪNG VÉ
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
                int? customerId = null;
                try
                {
                    var session = System.Web.HttpContext.Current?.Session;
                    if (session != null && session["CustomerId"] != null)
                    {
                        // session value might be stored as string or int
                        var raw = session["CustomerId"];
                        if (raw is int) customerId = (int)raw;
                        else if (raw is string) {
                          if (int.TryParse((string)raw, out var tmp)) customerId = tmp;
                        } else {
                          try { customerId = Convert.ToInt32(raw); } catch { customerId = null; }
                        }
                    }
                }
                catch (Exception sessEx)
                {
                    LoggingHelper.LogError(sessEx, "Session read failed");
                    customerId = null;
                }

                if (customerId.HasValue && booking.khach_hang_id != customerId.Value)
                {
                    var userRole = System.Web.HttpContext.Current?.Session["UserRole"] as string;
                    if (userRole != "Admin" && userRole != "Staff")
                    {
                        return Unauthorized();
                    }
                }

                // ✅ Tạo QR code thanh toán
                try
                {
                    // Use fully-qualified type in case of ambiguous references
                    var qrService = new WebCinema.Infrastructure.QRCodePaymentService();

                    decimal amount = 0m;
                    try
                    {
                        amount = booking.tong_tien;
                    }
                    catch
                    {
                        // fallback if tong_tien is non-nullable
                        amount = Convert.ToDecimal(booking.tong_tien);
                    }

                    string qrDescription = qrService.GenerateTransactionDescription(bookingId) ?? string.Empty;
                    string qrCodeUrl = qrService.GenerateQRCodeUrl(amount, qrDescription) ?? string.Empty;

                    LoggingHelper.LogInfo($"✅ Lấy QR code: Booking {bookingId}, Tổng tiền: {amount}");

                    return Ok(new
                    {
                        success = true,
                        message = "Lấy QR code thành công",
                        data = new
                        {
                            booking_id = bookingId,
                            qr_code_url = qrCodeUrl,
                            description = qrDescription,
                            amount = amount,
                            currency = "VNĐ"
                        }
                    });
                }
                catch (Exception qrEx)
                {
                    LoggingHelper.LogError(qrEx);
                    return InternalServerError(new Exception("Lỗi khi tạo QR code: " + qrEx.Message, qrEx));
                }
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

        /// <summary>
        /// POST: api/customer/check-promo
        /// Kiểm tra xem mã khuyến mãi có áp dụng được không
        /// Hiện tại chỉ support KM004 (cần mua Combo)
        /// </summary>
        [HttpPost]
        [Route("check-promo")]
        [AllowAnonymous]
        public IHttpActionResult CheckPromoCode([FromBody] JObject data)
        {
            try
            {
                string promoCode = data["promo_code"]?.Value<string>() ?? "";
                var foodItems = data["food_items"]?.ToObject<List<JObject>>() ?? new List<JObject>();

                if (string.IsNullOrWhiteSpace(promoCode))
                {
                    return BadRequest("Vui lòng nhập mã khuyến mãi");
                }

                // ✅ Hiện tại chỉ hỗ trợ KM004
                if (promoCode != "KM004")
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi này chưa được hỗ trợ trên API" });
                }

                var promo = db.Khuyen_Mais.FirstOrDefault(k => k.ma_khuyen_mai == promoCode);
                if (promo == null)
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi không tồn tại" });
                }

                // Kiểm tra số lượng còn lại
                if (promo.so_luong_con_lai <= 0)
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi đã hết" });
                }

                // Kiểm tra trạng thái
                if (promo.trang_thai != "Hoạt động")
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi hiện không hoạt động" });
                }

                // Kiểm tra ngày
                if (promo.ngay_bat_dau != DateTime.MinValue && DateTime.Now < promo.ngay_bat_dau)
                {
                    return Ok(new { success = false, message = $"Mã này chưa hoạt động. Bắt đầu từ {promo.ngay_bat_dau:dd/MM/yyyy}" });
                }

                if (promo.ngay_ket_thuc != DateTime.MinValue && DateTime.Now > promo.ngay_ket_thuc)
                {
                    return Ok(new { success = false, message = $"Mã này đã hết hạn (kết thúc {promo.ngay_ket_thuc:dd/MM/yyyy})" });
                }

                // ✅ KM004: Kiểm tra Combo
                if (promoCode == "KM004")
                {
                    bool hasCombo = false;

                    foreach (var item in foodItems)
                    {
                        if (item == null) continue;

                        int foodId = item["food_id"]?.Value<int>() ?? 0;

                        if (foodId > 0)
                        {
                            var food = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == foodId);
                            if (food != null)
                            {
                                if ((food.ten_san_pham ?? "").IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    (food.loai ?? "").IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    hasCombo = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!hasCombo)
                    {
                        return Ok(new { success = false, message = "Mã KM004 cần mua Combo để áp dụng" });
                    }
                }

                // ✅ Mã khuyến mãi áp dụng được
                string discountType = promo.loai_giam_gia == "%" || (promo.loai_giam_gia ?? "").Contains("%") ? "%" : "VND";

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        promo_code = promo.ma_khuyen_mai,
                        description = promo.mo_ta,
                        discount_value = promo.gia_tri_giam,
                        discount_type = discountType,
                        remaining_quantity = promo.so_luong_con_lai
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
        /// POST: api/customer/available-promos
        /// Lấy danh sách mã khuyến mãi có thể áp dụng cho khách hàng và các đồ ăn (giống BookingController.GetAvailablePromoCodes)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("available-promos")]
        [Authorize]
        public IHttpActionResult GetAvailablePromoCodes([FromBody] JObject data)
        {
            try
            {
                int customerId = data["customer_id"]?.Value<int>() ?? 0;
                var foodItemsJson = data["food_items_json"]?.Value<string>() ?? "[]";

                var customer = db.Khach_Hangs.FirstOrDefault(k => k.khach_hang_id == customerId);
                if (customer == null)
                {
                    return Ok(new { success = true, data = new List<object>() });
                }

                var promoCodes = db.Khuyen_Mais.ToList();
                var result = new List<object>();

                foreach (var km in promoCodes)
                {
                    try
                    {
                        bool isApplicable = true;
                        string reason = "";

                        string maKhuyen = km.ma_khuyen_mai;

                        if (maKhuyen == "KM001")
                        {
                            if (DateTime.Now.Month != 12)
                            {
                                isApplicable = false; reason = "Chỉ áp dụng tháng 12";
                            }
                        }
                        else if (maKhuyen == "KM003")
                        {
                            int points = customer.diem_tich_luy ?? 0;
                            if (points < 20) { isApplicable = false; reason = $"Cần 20+ điểm (hiện có {points})"; }
                        }
                        else if (maKhuyen == "KM004")
                        {
                            bool hasCombo = false;
                            try
                            {
                                LoggingHelper.LogInfo($"🔍 KM004 Start: foodItemsJson={(!string.IsNullOrEmpty(foodItemsJson) ? "exist" : "null")}");

                                var foodItems = Newtonsoft.Json.Linq.JArray.Parse(foodItemsJson ?? "[]");

                                foreach (var item in foodItems)
                                {
                                    // ✅ KIỂM TRA 1: FoodType (cột loai trong DB) - CHÍNH
                                    string foodType = item["FoodType"]?.ToString() ?? "";
                                    LoggingHelper.LogInfo($"  → Checking FoodType: '{foodType}'");
                                    if (foodType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        hasCombo = true;
                                        LoggingHelper.LogInfo("✅ KM004: Found Combo in FoodType!");
                                        break;
                                    }

                                    // ✅ KIỂM TRA 2: FoodName (cột ten_san_pham trong DB) - PHỤ
                                    string foodName = item["FoodName"]?.ToString() ?? "";
                                    LoggingHelper.LogInfo($"  → Checking FoodName: '{foodName}'");
                                    if (foodName.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        hasCombo = true;
                                        LoggingHelper.LogInfo("✅ KM004: Found Combo in FoodName!");
                                        break;
                                    }

                                    // ✅ KIỂM TRA 3: Database theo FoodId - BACKUP
                                    if (int.TryParse(item["FoodId"]?.ToString(), out int fid))
                                    {
                                        var fobj = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == fid);
                                        if (fobj != null)
                                        {
                                            string dbFoodType = fobj.loai ?? "";
                                            string dbFoodName = fobj.ten_san_pham ?? "";
                                            LoggingHelper.LogInfo($"  → FoodId {fid}: Type='{dbFoodType}', Name='{dbFoodName}'");

                                            if (dbFoodType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                dbFoodName.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                hasCombo = true;
                                                LoggingHelper.LogInfo("✅ KM004: Found Combo in DB!");
                                                break;
                                            }
                                        }
                                    }
                                }

                                LoggingHelper.LogInfo($"✅ KM004 Complete: hasCombo={hasCombo}");
                            }
                            catch (Exception ex)
                            {
                                LoggingHelper.LogError(new Exception("KM004 JSON parse error: " + ex.Message, ex));
                            }

                            if (!hasCombo)
                            {
                                isApplicable = false; reason = "Cần mua Combo";
                            }
                        }

                        // date checks
                        if (isApplicable && km.ngay_bat_dau != DateTime.MinValue && DateTime.Now < km.ngay_bat_dau) { isApplicable = false; reason = "Chưa đến ngày áp dụng"; }
                        if (isApplicable && km.ngay_ket_thuc != DateTime.MinValue && DateTime.Now > km.ngay_ket_thuc) { isApplicable = false; reason = "Đã hết hạn áp dụng"; }
                        if (km.so_luong_con_lai <= 0) { isApplicable = false; reason = "Mã đã hết"; }
                        if (km.trang_thai != "Hoạt động") { isApplicable = false; reason = "Không hoạt động"; }

                        decimal giaTri = km.gia_tri_giam;
                        string loaiGiam = "%";
                        if (!string.IsNullOrEmpty(km.loai_giam_gia) && !(km.loai_giam_gia.Contains("Phần") || km.loai_giam_gia.Contains("%"))) loaiGiam = "VND";

                        result.Add(new
                        {
                            maKhuyen = km.ma_khuyen_mai,
                            moTa = km.mo_ta ?? "",
                            giaTriGiam = giaTri,
                            loaiGiam = loaiGiam,
                            soLuongConLai = km.so_luong_con_lai,
                            isApplicable = isApplicable,
                            reason = reason
                        });
                    }
                    catch (Exception itemEx)
                    {
                        LoggingHelper.LogError(itemEx);
                    }
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// POST: api/customer/booking/{bookingId}/apply-promo
        /// Áp dụng mã khuyến mãi lên booking và trả về QR mới (giống BookingController.UpdateQRCodeWithPromo)
        /// ✅ Yêu cầu xác thực
        /// </summary>
        [HttpPost]
        [Route("booking/{bookingId:int}/apply-promo")]
        [Authorize]
        public IHttpActionResult ApplyPromoToBooking(int bookingId, [FromBody] JObject data)
        {
            try
            {
                string promoCode = data["promo_code"]?.Value<string>() ?? string.Empty;

                var booking = db.Dat_Ves.FirstOrDefault(b => b.Dat_Ve_id == bookingId);
                if (booking == null) return NotFound();

                // ✅ LẤY ORIGINAL TOTAL TỪ SESSION (GIỐNG WEB BookingController.UpdateQRCodeWithPromo)
                // Nếu có session thì dùng, nếu không thì dùng booking hiện tại
                decimal originalTotal = booking.tong_tien;
                try
                {
                    var session = System.Web.HttpContext.Current?.Session;
                    if (session != null && session[$"Booking_{bookingId}_OriginalTotal"] != null)
                    {
                        var raw = session[$"Booking_{bookingId}_OriginalTotal"];
                        if (raw is decimal) originalTotal = (decimal)raw;
                        else if (raw is double) originalTotal = Convert.ToDecimal((double)raw);
                        else if (raw is string && decimal.TryParse((string)raw, out var parsed)) originalTotal = parsed;
                        else
                        {
                            try { originalTotal = Convert.ToDecimal(raw); } catch { }
                        }
                        LoggingHelper.LogInfo($"✅ Read Session Booking_{bookingId}_OriginalTotal = {originalTotal}");
                    }
                }
                catch (Exception sessEx)
                {
                    LoggingHelper.LogError(sessEx, $"Failed to read original total from session for booking {bookingId}");
                }

                decimal discountAmount = 0m;
                string discountType = "";

                // ✅ TÍNH TOÁN DISCOUNT NẾU CÓ PROMO CODE
                if (!string.IsNullOrEmpty(promoCode))
                {
                    var promo = db.Khuyen_Mais.FirstOrDefault(km => km.ma_khuyen_mai == promoCode);
                    if (promo != null)
                    {
                        decimal giaTri = promo.gia_tri_giam;
                        // ✅ XÁC ĐỊNH LOẠI GIẢM (% HAY VND) - GIỐNG WEB
                        bool isPercent = !string.IsNullOrEmpty(promo.loai_giam_gia) && 
                            (promo.loai_giam_gia.Contains("Phần") || promo.loai_giam_gia.Contains("%"));
                        
                        if (isPercent)
                        {
                            discountAmount = (originalTotal * giaTri) / 100m;
                            discountType = "%";
                        }
                        else
                        {
                            discountAmount = giaTri;
                            discountType = "VND";
                        }

                        LoggingHelper.LogInfo($"✅ PromoCode {promoCode}: Original={originalTotal}, Discount={discountAmount}, Type={discountType}");
                    }
                }

                // ✅ TÍNH TỔNG TIỀN CUỐI CÙNG
                decimal finalTotal = originalTotal - discountAmount;
                if (finalTotal < 0) finalTotal = 0;

                // ✅ CẬP NHẬT BOOKING VỚI TỔNG TIỀN MỚI
                booking.tong_tien = finalTotal;
                booking.phuong_thuc_thanh_toan = promoCode ?? "";
                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Updated Dat_Ve: ID={bookingId}, FinalTotal={finalTotal}");

                // ✅ GIẢM SỐ LƯỢNG MÃ KHUYẾN MÃI NẾU ÁP DỤNG (GIỐNG WEB UpdateQRCodeWithPromo)
                if (!string.IsNullOrEmpty(promoCode))
                {
                    var promoObj = db.Khuyen_Mais.FirstOrDefault(km => km.ma_khuyen_mai == promoCode);
                    if (promoObj != null && promoObj.so_luong_con_lai > 0)
                    {
                        promoObj.so_luong_con_lai--;
                        db.SubmitChanges();
                        LoggingHelper.LogInfo($"✅ Decrement {promoCode}: Remaining={promoObj.so_luong_con_lai}");
                    }
                }

                // ✅ TẠO QR CODE MỚI VỚI GIÁ ĐÚNG
                var qrService = new WebCinema.Infrastructure.QRCodePaymentService();
                string desc = qrService.GenerateTransactionDescription(bookingId);
                string newQr = qrService.GenerateQRCodeUrl(finalTotal, desc);

                return Ok(new
                {
                    success = true,
                    new_qr_url = newQr,
                    final_total = finalTotal,
                    discount_amount = discountAmount,
                    discount_type = discountType
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return InternalServerError(ex);
            }
        }

        // Helper: detect Combo in various food item representations sent by clients
        private bool ContainsComboInFoodItems(Newtonsoft.Json.Linq.JArray foodItems)
        {
            try
            {
                if (foodItems == null) return false;

                foreach (var item in foodItems)
                {
                    if (item == null) continue;

                    // 1) Check common client-side property names for food type
                    string foodType = item["FoodType"]?.ToString()
                                      ?? item["food_type"]?.ToString()
                                      ?? item["loai"]?.ToString()
                                      ?? string.Empty;

                    if (!string.IsNullOrEmpty(foodType) && foodType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    // 2) Check common client-side property names for food name
                    string foodName = item["FoodName"]?.ToString()
                                      ?? item["food_name"]?.ToString()
                                      ?? item["name"]?.ToString()
                                      ?? item["ten_san_pham"]?.ToString()
                                      ?? string.Empty;

                    if (!string.IsNullOrEmpty(foodName) && foodName.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    // 3) DB lookup fallback using any common id fields
                    var idToken = item["FoodId"] ?? item["food_id"] ?? item["id"] ?? item["Do_An_id"];
                    if (idToken != null)
                    {
                        if (int.TryParse(idToken.ToString(), out int fid))
                        {
                            var fobj = db.Do_Ans.FirstOrDefault(d => d.Do_An_id == fid);
                            if (fobj != null)
                            {
                                string dbFoodType = fobj.loai ?? string.Empty;
                                string dbFoodName = fobj.ten_san_pham ?? string.Empty;
                                if (dbFoodType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    dbFoodName.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(new Exception("ContainsComboInFoodItems error: " + ex.Message, ex));
            }

            return false;
        }
    }
}
