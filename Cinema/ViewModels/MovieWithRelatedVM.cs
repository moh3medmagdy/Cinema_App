using Cinema.Models;

namespace Cinema.ViewModels
{
    public class MovieWithRelatedVM
    {
        public Movie Movie { get; set; } = null!;
        public IEnumerable<Movie> RelatedMovies { get; set; } = new List<Movie>();
    }
}