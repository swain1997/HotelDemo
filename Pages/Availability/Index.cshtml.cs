using HotelDemo.Infrastructure.Data;
using HotelDemo.Infrastructure.Services;
using HotelDemo.Infrastructure.Services.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Availability;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAvailabilityService _svc;

    public IndexModel(AppDbContext db, IAvailabilityService svc)
    {
        _db = db; _svc = svc;
    }

    [BindProperty(SupportsGet = true)]
    public int? pid { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? start { get; set; }

    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";
    public AvailabilityResult? Data { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // pick property (from query or cookie or first)
        if (pid.HasValue)
        {
            Response.Cookies.Append("pid", pid.Value.ToString(), new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite= SameSiteMode.Lax
            });
            CurrentPropertyId = pid.Value;
        }
        else if (Request.Cookies.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var parsed))
        {
            CurrentPropertyId = parsed;
        }
        else
        {
            CurrentPropertyId = await _db.Properties.OrderBy(p => p.Id).Select(p => p.Id).FirstOrDefaultAsync();
        }

        if (CurrentPropertyId == 0) return Page();

        CurrentPropertyName = await _db.Properties
            .Where(p => p.Id == CurrentPropertyId)
            .Select(p => p.Name)
            .FirstAsync();

        var s = start ?? DateOnly.FromDateTime(DateTime.Today);
        Data = await _svc.GetAvailabilityAsync(CurrentPropertyId, s, 14);

        return Page();
    }
}
