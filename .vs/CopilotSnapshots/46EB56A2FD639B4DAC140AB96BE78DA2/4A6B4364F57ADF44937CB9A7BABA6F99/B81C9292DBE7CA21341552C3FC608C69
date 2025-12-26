using System;
using System.Linq;
using System.Web.Mvc;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Areas.Admin.Controllers
{
    [RoleAuthorize(Roles = "Staff")]
    public class StaffPurchaseOrderController : Controller
    {
        private CSDLDataContext db = new CSDLDataContext();

        // GET: Admin/StaffPurchaseOrder/Create
        public ActionResult Create()
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            var products = db.Do_Ans.OrderBy(d => d.loai).ThenBy(d => d.ten_san_pham).ToList();
            ViewBag.Products = products;

            ViewBag.Suppliers = new SelectList(db.Nha_Cung_Caps.Where(n => n.trang_thai == "Hoạt động").OrderBy(n => n.ten_nha_cung_cap).ToList(), "nha_cung_cap_id", "ten_nha_cung_cap");

            return View();
        }

        // POST: Admin/StaffPurchaseOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection form)
        {
            try
            {
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.Charset = "utf-8";

                int supplierId = 0;
                if (!int.TryParse(form["nha_cung_cap_id"], out supplierId) || supplierId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn nhà cung cấp!";
                    return RedirectToAction("Create");
                }

                var supplier = db.Nha_Cung_Caps.FirstOrDefault(n => n.nha_cung_cap_id == supplierId);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Nhà cung cấp không hợp lệ.";
                    return RedirectToAction("Create");
                }

                if (!DateTime.TryParse(form["expectedDate"], out DateTime expectedDate))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập ngày dự kiến hợp lệ!";
                    return RedirectToAction("Create");
                }

                if (expectedDate.Date <= DateTime.Now.Date)
                {
                    TempData["ErrorMessage"] = "Ngày dự kiến phải sau ngày hôm nay.";
                    return RedirectToAction("Create");
                }

                string staffName = Session["EmployeeName"] as string ?? "Staff";

                var orderItems = new System.Collections.Generic.List<PurchaseOrderItemViewModel>();
                int itemCount = 0;

                for (int i = 1; i <= 50; i++)
                {
                    if (!string.IsNullOrEmpty(form[$"product_{i}"]))
                    {
                        itemCount = i;
                    }
                }

                for (int i = 1; i <= itemCount; i++)
                {
                    string productId = form[$"product_{i}"];
                    string quantity = form[$"quantity_{i}"];
                    string price = form[$"price_{i}"];
                    string notes = form[$"notes_{i}"];

                    if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(quantity))
                    {
                        if (int.TryParse(productId, out int pId) && int.TryParse(quantity, out int qty) && decimal.TryParse(price, out decimal unitPrice))
                        {
                            if (qty > 0 && unitPrice >= 0)
                            {
                                orderItems.Add(new PurchaseOrderItemViewModel
                                {
                                    ProductId = pId,
                                    Quantity = qty,
                                    UnitPrice = unitPrice,
                                    Notes = notes ?? string.Empty
                                });
                            }
                        }
                    }
                }

                if (orderItems.Count == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng thêm ít nhất 1 sản phẩm!";
                    return RedirectToAction("Create");
                }

                var pending = new PurchaseOrderPendingViewModel
                {
                    SupplierId = supplierId,
                    Supplier = supplier.ten_nha_cung_cap,
                    ExpectedDate = expectedDate,
                    CreatedByStaff = staffName,
                    CreatedDate = DateTime.Now,
                    Items = orderItems,
                    TotalAmount = orderItems.Sum(it => it.TotalPrice),
                    Notes = form["ghi_chu"] ?? string.Empty
                };

                Session["PendingPurchaseOrder"] = pending;

                return RedirectToAction("ConfirmOrder");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        // GET: Admin/StaffPurchaseOrder/ConfirmOrder
        public ActionResult ConfirmOrder()
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            if (Session["PendingPurchaseOrder"] == null)
            {
                return RedirectToAction("Create");
            }

            // Provide products for name lookup in confirm view
            ViewBag.Products = db.Do_Ans.ToList();

            return View();
        }

        // POST: Admin/StaffPurchaseOrder/SubmitOrder
        [HttpPost]
        public ActionResult SubmitOrder()
        {
            try
            {
                if (Session["PendingPurchaseOrder"] == null)
                {
                    return Json(new { success = false, message = "Dữ liệu phiếu nhập không tồn tại!" });
                }

                var orderData = Session["PendingPurchaseOrder"] as PurchaseOrderPendingViewModel;
                if (orderData == null)
                {
                    return Json(new { success = false, message = "Dữ liệu phiếu nhập không hợp lệ" });
                }

                // Persist to database similar to Admin PurchaseOrder.Create
                int? staffId = Session["EmployeeId"] as int?;
                if (!staffId.HasValue)
                {
                    return Json(new { success = false, message = "Bạn phải đăng nhập để thực hiện thao tác này." });
                }

                var phieu = new Phieu_Nhap_Hang
                {
                    ma_phieu = GeneratePurchaseOrderCode(),
                    ngay_lap_phieu = DateTime.Now,
                    ngay_nhap = orderData.ExpectedDate,
                    nha_cung_cap_id = orderData.SupplierId,
                    nhan_vien_id = staffId.Value,
                    trang_thai = "Chưa duyệt",
                    tong_tien = orderData.TotalAmount,
                    ghi_chu = orderData.Notes
                };

                db.Phieu_Nhap_Hangs.InsertOnSubmit(phieu);
                db.SubmitChanges();

                // Insert details
                foreach (var item in orderData.Items)
                {
                    var ct = new Chi_Tiet_Phieu_Nhap
                    {
                        phieu_nhap_id = phieu.phieu_nhap_id,
                        do_an_id = item.ProductId,
                        so_luong = item.Quantity,
                        don_gia = item.UnitPrice,
                        ghi_chu = item.Notes
                    };
                    db.Chi_Tiet_Phieu_Nhaps.InsertOnSubmit(ct);
                }

                db.SubmitChanges();

                LoggingHelper.LogInfo($"✅ Staff tạo phiếu nhập (lưu DB): Supplier={orderData.Supplier}, PhieuId={phieu.phieu_nhap_id}");

                Session.Remove("PendingPurchaseOrder");

                return Json(new
                {
                    success = true,
                    message = "Phiếu nhập đã được gửi thành công! Admin sẽ duyệt trong thời gian sớm nhất.",
                    redirectUrl = Url.Action("Index", "StaffInventoryManagement")
                });
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private string GeneratePurchaseOrderCode()
        {
            var today = DateTime.Now;
            var prefix = $"PN-{today:yyyyMMdd}";

            var lastCode = db.Phieu_Nhap_Hangs
                .Where(p => p.ma_phieu.StartsWith(prefix))
                .OrderByDescending(p => p.ma_phieu)
                .Select(p => p.ma_phieu)
                .FirstOrDefault();

            int sequence = 1;
            if (lastCode != null)
            {
                var parts = lastCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"{prefix}-{sequence:D3}"; // PN-20240101-001
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
