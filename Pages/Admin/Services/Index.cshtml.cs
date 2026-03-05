using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Services
{

    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Service> Services { get; set; }

        public async Task OnGetAsync()
        {

            Services = await _context.Services
            .OrderBy(x => x.Name)
            .ToListAsync();

        }

    }
}
