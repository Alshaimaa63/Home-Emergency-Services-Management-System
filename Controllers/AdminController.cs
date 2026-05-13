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

        // قمنا بحقن UserManager للتعامل مع اليوزرز والأدوار
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. لوحة التحكم المحدثة (Dashboard with Stats & Tabs) ---

        [Authorize(Policy = "AdminOnly")]
       
        public async Task<IActionResult> Dashboard()
        {
            // جلب البيانات
            var requests = await _context.Requests.Include(r => r.Category).Include(r => r.Customer).ToListAsync();
            var complaints = await _context.Complaints.ToListAsync();

            // جلب اليوزرز
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

            // إرسال البيانات للـ View
            ViewBag.Complaints = complaints;
            ViewBag.Providers = providers;
            ViewBag.Customers = customers;

            // حسابات الإحصائيات
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.PendingProviders = providers.Count(p => !p.IsVerified);
            ViewBag.OpenComplaints = complaints.Count(c => c.Status != "Solved");
            ViewBag.TotalOrders = requests.Count;

            // جلب محفظة الأدمن (تأكدي من صحة الإيميل)
            var admin = await _userManager.FindByEmailAsync("admin@home.com");
            ViewBag.AdminWallet = admin?.WalletBalance ?? 0;

            return View(requests);
        }

        // --- 2. إدارة المستخدمين (Verify & Delete) ---

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> VerifyProvider(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);

                // --- إضافة الإشعار هنا ---
                var notification = new Notification
                {
                    UserId = user.Id,
                    Message = "Congratulations! Your account has been verified by the Admin. You can now start browsing requests and submitting offers.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetUrl = Url.Action("AvailableRequests", "Provider") // هينقله لصفحة الطلبات المتاحة أول ما يدوس عليه
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                // -----------------------

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
                // إذا أردت حذف اليوزر نهائياً
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "User account has been permanently deleted.";
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

        // --- 4. نظام التقييم (كما هو) ---

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
                _context.ServiceReviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for your feedback!";
                return RedirectToAction("Index", "Home");
            }
            return View(review);
        }

        // --- 5. نظام الشكاوى (كما هو) ---

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