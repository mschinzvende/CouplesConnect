namespace CouplesConnect.Services
{
    public class EmailTemplateService
    {
        public string BuildTemplate(string title, string message, string ctaText = null, string ctaUrl = null)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>

<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif;'>

    <table width='100%' cellpadding='0' cellspacing='0'>
        <tr>
            <td align='center'>

                <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;margin-top:30px;border-radius:10px;overflow:hidden;'>

                    <!-- HEADER -->
                    <tr>
                        <td style='background:#4f46e5;color:#ffffff;padding:20px;text-align:center;font-size:20px;font-weight:bold;'>
                            CouplesConnect
                        </td>
                    </tr>

                    <!-- BODY -->
                    <tr>
                        <td style='padding:30px;color:#111827;font-size:14px;line-height:1.6;'>

                            <h2 style='margin-top:0;color:#111827;'>{title}</h2>

                            <p>{message}</p>

                            {(string.IsNullOrEmpty(ctaText) ? "" : $@"
                                <div style='margin-top:25px;text-align:center;'>
                                    <a href='{ctaUrl}'
                                       style='background:#4f46e5;color:#ffffff;padding:12px 20px;
                                              text-decoration:none;border-radius:6px;font-weight:600;'>
                                        {ctaText}
                                    </a>
                                </div>
                            ")}

                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style='background:#f9fafb;padding:20px;text-align:center;font-size:12px;color:#6b7280;'>

                            <p style='margin:0;'>© {DateTime.UtcNow.Year} CouplesConnect</p>
                            <p style='margin:5px 0;'>Helping couples build stronger relationships</p>

                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>";
        }
    }
}