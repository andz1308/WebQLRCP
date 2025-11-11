using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace WebCinema.Infrastructure
{
    public static class LoggingHelper
    {
        private static readonly string LogFilePath = HttpContext.Current != null 
            ? HttpContext.Current.Server.MapPath("~/App_Data/Logs/") 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");

        public static void LogError(Exception ex, string additionalInfo = "")
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }

                string logFileName = $"Error_{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(LogFilePath, logFileName);

                string logMessage = $@"
================================================================================
Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Message: {ex.Message}
{(string.IsNullOrEmpty(additionalInfo) ? "" : $"Additional Info: {additionalInfo}")}
Stack Trace: {ex.StackTrace}
Inner Exception: {ex.InnerException?.Message ?? "None"}
================================================================================
";

                File.AppendAllText(fullPath, logMessage);

                // Also write to Debug output
                Debug.WriteLine(logMessage);
            }
            catch (Exception logEx)
            {
                // If logging fails, write to Debug output at least
                Debug.WriteLine($"Logging failed: {logEx.Message}");
                Debug.WriteLine($"Original error: {ex.Message}");
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }

                string logFileName = $"Info_{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(LogFilePath, logFileName);

                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";

                File.AppendAllText(fullPath, logMessage);
                Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Log chi tiết query string (dùng cho VNPay callback debugging)
        /// </summary>
        public static void LogQueryString(NameValueCollection queryString, string source = "")
        {
            try
            {
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }

                string logFileName = $"VNPay_{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(LogFilePath, logFileName);

                string header = string.IsNullOrEmpty(source) ? "QueryString" : $"QueryString ({source})";
                string logMessage = $@"
================================================================================
[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {header}
";

                if (queryString != null && queryString.Count > 0)
                {
                    foreach (string key in queryString.Keys)
                    {
                        logMessage += $"   {key} = {queryString[key]}\n";
                    }
                }
                else
                {
                    logMessage += "   (No parameters)\n";
                }

                logMessage += "================================================================================\n";

                File.AppendAllText(fullPath, logMessage);
                Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogQueryString failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Log chi tiết payment debugging (hash, data, signature comparison)
        /// </summary>
        public static void LogPaymentDebug(string dataToHash, string expectedHash, string receivedHash, string source = "")
        {
            try
            {
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }

                string logFileName = $"VNPay_{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(LogFilePath, logFileName);

                string header = string.IsNullOrEmpty(source) ? "Payment Debug" : $"Payment Debug ({source})";
                string hashMatch = expectedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase) ? "✓ MATCH" : "✗ MISMATCH";

                string logMessage = $@"
================================================================================
[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {header}
Data to Hash:
{dataToHash}
---
Expected Hash: {expectedHash}
Received Hash: {receivedHash}
Status: {hashMatch}
================================================================================
";

                File.AppendAllText(fullPath, logMessage);
                Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogPaymentDebug failed: {ex.Message}");
            }
        }
    }
}