using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. لوحة التحكم المحدثة (إظهار إحصائيات الأرباح والمستخدمين) ---

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Dashboard()
        {
            // جلب البيانات الأساسية
            var requests = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Customer)
                .ToListAsync();

            var complaints = await _context.Complaints.ToListAsync();

            // جلب اليوزرز وفلترتهم حسب الأدوار
            var allUsers = await _context.Users.ToListAsync();
            var providers = new List<ApplicationUser>();
            var customers = new List<ApplicationUser>();

            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "ServiceProvider"))
                    providers.Add(user);
                else if (await _userManager.IsInRoleAsync(user, "Customer"))
                    customers.Add(user);
            }

            // حساب أرباح الموقع (الـ 10%) ديناميكياً من الطلبات المنتهية
            var totalCompletedRequestsValue = requests
                .Where(r => r.Status == "Completed" || r.Status == "Finished")
                .Sum(r => (decimal?)r.FinalPrice ?? 0m);

            ViewBag.TotalAdminProfit = totalCompletedRequestsValue * 0.10m; // إجمالي العمولات المستحقة للموقع

            // جلب محفظة الأدمن الفعلية (التي تتحدث عند كل عملية Finish)
            var admin = await _userManager.FindByEmailAsync("admin@servicepro.com");
            ViewBag.AdminWallet = admin?.WalletBalance ?? 0;

            // إرسال البيانات للـ View
            ViewBag.Complaints = complaints;
            ViewBag.Providers = providers;
            ViewBag.Customers = customers;

            // حسابات الإحصائيات
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.PendingProviders = providers.Count(p => !p.IsVerified);
            ViewBag.OpenComplaints = complaints.Count(c => c.Status != "Solved");
            ViewBag.TotalOrders = requests.Count;

            return View(requests);
        }

        // --- 2. إدارة المستخدمين (توثيق وحذف) ---

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> VerifyProvider(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);

                // إشعار الفني بالتوثيق
                var notification = new Notification
                {
                    UserId = user.Id,
                    Message = "Congratulations! Your account has been verified by the Admin. You can now start browsing requests and submitting offers.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetUrl = Url.Action("AvailableRequests", "Provider")
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Provider has been verified and notified successfully!";
            }
            return RedirectToAction("Dashboard");
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // 1. مسح الإشعارات المتعلقة بهذا اليوزر
                var userNotifications = _context.Notifications.Where(n => n.UserId == id);
                _context.Notifications.RemoveRange(userNotifications);

                // 2. مسح العروض (Offers) المقدمة من الفني أو المرتبطة بطلبات العميل
                var userOffers = _context.ServiceOffers.Where(o => o.ServiceProviderId == id || o.Request.CustomerId == id);
                _context.ServiceOffers.RemoveRange(userOffers);

                // 3. مسح التقييمات من جدول ServiceReviews الموحد
                var userReviews = _context.ServiceReviews.Where(r => r.CustomerId == id || r.ServiceProviderId == id);
                _context.ServiceReviews.RemoveRange(userReviews);

                // 4. مسح الطلبات (Requests) المرتبطة بهذا اليوزر (سواء كان كعميل أو كفني)
                var userRequests = _context.Requests.Where(r => r.CustomerId == id || r.ServiceProviderId == id);
                _context.Requests.RemoveRange(userRequests);

                // حفظ التغييرات المؤثرة على الجداول الفرعية أولاً
                await _context.SaveChangesAsync();

                // 5. مسح حساب اليوزر الأساسي نهائياً
                await _userManager.DeleteAsync(user);

                TempData["Success"] = "User account and all related operational histories have been permanently deleted.";
            }
            return RedirectToAction("Dashboard");
        }

        // --- 3. إدارة الطلبات والشكاوى ---

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

        // --- 4. نظام التقييم ---

        [HttpGet]
        public async Task<IActionResult> RateService(int requestId)
        {
            var request = await _context.Requests
                .Include(r => r.Provider)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            var model = new ServiceReview
            {
                RequestId = requestId,
                ServiceProviderId = request.ServiceProviderId ?? "",
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
            ModelState.Remove("Customer");
            ModelState.Remove("ServiceProvider");
            ModelState.Remove("Request");

            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                _context.ServiceReviews.Add(review); // تعديل اسم الجدول ليتوافق مع الـ DbContext الموحد
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for your feedback!";
                return RedirectToAction("Index", "Home");
            }
            return View(review);
        }

        // --- 5. نظام الشكاوى ---

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

                TempData["Success"] = "Your complaint has been submitted successfully.";
                return RedirectToAction("Index", "Home");
            }
            return View(complaint);
        }
    }
}