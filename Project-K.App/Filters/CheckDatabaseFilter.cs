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
        // Перевіряємо, чи заповнена база даних
        bool isDatabaseInitialized = _context.Users.Any() && _context.KurinLevels.Any() && _context.Levels.Any() &&  _context.Teams.Any();

        if (isDatabaseInitialized)
        {
            // Якщо база даних готова, перенаправляємо на HomeController -> Index
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Тут нічого робити не потрібно
    }
}
