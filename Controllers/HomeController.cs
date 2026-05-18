using System.Diagnostics;
using HomeServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeServices.Data;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // --- ?????: ????????? ????????? ??????? ??? ??? View ???? ?? (Index) ---

            // 1. ?? ???????? ???????? ????? ?? ????????? ?? ?????? ?? ??? ???? ??? Index
            if (User.IsInRole("ServiceProvider"))
            {
                var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ???? ?????????? ?????? ?? ?????? ?? ????????? ?? ????????
                ViewBag.TotalRequests = await _context.Requests.CountAsync(r => r.ServiceProviderId == providerId);
                ViewBag.PendingRequests = await _context.Requests.CountAsync(r => r.ServiceProviderId == providerId && r.Status == "Pending");

                // ????? View ???? (null model) ??? ????????? ?? ????? ??? Categories
                return View(new List<Category>());
            }

            // 2. ??? ?????? (????? ???? ???????)
            var categoriesQuery = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                categoriesQuery = categoriesQuery.Where(s => s.Name.Contains(searchString)
                                                        || s.Description.Contains(searchString));
                ViewData["SearchQuery"] = searchString;
            }

            var results = await categoriesQuery.ToListAsync();

            if (!string.IsNullOrEmpty(searchString) && results.Count == 0)
            {
                ViewBag.ErrorMessage = $"Sorry, we couldn't find any services matching '{searchString}'.";
            }

            // ?????? ????? ??? Index ????? ???? ???????
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