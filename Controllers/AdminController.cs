using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- مهام الأدمن (Admin Dashboard) ---

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Customer)
                .ToListAsync();

            var complaints = await _context.Complaints.ToListAsync();

            ViewBag.Complaints = complaints;
            return View(requests);
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                request.Status = "Cancelled by Admin";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ResolveComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint != null)
            {
                complaint.Status = "Solved";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        // --- نظام التقييم (المهمة 2) ---
        // ملاحظة: تم تعديل المسميات هنا لتطابق الموديل الجديد ServiceReview

        [HttpGet]
        public async Task<IActionResult> RateService(int requestId)
        {
            // جلب الطلب ومعاه بيانات البروفيدر والكاتيجوري
            var request = await _context.Requests
                .Include(r => r.Provider)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            var model = new ServiceReview
            {
                RequestId = requestId,
                ServiceProviderId = request.ServiceProviderId ?? "", // الاسم الجديد
                CustomerId = request.CustomerId ?? ""
            };

            ViewBag.ProviderName = request.Provider?.FullName ?? "Specialist";
            ViewBag.CategoryName = request.Category?.Name ?? "Service";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateService(ServiceReview review)
        {
            // إزالة الـ Validation للـ Navigation Properties لمنع الـ ModelState Error
            ModelState.Remove("Customer");
            ModelState.Remove("ServiceProvider"); // تم تحديث الاسم هنا
            ModelState.Remove("Request");

            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;

                // التأكد من استخدام الـ DbSet الصحيح في الـ Context
                _context.ReviewsReceived.Add(review);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for your feedback! Your review helps others choose the best providers.";
                return RedirectToAction("Index", "Home");
            }
            return View(review);
        }

        // --- نظام الشكاوى (Complaints) ---

        [HttpGet]
        public IActionResult SendComplaint()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendComplaint(Complaint complaint)
        {
            ModelState.Remove("UserEmail");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                complaint.UserEmail = User.Identity?.Name ?? "Anonymous";
                complaint.CreatedAt = DateTime.Now;

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your complaint has been submitted successfully. The admin will review it soon.";
                return RedirectToAction("Index", "Home");
            }
            return View(complaint);
        }
    }
}