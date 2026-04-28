using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace HomeServices.Controllers
{
    [Authorize(Roles = "Customer")] // كده العميل فقط هو اللي يقدر يفتح الكنترولر ده
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myRequests = _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Provider)
                .Where(r => r.CustomerId == userId)
                .OrderByDescending(r => r.CreatedAt);

            return View(await myRequests.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,PreferredSchedule,CategoryId")] Request request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                request.CustomerId = userId;
                request.Status = "Pending";
                request.CreatedAt = DateTime.Now;

                ModelState.Remove("CustomerId");
                ModelState.Remove("Customer");
                ModelState.Remove("Category");

                if (ModelState.IsValid)
                {
                    _context.Add(request);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", request.CategoryId);
            return View(request);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.Provider)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound();

            // نمنع التعديل لو الطلب مابقاش Pending
            if (request.Status != "Pending") return BadRequest("Cannot edit an accepted request.");

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", request.CategoryId);
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,PreferredSchedule,CategoryId")] Request request)
        {
            if (id != request.Id) return NotFound();

          
            ModelState.Remove("Customer");
            ModelState.Remove("Category");
            ModelState.Remove("Provider");
            ModelState.Remove("CustomerId");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRequest = await _context.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                    request.CustomerId = existingRequest.CustomerId;
                    request.Status = existingRequest.Status;
                    request.CreatedAt = existingRequest.CreatedAt;

                    _context.Update(request);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) { /* Handle error */ }
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null && request.Status == "Pending")
            {
                request.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
