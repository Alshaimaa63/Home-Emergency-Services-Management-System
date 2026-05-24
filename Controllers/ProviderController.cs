using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    [Authorize(Roles = "ServiceProvider")]
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProviderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = await _userManager.GetUserAsync(User);

            // إضافة تنبيه في الداش بورد لو الحساب لسه ملوش صلاحية
            if (provider != null && !provider.IsVerified)
            {
                ViewBag.VerificationMessage = "Your account is currently under review by our team. You will be able to submit offers once verified.";
            }

            var myRequests = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Customer)
                .Where(r => r.ServiceProviderId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.TotalEarnings = myRequests
                .Where(r => r.Status == "Completed")
                .Sum(r => r.FinalPrice ?? 0)
                .ToString("F2");

            return View(myRequests);
        }

        [AllowAnonymous]
        public IActionResult Profile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            return RedirectToAction("Profile", "Account", new { id = id });
        }

        public async Task<IActionResult> AvailableRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            // لو اليوزر مش موثق، رجعه للداش بورد برسالة تحذير
            if (user == null || !user.IsVerified)
            {
                TempData["Error"] = "Your account is still pending admin approval. You cannot view or apply for jobs yet.";
                return RedirectToAction("Dashboard");
            }

            // لو موثق، كمل عادي واعرض الطلبات
            var requests = await _context.Requests
                .Include(r => r.Category)
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOffer(int RequestId, decimal Amount)
        {
            // 1. الحماية الجديدة: التأكد من السعر
            if (Amount < 50)
            {
                TempData["Error"] = "Invalid offer amount. The minimum offer must be 50 EGP.";
                return RedirectToAction("Details", "Requests", new { id = RequestId });
            }

            // 2. التأكد من توثيق الحساب (منع تقديم العرض برمجياً لو مش متوثق)
            var provider = await _userManager.GetUserAsync(User);
            if (provider != null && !provider.IsVerified)
            {
                TempData["Error"] = "Action Blocked: Only verified providers can submit offers.";
                return RedirectToAction("Details", "Requests", new { id = RequestId });
            }

            // 3. تحديد هوية المستخدم
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 4. التأكد إن الفني مقدمش عرض قبل كده لنفس الطلب
            var existingOffer = await _context.ServiceOffers
                .FirstOrDefaultAsync(o => o.RequestId == RequestId && o.ServiceProviderId == userId);

            if (existingOffer != null)
            {
                TempData["Error"] = "You have already submitted an offer for this request.";
                return RedirectToAction("Details", "Requests", new { id = RequestId });
            }

            // 5. إنشاء العرض الجديد
            var offer = new ServiceOffer
            {
                RequestId = RequestId,
                ServiceProviderId = userId,
                Amount = Amount,
                CreatedAt = DateTime.Now
            };

            _context.ServiceOffers.Add(offer);

            // 6. إرسال إشعار للعميل
            var requestObj = await _context.Requests.FindAsync(RequestId);
            if (requestObj != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = requestObj.CustomerId,
                    Message = $"New offer received: A provider offered {Amount:C} for your request.",
                    TargetUrl = Url.Action("Details", "Requests", new { id = RequestId }),
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your offer has been submitted successfully!";
            return RedirectToAction("Details", "Requests", new { id = RequestId });
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myHistory = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Customer)
                .Where(r => r.ServiceProviderId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(myHistory);
        }

       
        [HttpPost]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            var provider = await _userManager.GetUserAsync(User);

            // هنجيب حساب الأدمن الرئيسي (بواسطة الإيميل)
            var adminUser = await _userManager.FindByEmailAsync("admin@home.com");

            if (request != null && request.Status == "Accepted" && request.FinalPrice.HasValue)
            {
                request.Status = "Completed";

                decimal totalAmount = request.FinalPrice.Value;
                decimal providerShare = totalAmount * 0.90m; // 90% للمقدم
                decimal adminShare = totalAmount * 0.10m;    // 10% ربح الموقع

                provider.WalletBalance += providerShare;

                // إضافة الربح لمحفظة الأدمن
                if (adminUser != null)
                {
                    adminUser.WalletBalance += adminShare;
                    _context.Update(adminUser);
                }

                _context.Update(provider);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Job completed! Your share: {providerShare:C}. Admin fee: {adminShare:C}";
            }
            return RedirectToAction("Dashboard");
        }
    }
}