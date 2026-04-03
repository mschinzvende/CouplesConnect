using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Sessions
{
    public class PreJoinModel : PageModel
    {
        public int SessionId { get; set; }

        public void OnGet(int sessionId)
        {
            SessionId = sessionId;
        }
    }
}