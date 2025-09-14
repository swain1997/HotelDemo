using HotelDemo.Domain.Enums;
using HotelDemo.Infrastructure.Data;
using HotelDemo.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HotelDemo.Pages.Bookings;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IBookingService _svc;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(AppDbContext db, IBookingService svc, UserManager<IdentityUser> userManager)
    {
        _db = db; _svc = svc; _userManager = userManager;
    }

    // Filters (GET)
    [BindProperty(SupportsGet = true)] public int? pid { get; set; }
    [BindProperty(SupportsGet = true)] public string? q { get; set; }
    [BindProperty(SupportsGet = true)] public BookingStatus? status { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? from { get; set; }   // <— DateTime? για αξιόπιστο binding
    [BindProperty(SupportsGet = true)] public DateTime? to { get; set; }     // <— DateTime? για αξιόπιστο binding

    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";
    public List<Row> Items { get; set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Status { get; set; } = "";
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public int Nights { get; set; }
        public string? LeadGuest { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Property selection via query/cookie
        CurrentPropertyId = await ResolvePropertyIdAsync(pid);

        if (CurrentPropertyId == 0) return Page();

        CurrentPropertyName = await _db.Properties
            .Where(p => p.Id == CurrentPropertyId)
            .Select(p => p.Name)
            .FirstAsync();

        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.LeadGuest)
            .Where(b => b.PropertyId == CurrentPropertyId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(b => b.Code.Contains(q));

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        if (from.HasValue)
        {
            var fromDo = DateOnly.FromDateTime(from.Value.Date);
            query = query.Where(b => b.CheckInDate >= fromDo);
        }

        if (to.HasValue)
        {
            var toDo = DateOnly.FromDateTime(to.Value.Date);
            query = query.Where(b => b.CheckOutDate <= toDo);
        }

        Items = await query
            .OrderByDescending(b => b.BookedAt)
            .Take(200)
            .Select(b => new Row
            {
                Id = b.Id,
                Code = b.Code,
                Status = b.Status.ToString(),
                CheckIn = b.CheckInDate,
                CheckOut = b.CheckOutDate,
                Nights = b.Nights,
                LeadGuest = b.LeadGuest != null ? (b.LeadGuest.FirstName + " " + b.LeadGuest.LastName) : null,
                TotalAmount = b.TotalAmount
            })
            .ToListAsync();

        return Page();
    }

    public class NewBookingInput
    {
        [DataType(DataType.Date)]
        public DateTime? CheckIn { get; set; }   // <— DateTime? για σίγουρο binding από <input type="date">
        [DataType(DataType.Date)]
        public DateTime? CheckOut { get; set; }

        public int Adults { get; set; } = 2;
        public int Children { get; set; } = 0;
        public int Infants { get; set; } = 0;
    }

    [BindProperty] public NewBookingInput New { get; set; } = new();

    public async Task<IActionResult> OnPostNewAsync()
    {
        // Property (ίδια λογική με GET)
        CurrentPropertyId = await ResolvePropertyIdAsync(pid);
        if (CurrentPropertyId == 0)
        {
            ModelState.AddModelError(string.Empty, "No property found.");
            return await OnGetAsync();
        }

        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? "system";

        // Defaults & μετατροπή σε DateOnly
        var ciDt = (New.CheckIn?.Date) ?? DateTime.Today;
        var coDt = (New.CheckOut?.Date) ?? ciDt.AddDays(2);

        var ci = DateOnly.FromDateTime(ciDt);
        var co = DateOnly.FromDateTime(coDt);

        var booking = await _svc.CreateAsync(
            CurrentPropertyId,
            ci,
            co,
            New.Adults,
            New.Children,
            New.Infants,
            userId
        );

        return RedirectToPage("/Bookings/Edit", new { id = booking.Id });
    }

    // ---------------- helpers ----------------

    private async Task<int> ResolvePropertyIdAsync(int? pidFromQueryOrPost)
    {
        if (pidFromQueryOrPost.HasValue)
        {
            // γράψε cookie σωστά (Path="/", Secure ανάλογα με https)
            var options = new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("pid", pidFromQueryOrPost.Value.ToString(), options);
            return pidFromQueryOrPost.Value;
        }

        if (Request.Cookies.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var parsed))
            return parsed;

        // fallback: πρώτο property
        var first = await _db.Properties.OrderBy(p => p.Id).Select(p => p.Id).FirstOrDefaultAsync();
        return first;
    }
}
