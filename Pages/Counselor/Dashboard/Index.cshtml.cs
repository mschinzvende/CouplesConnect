using CouplesConnect.Data.Models;
using Microsoft.CodeAnalysis.Scripting;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace CouplesConnect.Pages.Counselor.Dashboard
{

    [Authorize(Roles = "Counselor")]
    public class IndexModel : PageModel
    {

        public int TodaySessions { get; set; }
        public int TotalSessions { get; set; }
        public double AverageRating { get; set; }
        public decimal MonthlyIncome { get; set; }

        public string ChartLabels { get; set; }
        public string ChartData { get; set; }

        public void OnGet()
        {

            TodaySessions = 4;
            TotalSessions = 128;
            AverageRating = 4.8;
            MonthlyIncome = 1200;

            ChartLabels = JsonSerializer.Serialize(
            new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" });

            ChartData = JsonSerializer.Serialize(
            new[] { 3, 4, 2, 6, 3, 5 });

        }

    }
}