using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebCinema.Models;
using WebCinema.Infrastructure;

namespace WebCinema.Services
{
    /// <summary>
    /// Service để tạo file PDF phiếu nhập hàng (HTML-based PDF)
    /// </summary>
    public class PurchaseOrderPdfService
    {
        private CSDLDataContext db;

        public PurchaseOrderPdfService()
        {
            db = new CSDLDataContext();
        }

        /// <summary>
        /// Tạo PDF phiếu nhập từ Phieu_Nhap (lưu dưới dạng HTML + CSS)
        /// </summary>
        public string GeneratePurchaseOrderPdf(int phieuNhapId)
        {
            try
            {
                var phieu = db.Phieu_Nhaps.FirstOrDefault(p => p.phieu_nhap_id == phieuNhapId);
                if (phieu == null)
                    throw new Exception("Không tìm thấy phiếu nhập");

                var chiTiet = db.Chi_Tiet_Phieu_Nhaps
                    .Where(ct => ct.phieu_nhap_id == phieuNhapId)
                    .ToList();

                // ✅ Tạo thư mục nếu chưa tồn tại
                string pdfDir = System.Web.HttpContext.Current?.Server.MapPath("~/Content/phieunhap")
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/phieunhap");

                if (!Directory.Exists(pdfDir))
                    Directory.CreateDirectory(pdfDir);

                // ✅ Tên file: purchase_order_[id]_[datetime].html
                string fileName = $"phieu_nhap_{phieuNhapId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string filePath = Path.Combine(pdfDir, fileName);

                // ✅ Tạo HTML phiếu nhập
                string htmlContent = GeneratePurchaseOrderHtml(phieu, chiTiet);

                // ✅ Lưu file HTML
                File.WriteAllText(filePath, htmlContent, Encoding.UTF8);

                LoggingHelper.LogInfo($"✅ Tạo phiếu nhập HTML: {fileName}");

                return fileName;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "Lỗi tạo phiếu nhập PDF");
                throw;
            }
        }

