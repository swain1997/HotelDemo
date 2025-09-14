using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HotelDemo.Pages
{
    public class SetPidModel : PageModel
    {
        public IActionResult OnGet(int pid, string? returnUrl)
        {
            var options = new CookieOptions
            {
                Path = "/",                      // διαθέσιμο παντού
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = Request.IsHttps,        // μόνο σε https
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append("pid", pid.ToString(), options);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Index");
        }
    }
}
