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
                // Secrets are never rendered back into the form. Leaving a secret field blank keeps its stored value.
                Settings.WhatsAppAccessToken = null;
                Settings.SmtpPassword = null;
                Settings.SmileAndPayApiKey = null;
                Settings.SmileAndPayApiSecret = null;
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

            var storedWhatsAppAccessToken = settings.WhatsAppAccessToken;
            var storedSmtpPassword = settings.SmtpPassword;
            var storedSmileAndPayApiKey = settings.SmileAndPayApiKey;
            var storedSmileAndPayApiSecret = settings.SmileAndPayApiSecret;

            _context.Entry(settings).CurrentValues.SetValues(Settings);

            settings.WhatsAppAccessToken = PreserveOrEncrypt(Settings.WhatsAppAccessToken, storedWhatsAppAccessToken);
            settings.SmtpPassword = PreserveOrEncrypt(Settings.SmtpPassword, storedSmtpPassword);
            settings.SmileAndPayApiKey = PreserveOrEncrypt(Settings.SmileAndPayApiKey, storedSmileAndPayApiKey);
            settings.SmileAndPayApiSecret = PreserveOrEncrypt(Settings.SmileAndPayApiSecret, storedSmileAndPayApiSecret);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Settings saved successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTestWhatsAppAsync()
        {
            var client = new HttpClient();

            try
            {
                var savedSettings = await _context.PlatformSettings.AsNoTracking().FirstOrDefaultAsync();
                var accessToken = string.IsNullOrWhiteSpace(Settings.WhatsAppAccessToken)
                    ? _encryption.Decrypt(savedSettings?.WhatsAppAccessToken ?? string.Empty)
                    : Settings.WhatsAppAccessToken;

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);

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

        private string? PreserveOrEncrypt(string? submittedValue, string? storedEncryptedValue)
        {
            return string.IsNullOrWhiteSpace(submittedValue)
                ? storedEncryptedValue
                : _encryption.Encrypt(submittedValue);
        }
    }
}