        /// <summary>
        /// Tạo HTML nội dung phiếu nhập
        /// </summary>
        private string GeneratePurchaseOrderHtml(Phieu_Nhap phieu, List<Chi_Tiet_Phieu_Nhap> chiTiet)
        {
            StringBuilder html = new StringBuilder();

            html.Append(@"<!DOCTYPE html>");
            html.Append(@"<html>");
            html.Append(@"<head>");
            html.Append(@"<meta charset='UTF-8'>");
            html.Append(@"<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.Append(@"<style>");
            html.Append(@"body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background: #fff5f0; }");
            html.Append(@".container { max-width: 900px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(255, 107, 53, 0.2); border: 2px solid #ffe8dc; }");
            html.Append(@".header { text-align: center; border-bottom: 3px solid #ff6b35; padding-bottom: 20px; margin-bottom: 20px; }");
            html.Append(@".header h1 { margin: 0; color: #ff6b35; font-size: 2em; }");
            html.Append(@".header p { margin: 5px 0; color: #666; }");
            html.Append(@".info-section { margin: 20px 0; }");
            html.Append(@".info-section h3 { background: linear-gradient(135deg, #fff5f0 0%, #ffe8dc 100%); padding: 10px; margin: 0 0 10px 0; color: #ff6b35; border-left: 4px solid #ff6b35; font-weight: 700; }");
            html.Append(@".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }");
            html.Append(@".info-item { padding: 10px 0; border-bottom: 1px solid #ffe8dc; }");
            html.Append(@".info-label { font-weight: bold; color: #ff6b35; }");
            html.Append(@".info-value { color: #333; }");
            html.Append(@"table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
            html.Append(@"th { background: linear-gradient(135deg, #fff5f0 0%, #ffe8dc 100%); padding: 12px; text-align: left; border-bottom: 2px solid #ff6b35; font-weight: bold; color: #ff6b35; }");
            html.Append(@"td { padding: 10px 12px; border-bottom: 1px solid #ffe8dc; }");
            html.Append(@".text-right { text-align: right; }");
            html.Append(@".text-center { text-align: center; }");
            html.Append(@".badge-status { padding: 0.5rem 1rem; border-radius: 20px; font-weight: 700; font-size: 0.9rem; display: inline-block; }");
            html.Append(@".status-pending { background: #fff3cd; color: #856404; }");
            html.Append(@".status-approved { background: #d4edda; color: #155724; }");
            html.Append(@".status-completed { background: #d1ecf1; color: #0c5460; }");
            html.Append(@".footer { text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #ffe8dc; color: #666; font-size: 0.9em; }");
            html.Append(@".note-box { background: #fff5f0; border: 2px solid #ff9966; padding: 15px; margin: 15px 0; border-radius: 8px; border-left: 4px solid #ff6b35; }");
            html.Append(@"@media print { body { background: white; } .container { box-shadow: none; border: none; } }");
            html.Append(@"</style>");
            html.Append(@"</head>");
            html.Append(@"<body>");

            html.Append(@"<div class='container'>");

            // Header
            html.Append(@"<div class='header'>");
            html.Append(@"<h1>🏢 DAV CINEMA</h1>");
            html.Append(@"<p style='font-size: 1.5em; font-weight: bold;'>PHIẾU NHẬP KHO</p>");
            html.Append($@"<p style='color: #ff6b35; font-weight: bold; font-size: 1.2em;'>Mã phiếu: PN-{phieu.phieu_nhap_id}</p>");
            html.Append($@"<p>Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.Append(@"</div>");

            // Thông tin phiếu nhập
            html.Append(@"<div class='info-section'>");
            html.Append(@"<h3>📋 THÔNG TIN PHIẾU NHẬP</h3>");
            html.Append(@"<div class='info-grid'>");
            html.Append($@"<div class='info-item'><span class='info-label'>Ngày tạo:</span> <span class='info-value'>{phieu.ngay_nhap.Value:dd/MM/yyyy HH:mm}</span></div>");
            html.Append($@"<div class='info-item'><span class='info-label'>Nhân viên:</span> <span class='info-value'>{phieu.Nhan_Vien?.ho_ten ?? "N/A"}</span></div>");
            html.Append($@"<div class='info-item'><span class='info-label'>Rạp:</span> <span class='info-value'>{phieu.Rap?.ten_rap ?? "N/A"}</span></div>");
            
            // Trạng thái
            string statusClass = "";
            string statusText = "";
            if (phieu.trang_thai == "Chờ duyệt")
            {
                statusClass = "status-pending";
                statusText = "⏳ Chờ duyệt";
            }
            else if (phieu.trang_thai == "Đã duyệt")
            {
                statusClass = "status-approved";
                statusText = "✅ Đã duyệt";
            }
            else if (phieu.trang_thai == "Đã nhập")
            {
                statusClass = "status-completed";
                statusText = "📦 Đã nhập kho";
            }
            
            html.Append($@"<div class='info-item'><span class='info-label'>Trạng thái:</span> <span class='badge-status {statusClass}'>{statusText}</span></div>");
            html.Append(@"</div>");
            html.Append(@"</div>");

            // Nhà cung cấp (nếu có)
            if (phieu.Nha_Cung_Cap != null)
            {
                html.Append(@"<div class='info-section'>");
                html.Append(@"<h3>🚚 THÔNG TIN NHÀ CUNG CẤP</h3>");
                html.Append(@"<div class='info-grid'>");
                html.Append($@"<div class='info-item'><span class='info-label'>Tên nhà cung cấp:</span> <span class='info-value'>{phieu.Nha_Cung_Cap.ten_nha_cung_cap}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Địa chỉ:</span> <span class='info-value'>{phieu.Nha_Cung_Cap.dia_chi ?? "N/A"}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Điện thoại:</span> <span class='info-value'>{phieu.Nha_Cung_Cap.so_dien_thoai ?? "N/A"}</span></div>");
                html.Append($@"<div class='info-item'><span class='info-label'>Email:</span> <span class='info-value'>{phieu.Nha_Cung_Cap.email ?? "N/A"}</span></div>");
                html.Append(@"</div>");
                html.Append(@"</div>");
            }

            // Danh sách hàng hóa
            html.Append(@"<div class='info-section'>");
            html.Append(@"<h3>📦 CHI TIẾT HÀNG HÓA</h3>");
            html.Append(@"<table>");
            html.Append(@"<thead><tr><th>STT</th><th>Tên Sản Phẩm</th><th>Loại</th><th class='text-center'>Số Lượng</th></tr></thead>");
            html.Append(@"<tbody>");

            int stt = 1;
            foreach (var item in chiTiet)
            {
                html.Append($@"<tr>");
                html.Append($@"<td class='text-center'>{stt}</td>");
                html.Append($@"<td><strong>{item.Do_An?.ten_san_pham ?? "N/A"}</strong></td>");
                html.Append($@"<td><span style='background: linear-gradient(135deg, #ff9966 0%, #ff6b35 100%); color: white; padding: 0.3rem 0.7rem; border-radius: 6px; font-weight: 600;'>{item.Do_An?.loai ?? "N/A"}</span></td>");
                html.Append($@"<td class='text-center' style='font-size: 1.2em; font-weight: 700; color: #ff6b35;'>{item.so_luong_nhap}</td>");
                html.Append($@"</tr>");
                stt++;
            }

            html.Append(@"</tbody>");
            html.Append(@"</table>");
            html.Append(@"</div>");

            // Ghi chú (nếu có)
            if (!string.IsNullOrEmpty(phieu.ghi_chu))
            {
                html.Append(@"<div class='note-box'>");
                html.Append(@"<strong style='color: #ff6b35;'>📝 GHI CHÚ:</strong>");
                html.Append($@"<p style='margin: 5px 0 0 0;'>{phieu.ghi_chu}</p>");
                html.Append(@"</div>");
            }

            // Chữ ký
            html.Append(@"<div style='margin-top: 40px;'>");
            html.Append(@"<div class='info-grid'>");
            html.Append(@"<div style='text-align: center;'>");
            html.Append(@"<p style='font-weight: bold; color: #ff6b35;'>Người lập phiếu</p>");
            html.Append(@"<p style='font-style: italic; color: #999; font-size: 0.9em;'>(Ký, ghi rõ họ tên)</p>");
            html.Append(@"<br><br><br>");
            html.Append($@"<p style='font-weight: bold;'>{phieu.Nhan_Vien?.ho_ten ?? "___________________"}</p>");
            html.Append(@"</div>");
            html.Append(@"<div style='text-align: center;'>");
            html.Append(@"<p style='font-weight: bold; color: #ff6b35;'>Người duyệt</p>");
            html.Append(@"<p style='font-style: italic; color: #999; font-size: 0.9em;'>(Ký, ghi rõ họ tên)</p>");
            html.Append(@"<br><br><br>");
            html.Append(@"<p style='font-weight: bold;'>___________________</p>");
            html.Append(@"</div>");
            html.Append(@"</div>");
            html.Append(@"</div>");

            // Footer
            html.Append(@"<div class='footer'>");
            html.Append(@"<p>&copy; 2024 DAV Cinema. All rights reserved.</p>");
            html.Append(@"<p style='color: #ff6b35; font-weight: bold;'>Phiếu này chỉ có giá trị khi có đầy đủ chữ ký</p>");
            html.Append(@"<p>📞 Hotline: 1900-1234-5678 | 📧 Email: support@davcinema.vn | 🌐 Website: www.davcinema.vn</p>");
            html.Append(@"</div>");

            html.Append(@"</div>");
            html.Append(@"</body>");
            html.Append(@"</html>");

            return html.ToString();
        }

        ~PurchaseOrderPdfService()
        {
            if (db != null)
                db.Dispose();
        }
    }
}
