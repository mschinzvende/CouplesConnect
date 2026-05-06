using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace CouplesConnect.Services
{
    public class MessagingService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public MessagingService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _config = config;
            _http = httpFactory.CreateClient();
        }

        // =========================
        // 📧 EMAIL
        // =========================
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"]);
                var username = _config["Email:Username"];
                var password = _config["Email:Password"];
                var from = _config["Email:From"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(from, "CouplesConnect"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // ✅ CRITICAL
                };

                mail.To.Add(to);

                await client.SendMailAsync(mail);
            }
            catch
            {
            }
        }

        // =========================
        // 💬 WHATSAPP (Twilio Example)
        // =========================
        public async Task SendWhatsAppAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _config["Twilio:AccountSid"];
                var authToken = _config["Twilio:AuthToken"];
                var fromNumber = _config["Twilio:WhatsAppFrom"];

                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("From", $"whatsapp:{fromNumber}"),
                    new KeyValuePair<string, string>("To", $"whatsapp:{phoneNumber}"),
                    new KeyValuePair<string, string>("Body", message)
                });

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

                var byteArray = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(byteArray));

                await _http.SendAsync(request);
            }
            catch
            {
                // Fail silently for now
            }
        }

        // =========================
        // 🔥 COMBINED HELPER
        // =========================
        public async Task SendNotificationAsync(string email, string phone, string subject, string message)
        {
            if (!string.IsNullOrEmpty(email))
                await SendEmailAsync(email, subject, message);

            if (!string.IsNullOrEmpty(phone))
                await SendWhatsAppAsync(phone, message);
        }
    }
}