using System.Globalization;
using System.Text;
using System.Text.Json;
using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Reports;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

    public DateTime PeriodFrom { get; private set; }
    public DateTime PeriodTo { get; private set; }

    public int NewUsers { get; private set; }
    public int ApprovedCounselors { get; private set; }
    public int TotalBookings { get; private set; }
    public int CompletedBookings { get; private set; }
    public int ScheduledBookings { get; private set; }
    public int CancelledBookings { get; private set; }
    public int GroupBookings { get; private set; }
    public int ActiveSubscriptions { get; private set; }
    public int ActiveTrials { get; private set; }
    public int NewPaidSubscriptions { get; private set; }
    public int SessionsAllocated { get; private set; }
    public int SessionsRemaining { get; private set; }
    public int FeedbackResponses { get; private set; }
    public decimal AverageRating { get; private set; }
    public decimal PaidRevenueUsd { get; private set; }
    public decimal PaidRevenueZwg { get; private set; }
    public decimal PendingRevenueUsd { get; private set; }
    public decimal PendingRevenueZwg { get; private set; }
    public decimal CounselorEarnings { get; private set; }
    public int PaidPayments { get; private set; }
    public int FailedPayments { get; private set; }
    public int PendingPayments { get; private set; }

    public decimal CompletionRate => TotalBookings == 0 ? 0 : CompletedBookings * 100m / TotalBookings;
    public decimal CancellationRate => TotalBookings == 0 ? 0 : CancelledBookings * 100m / TotalBookings;
    public decimal PaymentSuccessRate => PaidPayments + FailedPayments + PendingPayments == 0
        ? 0
        : PaidPayments * 100m / (PaidPayments + FailedPayments + PendingPayments);
    public decimal SessionUtilizationRate => SessionsAllocated == 0
        ? 0
        : (SessionsAllocated - SessionsRemaining) * 100m / SessionsAllocated;
    public List<PackageReportRow> PackageReport { get; private set; } = new();
    public List<ServiceReportRow> ServiceReport { get; private set; } = new();
    public List<CounselorReportRow> CounselorReport { get; private set; } = new();
    public List<PaymentReportRow> RecentPayments { get; private set; } = new();

    public string TimelineLabelsJson { get; private set; } = "[]";
    public string UsdRevenueTimelineJson { get; private set; } = "[]";
    public string ZwgRevenueTimelineJson { get; private set; } = "[]";
    public string BookingTimelineJson { get; private set; } = "[]";
    public string BookingStatusLabelsJson { get; private set; } = "[]";
    public string BookingStatusValuesJson { get; private set; } = "[]";
    public string PaymentStatusLabelsJson { get; private set; } = "[]";
    public string PaymentStatusValuesJson { get; private set; } = "[]";

    public async Task OnGetAsync()
    {
        SetPeriod();
        var toExclusive = PeriodTo.AddDays(1);
        var now = DateTime.UtcNow;

        NewUsers = await _context.Users.AsNoTracking()
            .CountAsync(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive);
        ApprovedCounselors = await _context.CounselorProfiles.AsNoTracking().CountAsync(x => x.IsApproved);

        var bookingQuery = _context.Bookings.AsNoTracking()
            .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive);
        TotalBookings = await bookingQuery.CountAsync();
        CompletedBookings = await bookingQuery.CountAsync(x => x.BookingStatus == "Completed");
        ScheduledBookings = await bookingQuery.CountAsync(x => x.BookingStatus == "Scheduled");
        CancelledBookings = await bookingQuery.CountAsync(x => x.BookingStatus == "Cancelled");
        GroupBookings = await bookingQuery.CountAsync(x => x.IsGroup);

        ActiveSubscriptions = await _context.UserSubscriptions.AsNoTracking()
            .CountAsync(x => x.IsActive && !x.IsTrial && (!x.EndDate.HasValue || x.EndDate > now));
        ActiveTrials = await _context.UserSubscriptions.AsNoTracking()
            .CountAsync(x => x.IsActive && x.IsTrial && (!x.EndDate.HasValue || x.EndDate > now));

        var paidSubscriptionQuery = _context.UserSubscriptions.AsNoTracking()
            .Where(x => !x.IsTrial && x.PackageId != null &&
                        x.StartDate >= PeriodFrom && x.StartDate < toExclusive);
        NewPaidSubscriptions = await paidSubscriptionQuery.CountAsync();
        SessionsRemaining = await paidSubscriptionQuery.SumAsync(x => (int?)x.SessionsRemaining) ?? 0;
        SessionsAllocated = await paidSubscriptionQuery
            .Join(_context.Packages, x => x.PackageId, x => x.PackageId, (_, package) => package.SessionsPerMonth)
            .SumAsync(x => (int?)x) ?? 0;

        var paymentQuery = _context.SubscriptionPayments.AsNoTracking()
            .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive);
        PaidPayments = await paymentQuery.CountAsync(x => x.Status == SubscriptionPaymentStatuses.Paid);
        PendingPayments = await paymentQuery.CountAsync(x => x.Status == SubscriptionPaymentStatuses.Pending);
        FailedPayments = await paymentQuery.CountAsync(x =>
            x.Status == SubscriptionPaymentStatuses.Failed ||
            x.Status == SubscriptionPaymentStatuses.Cancelled ||
            x.Status == SubscriptionPaymentStatuses.PaidRequiresReview);
        PaidRevenueUsd = await paymentQuery
            .Where(x => x.Status == SubscriptionPaymentStatuses.Paid && x.CurrencyCode == "840")
            .SumAsync(x => (decimal?)x.Amount) ?? 0;
        PaidRevenueZwg = await paymentQuery
            .Where(x => x.Status == SubscriptionPaymentStatuses.Paid && x.CurrencyCode == "924")
            .SumAsync(x => (decimal?)x.Amount) ?? 0;
        PendingRevenueUsd = await paymentQuery
            .Where(x => x.Status == SubscriptionPaymentStatuses.Pending && x.CurrencyCode == "840")
            .SumAsync(x => (decimal?)x.Amount) ?? 0;
        PendingRevenueZwg = await paymentQuery
            .Where(x => x.Status == SubscriptionPaymentStatuses.Pending && x.CurrencyCode == "924")
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        CounselorEarnings = await _context.CounselorEarnings.AsNoTracking()
            .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var feedbackQuery = _context.SessionFeedbacks.AsNoTracking()
            .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive);
        FeedbackResponses = await feedbackQuery.CountAsync();
        AverageRating = await feedbackQuery.AverageAsync(x => (decimal?)x.Rating) ?? 0;

        await LoadPackageReportAsync(toExclusive);
        await LoadServiceReportAsync(bookingQuery);
        await LoadCounselorReportAsync(bookingQuery, toExclusive);
        await LoadRecentPaymentsAsync(paymentQuery);
        await LoadChartDataAsync(bookingQuery, paymentQuery);
    }

    public async Task<IActionResult> OnGetExportAsync(string report)
    {
        SetPeriod();
        var toExclusive = PeriodTo.AddDays(1);
        var csv = new StringBuilder();

        switch (report?.Trim().ToLowerInvariant())
        {
            case "payments":
                csv.AppendLine("Created,Paid,Client,Package,Order Reference,Gateway Reference,Currency,Amount,Status,Message");
                var payments = await _context.SubscriptionPayments.AsNoTracking()
                    .Include(x => x.User).Include(x => x.Package)
                    .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive)
                    .OrderByDescending(x => x.CreatedAt).ToListAsync();
                foreach (var item in payments)
                    AppendCsv(csv, item.CreatedAt, item.PaidAt, item.User.FullName, item.Package.Name,
                        item.OrderReference, item.GatewayTransactionReference, CurrencyName(item.CurrencyCode),
                        item.Amount, item.Status, item.StatusMessage);
                break;

            case "subscriptions":
                csv.AppendLine("Started,Ends,Client,Package,Trial,Active,Amount Paid,Sessions Remaining");
                var subscriptions = await (from sub in _context.UserSubscriptions.AsNoTracking()
                                           join user in _context.Users.AsNoTracking() on sub.UserId equals user.Id
                                           join package in _context.Packages.AsNoTracking()
                                               on sub.PackageId equals (int?)package.PackageId into packageGroup
                                           from package in packageGroup.DefaultIfEmpty()
                                           where sub.StartDate >= PeriodFrom && sub.StartDate < toExclusive
                                           orderby sub.StartDate descending
                                           select new
                                           {
                                               Subscription = sub,
                                               user.FullName,
                                               PackageName = package == null ? "Trial" : package.Name
                                           }).ToListAsync();
                foreach (var item in subscriptions)
                    AppendCsv(csv, item.Subscription.StartDate, item.Subscription.EndDate, item.FullName,
                        item.PackageName, item.Subscription.IsTrial, item.Subscription.IsActive,
                        item.Subscription.AmountPaid, item.Subscription.SessionsRemaining);
                break;

            case "bookings":
                csv.AppendLine("Created,Scheduled,Client,Counselor,Service,Status,Group,Subscription ID");
                var bookings = await _context.Bookings.AsNoTracking()
                    .Include(x => x.User).Include(x => x.Counselor).Include(x => x.Service)
                    .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive)
                    .OrderByDescending(x => x.CreatedAt).ToListAsync();
                foreach (var item in bookings)
                    AppendCsv(csv, item.CreatedAt, item.ScheduledStart, item.User.FullName,
                        item.Counselor.FullName, item.Service.Name, item.BookingStatus, item.IsGroup,
                        item.SubscriptionId);
                break;

            case "counselors":
                var bookingQuery = _context.Bookings.AsNoTracking()
                    .Where(x => x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive);
                await LoadCounselorReportAsync(bookingQuery, toExclusive);
                csv.AppendLine("Counselor,Bookings,Completed,Cancelled,Completion Rate,Earnings,Average Rating,Feedback");
                foreach (var item in CounselorReport)
                    AppendCsv(csv, item.Name, item.Bookings, item.Completed, item.Cancelled,
                        item.CompletionRate, item.Earnings, item.AverageRating, item.FeedbackCount);
                break;

            default:
                return BadRequest("Unknown report export.");
        }

        var fileName = $"{report}-{PeriodFrom:yyyyMMdd}-{PeriodTo:yyyyMMdd}.csv";
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private async Task LoadPackageReportAsync(DateTime toExclusive)
    {
        var packages = await _context.Packages.AsNoTracking()
            .ToDictionaryAsync(x => x.PackageId);
        var subscriptions = await _context.UserSubscriptions.AsNoTracking()
            .Where(x => !x.IsTrial && x.PackageId != null &&
                        x.StartDate >= PeriodFrom && x.StartDate < toExclusive)
            .GroupBy(x => x.PackageId!.Value)
            .Select(group => new
            {
                PackageId = group.Key,
                Count = group.Count(),
                SessionsRemaining = group.Sum(x => x.SessionsRemaining)
            }).ToDictionaryAsync(x => x.PackageId);
        var revenue = await _context.SubscriptionPayments.AsNoTracking()
            .Where(x => x.Status == SubscriptionPaymentStatuses.Paid &&
                        x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive)
            .GroupBy(x => new { x.PackageId, x.CurrencyCode })
            .Select(group => new { group.Key.PackageId, group.Key.CurrencyCode, Amount = group.Sum(x => x.Amount) })
            .ToListAsync();

        var packageIds = subscriptions.Keys.Union(revenue.Select(x => x.PackageId)).Distinct();
        PackageReport = packageIds.Where(packages.ContainsKey).Select(packageId =>
        {
            var package = packages[packageId];
            subscriptions.TryGetValue(packageId, out var subscription);
            return new PackageReportRow
            {
                PackageId = packageId,
                Name = package.Name,
                Subscriptions = subscription?.Count ?? 0,
                RevenueUsd = revenue.Where(x => x.PackageId == packageId && x.CurrencyCode == "840").Sum(x => x.Amount),
                RevenueZwg = revenue.Where(x => x.PackageId == packageId && x.CurrencyCode == "924").Sum(x => x.Amount),
                SessionsAllocated = (subscription?.Count ?? 0) * package.SessionsPerMonth,
                SessionsRemaining = subscription?.SessionsRemaining ?? 0
            };
        }).OrderByDescending(x => x.RevenueUsd).ThenByDescending(x => x.RevenueZwg).ToList();
    }

    private async Task LoadServiceReportAsync(IQueryable<Booking> bookingQuery)
    {
        ServiceReport = await bookingQuery.GroupBy(x => new { x.ServiceId, x.Service.Name })
            .Select(group => new ServiceReportRow
            {
                Name = group.Key.Name,
                Bookings = group.Count(),
                Completed = group.Count(x => x.BookingStatus == "Completed"),
                Cancelled = group.Count(x => x.BookingStatus == "Cancelled"),
                GroupBookings = group.Count(x => x.IsGroup)
            })
            .OrderByDescending(x => x.Bookings).ToListAsync();
    }

    private async Task LoadCounselorReportAsync(IQueryable<Booking> bookingQuery, DateTime toExclusive)
    {
        var bookingRows = await bookingQuery.GroupBy(x => x.CounselorId)
            .Select(group => new
            {
                CounselorId = group.Key,
                Bookings = group.Count(),
                Completed = group.Count(x => x.BookingStatus == "Completed"),
                Cancelled = group.Count(x => x.BookingStatus == "Cancelled")
            }).ToListAsync();

        var counselorIds = bookingRows.Select(x => x.CounselorId).ToList();
        var names = await _context.Users.AsNoTracking().Where(x => counselorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);
        var earnings = await _context.CounselorEarnings.AsNoTracking()
            .Where(x => counselorIds.Contains(x.CounselorId) &&
                        x.CreatedAt >= PeriodFrom && x.CreatedAt < toExclusive)
            .GroupBy(x => x.CounselorId)
            .Select(group => new { Id = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount);
        var ratings = await (from feedback in _context.SessionFeedbacks.AsNoTracking()
                             join booking in _context.Bookings.AsNoTracking()
                                 on feedback.SessionId equals booking.SessionId
                             where counselorIds.Contains(booking.CounselorId) &&
                                   feedback.CreatedAt >= PeriodFrom && feedback.CreatedAt < toExclusive
                             group feedback by booking.CounselorId into grouped
                             select new
                             {
                                 Id = grouped.Key,
                                 Average = grouped.Average(x => (decimal)x.Rating),
                                 Count = grouped.Count()
                             }).ToDictionaryAsync(x => x.Id);

        CounselorReport = bookingRows.Select(row => new CounselorReportRow
        {
            CounselorId = row.CounselorId,
            Name = names.GetValueOrDefault(row.CounselorId, "Unknown"),
            Bookings = row.Bookings,
            Completed = row.Completed,
            Cancelled = row.Cancelled,
            Earnings = earnings.GetValueOrDefault(row.CounselorId),
            AverageRating = ratings.TryGetValue(row.CounselorId, out var rating) ? rating.Average : 0,
            FeedbackCount = ratings.TryGetValue(row.CounselorId, out var feedback) ? feedback.Count : 0
        }).OrderByDescending(x => x.Completed).ThenByDescending(x => x.Bookings).ToList();
    }

    private async Task LoadRecentPaymentsAsync(IQueryable<SubscriptionPayment> paymentQuery)
    {
        RecentPayments = await paymentQuery.Include(x => x.User).Include(x => x.Package)
            .OrderByDescending(x => x.CreatedAt).Take(12)
            .Select(x => new PaymentReportRow
            {
                CreatedAt = x.CreatedAt,
                Client = x.User.FullName,
                Package = x.Package.Name,
                OrderReference = x.OrderReference,
                Amount = x.Amount,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                Message = x.StatusMessage
            }).ToListAsync();
    }

    private async Task LoadChartDataAsync(IQueryable<Booking> bookingQuery,
        IQueryable<SubscriptionPayment> paymentQuery)
    {
        var days = Enumerable.Range(0, (PeriodTo - PeriodFrom).Days + 1)
            .Select(offset => PeriodFrom.AddDays(offset)).ToList();
        var bookings = await bookingQuery.GroupBy(x => x.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() }).ToDictionaryAsync(x => x.Date, x => x.Count);
        var revenue = await paymentQuery.Where(x => x.Status == SubscriptionPaymentStatuses.Paid)
            .GroupBy(x => new { Date = x.CreatedAt.Date, x.CurrencyCode })
            .Select(group => new { group.Key.Date, group.Key.CurrencyCode, Amount = group.Sum(x => x.Amount) })
            .ToListAsync();
        var usdRevenue = revenue.Where(x => x.CurrencyCode == "840").ToDictionary(x => x.Date, x => x.Amount);
        var zwgRevenue = revenue.Where(x => x.CurrencyCode == "924").ToDictionary(x => x.Date, x => x.Amount);

        TimelineLabelsJson = JsonSerializer.Serialize(days.Select(x => x.ToString("dd MMM")));
        BookingTimelineJson = JsonSerializer.Serialize(days.Select(x => bookings.GetValueOrDefault(x)));
        UsdRevenueTimelineJson = JsonSerializer.Serialize(days.Select(x => usdRevenue.GetValueOrDefault(x)));
        ZwgRevenueTimelineJson = JsonSerializer.Serialize(days.Select(x => zwgRevenue.GetValueOrDefault(x)));

        var bookingStatuses = await bookingQuery.GroupBy(x => x.BookingStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).ToListAsync();
        BookingStatusLabelsJson = JsonSerializer.Serialize(bookingStatuses.Select(x => x.Status));
        BookingStatusValuesJson = JsonSerializer.Serialize(bookingStatuses.Select(x => x.Count));

        var paymentStatuses = await paymentQuery.GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).ToListAsync();
        PaymentStatusLabelsJson = JsonSerializer.Serialize(paymentStatuses.Select(x => x.Status));
        PaymentStatusValuesJson = JsonSerializer.Serialize(paymentStatuses.Select(x => x.Count));
    }

    private void SetPeriod()
    {
        var today = DateTime.UtcNow.Date;
        PeriodFrom = (From ?? today.AddDays(-29)).Date;
        PeriodTo = (To ?? today).Date;
        if (PeriodFrom > PeriodTo) (PeriodFrom, PeriodTo) = (PeriodTo, PeriodFrom);
        if ((PeriodTo - PeriodFrom).TotalDays > 366) PeriodFrom = PeriodTo.AddDays(-366);
        From = PeriodFrom;
        To = PeriodTo;
    }

    private static string CurrencyName(string code) => code == "924" ? "ZWG" : "USD";

    private static void AppendCsv(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(',', values.Select(value => Csv(value))));
    }

    private static string Csv(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "Yes" : "No",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    public static string DisplayCurrency(string code, decimal amount) =>
        $"{CurrencyName(code)} {amount:N2}";

    public class PackageReportRow
    {
        public int PackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Subscriptions { get; set; }
        public decimal RevenueUsd { get; set; }
        public decimal RevenueZwg { get; set; }
        public int SessionsAllocated { get; set; }
        public int SessionsRemaining { get; set; }
        public decimal Utilization => SessionsAllocated == 0 ? 0 :
            (SessionsAllocated - SessionsRemaining) * 100m / SessionsAllocated;
    }

    public class ServiceReportRow
    {
        public string Name { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int GroupBookings { get; set; }
        public decimal CompletionRate => Bookings == 0 ? 0 : Completed * 100m / Bookings;
    }

    public class CounselorReportRow
    {
        public Guid CounselorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public decimal Earnings { get; set; }
        public decimal AverageRating { get; set; }
        public int FeedbackCount { get; set; }
        public decimal CompletionRate => Bookings == 0 ? 0 : Completed * 100m / Bookings;
    }

    public class PaymentReportRow
    {
        public DateTime CreatedAt { get; set; }
        public string Client { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
        public string OrderReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}
