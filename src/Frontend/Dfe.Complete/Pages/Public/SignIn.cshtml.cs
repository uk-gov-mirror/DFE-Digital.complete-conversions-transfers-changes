using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Complete.Pages.Public
{
    [AllowAnonymous]
    public class SignIn : PageModel
    {
        public void OnGet()
        {
            // Page load logic can be added here if needed
        }
    }
}