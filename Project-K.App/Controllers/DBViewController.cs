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
using Project_K.BusinessLogic.Interfaces;

namespace Project_K.Controllers
{
    public class DBViewController : Controller
    {
        private readonly string? _apiKey;
        private readonly UserManager<User> _userManager;
        private readonly IMemberService _memberService;
        private readonly IConfiguration _configuration;
        private readonly ISelectListService _selectListService;


        public DBViewController(IMemberService memberService, IConfiguration configuration, UserManager<User> userManager, ISelectListService selectListService)
        {
            _memberService = memberService;
            _configuration = configuration;
            _apiKey = _configuration["ApiSettings:ApiKey"];
            _userManager = userManager;
            _selectListService = selectListService;
        }

        // GET: DBView
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _memberService.GetMembersDetailed());
        }

        // GET: DBView/Details/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(uint id)
        {

            var member = await _memberService.GetMember(id);
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
            ViewBag.Levels = _selectListService.GetSelectList("Levels");
            ViewData["KurinLevelId"] = _selectListService.GetSelectList("KurinLevels");
            ViewData["TeamId"] = _selectListService.GetSelectList("Teams");
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

            if (ModelState.IsValid)
            {
                await _memberService.CreateMember(memberDto, user.Id);

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

            ViewBag.Levels = _selectListService.GetSelectList("Levels");
            ViewData["KurinLevelId"] = _selectListService.GetSelectList("KurinLevels");
            ViewData["TeamId"] = _selectListService.GetSelectList("Teams");
            ViewData["ApiKey"] = _apiKey;
            return View(memberDto);
        }

        // GET: DBView/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(uint id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var memberDto = await _memberService.GetDto(id, user.Id);

            ViewBag.Levels = _selectListService.GetSelectList("Levels");
            ViewData["KurinLevelId"] = _selectListService.GetSelectList("KurinLevels");
            ViewData["TeamId"] = _selectListService.GetSelectList("Teams");
            ViewData["ApiKey"] = _apiKey;
            
            return View(memberDto);
        }

        // POST: DBView/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(uint id, [Bind("Id,FirstName,LastName,MiddleName,Nickname,TeamId,BirthDate,Phone,Email,Telegram,PlastJoin,Address,School,KurinLevelId,SelectedLevelId")] MemberDto memberDto)
        {
            if (ModelState.IsValid)
            {
                await _memberService.UpdateMember(id, memberDto);
                return RedirectToAction(nameof(Index));
            }
            // Log ModelState errors
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Property: {state.Key}, Error: {error.ErrorMessage}");
                }
            }
            ViewBag.Levels = _selectListService.GetSelectList("Levels");
            ViewData["KurinLevelId"] = _selectListService.GetSelectList("KurinLevels");
            ViewData["TeamId"] = _selectListService.GetSelectList("Teams");
            ViewData["ApiKey"] = _apiKey;
            return View(memberDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        // GET: DBView/Delete/5
        public async Task<IActionResult> Delete(uint id)
        {
            var member = await _memberService.GetMemberDetailed(id);

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
        public async Task<IActionResult> DeleteConfirmed(uint id)
        {
            if (await _memberService.DeleteMember(id))
            {
                return RedirectToAction(nameof(Index));
            }
            else 
            {
                return NotFound();
            }
        }


        private async Task<bool> MemberExists(uint id)
        {
            return await _memberService.IsMemberExists(id);
        }
    }
}
