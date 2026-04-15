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

        public class InputModel
        {
            public string FullName { get; set; }
            public string Bio { get; set; }
            public string Qualifications { get; set; }
            public int YearsOfExperience { get; set; }
            public string Specializations { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public List<IFormFile> CredentialFiles { get; set; }
        public RegisterCounselorModel(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    TempData["Error"] = "You must be logged in";
                    return RedirectToPage();
                }

                // ?? PREVENT DUPLICATE APPLICATION
                var existing = _context.CounselorProfiles
                    .FirstOrDefault(x => x.UserId == user.Id);

                if (existing != null)
                {
                    TempData["Error"] = "You have already applied";
                    return RedirectToPage();
                }


                if (CredentialFiles != null && CredentialFiles.Any())
                {
                    foreach (var file in CredentialFiles)
                    {
                        // ?? SIZE CHECK (5MB)
                        if (file.Length > 5 * 1024 * 1024)
                        {
                            TempData["Error"] = $"{file.FileName} is too large (max 5MB)";
                            return RedirectToPage();
                        }

                        // ?? TYPE CHECK
                        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };

                        if (!allowedTypes.Contains(file.ContentType))
                        {
                            TempData["Error"] = $"{file.FileName} is not a valid file type";
                            return RedirectToPage();
                        }
                    }
                }

                // ?? CREATE PROFILE
                var profile = new CounselorProfile
                {
                    UserId = user.Id,
                    FullName = Input.FullName,
                    Bio = Input.Bio,
                    Qualifications = Input.Qualifications,
                    YearsOfExperience = Input.YearsOfExperience,
                    Specializations = Input.Specializations,
                    IsApproved = false
                };

                _context.CounselorProfiles.Add(profile);
                await _context.SaveChangesAsync();

                // ?? HANDLE FILE UPLOADS
                if (CredentialFiles != null && CredentialFiles.Any())
                {
                    foreach (var file in CredentialFiles)
                    {


                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine("wwwroot/uploads/credentials", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var credential = new CounselorCredential
                            {
                                
                                CounselorId = user.Id,
                                CredentialType = "Document",
                                DocumentUrl = "/uploads/credentials/" + fileName,
                                UploadedAt = DateTime.UtcNow
                            };

                            _context.CounselorCredentials.Add(credential);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                

                TempData["Success"] = "Application submitted. Await approval.";
            }
            catch
            {
                TempData["Error"] = "Error submitting application";
            }

            return RedirectToPage();
        }
    }
}