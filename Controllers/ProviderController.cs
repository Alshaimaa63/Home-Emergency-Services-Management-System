using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    //[Authorize] // عشان مفيش حد يدخل لوحة التحكم غير لما يسجل دخول
    [Authorize(Roles = "Provider")] // كده الفني فقط هو اللي يقدر يفتح الكنترولر ده
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProviderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. لوحة التحكم (عرض ملخص بسيط)
        public IActionResult Dashboard()
        {
            return View();
        }

        // 2. عرض الطلبات المتاحة (اللي حالتها Pending)
        public async Task<IActionResult> AvailableRequests()
        {
            var requests = await _context.Requests
                .Include(r => r.Category)
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            return View(requests);
        }

        // 3. وظيفة قبول الطلب
        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.Requests.FindAsync(id);

            if (request != null && request.Status == "Pending")
            {
                request.Status = "Accepted";
                request.ProviderId = userId; // ربط الطلب بمقدم الخدمة اللي وافق عليه

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AvailableRequests));
        }

        // 4. عرض تاريخ الطلبات اللي مقدم الخدمة شغال عليها أو خلصها
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myHistory = await _context.Requests
                .Include(r => r.Category)
                .Where(r => r.ProviderId == userId)
                .ToListAsync();

            return View(myHistory);
        }

        // 5. وظيفة إتمام الطلب
        [HttpPost]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null && request.Status == "Accepted")
            {
                request.Status = "Completed";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(History));
        }
    }
}