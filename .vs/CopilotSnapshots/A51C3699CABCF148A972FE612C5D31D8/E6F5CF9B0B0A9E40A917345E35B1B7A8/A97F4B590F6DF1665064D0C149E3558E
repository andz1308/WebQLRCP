using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class CinemaManagementController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/CinemaManagement
        public ActionResult Index()
        {
            var cinemas = db.Raps.ToList();
            return View(cinemas);
        }

        // GET: Admin/CinemaManagement/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/CinemaManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Rap cinema)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Raps.InsertOnSubmit(cinema);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm rạp mới thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(cinema);
        }

        // GET: Admin/CinemaManagement/Edit/5
        public ActionResult Edit(int id)
        {
            var cinema = db.Raps.FirstOrDefault(r => r.rap_id == id);
            if (cinema == null)
            {
                return HttpNotFound();
            }
            return View(cinema);
        }

        // POST: Admin/CinemaManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Rap cinema)
        {
            try
            {
                var existingCinema = db.Raps.FirstOrDefault(r => r.rap_id == id);
                if (existingCinema == null)
                {
                    return HttpNotFound();
                }

                existingCinema.ten_rap = cinema.ten_rap;
                existingCinema.dia_chi = cinema.dia_chi;
                existingCinema.email = cinema.email;
                existingCinema.mo_ta = cinema.mo_ta;

                db.SubmitChanges();

                TempData["SuccessMessage"] = "Cập nhật rạp thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(cinema);
        }

        // POST: Admin/CinemaManagement/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                var cinema = db.Raps.FirstOrDefault(r => r.rap_id == id);
                if (cinema == null)
                {
                    return Json(new { success = false, message = "Rạp không tồn tại." });
                }

                db.Raps.DeleteOnSubmit(cinema);
                db.SubmitChanges();

                return Json(new { success = true, message = "Xóa rạp thành công!" });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/CinemaManagement/Rooms/5
        public ActionResult Rooms(int id)
        {
            var cinema = db.Raps.FirstOrDefault(r => r.rap_id == id);
            if (cinema == null)
            {
                return HttpNotFound();
            }

            ViewBag.CinemaName = cinema.ten_rap;
            ViewBag.CinemaId = id;
            var rooms = db.Phong_Chieus.Where(pc => pc.rap_id == id).ToList();
            return View(rooms);
        }

        // GET: Admin/CinemaManagement/CreateRoom/5
        public ActionResult CreateRoom(int cinemaId)
        {
            ViewBag.CinemaId = cinemaId;
            return View();
        }

        // POST: Admin/CinemaManagement/CreateRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoom(Phong_Chieu room)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Phong_Chieus.InsertOnSubmit(room);
                    db.SubmitChanges();

                    TempData["SuccessMessage"] = "Thêm phòng chiếu thành công!";
                    return RedirectToAction("Rooms", new { id = room.rap_id });
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.CinemaId = room.rap_id;
            return View(room);
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
