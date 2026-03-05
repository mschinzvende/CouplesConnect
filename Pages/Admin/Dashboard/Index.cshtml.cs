using CouplesConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CouplesConnect.Pages.Admin.Dashboard
{

    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalCounselors { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalRevenue { get; set; }

        public string ChartLabels { get; set; }
        public string ChartData { get; set; }

        public async Task OnGetAsync()
        {

            TotalUsers = await _context.Users.CountAsync();
            TotalCounselors = await _context.Counselors.CountAsync();
            TotalSessions = await _context.Sessions.CountAsync();

            TotalRevenue = 4500;

            ChartLabels = JsonSerializer.Serialize(
            new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" });

            ChartData = JsonSerializer.Serialize(
            new[] { 10, 25, 18, 40, 32, 55 });

        }

    }
}