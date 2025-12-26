using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebCinema.Models;

namespace WebCinema.Services
{
    public class MovieService
    {
        private CSDLDataContext db = new CSDLDataContext();

        // Lấy tất cả phim đang chiếu
        public List<Phim> GetAllMovies()
        {
            return db.Phims.ToList();
        }

        // Lấy phim theo ID
        public Phim GetMovieById(int id)
        {
            return db.Phims.FirstOrDefault(p => p.phim_id == id);
        }

        // Lấy phim đang chiếu (có suất chiếu >= hôm nay)
        public List<Phim> GetNowShowingMovies()
        {
            var today = DateTime.Today;
            return db.Phims
                .Where(p => p.Suat_Chieus.Any(sc => sc.ngay_chieu >= today))
                .ToList();
        }

        // Lấy thể loại của phim
        public List<string> GetMovieGenres(int phimId)
        {
            return db.Phim_LoaiPhims
                .Where(pl => pl.phim_id == phimId)
                .Select(pl => pl.Loai_Phim.ten_loai)
                .ToList();
        }

        // Lấy danh sách diễn viên của phim
        public List<Vai_Dien> GetMovieCast(int phimId)
        {
            return db.Vai_Diens
                .Where(vd => vd.phim_id == phimId)
                .ToList();
        }

        // Lấy điểm đánh giá trung bình của phim
        public double GetAverageRating(int phimId)
        {
            var ratings = db.Danh_Gias
                .Where(dg => dg.phim_id == phimId && dg.diem_rating.HasValue)
                .Select(dg => dg.diem_rating.Value)
                .ToList();

            return ratings.Any() ? ratings.Average() : 0;
        }

        // Lấy số lượng đánh giá
        public int GetRatingCount(int phimId)
        {
            return db.Danh_Gias
                .Count(dg => dg.phim_id == phimId && dg.diem_rating.HasValue);
        }

        // ✅ Kiểm tra suất chiếu đã đầy chỗ
        public bool IsShowtimeFull(int suatChieuId)
        {
            var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == suatChieuId);
            if (showtime == null) return true;

            int tongGhe = showtime.Phong_Chieu.suc_chua;
            int soVeDaBan = db.Ves.Count(v => 
                v.suat_chieu_id == suatChieuId 
                && v.Dat_Ve_id != null 
                && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán");

            return soVeDaBan >= tongGhe;
        }

        // ✅ Lấy số ghế còn trống của suất chiếu
        public int GetAvailableSeats(int suatChieuId)
        {
            var showtime = db.Suat_Chieus.FirstOrDefault(sc => sc.suat_chieu_id == suatChieuId);
            if (showtime == null) return 0;

            int tongGhe = showtime.Phong_Chieu.suc_chua;
            int soVeDaBan = db.Ves.Count(v => 
                v.suat_chieu_id == suatChieuId 
                && v.Dat_Ve_id != null 
                && v.Dat_Ve.trang_thai_Dat_Ve == "Đã Thanh toán");

            return Math.Max(0, tongGhe - soVeDaBan);
        }

        // Lấy các suất chiếu của phim (chỉ suất còn trong khung thời gian hợp lệ)
        // ✅ KHÔNG LỌC suất đầy - trả về tất cả để View hiển thị với trạng thái disabled
        public List<Suat_Chieu> GetMovieShowtimes(int phimId)
        {
            var now = DateTime.Now;
            
            // Lấy tất cả suất chiếu từ hôm nay trở đi
            var showtimes = db.Suat_Chieus
                .Where(sc => sc.phim_id == phimId && sc.ngay_chieu >= now.Date)
                .ToList();

            // ✅ Chỉ lọc theo thời gian (chưa quá 1 giờ sau giờ bắt đầu)
            // KHÔNG lọc suất đầy - để View tự xử lý hiển thị
            var validShowtimes = showtimes.Where(sc =>
            {
                DateTime showtimeStart = sc.ngay_chieu.Date.Add(sc.Ca_Chieu.gio_bat_dau);
                DateTime allowedEndTime = showtimeStart.AddHours(1);
                
                return now <= allowedEndTime;
            })
            .OrderBy(sc => sc.ngay_chieu)
            .ThenBy(sc => sc.ca_chieu_id)
            .ToList();

            return validShowtimes;
        }

        // Build a detailed view model from DB
        public MovieDetailViewModel GetMovieDetailViewModel(int phimId)
        {
            var phim = GetMovieById(phimId);
            if (phim == null) return null;

            var genres = GetMovieGenres(phimId);
            var cast = GetMovieCast(phimId);
            
            // ✅ Lấy tất cả suất chiếu hợp lệ (kể cả đã đầy)
            var showtimes = GetMovieShowtimes(phimId);
            
            var avg = GetAverageRating(phimId);
            var count = GetRatingCount(phimId);

            var vm = new MovieDetailViewModel
            {
                Movie = phim,
                Genres = genres,
                Cast = cast,
                Showtimes = showtimes,
                AverageRating = avg,
                RatingCount = count,
                ImagePath = phim.hinh_anh, // DB field
                TrailerUrl = phim.video // DB field
            };

            return vm;
        }
    }
}