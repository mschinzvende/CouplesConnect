using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using CouplesConnect.Models;
using Humanizer;
using Newtonsoft.Json;

namespace CouplesConnect.Services
{
    public class MessagingService
    {
        private readonly HttpClient _http;
        private readonly SettingsService _settings;
        private readonly EmailTemplateService _template;

        public MessagingService(
            IHttpClientFactory httpFactory,
            SettingsService settings,
            EmailTemplateService template)
        {
            _http = httpFactory.CreateClient();
            _settings = settings;
            _template = template;
        }

        #region Email

        public async Task<bool> SendEmailAsync(
     string to,
     EmailMessage email)
        {
            try
            {
                var config = await _settings.GetAsync();

                using var client = new SmtpClient(
                    config.SmtpServer,
                    config.SmtpPort)
                {
                    Credentials = new NetworkCredential(
                        config.SmtpUsername,
                        config.SmtpPassword),

                    EnableSsl = config.EnableSSL
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(
                        config.CompanyEmail,
                        config.CompanyName),

                    Subject = email.Subject,
                    Body = email.HtmlBody,
                    IsBodyHtml = true
                };

                mail.To.Add(to);

                await client.SendMailAsync(mail);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region WhatsApp Cloud API


        public async Task<bool> SendWhatsAppAsync(
            string phoneNumber,
            string templateName, string message)
        {
            try
            {
                var config = await _settings.GetAsync();

                var url =
                    $"{config.WhatsAppApiUrl}/{config.WhatsAppPhoneNumberId}/messages";

                _http.DefaultRequestHeaders.Clear();

                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        config.WhatsAppAccessToken);

                var messageData = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = "en" },
                        components = new object[]
                     {
                // BODY
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = message }
                        
                    }
                }


                     }
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(messageData),
                Encoding.UTF8,
                "application/json");

                var response = await _http.PostAsync("", content);


                var responseContent = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendWhatsAppWithButtonAsync(
            string phoneNumber,
            string inviterName,string inviteeName, string inviterToken, string templateName, string sessionTime)
        {
            try
            {
                var config = await _settings.GetAsync();

                var url =
                    $"{config.WhatsAppApiUrl}/{config.WhatsAppPhoneNumberId}/messages";

                _http.DefaultRequestHeaders.Clear();

                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        config.WhatsAppAccessToken);

                var messageData = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = "en" },
                        components = new object[]
                     {
                // BODY
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = inviteeName },
                        new { type = "text", text = inviterName },
                        new { type = "text", text = sessionTime },

                    }
                },

                

                //BUTTON 1 → Accept
                new
                {
                    type = "button",
                    sub_type = "URL",
                    index = "0",
                    parameters = new object[]
                    {
                        new { type = "text", text = inviterToken }
                    }
                },

                
                     }
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(messageData),
                Encoding.UTF8,
                "application/json");

                var response = await _http.PostAsync(url, content);


                var responseContent = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Combined

        public async Task<bool> SendNotificationAsync(
            string? emailAddress,
            string? phone,
            string subject,
            string message)
        {
            bool success = true;

            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                var email = new EmailMessage
                {
                    Subject = subject,
                    HtmlBody = await _template.BuildTemplateAsync(
                        subject,
                        message)
                };

                success &= await SendEmailAsync(
                    emailAddress,
                    email);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                success &= await SendWhatsAppAsync(
                    phone,"general_notification",
                    message);
            }

            return success;
        }

        #endregion
    }
}