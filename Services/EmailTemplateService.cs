using CouplesConnect.Models;

namespace CouplesConnect.Services
{
    public class EmailTemplateService
    {
        private readonly SettingsService _settings;

        public EmailTemplateService(SettingsService settings)
        {
            _settings = settings;
        }

        public async Task<string> BuildTemplateAsync(
            string title,
            string message,
            string? ctaText = null,
            string? ctaUrl = null)
        {
            var settings = await _settings.GetAsync();

            var companyName = settings.CompanyName ?? "Couples Connect";
            var companyEmail = settings.CompanyEmail ?? "";
            var companyPhone = settings.CompanyPhone ?? "";

            return $@"
<!DOCTYPE html>
<html>

<head>

<meta charset='UTF-8'>

<meta name='viewport'
      content='width=device-width, initial-scale=1.0'>

</head>

<body style='margin:0;
             padding:0;
             background:#f4f6f8;
             font-family:Segoe UI,Arial,sans-serif;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'>

<tr>

<td align='center'>

<table width='650'
       cellpadding='0'
       cellspacing='0'
       style='background:#ffffff;
              margin:30px auto;
              border-radius:10px;
              overflow:hidden;
              box-shadow:0 4px 20px rgba(0,0,0,.08);'>

<!-- HEADER -->

<tr>

<td style='background:#4f46e5;
           color:#ffffff;
           text-align:center;
           padding:30px;'>

<h1 style='margin:0;font-size:28px;'>

{companyName}

</h1>

</td>

</tr>

<!-- BODY -->

<tr>

<td style='padding:40px;'>

<h2 style='margin-top:0;
           color:#111827;'>

{title}

</h2>

<p style='font-size:15px;
          color:#374151;
          line-height:1.8;'>

{message}

</p>

{(string.IsNullOrWhiteSpace(ctaText)
? ""
: $@"

<div style='text-align:center;
            margin-top:35px;'>

<a href='{ctaUrl}'

style='display:inline-block;
background:#4f46e5;
color:white;
padding:14px 28px;
border-radius:6px;
text-decoration:none;
font-weight:bold;'>

{ctaText}

</a>

</div>

")}

</td>

</tr>

<!-- FOOTER -->

<tr>

<td style='background:#f9fafb;
padding:25px;
text-align:center;
font-size:13px;
color:#6b7280;'>

<div>

<strong>{companyName}</strong>

</div>

{(!string.IsNullOrWhiteSpace(companyEmail)
? $"<div>{companyEmail}</div>"
: "")}

{(!string.IsNullOrWhiteSpace(companyPhone)
? $"<div>{companyPhone}</div>"
: "")}

<div style='margin-top:15px;'>

© {DateTime.Now.Year} {companyName}

</div>

</td>

</tr>

</table>

</td>

</tr>

</table>

</body>

</html>";
        }







        public async Task<EmailMessage> BuildBookingConfirmationAsync(
    string customerName,
    string counselorName,
    DateTime sessionDate,
    string bookingUrl)
        {
            var body = $@"
<p>Dear <strong>{customerName}</strong>,</p>

<p>
Your counselling session has been successfully booked.
</p>

<table style='width:100%;border-collapse:collapse;'>

<tr>
    <td style='padding:8px;'><strong>Counsellor</strong></td>
    <td style='padding:8px;'>{counselorName}</td>
</tr>

<tr>
    <td style='padding:8px;'><strong>Date</strong></td>
    <td style='padding:8px;'>{sessionDate:dddd, dd MMMM yyyy}</td>
</tr>

<tr>
    <td style='padding:8px;'><strong>Time</strong></td>
    <td style='padding:8px;'>{sessionDate:h:mm tt}</td>
</tr>

</table>

<p>
Please arrive a few minutes before your scheduled session.
</p>";

            return new EmailMessage
            {
                Subject = "Booking Confirmed",
                HtmlBody = await BuildTemplateAsync(
                    "Booking Confirmed",
                    body,
                    "View My Bookings",
                    bookingUrl)
            };
        }



        public async Task<EmailMessage> BuildBookingReminderAsync(
    string customerName,
    string counselorName,
    DateTime sessionDate,
    string joinUrl)
        {
            var body = $@"
<p>Dear <strong>{customerName}</strong>,</p>

<p>
This is a friendly reminder that your counselling session is coming up soon.
</p>

<table style='width:100%;border-collapse:collapse;'>

<tr>
    <td style='padding:8px;'><strong>Counsellor</strong></td>
    <td style='padding:8px;'>{counselorName}</td>
</tr>

<tr>
    <td style='padding:8px;'><strong>Date</strong></td>
    <td style='padding:8px;'>{sessionDate:dddd, dd MMMM yyyy}</td>
</tr>

<tr>
    <td style='padding:8px;'><strong>Time</strong></td>
    <td style='padding:8px;'>{sessionDate:h:mm tt}</td>
</tr>

</table>

<p>
Please click the button below when it is time to join your session.
</p>";

            return new EmailMessage
            {
                Subject = "Session Reminder",
                HtmlBody = await BuildTemplateAsync(
                    "Session Reminder",
                    body,
                    "Join Session",
                    joinUrl)
            };
        }

        public async Task<EmailMessage> BuildInvitationAsync(
    string invitedBy,
    string acceptUrl)
        {
            var body = $@"
<p>Hello,</p>

<p>

<strong>{invitedBy}</strong>

has invited you to participate in a counselling session on Couples Connect.

</p>

<p>

Accept the invitation to join the session.

</p>";

            return new EmailMessage
            {
                Subject = "Counselling Invitation",
                HtmlBody = await BuildTemplateAsync(
                    "You're Invited",
                    body,
                    "Accept Invitation",
                    acceptUrl)
            };
        }

        public async Task<EmailMessage> BuildInvitationAcceptedAsync(
    string participantName)
        {
            var body = $@"
<p>Hello,</p>

<p>

<strong>{participantName}</strong>

has accepted your counselling invitation.

</p>

<p>

You can now proceed with the scheduled counselling session.

</p>";

            return new EmailMessage
            {
                Subject = "Invitation Accepted",
                HtmlBody = await BuildTemplateAsync(
                    "Invitation Accepted",
                    body)
            };
        }

        public async Task<EmailMessage> BuildSessionStartedAsync(
    string customerName,
    string joinUrl)
        {
            var body = $@"
<p>Dear {customerName},</p>

<p>

Your counselling session has started.

</p>

<p>

Click below to join immediately.

</p>";

            return new EmailMessage
            {
                Subject = "Session Started",
                HtmlBody = await BuildTemplateAsync(
                    "Session Started",
                    body,
                    "Join Session",
                    joinUrl)
            };
        }

        public async Task<EmailMessage> BuildSessionCompletedAsync(
    string customerName)
        {
            var body = $@"
<p>Dear {customerName},</p>

<p>

Thank you for attending your counselling session.

</p>

<p>

We appreciate your trust in Couples Connect.

</p>";

            return new EmailMessage
            {
                Subject = "Thank You",
                HtmlBody = await BuildTemplateAsync(
                    "Session Completed",
                    body)
            };
        }

        public async Task<EmailMessage> BuildWelcomeAsync(
    string customerName)
        {
            var body = $@"
<p>

Welcome <strong>{customerName}</strong>!

</p>

<p>

Thank you for joining Couples Connect.

</p>

<p>

You can now browse counsellors and schedule sessions.

</p>";

            return new EmailMessage
            {
                Subject = "Welcome to Couples Connect",
                HtmlBody = await BuildTemplateAsync(
                    "Welcome",
                    body,
                    "Book Your First Session",
                    "/User/Bookings/Index")
            };
        }

        public async Task<EmailMessage> BuildAccountConfirmationAsync(
            string customerName,
            string confirmationUrl)
        {
            var body = $@"
<p>Welcome <strong>{customerName}</strong>,</p>

<p>
Thank you for creating your Couples Connect account. Please confirm your email address to activate your account and keep it secure.
</p>

<p>
If you did not create this account, you can safely ignore this email.
</p>";

            return new EmailMessage
            {
                Subject = "Confirm your Couples Connect account",
                HtmlBody = await BuildTemplateAsync(
                    "Confirm your email address",
                    body,
                    "Confirm email",
                    confirmationUrl)
            };
        }

        public async Task<EmailMessage> BuildPasswordResetAsync(
            string customerName,
            string resetUrl)
        {
            var body = $@"
<p>Hello <strong>{customerName}</strong>,</p>

<p>
We received a request to reset the password for your Couples Connect account. Use the button below to choose a new password.
</p>

<p>
If you did not request a password reset, you can safely ignore this email. Your current password will remain unchanged.
</p>";

            return new EmailMessage
            {
                Subject = "Reset your Couples Connect password",
                HtmlBody = await BuildTemplateAsync(
                    "Reset your password",
                    body,
                    "Reset password",
                    resetUrl)
            };
        }

        public async Task<EmailMessage> BuildPasswordChangedAlertAsync(string customerName)
        {
            var body = $@"
<p>Hello <strong>{customerName}</strong>,</p>

<p>Your Couples Connect password was changed successfully.</p>

<p>If you did not make this change, please reset your password immediately and contact support.</p>";

            return new EmailMessage
            {
                Subject = "Your Couples Connect password was changed",
                HtmlBody = await BuildTemplateAsync("Password changed", body)
            };
        }

        public async Task<EmailMessage> BuildGeneralNotificationAsync(
    string subject,
    string message,
    string? buttonText = null,
    string? buttonUrl = null)
        {
            return new EmailMessage
            {
                Subject = subject,
                HtmlBody = await BuildTemplateAsync(
                    subject,
                    message,
                    buttonText,
                    buttonUrl)
            };
        }


    }
}
