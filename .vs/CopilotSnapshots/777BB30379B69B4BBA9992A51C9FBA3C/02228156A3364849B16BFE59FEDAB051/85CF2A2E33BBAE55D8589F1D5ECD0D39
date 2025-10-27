using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebCinema.Services;
using WebCinema.Models;

namespace WebCinema.Controllers
{
    public class HomeController : Controller
    {
        private MovieService movieService = new MovieService();

        public ActionResult Index()//Trang chủ
        {
            // Lấy danh sách phim đang chiếu
            var movies = movieService.GetNowShowingMovies();
            
            // Tạo danh sách ViewModel để truyền thêm thông tin
            var movieViewModels = movies.Select(m => new MovieViewModel
            {
                Movie = m,
                Genres = movieService.GetMovieGenres(m.phim_id),
                AverageRating = movieService.GetAverageRating(m.phim_id),
                RatingCount = movieService.GetRatingCount(m.phim_id)
            })
            .Take(8)
            .ToList();

            return View(movieViewModels);
        }

        public ActionResult Detail(int? id)//Trang chi tiết phim
        {
            // Nếu không có id thì chuyển hướng về trang chính (tránh lỗi khi truy cập /Home/Detail trực tiếp)
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            var vm = movieService.GetMovieDetailViewModel(id.Value);
            if (vm == null) return HttpNotFound();

            return View(vm);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}