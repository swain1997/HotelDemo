using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelDemo.Domain.Entities;
using HotelDemo.Infrastructure.Data;               // AppDbContext     // Property (αν είναι σε αυτό το namespace)
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Pages.Booking
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        // Το τρέχον επιλεγμένο Property Id (για το dropdown)
        public int? CurrentPid { get; private set; }

        // Τα properties για το dropdown
        public List<Property> Props { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(int? pid)
        {
            // 1) Αν ήρθε pid από το query, αυτό υπερισχύει
            if (pid.HasValue)
            {
                CurrentPid = pid;

                // 2) Γράψε/ανανεώσε το cookie για επόμενα requests
                Response.Cookies.Append(
                    "pid",
                    pid.Value.ToString(),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax
                    }
                );

                // Δεν κάνουμε redirect: έτσι το dropdown δείχνει αμέσως το σωστό selected
                // Αν θες καθαρό URL, μπορείς να βάλεις: return RedirectToPage();
            }
            else
            {
                // 3) Αλλιώς, πάρε το current από το cookie (αν υπάρχει)
                if (Request.Cookies.TryGetValue("pid", out var pidStr)
                    && int.TryParse(pidStr, out var pidVal))
                {
                    CurrentPid = pidVal;
                }
            }

            // 4) Φέρε τα properties αλφαβητικά
            Props = await _db.Properties
                             .OrderBy(p => p.Name)
                             .ToListAsync();

            return Page();
        }
    }
}
