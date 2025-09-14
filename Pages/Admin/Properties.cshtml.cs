using System.ComponentModel.DataAnnotations;
using HotelDemo.Domain.Entities;
using HotelDemo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Admin;

public class PropertiesModel : PageModel
{
    private readonly AppDbContext _db;
    public PropertiesModel(AppDbContext db) => _db = db;

    public List<Row> Items { get; set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? City { get; set; }
        public string CountryCode { get; set; } = "";
        public TimeOnly CheckIn { get; set; }
        public TimeOnly CheckOut { get; set; }
    }

    public async Task OnGetAsync()
    {
        Items = await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new Row
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Email = p.Email,
                City = p.City,
                CountryCode = p.CountryCode,
                CheckIn = p.DefaultCheckInTime,
                CheckOut = p.DefaultCheckOutTime
            })
            .ToListAsync();

        // default values για τη φόρμα (14:00 / 11:00)
        if (Add.DefaultCheckInTime == default) Add.DefaultCheckInTime = new TimeOnly(14, 0);
        if (Add.DefaultCheckOutTime == default) Add.DefaultCheckOutTime = new TimeOnly(11, 0);
    }

    // ---------------- Create ----------------
    public class AddInput
    {
        [Required, StringLength(20)] public string Code { get; set; } = "";
        [Required, StringLength(200)] public string Name { get; set; } = "";
        [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
        [StringLength(50)] public string? Phone { get; set; }
        [StringLength(200)] public string? Address { get; set; }
        [StringLength(100)] public string? City { get; set; }
        [StringLength(20)] public string? PostalCode { get; set; }
        [Required, StringLength(2, MinimumLength = 2)] public string CountryCode { get; set; } = "GR";

        [Display(Name = "Default Check-in")] public TimeOnly DefaultCheckInTime { get; set; }
        [Display(Name = "Default Check-out")] public TimeOnly DefaultCheckOutTime { get; set; }
    }

    [BindProperty] public AddInput Add { get; set; } = new();

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        // έλεγχος μοναδικού Code
        var code = Add.Code.Trim();
        var exists = await _db.Properties.AnyAsync(p => p.Code == code);
        if (exists)
        {
            ModelState.AddModelError(nameof(Add.Code), "Code already exists.");
            await OnGetAsync();
            return Page();
        }

        var p = new Property
        {
            Code = code,
            Name = Add.Name.Trim(),
            Email = Add.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(Add.Phone) ? null : Add.Phone!.Trim(),
            Address = string.IsNullOrWhiteSpace(Add.Address) ? null : Add.Address!.Trim(),
            City = string.IsNullOrWhiteSpace(Add.City) ? null : Add.City!.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(Add.PostalCode) ? null : Add.PostalCode!.Trim(),
            CountryCode = Add.CountryCode.Trim().ToUpperInvariant(),
            DefaultCheckInTime = Add.DefaultCheckInTime,
            DefaultCheckOutTime = Add.DefaultCheckOutTime
        };

        _db.Properties.Add(p);
        await _db.SaveChangesAsync();

        TempData["ok"] = "Property created.";
        return RedirectToPage();
    }

    // ---------------- Delete ----------------
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var p = await _db.Properties.FindAsync(id);
        if (p == null) return RedirectToPage();

        try
        {
            _db.Properties.Remove(p);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Property deleted.";
        }
        catch (DbUpdateException)
        {
            TempData["err"] = "Cannot delete: this property has related data (rooms/roomtypes/bookings/guests).";
        }

        // αν έσβησες το τρέχον pid, καθάρισέ το (ίδιο Path με αυτό που βάλαμε στο cookie)
        if (Request.Cookies.TryGetValue("pid", out var pidStr) &&
            int.TryParse(pidStr, out var pidVal) &&
            pidVal == id)
        {
            Response.Cookies.Delete("pid", new CookieOptions { Path = "/" });
        }

        return RedirectToPage();
    }
}
