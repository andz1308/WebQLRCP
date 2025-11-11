using System;

namespace WebCinema.Models
{
    /// <summary>
    /// ViewModel for actor's movie participation
    /// </summary>
    public class ActorMovieViewModel
    {
        public Phim Movie { get; set; }
        public string Role { get; set; }
    }
}
