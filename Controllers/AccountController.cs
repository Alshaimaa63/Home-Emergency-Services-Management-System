using HomeServices.Data;
using HomeServices.Models;
using HomeServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                    // العميل يبدأ بـ 1000 رصيد تجريبي
                    WalletBalance = model.Role == "Customer" ? 1000.00m : 0.00m,
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

                    if (model.Role == "Admin") return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // ==========================================
        // 2. تسجيل الدخول (Login)
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
        // 3. عرض الملف الشخصي (Profile)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Profile(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUserId = string.IsNullOrEmpty(id) ? currentUserId : id;

            if (targetUserId == null) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.ReviewsReceived).ThenInclude(r => r.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(id) || id == currentUserId)
            {
                var userForRefresh = await _userManager.FindByIdAsync(currentUserId);
                await _signInManager.RefreshSignInAsync(userForRefresh);
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(user);
        }

        // ==========================================
        // 4. تعديل الملف الشخصي (EditProfile)
        // ==========================================
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
                // 🔥 حماية الداتابيز: لو السيرة الذاتية أطول من 1000 حرف، هنقص الزيادة 🔥
                if (!string.IsNullOrEmpty(bio) && bio.Length > 1000)
                {
                    user.Bio = bio.Substring(0, 1000);
                }
                else
                {
                    user.Bio = bio;
                }

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
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your profile has been updated successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(user);
        }
    }
}