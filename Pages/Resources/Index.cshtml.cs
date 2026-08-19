using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Resources;

public class IndexModel : PageModel
{
    public IReadOnlyList<ResourceCollection> Collections { get; private set; } = Array.Empty<ResourceCollection>();
    public int AvailableActivityCount { get; private set; }

    public void OnGet()
    {
        Collections = BuildCollections();
        AvailableActivityCount = Collections.Sum(collection => collection.Activities.Count(activity => activity.IsAvailable));
    }

    private static IReadOnlyList<ResourceCollection> BuildCollections() =>
    [
        new(
            1,
            "communication-emotional-connection",
            "Communication & Emotional Connection",
            "Build habits of listening, appreciation, and daily connection that help both partners feel seen and understood.",
            "mdi mdi-message-heart-outline",
            "#4f46e5",
            [
                new("Daily Check-In", "A simple guided practice for making space for each other every day.", "Daily practice", "5 minutes", "mdi mdi-message-text-outline", "/Resources/DailyCheckIn/Index", true),
                new("Love Language Planner", "Turn what makes each partner feel valued into small, intentional actions.", "Connection tool", "20 minutes", "mdi mdi-heart-outline", "/Resources/LoveLanguagePlanner/Index", true)
            ]),
        new(
            2,
            "financial-partnership",
            "Financial Partnership",
            "Create openness around money, shared decisions, priorities, and the practical plans that support your life together.",
            "mdi mdi-wallet-outline",
            "#0f9b8e",
            []),
        new(
            3,
            "in-laws-extended-family",
            "In-Laws & Extended Family",
            "Navigate family relationships with respect, healthy boundaries, and a united approach as a couple.",
            "mdi mdi-account-group-outline",
            "#d97706",
            [
                new("Family Boundaries", "Work through practical steps for creating respectful boundaries around your relationship.", "Guided exercise", "15 minutes", "mdi mdi-account-multiple-check-outline", "/Resources/FamilyBoundaries/Index", true)
            ]),
        new(
            4,
            "shared-vision-goals-values",
            "Shared Vision, Goals & Values",
            "Clarify the future you want to build, the values that guide you, and the goals you can work toward together.",
            "mdi mdi-compass-outline",
            "#2563eb",
            [
                new("Shared Vision Board", "Turn shared hopes, values, and goals into a practical picture of your future.", "In development", "Coming soon", "mdi mdi-image-outline", null, false)
            ]),
        new(
            5,
            "conflict-resolution-emotional-safety",
            "Conflict Resolution & Emotional Safety",
            "Learn to slow difficult moments down, repair after conflict, and create a relationship where both people can speak honestly.",
            "mdi mdi-handshake-outline",
            "#dc4c64",
            [
                new("Conflict Repair", "Use a clear, compassionate process to move from tension toward understanding and repair.", "Guided exercise", "10 minutes", "mdi mdi-tools", "/Resources/ConflictRepair/Index", true)
            ]),
        new(
            6,
            "intimacy-sexual-fulfillment",
            "Intimacy & Sexual Fulfillment",
            "Strengthen emotional closeness, trust, affection, and intentional conversations about intimacy.",
            "mdi mdi-heart-multiple-outline",
            "#be3d7d",
            [
                new("Guided Intimacy Builder", "Explore meaningful ways to nurture trust, affection, and emotional closeness.", "Connection tool", "20 minutes", "mdi mdi-heart-plus-outline", "/Resources/GuidedIntimacyBuilder/Index", true)
            ]),
        new(
            7,
            "time-priorities-work-life-balance",
            "Time, Priorities & Work-Life Balance",
            "Protect quality time, make room for rest, and plan moments that keep your relationship from being lost in the busy.",
            "mdi mdi-clock-outline",
            "#7c3aed",
            [
                new("Date Night Generator", "Find a simple, intentional date idea that fits your time, mood, and budget.", "Planning tool", "5 minutes", "mdi mdi-calendar-heart", "/Resources/DateNightGenerator/Index", true)
            ]),
        new(
            8,
            "technology-boundaries-privacy",
            "Technology Use, Boundaries & Privacy",
            "Set shared expectations around screens, social media, privacy, and digital habits that protect connection.",
            "mdi mdi-cellphone-lock",
            "#0284c7",
            []),
        new(
            9,
            "parenting-family-life-household-roles",
            "Parenting, Family Life & Household Roles",
            "Approach parenting, household responsibilities, and family rhythms as a supportive, coordinated team.",
            "mdi mdi-home-heart",
            "#16a34a",
            []),
        new(
            10,
            "life-transitions-crisis",
            "Navigating Life Transitions & Crisis Together",
            "Stay connected through change, uncertainty, loss, stress, and the seasons that ask more of your partnership.",
            "mdi mdi-weather-lightning-rainy",
            "#64748b",
            [
                new("Gratitude Journal", "Create a steady practice of noticing the good and supporting resilience together.", "Reflection tool", "Daily", "mdi mdi-notebook-heart-outline", "/Resources/GratitudeJournal/Index", true),
                new("Relationship Health Quiz", "Reflect on the strengths in your relationship and identify the areas that need care.", "In development", "Coming soon", "mdi mdi-clipboard-text-outline", null, false)
            ])
    ];
}

public sealed record ResourceCollection(
    int Number,
    string Slug,
    string Name,
    string Description,
    string Icon,
    string Color,
    IReadOnlyList<ResourceActivity> Activities);

public sealed record ResourceActivity(
    string Name,
    string Description,
    string Type,
    string Duration,
    string Icon,
    string? Href,
    bool IsAvailable);
