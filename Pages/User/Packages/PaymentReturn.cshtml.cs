using CouplesConnect.Data;
using CouplesConnect.Models;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.User.Packages;

[Authorize(Roles = "User")]
public class PaymentReturnModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SubscriptionPaymentService _payments;

    public PaymentReturnModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        SubscriptionPaymentService payments)
    {
        _context = context;
        _userManager = userManager;
        _payments = payments;
    }

    public async Task<IActionResult> OnGetAsync(string? orderReference, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (string.IsNullOrWhiteSpace(orderReference) ||
            !await _context.SubscriptionPayments.AnyAsync(x =>
                x.OrderReference == orderReference && x.UserId == user.Id, cancellationToken))
        {
            TempData["Error"] = "The payment reference is invalid.";
            return RedirectToPage("/User/Packages/Index");
        }

        var result = await _payments.ReconcileAsync(orderReference, cancellationToken);
        if (result.Outcome == PaymentReconciliationOutcome.Paid)
            TempData["Success"] = result.Message;
        else if (result.Outcome == PaymentReconciliationOutcome.Pending)
            TempData["Info"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToPage("/User/Packages/Index");
    }
}
