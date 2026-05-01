using System.Diagnostics;
using HomeServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeServices.Data;

namespace HomeServices.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // injecting the database context
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString)
        {
            // Start with all categories
            var categories = from c in _context.Categories
                             select c;

            // If user searched for something
            if (!string.IsNullOrEmpty(searchString))
            {
                categories = categories.Where(s => s.Name.Contains(searchString)
                                               || s.Description.Contains(searchString));
                ViewData["SearchQuery"] = searchString; // To keep the text in the search bar
            }

            var results = await categories.ToListAsync();

            // If search yielded no results, we pass that info to the view
            if (!string.IsNullOrEmpty(searchString) && results.Count == 0)
            {
                ViewBag.ErrorMessage = $"Sorry, we couldn't find any services matching '{searchString}'.";
            }

            return View(results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
