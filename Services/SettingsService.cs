using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Services
{
    public class SettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryption;

        private PlatformSetting _settings;

        public SettingsService(
            ApplicationDbContext context,
            EncryptionService encryption)
        {
            _context = context;
            _encryption = encryption;
        }

        public async Task<PlatformSetting> GetAsync()
        {
            if (_settings != null)
                return _settings;

            _settings = await _context.PlatformSettings
    .AsNoTracking()
    .FirstOrDefaultAsync();

            if (_settings == null)
                throw new Exception("Platform settings have not been configured.");

            _settings.WhatsAppAccessToken =
                _encryption.Decrypt(_settings.WhatsAppAccessToken);

            _settings.SmtpPassword =
                _encryption.Decrypt(_settings.SmtpPassword);

            _settings.PaynowIntegrationKey =
                _encryption.Decrypt(_settings.PaynowIntegrationKey);

            return _settings;
        }
    }
}