using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using MailKit.Net.Smtp;
using System.Text;
using System.Text.Json;
using CouplesConnect.Models;
using Humanizer;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace CouplesConnect.Services
{
    public class MessagingService
    {
        private readonly HttpClient _http;
        private readonly SettingsService _settings;
        private readonly EmailTemplateService _template;
        private readonly EncryptionService _encryption;

        public MessagingService(
            IHttpClientFactory httpFactory,
            SettingsService settings,
            EmailTemplateService template,
            EncryptionService encryption)
        {
            _http = httpFactory.CreateClient();
            _settings = settings;
            _template = template;
            _encryption = encryption;
        }

        

      /*  public async Task<bool> SendEmail1Async(
    string to,
    EmailMessage email)
        {
            try
            {
                var config = await _settings.GetAsync();

                var password = _encryption.Decrypt(config.SmtpPassword);

                using var client = new SmtpClient(
                    config.SmtpServer,
                    config.SmtpPort)
                {
                    Credentials = new NetworkCredential(
                        config.SmtpUsername,
                        password),

                    EnableSsl = config.EnableSSL,

                    UseDefaultCredentials = false,

                    // Prevent SMTP from appearing to hang forever
                    Timeout = 150
                };

                using var mail = new MailMessage
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
            catch (SmtpException ex)
            {
                Console.WriteLine("SMTP ERROR");
                Console.WriteLine($"Status: {ex.StatusCode}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");

                throw; // TEMPORARILY while debugging
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR");
                Console.WriteLine(ex.ToString());

                throw; // TEMPORARILY while debugging
            }
        }
      */

        public async Task<bool> SendEmailAsync(string to, EmailMessage email)
        {
            var cfg = await _settings.GetAsync();

            if (cfg == null ||
                string.IsNullOrWhiteSpace(cfg.SmtpServer) ||
                string.IsNullOrWhiteSpace(cfg.CompanyEmail) ||
                string.IsNullOrWhiteSpace(cfg.SmtpUsername) ||
                string.IsNullOrWhiteSpace(cfg.SmtpPassword))
            {
                throw new Exception("SMTP configuration is incomplete in SystemSettings.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.CompanyEmail ?? cfg.CompanyEmail, cfg.CompanyEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = email.Subject;

            message.Body = new TextPart("html")
            {
                Text = email.HtmlBody
            };

            try
            {
                using var client = new MailKit.Net.Smtp.SmtpClient();

                // Let MailKit figure out best SSL mode
                var secureOption = SecureSocketOptions.Auto;

                // If you KNOW you’re using port 465, you can force it:
                // secureOption = SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(cfg.SmtpServer, cfg.SmtpPort, secureOption);

                try
                {
                    await client.AuthenticateAsync(
                        cfg.SmtpUsername,
                        cfg.SmtpPassword
                    );
                }
                catch (MailKit.Security.AuthenticationException ex)
                {
                    Console.WriteLine("SMTP AUTHENTICATION FAILED");
                    Console.WriteLine(ex.ToString());
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SMTP ERROR");
                    Console.WriteLine(ex.ToString());
                    throw;
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Bubble the full detail up so you can see it in TempData or logs
                throw new Exception($"MailKit SMTP error: {ex.Message}", ex);

               
            }
        }



      

        


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

                var response = await _http.PostAsync(url, content);


                var responseContent = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        public async Task<bool> SendWhatsAppToCounselorAsync(
            string phoneNumber,
            string templateName, string counselorName, string clientName, string serviceName, string date, string time)
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
                        new { type = "text", text = counselorName},
                        new { type = "text", text = clientName },
                        new { type = "text", text = serviceName },
                        new { type = "text", text = time },
                        new { type = "text", text = date }

                    }
                }

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

        public async Task<bool> SendWhatsAppToClientAsync(
            string phoneNumber,
            string templateName, string clientName, string counselorName, string serviceName, string date, string time)
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
                        new { type = "text", text = clientName},
                        new { type = "text", text = counselorName },
                        new { type = "text", text = serviceName },
                        new { type = "text", text = time },
                        new { type = "text", text = date }

                    }
                }

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

        
       

        public async Task<bool> SendWhatsAppAccountConfirmationAsync(
            string phoneNumber,
            string customerName,
            string confirmationToken)
        {
            try
            {
                var config = await _settings.GetAsync();
                var url = $"{config.WhatsAppApiUrl}/{config.WhatsAppPhoneNumberId}/messages";

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", config.WhatsAppAccessToken);

                // Meta template: account_confirmation
                // Body: Hello {{1}}, confirm your Couples Connect account.
                // URL button: https://YOUR-DOMAIN/account/confirm-email?token={{1}}
                var messageData = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new
                    {
                        name = "account_confirmation",
                        language = new { code = "en" },
                        components = new object[]
                        {
                            new
                            {
                                type = "body",
                                parameters = new object[]
                                {
                                    new { type = "text", text = customerName }
                                }
                            },
                            new
                            {
                                type = "button",
                                sub_type = "URL",
                                index = "0",
                                parameters = new object[]
                                {
                                    new { type = "text", text = confirmationToken }
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(messageData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendWhatsAppPasswordResetAsync(
            string phoneNumber,
            string customerName,
            string resetToken)
        {
            try
            {
                var config = await _settings.GetAsync();
                var url = $"{config.WhatsAppApiUrl}/{config.WhatsAppPhoneNumberId}/messages";

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", config.WhatsAppAccessToken);

                // Meta template: password_reset
                // Body: Hello {{1}}, use the button below to reset your password.
                // URL button: https://YOUR-DOMAIN/account/reset-password?token={{1}}
                var messageData = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new
                    {
                        name = "password_reset",
                        language = new { code = "en" },
                        components = new object[]
                        {
                            new
                            {
                                type = "body",
                                parameters = new object[]
                                {
                                    new { type = "text", text = customerName }
                                }
                            },
                            new
                            {
                                type = "button",
                                sub_type = "URL",
                                index = "0",
                                parameters = new object[]
                                {
                                    new { type = "text", text = resetToken }
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(messageData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

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

                success = await SendEmailAsync(
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

        
    }
}
