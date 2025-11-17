using System;
using System.IO;
using System.Web;
using QRCoder;

namespace WebCinema.Infrastructure
{
    /// <summary>
    /// Service ?? sinh ra QR code t? mã vé và l?u vào folder
    /// </summary>
    public class QRCodeTicketService
    {
        private readonly string _qrFolder;

        public QRCodeTicketService()
        {
            // ? ???ng d?n folder qr trong wwwroot
            _qrFolder = Path.Combine(HttpContext.Current?.Server.MapPath("~") ?? "", "Content", "qr");
            
            // ? T?o folder n?u ch?a t?n t?i
            if (!Directory.Exists(_qrFolder))
            {
                Directory.CreateDirectory(_qrFolder);
            }
        }

        /// <summary>
        /// Sinh QR code t? mã vé và l?u vào file
        /// </summary>
        /// <param name="qrCode">Mã QR code (t? Ve.ma_qr_code)</param>
        /// <returns>???ng d?n t??ng ??i c?a file ?nh QR code</returns>
        public string GenerateAndSaveQRCode(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                    return null;

                // ? T?o tên file duy nh?t t? mã QR code
                string safeFileName = qrCode.Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                string fileName = $"qr_{safeFileName}.png";
                string filePath = Path.Combine(_qrFolder, fileName);

                // ? N?u file ?ã t?n t?i, tr? v? ???ng d?n
                if (File.Exists(filePath))
                {
                    return $"/Content/qr/{fileName}";
                }

                // ? Sinh QR code b?ng QRCoder
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCode, QRCodeGenerator.ECCLevel.Q);
                    
                    // ? Dùng PngByteQRCode thay vì GetGraphic
                    PngByteQRCode qrCode_png = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeImage = qrCode_png.GetGraphic(10);

                    // ? L?u byte array vào file PNG
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(qrCodeImage, 0, qrCodeImage.Length);
                        fs.Flush();
                    }
                }

                LoggingHelper.LogInfo($"? Sinh QR code: {qrCode} -> {fileName}");

                // ? Tr? v? ???ng d?n t??ng ??i
                return $"/Content/qr/{fileName}";
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// L?y ???ng d?n QR code t? mã vé (không sinh l?i n?u ?ã t?n t?i)
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

                // ? N?u file t?n t?i, tr? v? ???ng d?n
                if (File.Exists(filePath))
                {
                    return $"/Content/qr/{fileName}";
                }

                // ? N?u không t?n t?i, sinh m?i
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
                    LoggingHelper.LogInfo($"? Xóa QR code: {fileName}");
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
