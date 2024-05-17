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
    public class IssuesController : Controller
    {
        private readonly Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IssuesController(Context context, IHttpContextAccessor httpContextAccessor)
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


        // GET: Issues
        public async Task<IActionResult> Index()
        {
            return View(await _context.Issues.Include(i => i.User).ToListAsync());
        }

        // GET: Issues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .Include(i => i.User)
                .Include(i => i.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (issue == null)
            {
                return NotFound();
            }

            return View(issue);
        }

        // POST: Issues/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMessage(int? issueId, string commentText)
        {
            if (issueId == null)
            {
                return NotFound("IssueId not found.");
            }

            var issue = await _context.Issues.Include(i => i.Messages).FirstOrDefaultAsync(m => m.Id == issueId);

            if (issue == null)
            {
                return NotFound("Issue not found.");
            }

            if (issue.Solved == true)
                return BadRequest("Issue is solved.");

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

            var message = new Message
            {
                IssueId = issue.Id,
                Comment = commentText,
                UserId = user.Id,
                CreateTime = DateTime.Now
            };

            issue.Messages.Add(message);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = issueId });
        }

        // POST: Issues/RemoveMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMessage(int? issueId, int? messageId)
        {
            if (issueId == null || messageId == null)
            {
                return NotFound("IssueId or messageId not found.");
            }

            var issue = await _context.Issues.Include(i => i.Messages).FirstOrDefaultAsync(m => m.Id == issueId);

            if (issue == null)
            {
                return NotFound("Issue not found");
            }

            if (issue.Solved == true)
                return BadRequest("Issue is solved.");

            var message = issue.Messages.FirstOrDefault(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound("Message not found.");
            }

            var userId = GetUserIdFromSession();

            if (userId == null)
            {
                return RedirectToAction("Login", "Users");
            }

            if (message.UserId != userId)
            {
                return BadRequest("You are not owner of this message.");
            }

            issue.Messages.Remove(message);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = issueId });
        }

        // POST: Issues/RemoveMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeIssueStatus(int? issueId)
        {
            if (issueId == null)
                return NotFound("IssueId not found.");

            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == issueId);
            if (issue == null)
                return NotFound("Issue not found.");

            if (!IsUserAnAdmin())
                return BadRequest("You are not admin.");

            issue.Solved = !issue.Solved;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = issueId });
        }

        public async Task<IActionResult> CreateIssueByUser(string title, string problemText)
        {
            var userId = GetUserIdFromSession();
            if (userId == null)
                return RedirectToAction("Login", "Users");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            var issue = new Issue
            {
                UserId = user.Id,
                Title = title,
                Problem = problemText,
                Solved = false,
            };

            _context.Add(issue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = issue.Id });
        }

        // GET: Issues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .FirstOrDefaultAsync(m => m.Id == id);
            if (issue == null)
            {
                return NotFound();
            }

            if (!IsUserAnAdmin() && issue.UserId != GetUserIdFromSession())
                return RedirectToAction("Index", "Home");

            return View(issue);
        }

        // POST: Issues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
                return NotFound("Issue not found.");

            if (!IsUserAnAdmin() && issue.UserId != GetUserIdFromSession())
                return BadRequest("You are not owner of the issue.");

            _context.Issues.Remove(issue);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IssueExists(int id)
        {
            return _context.Issues.Any(e => e.Id == id);
        }
    }
}
