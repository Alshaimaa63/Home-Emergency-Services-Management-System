using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myRequests = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Provider)
                .Where(r => r.CustomerId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.CompletedCount = myRequests.Count(r => r.Status == "Completed");
            ViewBag.InProgressCount = myRequests.Count(r => r.Status == "Pending" || r.Status == "Accepted");

            return View(myRequests);
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,PreferredSchedule,CategoryId")] Request request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                request.CustomerId = userId;
                request.Status = "Pending";
                request.CreatedAt = DateTime.Now;

                ModelState.Remove("CustomerId");
                ModelState.Remove("Customer");
                ModelState.Remove("Category");

                if (ModelState.IsValid)
                {
                    _context.Add(request);
                    await _context.SaveChangesAsync();

                    // --- التعديل هنا لفلترة مقدمي الخدمة الموثقين فقط ---

                    // 1. جلب كل اليوزرز اللي ليهم دور مقدم خدمة
                    var allProviders = await _userManager.GetUsersInRoleAsync("ServiceProvider");

                    // 2. فلترة القائمة برمجياً لاختيار الموثقين فقط (IsVerified == true)
                    var verifiedProviders = allProviders.Where(p => p.IsVerified).ToList();

                    foreach (var p in verifiedProviders)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = p.Id,
                            Message = $"New Request: {request.Description.Substring(0, Math.Min(15, request.Description.Length))}...",
                            TargetUrl = Url.Action("Details", "Requests", new { id = request.Id }),
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        });
                    }

                    await _context.SaveChangesAsync();
                    // ------------------------------------------------

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", request.CategoryId);
            return View(request);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Customer)
                .Include(r => r.Provider)
                .Include(r => r.Offers!)
                    .ThenInclude(o => o.Provider)
                        .ThenInclude(p => p.ReviewsReceived)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(int offerId)
        {
            var offer = await _context.ServiceOffers
                .Include(o => o.Request).ThenInclude(r => r.Customer)
                .Include(o => o.Provider)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null) return NotFound();

            var request = offer.Request;
            var customer = request.Customer;
            var provider = offer.Provider;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (customer.Id != userId) return Forbid();

            if (customer.WalletBalance < offer.Amount)
            {
                TempData["Error"] = "Insufficient balance in your wallet.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            customer.WalletBalance -= offer.Amount;
            request.Status = "Accepted";
            request.ServiceProviderId = provider.Id;
            request.FinalPrice = offer.Amount;
            offer.IsAccepted = true;

            _context.Notifications.Add(new Notification
            {
                UserId = provider.Id,
                Message = "Your offer was accepted! You can now start working.",
                TargetUrl = Url.Action("Dashboard", "Provider"),
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Offer accepted! {offer.Amount:C} deducted from wallet.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Transaction failed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("Cannot edit.");

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", request.CategoryId);
            return View(request);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,PreferredSchedule,CategoryId")] Request request)
        {
            if (id != request.Id) return NotFound();

            ModelState.Remove("Customer");
            ModelState.Remove("Category");
            ModelState.Remove("Provider");
            ModelState.Remove("CustomerId");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRequest = await _context.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                    request.CustomerId = existingRequest.CustomerId;
                    request.Status = existingRequest.Status;
                    request.CreatedAt = existingRequest.CreatedAt;

                    _context.Update(request);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) { }
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null && request.Status == "Pending")
            {
                request.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [Authorize(Roles = "ServiceProvider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            // 1. جلب الطلب فقط (بدون تعقيدات اليوزر)
            var request = await _context.Requests.FindAsync(id);

            if (request == null) return NotFound();

            // 2. تحديث حالة الطلب (عشان تظهر الفورم عند العميل)
            request.Status = "Completed";

            // 3. 🔥 إرسال الإشعار للعميل (ده اللي يهمنا) 🔥
            var notification = new Notification
            {
                UserId = request.CustomerId, // العميل هو اللي هيستلم
                Message = "✅ Service completed! Tap here to rate the provider.",
                TargetUrl = Url.Action("Details", "Requests", new { id = request.Id }), // هيوديه للصفحة اللي فيها النجوم
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            // 4. حفظ التغييرات
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order marked as completed!";

            // يرجعه لصفحة الشغل بتاعته
            return RedirectToAction("Dashboard", "Provider");
        }









    }

}