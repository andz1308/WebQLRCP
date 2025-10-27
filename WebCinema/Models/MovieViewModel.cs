using System.Collections.Generic;

namespace WebCinema.Models
{
    public class MovieViewModel
    {
        public Phim Movie { get; set; }
        public List<string> Genres { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }

        // Resolved image path (absolute URL or virtual path starting with '/').
        public string ImagePath { get; set; }
    }
}