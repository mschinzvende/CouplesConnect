using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Account
{
    public class RegisterCounselorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterCounselorModel(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Service> Services { get; set; } = new();

        public class InputModel
        {
            public string FullName { get; set; }
            public string Bio { get; set; }
            public string Qualifications { get; set; }
            public int YearsOfExperience { get; set; }
            

            public DateTime DateOfBirth { get; set; }
            public string Gender { get; set; }

            public List<int> SelectedServiceIds { get; set; } = new();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public List<IFormFile> CredentialFiles { get; set; }

        public void OnGet()
        {
            Services = _context.Services.Where(s => s.IsActive).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Services = _context.Services.Where(s => s.IsActive).ToList();

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    TempData["Error"] = "You must be logged in";
                    return Page();
                }

                var existing = _context.CounselorProfiles
                    .FirstOrDefault(x => x.UserId == user.Id);

                if (existing != null)
                {
                    TempData["Error"] = "You have already applied";
                    return Page();
                }
                var today = DateTime.UtcNow;

                // ? FUTURE DATE CHECK
                if (Input.DateOfBirth > today)
                {
                    TempData["Error"]= "Date of Birth cannot be in the future.";
                    return Page();
                }

                // AGE CALCULATION
                var age = today.Year - Input.DateOfBirth.Year;
                if (today < Input.DateOfBirth.AddYears(age)) age--;

                // ? MIN AGE CHECK
                if (age < 25)
                {
                    TempData["Error"] = "You must be at least 25 years old to apply as a counselor.";
                    return Page();
                }





                var profile = new CounselorProfile
                {
                    UserId = user.Id,
                    FullName = Input.FullName,
                    Bio = Input.Bio,
                    Qualifications = Input.Qualifications,
                    YearsOfExperience = Input.YearsOfExperience,
                    
                    DateOfBirth = Input.DateOfBirth,
                    Age = age,
                    Gender = Input.Gender,
                    IsApproved = false
                };

                _context.CounselorProfiles.Add(profile);
                await _context.SaveChangesAsync();

                // SAVE SERVICES
                if (Input.SelectedServiceIds != null && Input.SelectedServiceIds.Any())
                {
                    foreach (var serviceId in Input.SelectedServiceIds)
                    {
                        _context.CounselorServices.Add(new CounselorService
                        {
                            CounselorId = user.Id,
                            ServiceId = serviceId
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                // FILE UPLOADS (UNCHANGED)
                if (CredentialFiles != null && CredentialFiles.Any())
                {
                    foreach (var file in CredentialFiles)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var path = Path.Combine("wwwroot/uploads/credentials", fileName);

                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.CounselorCredentials.Add(new CounselorCredential
                        {
                            CounselorId = user.Id,
                            DocumentUrl = "/uploads/credentials/" + fileName,
                            UploadedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Application submitted. Await approval.";
                return RedirectToPage();
            }
            catch
            {
                TempData["Error"] = "Error submitting application";
                return Page();
            }
        }
    }
}