using HotelDemo.Domain.Enums;
using HotelDemo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int? pid { get; set; } // property id από query

    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";

    // KPIs
    public int RoomsTotal { get; set; }
    public int RoomsActive { get; set; }
    public int ArrivalsToday { get; set; }
    public int DeparturesToday { get; set; }
    public int InHouse { get; set; }
    public decimal PaymentsThisMonth { get; set; }

    public List<BookingRow> TodayArrivals { get; set; } = new();
    public List<BookingRow> TodayDepartures { get; set; } = new();

    public class BookingRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Nights { get; set; }
        public string? LeadGuest { get; set; }
        public string Status { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // 1) Βρες τρέχον property (query -> cookie -> πρώτο)
        if (pid.HasValue)
        {
            Response.Cookies.Append("pid", pid.Value.ToString(), new CookieOptions
            {
                Path = "/",                               // διαθέσιμο παντού
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,                 // μόνο σε https
                SameSite = SameSiteMode.Lax
            });
            CurrentPropertyId = pid.Value;
        }
        else if (Request.Cookies.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var parsed))
        {
            CurrentPropertyId = parsed;
        }
        else
        {
            CurrentPropertyId = await _db.Properties
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();
        }

        if (CurrentPropertyId == 0)
            return Page();

        CurrentPropertyName = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == CurrentPropertyId)
            .Select(p => p.Name)
            .FirstAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var monthEnd = monthStart.AddMonths(1);

        // 2) KPIs
        RoomsTotal = await _db.Rooms
            .AsNoTracking()
            .CountAsync(r => r.PropertyId == CurrentPropertyId);

        RoomsActive = await _db.Rooms
            .AsNoTracking()
            .CountAsync(r => r.PropertyId == CurrentPropertyId && r.IsActive);

        ArrivalsToday = await _db.Bookings
            .AsNoTracking()
            .CountAsync(b =>
                b.PropertyId == CurrentPropertyId &&
                b.CheckInDate == today &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.NoShow);

        DeparturesToday = await _db.Bookings
            .AsNoTracking()
            .CountAsync(b =>
                b.PropertyId == CurrentPropertyId &&
                b.CheckOutDate == today &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.NoShow);

        InHouse = await _db.Bookings
            .AsNoTracking()
            .CountAsync(b =>
                b.PropertyId == CurrentPropertyId &&
                b.Status == BookingStatus.CheckedIn);

        PaymentsThisMonth = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PropertyId == CurrentPropertyId &&
                        p.ReceivedAt >= monthStart &&
                        p.ReceivedAt < monthEnd)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // 3) Λίστες
        TodayArrivals = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.PropertyId == CurrentPropertyId &&
                        b.CheckInDate == today &&
                        b.Status != BookingStatus.Cancelled &&
                        b.Status != BookingStatus.NoShow)
            .OrderBy(b => b.Code)
            .Select(b => new BookingRow
            {
                Id = b.Id,
                Code = b.Code,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Nights = b.Nights,
                LeadGuest = b.LeadGuest != null ? (b.LeadGuest.FirstName + " " + b.LeadGuest.LastName) : null,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        TodayDepartures = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.PropertyId == CurrentPropertyId &&
                        b.CheckOutDate == today &&
                        b.Status != BookingStatus.Cancelled &&
                        b.Status != BookingStatus.NoShow)
            .OrderBy(b => b.Code)
            .Select(b => new BookingRow
            {
                Id = b.Id,
                Code = b.Code,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Nights = b.Nights,
                LeadGuest = b.LeadGuest != null ? (b.LeadGuest.FirstName + " " + b.LeadGuest.LastName) : null,
                Status = b.Status.ToString()
            })
            .ToListAsync();

        return Page();
    }
}
