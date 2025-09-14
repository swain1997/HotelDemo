using System.ComponentModel.DataAnnotations;
using HotelDemo.Domain.Entities;
using HotelDemo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Admin;

public class RoomsModel : PageModel
{
    private readonly AppDbContext _db;
    public RoomsModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int? pid { get; set; }
    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";

    public List<Row> Items { get; set; } = new();
    public List<RoomTypeOpt> RoomTypeOptions { get; set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoomTypeOpt { public int Id { get; set; } public string Name { get; set; } = ""; }

    public async Task<IActionResult> OnGetAsync()
    {
        await ResolvePropertyAsync();
        if (CurrentPropertyId == 0) return Page();

        RoomTypeOptions = await _db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.PropertyId == CurrentPropertyId && rt.IsActive)
            .OrderBy(rt => rt.DisplayOrder).ThenBy(rt => rt.Name)
            .Select(rt => new RoomTypeOpt { Id = rt.Id, Name = rt.Name })
            .ToListAsync();

        Items = await _db.Rooms
            .AsNoTracking()
            .Where(r => r.PropertyId == CurrentPropertyId)
            .OrderBy(r => r.Code)
            .Select(r => new Row
            {
                Id = r.Id,
                Code = r.Code,
                RoomTypeName = r.RoomType != null ? r.RoomType.Name : $"#{r.RoomTypeId}",
                Price = r.BasePricePerNight,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Page();
    }

    private async Task ResolvePropertyAsync()
    {
        if (pid.HasValue)
        {
            Response.Cookies.Append("pid", pid.Value.ToString(), new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,
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

        if (CurrentPropertyId != 0)
        {
            CurrentPropertyName = await _db.Properties
                .AsNoTracking()
                .Where(p => p.Id == CurrentPropertyId)
                .Select(p => p.Name)
                .FirstAsync();
        }
    }

    // -------- Create --------
    public class AddInput
    {
        [Required] public int RoomTypeId { get; set; }
        [Required, StringLength(20)] public string Code { get; set; } = "";
        [Range(0, 100000)] public decimal BasePricePerNight { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(2000)] public string? Notes { get; set; }
    }

    [BindProperty] public AddInput Add { get; set; } = new();

    public async Task<IActionResult> OnPostAddAsync()
    {
        await ResolvePropertyAsync();
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }

        // Validate room type belongs to current property
        var belongs = await _db.RoomTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.Id == Add.RoomTypeId && rt.PropertyId == CurrentPropertyId);
        if (!belongs)
        {
            ModelState.AddModelError(nameof(Add.RoomTypeId), "Invalid room type for this property.");
            await OnGetAsync(); return Page();
        }

        // Unique code per property
        var exists = await _db.Rooms
            .AsNoTracking()
            .AnyAsync(r => r.PropertyId == CurrentPropertyId && r.Code == Add.Code);
        if (exists)
        {
            ModelState.AddModelError(nameof(Add.Code), "Code already exists for this property.");
            await OnGetAsync(); return Page();
        }

        var room = new Room
        {
            PropertyId = CurrentPropertyId,
            RoomTypeId = Add.RoomTypeId,
            Code = Add.Code.Trim(),
            BasePricePerNight = Add.BasePricePerNight,
            IsActive = Add.IsActive,
            Notes = string.IsNullOrWhiteSpace(Add.Notes) ? null : Add.Notes!.Trim()
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        TempData["ok"] = "Room created.";
        return RedirectToPage(); // cookie κρατά το ενεργό property
    }

    // -------- Delete --------
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var r = await _db.Rooms.FindAsync(id);
        if (r == null) return RedirectToPage();

        try
        {
            _db.Rooms.Remove(r);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Room deleted.";
        }
        catch (DbUpdateException)
        {
            TempData["err"] = "Cannot delete: this room is referenced by bookings.";
        }

        return RedirectToPage(); // δεν χρειάζεται pid στο query
    }
}
