using Microsoft.AspNetCore.Mvc;  
using Microsoft.AspNetCore.Mvc.RazorPages;  
using Microsoft.AspNetCore.Authentication;  
using Microsoft.AspNetCore.Authentication.Cookies;  
using System.Security.Claims;  

namespace FGFB.Pages.Commish  
{  
    public class RegistrationsLoginModel : PageModel  
    {  
        [BindProperty]  
        public string Email { get; set; }  
        [BindProperty]  
        public string Password { get; set; }  

        public void OnGet()  
        {  
        }  

        public async Task<IActionResult> OnPostAsync()  
        {  
            // Validate email  
            if (Email != "judy@fangirlfootball.com" && Email != "stacy@fangirlfootball.com")  
            {  
                ModelState.AddModelError(string.Empty, "Invalid email.");  
                return Page();  
            }  

            // Validate password  
            if (Password != "X976ksfootball")  
            {  
                ModelState.AddModelError(string.Empty, "Invalid password.");  
                return Page();  
            }  

            // Set cookie authentication  
            var claims = new List<Claim>  
            {  
                new Claim(ClaimTypes.Name, Email)  
            };  
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);  
            var authProperties = new AuthenticationProperties  
            {  
                IsPersistent = true  
            };  

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);  

            // Redirect to Registrations page  
            return RedirectToPage("/Commish/Registrations");  
        }  
    }  
}