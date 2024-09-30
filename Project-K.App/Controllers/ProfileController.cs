using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Models;

namespace Project_K.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
    private readonly KurinDbContext _context;

    public ProfileController(UserManager<User> userManager, KurinDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Отримати Id поточного користувача
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Знайти мембера за UserId
        var member = await _context.Members
        .Include(m => m.KurinLevel)
        .Include(m => m.Team)
        .Include(m => m.User)
        .Include(m => m.MemberLevels)
        .ThenInclude(ml => ml.Level)
        .FirstOrDefaultAsync(m => m.UserId == userId);

        if (member == null)
        {
            return RedirectToAction("Register", "DBView");
        }

        return View(member);
    }
    }
}
