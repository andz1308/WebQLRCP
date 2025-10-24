using System;
using System.Collections.Generic;

namespace WebCinema.Models
{
    public class MovieDetailViewModel
    {
        public Phim Movie { get; set; }
        public List<string> Genres { get; set; }
        public List<Vai_Dien> Cast { get; set; }
        public List<Suat_Chieu> Showtimes { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }

        // ImagePath is a virtual path (may be URL or relative path starting with ~ or /)
        public string ImagePath { get; set; }

        // Trailer URL (could be full URL or relative)
        public string TrailerUrl { get; set; }
    }
}