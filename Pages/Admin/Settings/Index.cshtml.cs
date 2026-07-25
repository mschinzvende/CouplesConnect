using CouplesConnect.Data;
using CouplesConnect.Models;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace CouplesConnect.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryption;

        public IndexModel(
     ApplicationDbContext context,
     EncryptionService encryption)
        {
            _context = context;
            _encryption = encryption;
        }

        [BindProperty]
        public PlatformSetting Settings { get; set; } = new();

        public async Task OnGetAsync()
        {
            Settings = await _context.PlatformSettings.FirstOrDefaultAsync();

            if (Settings != null)
            {
                Settings.WhatsAppAccessToken =
                    _encryption.Decrypt(Settings.WhatsAppAccessToken);

                Settings.SmtpPassword =
                    _encryption.Decrypt(Settings.SmtpPassword);

                Settings.PaynowIntegrationKey =
                    _encryption.Decrypt(Settings.PaynowIntegrationKey);
            }

            if (Settings == null)
            {
                Settings = new PlatformSetting();

                _context.PlatformSettings.Add(Settings);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();

            if (settings == null)
                return NotFound();

            _context.Entry(settings).CurrentValues.SetValues(Settings);

            settings.WhatsAppAccessToken =
                _encryption.Encrypt(Settings.WhatsAppAccessToken);

            settings.SmtpPassword =
                _encryption.Encrypt(Settings.SmtpPassword);

            settings.PaynowIntegrationKey =
                _encryption.Encrypt(Settings.PaynowIntegrationKey);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Settings saved successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTestWhatsAppAsync()
        {
            var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        Settings.WhatsAppAccessToken);

                var response =
                    await client.GetAsync(
                        $"https://graph.facebook.com/v22.0/{Settings.WhatsAppPhoneNumberId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "WhatsApp connection successful.";
                }
                else
                {
                    TempData["Error"] =
                        await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}