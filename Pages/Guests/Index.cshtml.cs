using System.ComponentModel.DataAnnotations;
using HotelDemo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Guests;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    // Query params
    [BindProperty(SupportsGet = true)] public int? pid { get; set; }
    [BindProperty(SupportsGet = true)] public string? q { get; set; }      // search text
    [BindProperty(SupportsGet = true)] public int take { get; set; } = 100;

    public int CurrentPropertyId { get; set; }
    public string CurrentPropertyName { get; set; } = "";
    public List<Row> Items { get; set; } = new();

    public class Row
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Nationality { get; set; }
        public string? Doc { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPropertyId = await ResolvePropertyIdAsync(pid);
        if (CurrentPropertyId == 0) return Page();

        CurrentPropertyName = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == CurrentPropertyId)
            .Select(p => p.Name)
            .FirstAsync();

        var queryable = _db.Guests
            .AsNoTracking()
            .Where(g => g.PropertyId == CurrentPropertyId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = q.Trim();
            queryable = queryable.Where(g =>
                g.FirstName.Contains(like) || g.LastName.Contains(like) ||
                (g.Email != null && g.Email.Contains(like)) ||
                (g.Phone != null && g.Phone.Contains(like)) ||
                (g.DocumentNumber != null && g.DocumentNumber.Contains(like)));
        }

        Items = await queryable
            .OrderBy(g => g.LastName).ThenBy(g => g.FirstName)
            .Take(Math.Clamp(take, 10, 500))
            .Select(g => new Row
            {
                Id = g.Id,
                FullName = g.FirstName + " " + g.LastName,
                Email = g.Email,
                Phone = g.Phone,
                Nationality = g.NationalityCode,
                Doc = (g.DocumentType ?? "") + (string.IsNullOrEmpty(g.DocumentNumber) ? "" : $" #{g.DocumentNumber}")
            })
            .ToListAsync();

        return Page();
    }

    // -------- Quick Add --------
    public class AddInput
    {
        [Required, StringLength(100)] public string FirstName { get; set; } = "";
        [Required, StringLength(100)] public string LastName { get; set; } = "";
        [EmailAddress, StringLength(200)] public string? Email { get; set; }
        [StringLength(50)] public string? Phone { get; set; }
        [StringLength(2)] public string? NationalityCode { get; set; }  // ISO2
        [StringLength(30)] public string? DocumentType { get; set; }
        [StringLength(50)] public string? DocumentNumber { get; set; }
    }

    [BindProperty] public AddInput Add { get; set; } = new();

    public async Task<IActionResult> OnPostAddAsync()
    {
        CurrentPropertyId = await ResolvePropertyIdAsync(pid);

        if (!ModelState.IsValid)
        {
            // επαναφόρτωση λίστας για να φανεί το validation summary
            await OnGetAsync();
            return Page();
        }

        _db.Guests.Add(new Domain.Entities.Guest
        {
            PropertyId = CurrentPropertyId,
            FirstName = Add.FirstName,
            LastName = Add.LastName,
            Email = Add.Email,
            Phone = Add.Phone,
            NationalityCode = string.IsNullOrWhiteSpace(Add.NationalityCode) ? null : Add.NationalityCode!.Trim().ToUpperInvariant(),
            DocumentType = Add.DocumentType,
            DocumentNumber = Add.DocumentNumber
        });

        await _db.SaveChangesAsync();

        // clear POST; το ενεργό property κρατιέται στο cookie
        return RedirectToPage();
    }

    // -------------- helpers --------------
    private async Task<int> ResolvePropertyIdAsync(int? pidFromQueryOrPost)
    {
        if (pidFromQueryOrPost.HasValue)
        {
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
        return await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
    }
}
