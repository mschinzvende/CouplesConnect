using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CouplesConnect.ViewModels;

public class ResourceViewModel
{
    public Guid? ResourceId { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public string HtmlPage { get; set; }

    public string EstimatedDuration { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public IFormFile? ThumbnailFile { get; set; }

    public string? ExistingThumbnail { get; set; }

    public List<SelectListItem>? Categories { get; set; }
}