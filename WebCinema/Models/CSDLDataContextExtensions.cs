using System;
using System.Configuration;

namespace WebCinema.Models
{
    public partial class CSDLDataContext
    {
        public CSDLDataContext()
            : base(GetConnectionString())
        {
        }

        private static string GetConnectionString()
        {
            string conn = ConfigurationManager.ConnectionStrings["CSDLConnectionString5"]?.ConnectionString
                ?? ConfigurationManager.ConnectionStrings["CinemaDBConnectionString5"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException(
                    "No database connection string found. Please add a connection string named 'CSDLConnectionString3' or 'CinemaDBConnectionString3' to Web.config.");
            }

            return conn;
        }
    }
}