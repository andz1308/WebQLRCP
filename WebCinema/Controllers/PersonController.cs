using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;

namespace WebCinema.Controllers
{
    public class PersonController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Person/Actor/5
        public ActionResult Actor(int id)
        {
            var actor = db.Dien_Viens.FirstOrDefault(a => a.dienvien_id == id);
            if (actor == null)
            {
                return HttpNotFound();
            }

            // Get movies featuring this actor
            var movies = db.Vai_Diens
                .Where(v => v.dien_vien_id == id)
                .Select(v => new
                {
                    Movie = v.Phim,
                    Role = v.ten_vai_dien
                })
                .ToList();

            ViewBag.Movies = movies;
            return View(actor);
        }

        // GET: Person/Director/5
        public ActionResult Director(int id)
        {
            var director = db.Dao_Diens.FirstOrDefault(d => d.daodien_id == id);
            if (director == null)
            {
                return HttpNotFound();
            }

            // Get movies directed by this director
            var movies = db.Phims.Where(p => p.dao_dien_id == id).ToList();

            ViewBag.Movies = movies;
            return View(director);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}