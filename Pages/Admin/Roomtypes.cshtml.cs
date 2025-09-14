using System.ComponentModel.DataAnnotations;
using HotelDemo.Domain.Entities;
using HotelDemo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Admin;

public class RoomTypesModel : PageModel
{
    private readonly AppDbContext _db;
    public RoomTypesModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int? pid { get; set; }
    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";

    public List<Row> Items { get; set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int BaseOccupancy { get; set; }
        public int MaxOccupancy { get; set; }
        public string BedConfiguration { get; set; } = "";
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await ResolvePropertyAsync();
        if (CurrentPropertyId == 0) return Page();

        Items = await _db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.PropertyId == CurrentPropertyId)
            .OrderBy(rt => rt.DisplayOrder).ThenBy(rt => rt.Name)
            .Select(rt => new Row
            {
                Id = rt.Id,
                Code = rt.Code,
                Name = rt.Name,
                BaseOccupancy = rt.BaseOccupancy,
                MaxOccupancy = rt.MaxOccupancy,
                BedConfiguration = rt.BedConfiguration,
                IsActive = rt.IsActive,
                DisplayOrder = rt.DisplayOrder
            })
            .ToListAsync();

        // default values for add form
        if (Add.BaseOccupancy == 0) Add.BaseOccupancy = 2;
        if (Add.MaxOccupancy == 0) Add.MaxOccupancy = 2;
        if (Add.DisplayOrder == 0) Add.DisplayOrder = 0;

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
        [Required, StringLength(20)] public string Code { get; set; } = "";
        [Required, StringLength(150)] public string Name { get; set; } = "";
        [StringLength(1000)] public string? Description { get; set; }
        [Range(1, 10)] public int BaseOccupancy { get; set; }
        [Range(1, 10)] public int MaxOccupancy { get; set; }
        [Required, StringLength(100)] public string BedConfiguration { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    [BindProperty] public AddInput Add { get; set; } = new();

    public async Task<IActionResult> OnPostAddAsync()
    {
        await ResolvePropertyAsync();
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }

        if (Add.MaxOccupancy < Add.BaseOccupancy)
        {
            ModelState.AddModelError(nameof(Add.MaxOccupancy), "Max occupancy must be ≥ Base occupancy.");
            await OnGetAsync(); return Page();
        }

        var exists = await _db.RoomTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.PropertyId == CurrentPropertyId && rt.Code == Add.Code);

        if (exists)
        {
            ModelState.AddModelError(nameof(Add.Code), "Code already exists for this property.");
            await OnGetAsync(); return Page();
        }

        var rt = new RoomType
        {
            PropertyId = CurrentPropertyId,
            Code = Add.Code.Trim(),
            Name = Add.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Add.Description) ? null : Add.Description!.Trim(),
            BaseOccupancy = Add.BaseOccupancy,
            MaxOccupancy = Add.MaxOccupancy,
            BedConfiguration = Add.BedConfiguration.Trim(),
            IsActive = Add.IsActive,
            DisplayOrder = Add.DisplayOrder
        };

        _db.RoomTypes.Add(rt);
        await _db.SaveChangesAsync();
        TempData["ok"] = "Room type created.";
        return RedirectToPage(); // κρατάμε property από το cookie
    }

    // -------- Delete --------
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var rt = await _db.RoomTypes.FindAsync(id);
        if (rt == null) return RedirectToPage();

        try
        {
            _db.RoomTypes.Remove(rt);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Room type deleted.";
        }
        catch (DbUpdateException)
        {
            TempData["err"] = "Cannot delete: there are related rooms or bookings.";
        }

        return RedirectToPage(); // δεν χρειάζεται pid στο query
    }
}
