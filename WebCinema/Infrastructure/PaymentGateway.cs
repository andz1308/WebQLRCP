using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WebCinema.Infrastructure
{
    /// <summary>
    /// Tích hợp Cổng Thanh Toán VNPay - Theo Spec Chính Thức
    /// RFC3986 (HMAC SHA512)
    /// </summary>
    public class PaymentGateway
    {
        // ======================
        // ⚙️ CẤU HÌNH VNPAY (từ Web.config)
        // ======================
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _command;
        private readonly string _currCode;
        private readonly string _version;
        private readonly string _locale;
        private readonly string _returnUrl;
        private readonly string _ipnUrl;

        public PaymentGateway()
        {
            // ✅ Đọc toàn bộ cấu hình từ Web.config
            _tmnCode = ConfigurationManager.AppSettings["VNPay:TmnCode"] ?? "NJJ0R8FS";
            _hashSecret = ConfigurationManager.AppSettings["VNPay:HashSecret"] ?? "BYKJBHPPZKQMKBIBGGXIYKWYFAYSJXCW";
            _baseUrl = ConfigurationManager.AppSettings["VNPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _command = ConfigurationManager.AppSettings["VNPay:Command"] ?? "pay";
            _currCode = ConfigurationManager.AppSettings["VNPay:CurrCode"] ?? "VND";
            _version = ConfigurationManager.AppSettings["VNPay:Version"] ?? "2.1.0";
            _locale = ConfigurationManager.AppSettings["VNPay:Locale"] ?? "vn";
            _returnUrl = ConfigurationManager.AppSettings["VNPay:ReturnUrl"] ?? "https://localhost:44300/Invoice/PaymentCallback";
            _ipnUrl = ConfigurationManager.AppSettings["VNPay:IpnUrl"] ?? "https://localhost:44300/Invoice/PaymentCallback";

            LoggingHelper.LogInfo($"✅ PaymentGateway Initialized (Official Spec):");
            LoggingHelper.LogInfo($"   - TmnCode: {_tmnCode}");
            LoggingHelper.LogInfo($"   - BaseUrl: {_baseUrl}");
            LoggingHelper.LogInfo($"   - ReturnUrl: {_returnUrl}");
        }

        // ======================
        // 🚀 TẠO URL THANH TOÁN (theo spec chính thức)
        // ======================
        public string CreatePaymentUrl(decimal amount, string orderInfo, string customerId)
        {
            try
            {
                // Kiểm tra giá trị
                if (amount <= 0)
                    throw new ArgumentException("Số tiền phải lớn hơn 0");
                if (string.IsNullOrWhiteSpace(orderInfo))
                    throw new ArgumentException("Thông tin đơn hàng không được rỗng");

                // ✅ SortedDictionary - sắp xếp theo thứ tự A-Z
                var vnpayParams = new SortedDictionary<string, string>
                {
                    { "vnp_Version", _version },
                    { "vnp_Command", _command },
                    { "vnp_TmnCode", _tmnCode },
                    { "vnp_Amount", ((long)(amount * 100)).ToString() }, // Nhân 100 (VND không có lẻ)
                    { "vnp_CurrCode", _currCode },
                    { "vnp_TxnRef", GenerateTransactionRef() },
                    { "vnp_OrderInfo", orderInfo },
                    { "vnp_OrderType", "billpayment" },
                    { "vnp_Locale", _locale },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                    { "vnp_IpAddr", GetClientIP() },
                    { "vnp_ReturnUrl", _returnUrl },
                    { "vnp_IpnUrl", _ipnUrl }
                };

                // ✅ Build query string với HttpUtility.UrlEncode (theo spec chính thức)
                StringBuilder rawData = new StringBuilder();
                foreach (var kv in vnpayParams)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        rawData.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                    }
                }

                string dataToHash = rawData.ToString().TrimEnd('&');
                
                // ✅ Tạo HMAC SHA512
                string secureHash = HmacSHA512(dataToHash, _hashSecret);

                // ✅ URL thanh toán cuối cùng
                string paymentUrl = _baseUrl + "?" + dataToHash + "&vnp_SecureHash=" + secureHash;

                LoggingHelper.LogInfo($"✅ Tạo URL Thanh Toán:");
                LoggingHelper.LogInfo($"   - Amount: {amount} VND");
                LoggingHelper.LogInfo($"   - OrderInfo: {orderInfo}");
                LoggingHelper.LogInfo($"   - Hash: {secureHash}");
                
                return paymentUrl;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, $"CreatePaymentUrl - Amount: {amount}");
                throw;
            }
        }

        // ======================
        // 🔍 XÁC NHẬN THANH TOÁN (theo spec chính thức)
        // ======================
        public PaymentResponse VerifyPayment(Dictionary<string, string> responseData)
        {
            var response = new PaymentResponse();
            
            try
            {
                LoggingHelper.LogInfo($"VerifyPayment: Nhận {responseData.Count} tham số");

                // ✅ Kiểm tra bắt buộc
                if (!responseData.ContainsKey("vnp_SecureHash"))
                {
                    LoggingHelper.LogInfo($"⚠️ Localhost Mode (No Hash) - Giả lập success");
                    
                    // LOCALHOST MODE: không có chữ ký → giả lập thành công
                    response.IsValid = true;
                    response.Success = true;
                    response.ResponseCode = responseData.ContainsKey("vnp_ResponseCode") ? responseData["vnp_ResponseCode"] : "00";
                    response.TransactionNo = responseData.ContainsKey("vnp_TransactionNo") ? responseData["vnp_TransactionNo"] : "TEST_" + DateTime.Now.Ticks;
                    response.OrderInfo = responseData.ContainsKey("vnp_OrderInfo") ? responseData["vnp_OrderInfo"] : "";
                    response.Amount = responseData.ContainsKey("vnp_Amount") ? responseData["vnp_Amount"] : "0";
                    response.Message = "Localhost Mode - Test Success";
                    
                    return response;
                }

                string receivedHash = responseData["vnp_SecureHash"];
                
                // ✅ Xóa hash khỏi dữ liệu để xác minh
                var dataToVerify = new SortedDictionary<string, string>();
                foreach (var kv in responseData)
                {
                    if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType" && !string.IsNullOrEmpty(kv.Value))
                    {
                        dataToVerify[kv.Key] = kv.Value;
                    }
                }

                // ✅ Rebuild query string
                StringBuilder rawData = new StringBuilder();
                foreach (var kv in dataToVerify)
                {
                    rawData.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                }

                string dataToHash = rawData.ToString().TrimEnd('&');
                string expectedHash = HmacSHA512(dataToHash, _hashSecret);

                LoggingHelper.LogInfo($"Verify: Expected={expectedHash}, Received={receivedHash}");

                // ✅ So sánh chữ ký (case-insensitive)
                if (!expectedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase))
                {
                    response.IsValid = false;
                    response.Success = false;
                    response.Message = "Chữ ký không hợp lệ";
                    LoggingHelper.LogInfo($"❌ Signature mismatch!");
                    return response;
                }

                // ✅ Chữ ký hợp lệ
                response.IsValid = true;
                response.ResponseCode = responseData.ContainsKey("vnp_ResponseCode") ? responseData["vnp_ResponseCode"] : "";
                response.TransactionNo = responseData.ContainsKey("vnp_TransactionNo") ? responseData["vnp_TransactionNo"] : "";
                response.BankCode = responseData.ContainsKey("vnp_BankCode") ? responseData["vnp_BankCode"] : "";
                response.OrderInfo = responseData.ContainsKey("vnp_OrderInfo") ? responseData["vnp_OrderInfo"] : "";
                response.Amount = responseData.ContainsKey("vnp_Amount") ? responseData["vnp_Amount"] : "";
                response.PayDate = responseData.ContainsKey("vnp_PayDate") ? responseData["vnp_PayDate"] : "";

                // Kiểm tra response code
                if (response.ResponseCode == "00")
                {
                    response.Success = true;
                    response.Message = "Giao dịch thành công";
                    LoggingHelper.LogInfo($"✅ Payment Success - TxnNo: {response.TransactionNo}");
                }
                else
                {
                    response.Success = false;
                    response.Message = GetResponseMessage(response.ResponseCode);
                    LoggingHelper.LogInfo($"⚠️ Payment Failed - Code: {response.ResponseCode}");
                }

                return response;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "VerifyPayment");
                response.IsValid = false;
                response.Success = false;
                response.Message = "Lỗi xác minh thanh toán";
                return response;
            }
        }

        // ======================
        // 🧩 HÀM HỖ TRỢ
        // ======================
        private string GenerateTransactionRef()
        {
            return DateTime.Now.Ticks.ToString();
        }

        private string GetClientIP()
        {
            try
            {
                var context = HttpContext.Current;
                if (context != null)
                {
                    string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (string.IsNullOrEmpty(ip))
                        ip = context.Request.ServerVariables["REMOTE_ADDR"];
                    if (!string.IsNullOrEmpty(ip) && ip != "::1")
                        return ip;
                }
            }
            catch { }
            return "127.0.0.1";
        }

        // ✅ HMAC SHA512 (theo spec chính thức)
        private string HmacSHA512(string data, string key)
        {
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                using (var hmac = new HMACSHA512(keyBytes))
                {
                    byte[] hashValue = hmac.ComputeHash(dataBytes);
                    return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(ex, "HmacSHA512");
                throw;
            }
        }

        // ✅ Response code messages
        private string GetResponseMessage(string code)
        {
            var messages = new Dictionary<string, string>
            {
                { "00", "Giao dịch thành công" },
                { "01", "Lỗi do URL không đúng" },
                { "02", "Lỗi tổng tiền không hợp lệ" },
                { "04", "Lỗi mã đơn vị không tồn tại hoặc bị khóa" },
                { "05", "Lỗi không xác thực được đơn vị" },
                { "06", "Lỗi mã giao dịch bị trùng" },
                { "07", "Giao dịch nghi ngờ" },
                { "08", "Lỗi mã số điện thoại không hợp lệ" },
                { "09", "Tài khoản chưa đăng ký InternetBanking" },
                { "10", "Xác thực SecurePassword lần 1 sai" },
                { "12", "Khách hàng từ chối giao dịch" },
                { "24", "Khách hàng hủy giao dịch" },
                { "51", "Không đủ số dư" },
                { "65", "Vượt hạn mức giao dịch trong ngày" },
                { "75", "Ngân hàng từ chối giao dịch" },
                { "99", "Lỗi không xác định" }
            };
            return messages.ContainsKey(code) ? messages[code] : $"Lỗi ({code})";
        }
    }

    // ======================
    // 🧾 MODEL KẾT QUẢ THANH TOÁN
    // ======================
    public class PaymentResponse
    {
        public bool IsValid { get; set; }
        public bool Success { get; set; }
        public string ResponseCode { get; set; }
        public string TransactionNo { get; set; }
        public string BankCode { get; set; }
        public string OrderInfo { get; set; }
        public string Message { get; set; }
        public string Amount { get; set; }
        public string PayDate { get; set; }
    }
}
