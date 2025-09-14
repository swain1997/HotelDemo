using System.ComponentModel.DataAnnotations;
using HotelDemo.Domain.Enums;
using HotelDemo.Infrastructure.Data;
using HotelDemo.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Bookings;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IBookingService _svc;
    private readonly UserManager<IdentityUser> _userManager;

    public EditModel(AppDbContext db, IBookingService svc, UserManager<IdentityUser> userManager)
    {
        _db = db; _svc = svc; _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public int id { get; set; }
    [BindProperty(SupportsGet = true)] public string? t { get; set; } // active tab: details|rooms|guests|payments

    public Vm Booking { get; set; } = new();
    public List<RoomTypeOpt> RoomTypeOptions { get; set; } = new();
    public Dictionary<int, List<RoomOpt>> RoomsByType { get; set; } = new();

    public class Vm
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public int PropertyId { get; set; }
        public BookingStatus Status { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public int Nights { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }

        public List<RoomLine> Rooms { get; set; } = new();
        public List<GuestLine> Guests { get; set; } = new();
        public List<PaymentLine> Payments { get; set; } = new();
    }

    public class RoomLine
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = "";
        public int? RoomId { get; set; }
        public string? RoomCode { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public int Nights { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class GuestLine
    {
        public int BookingGuestId { get; set; }
        public int GuestId { get; set; }
        public string FullName { get; set; } = "";
        public bool IsLead { get; set; }
    }

    public class PaymentLine
    {
        public int Id { get; set; }
        public string Method { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
    }

    public class RoomTypeOpt { public int Id { get; set; } public string Name { get; set; } = ""; }
    public class RoomOpt { public int Id { get; set; } public string Code { get; set; } = ""; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();

        // Prefill της φόρμας Details από τα δεδομένα της κράτησης
        Details = new DetailsInput
        {
            Status = Booking.Status,
            CheckIn = Booking.CheckIn.ToDateTime(TimeOnly.MinValue),
            CheckOut = Booking.CheckOut.ToDateTime(TimeOnly.MinValue),
            Adults = Booking.Adults,
            Children = Booking.Children,
            Infants = Booking.Infants,
            ContactEmail = Booking.ContactEmail,
            ContactPhone = Booking.ContactPhone,
            Notes = Booking.Notes
        };

        return Page();
    }

    private async Task LoadAsync()
    {
        var b = await _db.Bookings
            .Include(x => x.Property)
            .Include(x => x.BookingRooms).ThenInclude(br => br.Room)
            .Include(x => x.BookingRooms).ThenInclude(br => br.RoomType)
            .Include(x => x.BookingGuests).ThenInclude(bg => bg.Guest)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (b == null) throw new InvalidOperationException("Booking not found.");

        Booking = new Vm
        {
            Id = b.Id,
            Code = b.Code,
            PropertyName = b.Property?.Name ?? "",
            PropertyId = b.PropertyId,
            Status = b.Status,
            CheckIn = b.CheckInDate,
            CheckOut = b.CheckOutDate,
            Nights = b.Nights,
            Adults = b.Adults,
            Children = b.Children,
            Infants = b.Infants,
            ContactEmail = b.ContactEmail,
            ContactPhone = b.ContactPhone,
            Notes = b.Notes,
            TotalAmount = b.TotalAmount,
            Rooms = b.BookingRooms.OrderBy(r => r.Id).Select(r => new RoomLine
            {
                Id = r.Id,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = r.RoomType!.Name,
                RoomId = r.RoomId,
                RoomCode = r.Room?.Code,
                CheckIn = r.CheckInDate,
                CheckOut = r.CheckOutDate,
                Nights = r.Nights,
                LineTotal = r.LineTotal
            }).ToList(),
            Guests = b.BookingGuests.OrderBy(g => g.BookingId).Select(g => new GuestLine
            {
                BookingGuestId = g.Id,
                GuestId = g.GuestId,
                FullName = g.Guest != null ? $"{g.Guest.FirstName} {g.Guest.LastName}" : $"#{g.GuestId}",
                IsLead = g.IsLeadGuest
            }).ToList(),
            Payments = b.Payments.OrderByDescending(p => p.ReceivedAt).Select(p => new PaymentLine
            {
                Id = p.Id,
                Method = p.Method,
                Amount = p.Amount,
                ReceivedAt = p.ReceivedAt
            }).ToList()
        };

        // RoomType options & Rooms per type (dropdowns)
        RoomTypeOptions = await _db.RoomTypes
            .Where(rt => rt.PropertyId == Booking.PropertyId && rt.IsActive)
            .OrderBy(rt => rt.DisplayOrder).ThenBy(rt => rt.Name)
            .Select(rt => new RoomTypeOpt { Id = rt.Id, Name = rt.Name })
            .ToListAsync();

        var roomGroups = await _db.Rooms
            .Where(r => r.PropertyId == Booking.PropertyId && r.IsActive)
            .OrderBy(r => r.Code)
            .GroupBy(r => r.RoomTypeId)
            .Select(g => new { RoomTypeId = g.Key, Items = g.Select(r => new RoomOpt { Id = r.Id, Code = r.Code }).ToList() })
            .ToListAsync();

        RoomsByType = roomGroups.ToDictionary(x => x.RoomTypeId, x => x.Items);

        if (string.IsNullOrEmpty(t)) t = "details";
    }

    // ---------- Forms / Inputs ----------
    public class DetailsInput
    {
        public BookingStatus Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckIn { get; set; }     // DateTime? για σίγουρο binding από <input type="date">

        [DataType(DataType.Date)]
        public DateTime? CheckOut { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Notes { get; set; }
    }

    [BindProperty] public DetailsInput Details { get; set; } = new();

    public class AddRoomInput { public int RoomTypeId { get; set; } public int? RoomId { get; set; } }
    [BindProperty] public AddRoomInput AddRoom { get; set; } = new();

    public class AssignRoomInput { public int LineId { get; set; } public int? RoomId { get; set; } }
    [BindProperty] public AssignRoomInput AssignRoom { get; set; } = new();

    public class AddGuestInput { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; public bool IsLead { get; set; } }
    [BindProperty] public AddGuestInput AddGuest { get; set; } = new();

    public class RemoveGuestInput { public int BookingGuestId { get; set; } }
    [BindProperty] public RemoveGuestInput RemoveGuest { get; set; } = new();

    public class AddPaymentInput
    {
        public string Method { get; set; } = "Cash";
        public decimal Amount { get; set; }
    }
    [BindProperty] public AddPaymentInput Pay { get; set; } = new();

    // ---------- Handlers ----------
    public async Task<IActionResult> OnPostDetailsAsync()
    {
        try
        {
            // Πάρε τις τρέχουσες ημερομηνίες ως fallback
            var cur = await _db.Bookings
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.CheckInDate, x.CheckOutDate })
                .FirstAsync();

            var ci = Details.CheckIn.HasValue
                ? DateOnly.FromDateTime(Details.CheckIn.Value.Date)
                : cur.CheckInDate;

            var co = Details.CheckOut.HasValue
                ? DateOnly.FromDateTime(Details.CheckOut.Value.Date)
                : cur.CheckOutDate;

            await _svc.UpdateDetailsAsync(
                id,
                ci,
                co,
                Details.Adults,
                Details.Children,
                Details.Infants,
                Details.ContactEmail,
                Details.ContactPhone,
                Details.Notes,
                Details.Status
            );
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return RedirectToPage(new { id, t = "details" });
    }

    public async Task<IActionResult> OnPostAddRoomAsync()
    {
        try { await _svc.AddRoomAsync(id, AddRoom.RoomTypeId, AddRoom.RoomId); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "rooms" });
    }

    public async Task<IActionResult> OnPostAssignRoomAsync()
    {
        try { await _svc.AssignRoomAsync(AssignRoom.LineId, AssignRoom.RoomId); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "rooms" });
    }

    public async Task<IActionResult> OnPostRemoveRoomAsync(int lineId)
    {
        try { await _svc.RemoveRoomAsync(lineId); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "rooms" });
    }

    public async Task<IActionResult> OnPostAddGuestAsync()
    {
        try
        {
            var booking = await _db.Bookings.AsNoTracking().Where(b => b.Id == id).Select(b => new { b.PropertyId }).FirstAsync();
            var g = await _svc.AddGuestQuickAsync(booking.PropertyId, AddGuest.FirstName, AddGuest.LastName);
            await _svc.AttachGuestAsync(id, g.Id, AddGuest.IsLead);
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "guests" });
    }

    public async Task<IActionResult> OnPostRemoveGuestAsync()
    {
        try { await _svc.RemoveGuestAsync(RemoveGuest.BookingGuestId); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "guests" });
    }

    public async Task<IActionResult> OnPostAddPaymentAsync()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "system";
            var booking = await _db.Bookings.AsNoTracking().Where(b => b.Id == id).Select(b => new { b.PropertyId }).FirstAsync();
            await _svc.AddPaymentAsync(id, booking.PropertyId, Pay.Method, Pay.Amount, userId);
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        return RedirectToPage(new { id, t = "payments" });
    }
}
