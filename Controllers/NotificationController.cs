using HomeServices.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // عرض الإشعارات وتحويلها لحالة "مقروءة"
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. جلب كل إشعارات المستخدم مرتبة من الأحدث للأقدم
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // 2. فلترة الإشعارات غير المقروءة فقط
            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();

            if (unreadNotifications.Any())
            {
                // 3. تحويلها لمقروءة
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                // 4. حفظ التغييرات في قاعدة البيانات عشان اللمبة الحمراء تطفي
                await _context.SaveChangesAsync();
            }

            return View(notifications);
        }

        
    }
}