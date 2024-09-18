using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_K.Data;
using Project_K.Models;
using Project_K.Dtos;

namespace Project_K.Controllers
{
    public class DBViewController : Controller
    {
        private readonly KurinDbContext _context;

        public DBViewController(KurinDbContext context)
        {
            _context = context;
        }

        // GET: DBView
        public async Task<IActionResult> Index()
        {
            var kurinDbContext = _context.Members.Include(m => m.Address).Include(m => m.KurinLevel).Include(m => m.School).Include(m => m.MemberLevels).ThenInclude(ml => ml.Level);
            return View(await kurinDbContext.ToListAsync());
        }

        // GET: DBView/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.Address)
                .Include(m => m.KurinLevel)
                .Include(m => m.School)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // GET: DBView/Create
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Id", "AddressName");
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name");
            ViewData["SchoolId"] = new SelectList(_context.Schools, "Id", "Name");
            return View();
        }

        // POST: DBView/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,MiddleName,Nickname,BirthDate,Phone,Email,Telegram,PlastJoin,AddressId,SchoolId,KurinLevelId")] MemberDto memberDto)
        {
            if (ModelState.IsValid)
            {
                // Convert MemberDto to Member
                var member = new Member
                {
                    FirstName = memberDto.FirstName,
                    LastName = memberDto.LastName,
                    MiddleName = memberDto.MiddleName,
                    Nickname = memberDto.Nickname,
                    BirthDate = memberDto.BirthDate,
                    Phone = memberDto.Phone,
                    Email = memberDto.Email,
                    Telegram = memberDto.Telegram,
                    PlastJoin = memberDto.PlastJoin,
                    AddressId = memberDto.AddressId,
                    SchoolId = memberDto.SchoolId,
                    KurinLevelId = memberDto.KurinLevelId
                };
                _context.Add(member);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Id", "AddressName", memberDto.AddressId);
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name", memberDto.KurinLevelId);
            ViewData["SchoolId"] = new SelectList(_context.Schools, "Id", "Name", memberDto.SchoolId);
            return View(memberDto);
        }

        // GET: DBView/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Id", "AddressName", member.AddressId);
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name", member.KurinLevelId);
            ViewData["SchoolId"] = new SelectList(_context.Schools, "Id", "Name", member.SchoolId);
            return View(member);
        }

        // POST: DBView/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,MiddleName,Nickname,BirthDate,Phone,Email,Telegram,PlastJoin,AddressId,SchoolId,KurinLevelId")] MemberDto memberDto)
        {
            var member = await _context.Members.FindAsync(id);
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
                member.BirthDate = memberDto.BirthDate;
                member.Phone = memberDto.Phone;
                member.Email = memberDto.Email;
                member.Telegram = memberDto.Telegram;
                member.PlastJoin = memberDto.PlastJoin;
                member.AddressId = memberDto.AddressId;
                member.SchoolId = memberDto.SchoolId;
                member.KurinLevelId = memberDto.KurinLevelId;

                _context.Update(member);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Id", "AddressName", memberDto.AddressId);
            ViewData["KurinLevelId"] = new SelectList(_context.KurinLevels, "Id", "Name", memberDto.KurinLevelId);
            ViewData["SchoolId"] = new SelectList(_context.Schools, "Id", "Name", memberDto.SchoolId);
            return View(memberDto);
        }

        // GET: DBView/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.Address)
                .Include(m => m.KurinLevel)
                .Include(m => m.School)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: DBView/Delete/5
        [HttpPost, ActionName("Delete")]
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
