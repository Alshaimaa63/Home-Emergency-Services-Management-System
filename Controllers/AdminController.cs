using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Controllers
{
    // Policy removed from class level to allow all authenticated users to access specific actions
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // AdminOnly Policy applied strictly to the Dashboard
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.Requests.Include(r => r.Category).Include(r => r.Customer).ToListAsync();
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
                // Admin cancels the request directly
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
                complaint.Status = "Solved"; // Change status to Solved
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        // Accessible to any registered user (Customer or Provider)
        [HttpGet]
        public IActionResult SendComplaint()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendComplaint(Complaint complaint)
        {
            // Remove manual validation for fields handled by the server
            ModelState.Remove("UserEmail");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                complaint.UserEmail = User.Identity.Name;
                complaint.CreatedAt = DateTime.Now;

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                // Success message for the user
                TempData["Success"] = "Your complaint has been submitted successfully. The admin will review it soon.";
                return RedirectToAction("Index", "Home");
            }

            // Return to view with validation errors if model is invalid
            return View(complaint);
        }
    }
}