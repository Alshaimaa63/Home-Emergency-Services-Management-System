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

        // ==========================================
        // 1. عرض طلبات العميل
        // ==========================================
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

        // ==========================================
        // 2. إنشاء طلب جديد
        // ==========================================
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

                    var allProviders = await _userManager.GetUsersInRoleAsync("ServiceProvider");
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
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", request.CategoryId);
            return View(request);
        }

        // ==========================================
        // 3. تفاصيل الطلب والعروض
        // ==========================================
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

        // ==========================================
        // 4. قبول عرض السعر (خصم من العميل)
        // ==========================================
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

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Offer accepted! {offer.Amount:0.00} EGP deducted from wallet.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. إنهاء العمل (تقسيم المبلغ 90% للفني و 10% للموقع)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "ServiceProvider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Provider)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            request.Status = "Completed";

            // 🔥 الربط المالي الجديد مع نسبة 10% للموقع 🔥
            if (request.Provider != null && request.FinalPrice > 0)
            {
                var providerUser = request.Provider as ApplicationUser;

                if (providerUser != null)
                {
                    // 1. الحسبة المالية
                    decimal totalAmount = (decimal)request.FinalPrice;
                    decimal siteCommission = totalAmount * 0.10m; // عمولة 10%
                    decimal providerProfit = totalAmount - siteCommission; // الصافي 90%

                    // 2. إضافة الـ 90% لمحفظة الفني
                    providerUser.WalletBalance += providerProfit;
                    _context.Users.Update(providerUser);

                    // 3. إضافة الـ 10% لمحفظة الأدمن (Profit)
                    // ملحوظة: تأكدي أن إيميل الأدمن مطابق لما في الداتابيز
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@servicepro.com");
                    if (adminUser != null)
                    {
                        adminUser.WalletBalance += siteCommission;
                        _context.Users.Update(adminUser);
                    }
                }
            }

            // إرسال إشعار للعميل للتقييم
            _context.Notifications.Add(new Notification
            {
                UserId = request.CustomerId,
                Message = "✅ Service completed! Tap here to rate the provider.",
                TargetUrl = Url.Action("Details", "Requests", new { id = request.Id }),
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Job Finished! Funds processed (90% to you, 10% platform fee).";

            return RedirectToAction("Dashboard", "Provider");
        }

        // ==========================================
        // 6. عمليات إضافية (تعديل وإلغاء)
        // ==========================================
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
                var existingRequest = await _context.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                request.CustomerId = existingRequest.CustomerId;
                request.Status = existingRequest.Status;
                request.CreatedAt = existingRequest.CreatedAt;

                _context.Update(request);
                await _context.SaveChangesAsync();
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
    }
}