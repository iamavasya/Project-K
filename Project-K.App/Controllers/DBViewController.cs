using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_K.Infrastructure.Data;
using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Project_K.Controllers
{

    public class DBViewController : Controller
    {
        private readonly string? _apiKey;
        private readonly UserManager<User> _userManager;
        private readonly KurinDbContext _context;
        private readonly IConfiguration _configuration;


        public DBViewController(KurinDbContext context, IConfiguration configuration, UserManager<User> userManager)
        {
            _context = context;
            _configuration = configuration;
            _apiKey = _configuration["ApiSettings:ApiKey"];
            _userManager = userManager;
        }

        // GET: DBView
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var kurinDbContext = _context.Members.Include(m => m.KurinLevel).Include(m => m.Team).Include(m => m.MemberLevels).ThenInclude(ml => ml.Level);
            return View(await kurinDbContext.ToListAsync());
        }

        // GET: DBView/Details/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.KurinLevel)
                .Include(m => m.Team)
                .Include(m => m.MemberLevels)
                .ThenInclude(ml => ml.Level)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // GET: DBView/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Levels = new SelectList(_context.Levels, "Id", "Name");
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name");
            ViewData["TeamId"] = new SelectList(_context.Teams, "Id", "Name");
            ViewData["ApiKey"] = _apiKey;
            return View();
        }

        // POST: DBView/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,MiddleName,Nickname,TeamId,BirthDate,Phone,Email,Telegram,PlastJoin,Address,School,KurinLevelId,SelectedLevelId")] MemberDto memberDto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }
            memberDto.UserId = user.Id;
            if (ModelState.IsValid)
            {
                // Convert MemberDto to Member
                var member = new Member
                {
                    FirstName = memberDto.FirstName,
                    LastName = memberDto.LastName,
                    MiddleName = memberDto.MiddleName,
                    Nickname = memberDto.Nickname,
                    TeamId = memberDto.TeamId,
                    BirthDate = memberDto.BirthDate,
                    Phone = memberDto.Phone,
                    Email = memberDto.Email,
                    Telegram = memberDto.Telegram,
                    PlastJoin = memberDto.PlastJoin,
                    Address = memberDto.Address,
                    School = memberDto.School,
                    KurinLevelId = memberDto.KurinLevelId,
                    UserId = memberDto.UserId
                };
                _context.Add(member);
                await _context.SaveChangesAsync();

                var memberLevel = new MemberLevel
                {
                    MemberId = member.Id,
                    LevelId = memberDto.SelectedLevelId,
                    AchieveDate = null
                };

                _context.Add(memberLevel);
                await _context.SaveChangesAsync();

                user.IsMemberInfoCompleted = true;
                await _userManager.UpdateAsync(user);

                return RedirectToAction("Index", "Profile");
            }
            // Log ModelState errors
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Property: {state.Key}, Error: {error.ErrorMessage}");
                }
            }

            ViewBag.Levels = new SelectList(_context.Levels, "Id", "Name");
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name");
            ViewData["TeamId"] = new SelectList(_context.Teams, "Id", "Name");
            ViewData["ApiKey"] = _apiKey;
            return View(memberDto);
        }

        // GET: DBView/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }


            var member = await _context.Members.Include(m => m.MemberLevels).FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }
            var memberDto = new MemberDto
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                MiddleName = member.MiddleName,
                Nickname = member.Nickname,
                TeamId = member.TeamId,
                BirthDate = member.BirthDate,
                Phone = member.Phone,
                Email = member.Email,
                Telegram = member.Telegram,
                PlastJoin = member.PlastJoin,
                Address = member.Address,
                School = member.School,
                KurinLevelId = member.KurinLevelId,
                SelectedLevelId = member.MemberLevels.FirstOrDefault()?.LevelId ?? 0,
                UserId = user.Id
            };

            ViewBag.Levels = new SelectList(_context.Levels, "Id", "Name", memberDto.SelectedLevelId);
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name", memberDto.KurinLevelId);
            ViewData["TeamId"] = new SelectList(_context.Teams, "Id", "Name", memberDto.TeamId);
            ViewData["ApiKey"] = _apiKey;
            
            return View(memberDto);
        }

        // POST: DBView/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,MiddleName,Nickname,TeamId,BirthDate,Phone,Email,Telegram,PlastJoin,Address,School,KurinLevelId,SelectedLevelId")] MemberDto memberDto)
        {
            var member = await _context.Members
                .Include(m => m.MemberLevels)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                member.FirstName = memberDto.FirstName;
                member.LastName = memberDto.LastName;
                member.MiddleName = memberDto.MiddleName;
                member.Nickname = memberDto.Nickname;
                member.TeamId = memberDto.TeamId;
                member.BirthDate = memberDto.BirthDate;
                member.Phone = memberDto.Phone;
                member.Email = memberDto.Email;
                member.Telegram = memberDto.Telegram;
                member.PlastJoin = memberDto.PlastJoin;
                member.Address = memberDto.Address;
                member.School = memberDto.School;
                member.KurinLevelId = memberDto.KurinLevelId;

                _context.Update(member);
                await _context.SaveChangesAsync();

                var memberLevel = member.MemberLevels.FirstOrDefault();
                if (memberLevel != null)
                {
                    memberLevel.LevelId = memberDto.SelectedLevelId;
                    _context.Update(memberLevel);
                }
                else
                {
                    memberLevel = new MemberLevel
                    {
                        MemberId = member.Id,
                        LevelId = memberDto.SelectedLevelId,
                        AchieveDate = null
                    };
                    _context.Add(memberLevel);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Levels = new SelectList(_context.Levels, "Id", "Name", memberDto.SelectedLevelId);
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name", memberDto.KurinLevelId);
            ViewData["TeamId"] = new SelectList(_context.Teams, "Id", "Name", memberDto.TeamId);
            ViewData["ApiKey"] = _apiKey;
            return View(memberDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        // GET: DBView/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.KurinLevel)
                .Include(m => m.Team)
                .Include(m => m.MemberLevels)
                .ThenInclude(ml => ml.Level)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: DBView/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpDelete, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.Id == id);
        }
    }
}
