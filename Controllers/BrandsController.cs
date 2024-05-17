using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Autoparts;
using Autoparts.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace Autoparts.Controllers
{
    public class BrandsController : Controller
    {
        private readonly Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BrandsController(Context context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Brands
        public async Task<IActionResult> Index()
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            return View(await _context.Brands.ToListAsync());
        }

        // GET: Brands/Create
        public IActionResult Create()
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Brands/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brands/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brands/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                _context.Brands.Remove(brand);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
