using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Autoparts;
using Autoparts.Models;
using System.Runtime.ConstrainedExecution;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Autoparts.Controllers
{
    public class PartsController : Controller
    {
        private readonly Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PartsController(Context context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if HTTP context exists
            if (_httpContextAccessor.HttpContext == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            // Retrieve session data
            var login = _httpContextAccessor.HttpContext.Session.GetString("Login");
            var role = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var id = _httpContextAccessor.HttpContext.Session.GetInt32("Id");
            var password = _httpContextAccessor.HttpContext.Session.GetString("Password");

            // Check if session data is valid
            var isValidSession = !string.IsNullOrEmpty(login) &&
                                 !string.IsNullOrEmpty(role) &&
                                 id.HasValue &&
                                 !string.IsNullOrEmpty(password);

            // If session is valid, set ViewBag properties
            if (isValidSession)
            {
                var existingUser = _context.Users.FirstOrDefault(u =>
                    u.Login == login &&
                    u.Password == password &&
                    u.Id == id);

                if (existingUser != null)
                {
                    ViewBag.Id = id;
                    ViewBag.Login = login;
                    ViewBag.Role = role;
                    base.OnActionExecuting(context);
                    return;
                }
            }

            // Clear session
            _httpContextAccessor.HttpContext.Session.Remove("Id");
            _httpContextAccessor.HttpContext.Session.Remove("Login");
            _httpContextAccessor.HttpContext.Session.Remove("Role");
            _httpContextAccessor.HttpContext.Session.Remove("Password");

            // Reset ViewBag properties
            ViewBag.Id = null;
            ViewBag.Login = null;
            ViewBag.Role = null;

            base.OnActionExecuting(context);
        }

        private bool IsUserAnAdmin()
        {
            var role = _httpContextAccessor.HttpContext!.Session.GetString("Role");
            return role == "Admin";
        }

        private int? GetUserIdFromSession()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }

        private bool IsUserAuthenticated()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Login").IsNullOrEmpty();
        }

        // GET: Parts
        public async Task<IActionResult> Index()
        {
            var context = _context.Parts.Include(p => p.Brand).Include(p => p.Category);
            return View(await context.ToListAsync());
        }

        // GET: Parts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var part = await _context.Parts
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (part == null)
            {
                return NotFound();
            }

            var userId = GetUserIdFromSession();
            if (userId != null)
            {
                var user = await _context.Users
                    .Include(u => u.RelatedParts)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    // Check if the part is already in the related parts list
                    if (!user.RelatedParts.Contains(part))
                    {
                        // Add the part to the related parts list
                        user.RelatedParts.Add(part);

                        // If the count of related parts exceeds 20, remove the oldest one
                        if (user.RelatedParts.Count > 20)
                        {
                            var oldestPart = user.RelatedParts.OrderBy(rp => rp.Id).First();
                            user.RelatedParts.Remove(oldestPart);
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return View(part);
        }

        // POST: Issues/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int? partId, string commentText, int rating)
        {
            if (partId == null)
            {
                return NotFound("PartId not found.");
            }

            var part = await _context.Parts.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == partId);
            if (part == null)
            {
                return NotFound("Part not found.");
            }

            var userId = GetUserIdFromSession();

            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var review = new Review
            {
                PartId = part.Id,
                Comment = commentText,
                UserId = user.Id,
                CreateTime = DateTime.Now,
                Rating = rating
            };

            part.Reviews.Add(review);

            var ratingSum = 0;
            foreach (var r in part.Reviews)
            {
                ratingSum += r.Rating;
            }
            part.TotalRating = Math.Round((double) ratingSum / part.Reviews.Count(),2);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = partId });
        }

        // POST: Issues/RemoveMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveReview(int? partId, int? reviewId)
        {
            if (partId == null)
            {
                return NotFound("PartId not found.");
            }

            var part = await _context.Parts.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == partId);
            if (part == null)
            {
                return NotFound("Part not found.");
            }

            var review = part.Reviews.FirstOrDefault(m => m.Id == reviewId);

            if (review == null)
            {
                return NotFound();
            }

            var userId = GetUserIdFromSession();

            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            if (!IsUserAnAdmin() && review.UserId != userId)
            {
                return BadRequest("You are not owner of this review.");
            }

            part.Reviews.Remove(review);

            if (part.Reviews.Count > 0)
            {
                var ratingSum = 0;
                foreach (var r in part.Reviews)
                {
                    ratingSum += r.Rating;
                }
                part.TotalRating = (double) ratingSum / part.Reviews.Count();
            }
            else
            {
                part.TotalRating = null;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = partId });
        }

        // GET: Parts/Create
        public IActionResult Create()
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Parts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BrandId,CategoryId,TotalRating,TotalWishes,Name,Description,Photos")] Part part, List<IFormFile> Photos)
        {
            if (!ModelState.IsValid)
            {
                ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", part.BrandId);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", part.CategoryId);
                return View(part);
            }

            try
            {
                if (Photos != null && Photos.Any())
                {
                    foreach (var file in Photos.Where(f => f != null && f.Length > 0))
                    {
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", fileName);

                        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos")))
                        {
                            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos"));
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                            part.Photos.Add(fileName);
                        }
                    }
                }

                _context.Add(part);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                ViewData["ErrorMessage"] = "An error occurred while processing the request.";
                ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", part.BrandId);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", part.CategoryId);
                return View(part);
            }
        }

        // GET: Parts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
            {
                return NotFound();
            }

            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", part.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", part.CategoryId);
            return View(part);
        }

        // POST: Parts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BrandId,CategoryId,TotalRating,TotalWishes,Name,Description,Photos")] Part part, List<IFormFile> Photos)
        {
            if (id != part.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", part.BrandId);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", part.CategoryId);
                return View(part);
            }

            try
            {
                var originalPart = await _context.Parts.FirstOrDefaultAsync(p => p.Id == part.Id);

                if (originalPart == null)
                {
                    return NotFound();
                }

                if (Photos != null && Photos.Any())
                {
                    // Remove existing photos
                    foreach (var photo in originalPart.Photos)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", photo);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    originalPart.Photos.Clear();

                    // Add new photos
                    foreach (var file in Photos.Where(f => f != null && f.Length > 0))
                    {
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                            originalPart.Photos.Add(fileName);
                        }
                    }
                }

                // Update part properties
                originalPart.BrandId = part.BrandId;
                originalPart.CategoryId = part.CategoryId;
                originalPart.TotalRating = part.TotalRating;
                originalPart.Name = part.Name;
                originalPart.Description = part.Description;

                // Save changes
                _context.Update(originalPart);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartExists(part.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // GET: Parts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
            {
                return NotFound();
            }

            var part = await _context.Parts
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (part == null)
            {
                return NotFound();
            }

            return View(part);
        }

        // POST: Parts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part != null)
            {
                if (part.Photos != null && part.Photos.Any())
                {
                    foreach (var photo in part.Photos)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", photo);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                _context.Parts.Remove(part);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PartExists(int id)
        {
            return _context.Parts.Any(e => e.Id == id);
        }
    }
}
