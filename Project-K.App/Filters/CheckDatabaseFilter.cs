using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class CheckDatabaseStateFilter : IActionFilter
{
    private readonly KurinDbContext _context;

    public CheckDatabaseStateFilter(KurinDbContext context)
    {
        _context = context;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_context.Users.Any()) context.Result = new RedirectToActionResult("SeedAdmin", "Seed", null);
        else if (!_context.Levels.Any() && !_context.Teams.Any() && !_context.KurinLevels.Any()) context.Result = new RedirectToActionResult("SeedOtherData", "Seed", null);
        else context.Result = new RedirectToActionResult("Index", "Home", null);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Тут нічого робити не потрібно
    }
}
