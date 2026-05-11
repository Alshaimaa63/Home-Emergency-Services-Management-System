using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int RequestId, string ServiceProviderId, int Rating, string Comment)
        {
            // 1. جلب ID العميل الحالي
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. التأكد من وصول البيانات الأساسية
            if (string.IsNullOrEmpty(ServiceProviderId) || RequestId <= 0)
            {
                TempData["Error"] = "Missing required data (Provider ID or Request ID).";
                return RedirectToAction("Details", "Requests", new { id = RequestId });
            }

            // 3. التأكد من عدم وجود تقييم سابق لهذا الطلب
            var existingReview = await _context.ReviewsReceived
                .AnyAsync(r => r.RequestId == RequestId);

            if (existingReview)
            {
                TempData["Error"] = "You have already submitted a review for this service.";
                return RedirectToAction("Details", "Requests", new { id = RequestId });
            }

            // 4. إنشاء كائن التقييم
            var review = new ServiceReview
            {
                RequestId = RequestId,
                ServiceProviderId = ServiceProviderId,
                CustomerId = customerId,
                Rating = Rating,
                Comment = Comment ?? "No comment provided.",
                CreatedAt = DateTime.Now
            };

            // 5. الحفظ وإرسال الإشعار
            try
            {
                // إضافة التقييم
                _context.ReviewsReceived.Add(review);

                // --- إضافة الإشعار للفني هنا ---
                var notification = new Notification
                {
                    UserId = ServiceProviderId, // الفني هو المستلم
                    Message = $"Customer {User.Identity.Name} rated you {Rating} stars!",
                    TargetUrl = "/Account/Profile", // يوجهه لصفحة بروفايله عشان يشوف النجوم
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                // -----------------------------

                // حفظ التغييرين معاً (التقييم + الإشعار)
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you! Your review has been published and the provider has been notified.";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Database Error: " + innerMessage;
            }

            return RedirectToAction("Details", "Requests", new { id = RequestId });
        }
    }
}