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

		// --- التعديل هنا لحل مشكلة الإيرور ---
		public async Task<IActionResult> Dashboard()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// 1. نجيب كل الطلبات المرتبطة بالبروفيدر ده (المقبولة والمكتملة)
			var myRequests = await _context.Requests
				.Include(r => r.Category)
				.Include(r => r.Customer)
				.Where(r => r.ServiceProviderId == userId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			// 2. نحسب إجمالي الأرباح من الطلبات المكتملة فقط
			ViewBag.TotalEarnings = myRequests
				.Where(r => r.Status == "Completed")
				.Sum(r => r.FinalPrice ?? 0)
				.ToString("F2"); // تنسيق رقمي

			// 3. نبعت اللستة للـ View عشان الجدول يشتغل
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
			var requests = await _context.Requests
				.Include(r => r.Category)
				.Where(r => r.Status == "Pending")
				.ToListAsync();

			return View(requests);
		}

		[HttpPost]
		public async Task<IActionResult> SubmitOffer(int RequestId, decimal Amount)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var existingOffer = await _context.ServiceOffers
				.FirstOrDefaultAsync(o => o.RequestId == RequestId && o.ServiceProviderId == userId);

			if (existingOffer != null)
			{
				TempData["Error"] = "You have already submitted an offer for this request.";
				return RedirectToAction("Details", "Requests", new { id = RequestId });
			}

			var offer = new ServiceOffer
			{
				RequestId = RequestId,
				ServiceProviderId = userId,
				Amount = Amount,
				CreatedAt = DateTime.Now
			};

			_context.ServiceOffers.Add(offer);

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
				.Include(r => r.Customer) // أضفنا الـ Customer عشان لو الجدول محتاج اسمه
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

			if (request != null && request.Status == "Accepted" && request.FinalPrice.HasValue)
			{
				request.Status = "Completed";

				decimal providerShare = request.FinalPrice.Value * 0.90m;
				provider.WalletBalance += providerShare;

				_context.Notifications.Add(new Notification
				{
					UserId = request.CustomerId,
					Message = "Service Completed! Please rate the provider and leave your feedback.",
					TargetUrl = Url.Action("Details", "Requests", new { id = request.Id }),
					CreatedAt = DateTime.Now
				});

				_context.Update(provider);
				await _context.SaveChangesAsync();

				TempData["Success"] = $"Job completed! {providerShare:C} has been added to your wallet.";
			}
			else
			{
				TempData["Error"] = "Could not complete the request. Please check final price.";
			}

			return RedirectToAction(nameof(Dashboard)); // عدلناها تروح للـ Dashboard أحسن
		}
	}
}