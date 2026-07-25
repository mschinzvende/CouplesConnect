using Microsoft.AspNetCore.DataProtection;

namespace CouplesConnect.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("PlatformSettings");
        }

        public string Encrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return _protector.Protect(value);
        }

        public string Decrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            try
            {
                return _protector.Unprotect(value);
            }
            catch
            {
                return value;
            }
        }
    }
}