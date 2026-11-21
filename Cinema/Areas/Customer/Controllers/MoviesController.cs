using Cinema.Services;
using Cinema.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public IActionResult Index(string? searchString, int page = 1)
        {
            var result = _movieService.GetPaged(searchString, page, 6);

            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalMovies = result.TotalCount;

            return View(result.Items);
        }
        [HttpGet]
        public IActionResult ProductDetails(int id)
        {
            var movie = _movieService.GetByIdWithDetails(id);

            if (movie == null)
                return NotFound();

            var pagedResult = _movieService.GetPaged(null, 1, 10);

            var vm = new MovieWithRelatedVM
            {
                Movie = movie,
                RelatedMovies = pagedResult.Items
                    .Where(m => m.Id != id)
                    .Take(4)
                    .ToList()
            };

            return View(vm);
        }
    }
}