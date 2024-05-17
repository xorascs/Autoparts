using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Autoparts;
using Autoparts.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Autoparts.Controllers
{
    public class UsersController : Controller
    {
        private readonly Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersController(Context context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.RelatedParts)
                    .ThenInclude(rp => rp.Brand)
                .Include(u => u.Wishes)
                    .ThenInclude(w => w.Part)
                        .ThenInclude(p => p.Brand)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Part)
                        .ThenInclude(p => p.Brand)
                .Include(u => u.Issues)
                    .ThenInclude(i => i.Messages)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public async Task<IActionResult> AddToWishes(int? partId, string? commentText)
        {
            if (partId == null)
                return NotFound("PartId not found.");

            var part = await _context.Parts.FirstOrDefaultAsync(m => m.Id == partId);
            if (part == null)
                return NotFound("Part not found.");

            var userId = GetUserIdFromSession();
            if (userId == null)
                return NotFound("UserId not found.");

            var user = await _context.Users
                .Include(u => u.Wishes)
                    .ThenInclude(w => w.Part)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            // Check if any wish already contains the part
            if (user.Wishes.Any(w => w.Part!.Id == partId))
            {
                return RedirectToAction("Details", "Users", new { id = userId });
            }

            // Add the part to the user's wishes
            user.Wishes.Add(new Wish { Part = part, Comment = commentText, CreateTime = DateTime.Now });

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Users", new { id = userId });
        }

        public async Task<IActionResult> RemoveFromWishes(int? partId)
        {
            if (partId == null)
                return NotFound("PartId not found.");

            var userId = GetUserIdFromSession();
            if (userId == null)
                return NotFound("UserId not found.");

            var user = await _context.Users
                .Include(u => u.Wishes)
                    .ThenInclude(w => w.Part)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var wish = user.Wishes.FirstOrDefault(w => w.Part!.Id == partId);
            if (wish == null)
            {
                return BadRequest("The part is not in your wishes.");
            }

            // Remove the part from the user's wishes
            user.Wishes.Remove(wish);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Users", new { id = userId });
        }


        public IActionResult Login()
        {
            return IsUserAuthenticated() ? RedirectToAction("Index", "Home") : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("Login,Password")] UserFormViewModel user)
        {
            if (ModelState.IsValid)
            {
                var isUserExist = await _context.Users.FirstOrDefaultAsync(
                    u => u.Login == user.Login && u.Password == user.Password);

                if (isUserExist == null)
                {
                    ModelState.AddModelError("Login", "Incorrect login or password.");
                    return View(user);
                }

                SetUserSession(isUserExist);
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        public IActionResult Register()
        {
            return IsUserAuthenticated() ? RedirectToAction("Index", "Home") : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Login,Password")] UserFormViewModel user)
        {
            if (ModelState.IsValid)
            {
                var isUserExist = await _context.Users.FirstOrDefaultAsync(u => u.Login == user.Login);

                if (isUserExist != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }
                var userData = new User { Login = user.Login, Password = user.Password };
                _context.Users.Add(userData);
                await _context.SaveChangesAsync();

                SetUserSession(userData);
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        public IActionResult LogOut()
        {
            // Clear session data
            _httpContextAccessor.HttpContext?.Session.Clear();

            // Clear ViewData
            ViewData["Login"] = null;
            ViewData["Role"] = null;

            return RedirectToAction("Index", "Home");
        }

        private void SetUserSession(User user)
        {
            _httpContextAccessor.HttpContext!.Session.SetInt32("Id", user.Id);
            _httpContextAccessor.HttpContext.Session.SetString("Login", user.Login);
            _httpContextAccessor.HttpContext.Session.SetString("Role", user.Role.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("Password", user.Password);
        }

        public IActionResult Create()
        {
            if (!IsUserAnAdmin())
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Role,Login,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                var isUserExist = await _context.Users.FirstOrDefaultAsync(u => u.Login == user.Login);

                if (isUserExist != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!IsUserAnAdmin() && user.Id != GetUserIdFromSession())
                return RedirectToAction("Index", "Home");

            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Role,Login,Password")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Find the existing user entity
                    var existingUser = await _context.Users.FindAsync(user.Id);

                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Detach the existing user entity from the context
                    _context.Entry(existingUser).State = EntityState.Detached;

                    var isUserExist = await _context.Users.FirstOrDefaultAsync(u => u.Login == user.Login && u.Id != user.Id);

                    if (isUserExist != null)
                    {
                        ModelState.AddModelError("Login", "User with this login already exists.");
                        return View(user);
                    }

                    if (user.Id == GetUserIdFromSession())
                    {
                        SetUserSession(user);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (!IsUserAnAdmin() && user.Id != GetUserIdFromSession())
                return RedirectToAction("Index", "Home");

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
