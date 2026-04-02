using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Sessions
{
    public class RoomModel : PageModel
    {
        private static string _offer;
        private static string _answer;

        public void OnGet(int sessionId)
        {
        }

        public async Task<IActionResult> OnPostOfferAsync()
        {
            using var reader = new StreamReader(Request.Body);
            _offer = await reader.ReadToEndAsync();

            return new JsonResult(true);
        }

        public IActionResult OnGetGetOffer()
        {
            return new JsonResult(_offer);
        }

        public async Task<IActionResult> OnPostAnswerAsync()
        {
            using var reader = new StreamReader(Request.Body);
            _answer = await reader.ReadToEndAsync();

            return new JsonResult(true);
        }

        public IActionResult OnGetGetAnswer()
        {
            return new JsonResult(_answer);
        }
    }
}