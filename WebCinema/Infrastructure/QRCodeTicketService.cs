using System;
using System.IO;
using System.Web;
using QRCoder;

namespace WebCinema.Infrastructure
{
    /// <summary>
    /// Service để sinh ra QR code từ mã vé và lưu vào folder
    /// </summary>
    public class QRCodeTicketService
    {
        private readonly string _qrFolder;

        public QRCodeTicketService()
        {
            // ✅ Đường dẫn folder qr trong wwwroot
            _qrFolder = Path.Combine(HttpContext.Current?.Server.MapPath("~") ?? "", "Content", "qr");
            
            // ✅ Tạo folder nếu chưa tồn tại
            if (!Directory.Exists(_qrFolder))
            {
                Directory.CreateDirectory(_qrFolder);
            }
        }

        /// <summary>
        /// Sinh QR code từ mã vé và lưu vào file
        /// </summary>
        /// <param name="qrCode">Mã QR code (từ Ve.ma_qr_code)</param>
        /// <returns>Đường dẫn tương đối của file ảnh QR code</returns>
        public string GenerateAndSaveQRCode(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                    return null;

                // ✅ Tạo tên file duy nhất từ mã QR code
                string safeFileName = qrCode.Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                string fileName = $"qr_{safeFileName}.png";
                string filePath = Path.Combine(_qrFolder, fileName);

                // ✅ Nếu file đã tồn tại, trả về đường dẫn
                if (File.Exists(filePath))
                {
                    return $"/Content/qr/{fileName}";
                }

                // ✅ Sinh QR code bằng QRCoder
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCode, QRCodeGenerator.ECCLevel.Q);
                    
                    // ✅ Dùng PngByteQRCode thay vì GetGraphic
                    PngByteQRCode qrCode_png = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeImage = qrCode_png.GetGraphic(10);

                    // ✅ Lưu byte array vào file PNG
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(qrCodeImage, 0, qrCodeImage.Length);
                        fs.Flush();
                    }
                }

                LoggingHelper.LogInfo($"✅ Sinh QR code: {qrCode} -> {fileName}");

                // ✅ Trả về đường dẫn tương đối
                return $"/Content/qr/{fileName}";
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Lấy đường dẫn QR code từ mã vé (không sinh lại nếu đã tồn tại)
        /// </summary>
        public string GetQRCodePath(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                    return null;

                string safeFileName = qrCode.Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                string fileName = $"qr_{safeFileName}.png";
                string filePath = Path.Combine(_qrFolder, fileName);

                // ✅ Nếu file tồn tại, trả về đường dẫn
                if (File.Exists(filePath))
                {
                    return $"/Content/qr/{fileName}";
                }

                // ✅ Nếu không tồn tại, sinh mới
                return GenerateAndSaveQRCode(qrCode);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Xóa file QR code
        /// </summary>
        public bool DeleteQRCode(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                    return false;

                string safeFileName = qrCode.Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                string fileName = $"qr_{safeFileName}.png";
                string filePath = Path.Combine(_qrFolder, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoggingHelper.LogInfo($"✅ Xóa QR code: {fileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return false;
            }
        }
    }
}
