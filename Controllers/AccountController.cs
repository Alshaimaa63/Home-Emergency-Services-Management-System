using HomeServices.Data;
using HomeServices.Models;
using HomeServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 RoleManager<IdentityRole> roleManager,
                                 ApplicationDbContext context,
                                 IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. تسجيل مستخدم جديد (Register)
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // تنظيف النصوص من المسافات الزائدة
                string cleanName = System.Text.RegularExpressions.Regex.Replace(model.FullName.Trim(), @"\s+", " ");
                string cleanAddress = System.Text.RegularExpressions.Regex.Replace(model.Address.Trim(), @"\s+", " ");

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = cleanName,
                    Address = cleanAddress,
                    PhoneNumber = model.PhoneNumber,
                    CreatedAt = DateTime.Now,

                    // --- التعديل الجوهري هنا ---
                    // العميل يبدأ بـ 1000، والبروفايدر أو أي دور آخر يبدأ بـ 0
                    WalletBalance = model.Role == "Customer" ? 1000.00m : 0.00m,

                    // البروفايدر يبدأ غير موثق والعميل موثق تلقائياً (أو حسب رغبتك)
                    IsVerified = model.Role == "Customer",

                    Bio = model.Role == "ServiceProvider" ? "Professional service provider ready to help!" : null,
                    Specialty = model.Role == "ServiceProvider" ? "General Maintenance" : null,
                    ProfilePicture = "default-user.png"
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));

                    await _userManager.AddToRoleAsync(user, model.Role);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // توجيه ذكي بعد التسجيل مباشرة
                    if (model.Role == "Admin") return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // ==========================================
        // 2. تسجيل الدخول (Login) مع التوجيه للأدمن
        // ==========================================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    // إذا كان أدمن، يفتح الداش بورد فوراً
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 3. عرض وتعديل الملف الشخصي (Profile)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Profile(string id)
        {
            // منع الأدمن من دخول صفحة البروفايل العادية
            if (User.IsInRole("Admin") && string.IsNullOrEmpty(id))
                return RedirectToAction("Dashboard", "Admin");

            var userId = string.IsNullOrEmpty(id) ? _userManager.GetUserId(User) : id;
            var user = await _context.Users
                .Include(u => u.ReviewsReceived).ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(user);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string bio, string specialty, string address, string phoneNumber, IFormFile profilePic)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Address = address ?? user.Address;
            user.PhoneNumber = phoneNumber ?? user.PhoneNumber;

            if (User.IsInRole("ServiceProvider"))
            {
                user.Bio = bio;
                user.Specialty = specialty;
            }

            if (profilePic != null && profilePic.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePic.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(fileStream);
                }
                user.ProfilePicture = uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Your profile has been updated successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(user);
        }
    }
}