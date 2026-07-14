using CouplesConnect.Models;

namespace CouplesConnect.Data;

public static class ResourceSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (context.ResourceCategories.Any())
            return;

        var communication = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Communication",
            Color = "#3B82F6",
            Icon = "mdi-chat-processing",
            DisplayOrder = 1
        };

        var assessment = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Assessment",
            Color = "#8B5CF6",
            Icon = "mdi-clipboard-check",
            DisplayOrder = 2
        };

        var planning = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Planning",
            Color = "#F59E0B",
            Icon = "mdi-calendar-star",
            DisplayOrder = 3
        };

        var reflection = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Reflection",
            Color = "#10B981",
            Icon = "mdi-book-heart",
            DisplayOrder = 4
        };

        var connection = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Connection",
            Color = "#EC4899",
            Icon = "mdi-heart",
            DisplayOrder = 5
        };

        var challenge = new ResourceCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = "Challenge",
            Color = "#EF4444",
            Icon = "mdi-trophy",
            DisplayOrder = 6
        };

        context.ResourceCategories.AddRange(
            communication,
            assessment,
            planning,
            reflection,
            connection,
            challenge);

        await context.SaveChangesAsync();

        context.Resources.AddRange(

            new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = "Daily Check-In",
                Description = "Strengthen communication through guided daily conversations.",
                CategoryId = communication.CategoryId,
                Thumbnail = "/resources/images/daily-checkin.jpg",
                HtmlPage = "daily-checkin/index.html",
                EstimatedDuration = "5 mins",
                IsFeatured = true,
                DisplayOrder = 1
            },

            new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = "Conflict Repair",
                Description = "Learn practical ways to resolve disagreements.",
                CategoryId = communication.CategoryId,
                Thumbnail = "/resources/images/conflict.jpg",
                HtmlPage = "conflict-repair/index.html",
                EstimatedDuration = "10 mins",
                IsFeatured = true,
                DisplayOrder = 2
            },

            new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = "Relationship Health Quiz",
                Description = "Assess your relationship health together.",
                CategoryId = assessment.CategoryId,
                Thumbnail = "/resources/images/quiz.jpg",
                HtmlPage = "relationship-health-quiz/index.html",
                EstimatedDuration = "15 mins",
                IsFeatured = true,
                DisplayOrder = 3
            },

            new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = "Love Language Planner",
                Description = "Discover how each partner feels loved.",
                CategoryId = assessment.CategoryId,
                Thumbnail = "/resources/images/love-language.jpg",
                HtmlPage = "love-language/index.html",
                EstimatedDuration = "15 mins",
                DisplayOrder = 4
            },

            new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = "Shared Vision Board",
                Description = "Create goals and dreams together.",
                CategoryId = planning.CategoryId,
                Thumbnail = "/resources/images/vision.jpg",
                HtmlPage = "vision-board/index.html",
                EstimatedDuration = "20 mins",
                DisplayOrder = 5
            }

        );

        await context.SaveChangesAsync();
    }
}