using Cinema.Data;
using Cinema.Models;
using Cinema.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Cinema.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action("ConfirmEmail", "Account",
                    new { area = "", id = user.Id, token }, Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
                    $"<h1>Please click <a href='{link}'>here</a> to confirm your email</h1>");

                TempData["Message"] = "Registered successfully. Please check your email to confirm your account.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string id, string token)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            TempData["Message"] = result.Succeeded
                ? "Email confirmed successfully. You can login now."
                : "Invalid or expired link.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManager.FindByEmailAsync(loginVM.UserNameOrEmail) ??
                       await _userManager.FindByNameAsync(loginVM.UserNameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Email or Password");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, loginVM.Password, loginVM.RememberMe, true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    ModelState.AddModelError("", "too many attempts please try again later");
                else if (result.IsNotAllowed)
                    ModelState.AddModelError("", "please confirm your email first");
                else
                    ModelState.AddModelError("", "Invalid Email or Password");

                return View(loginVM);
            }

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.UserNameOrEmail) ??
                       await _userManager.FindByNameAsync(vm.UserNameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid UserName or Email");
                return View(vm);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError("", "This email is already confirmed");
                return View(vm);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action("ConfirmEmail", "Account",
                new { area = "", id = user.Id, token }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
                $"<h1>Please click <a href='{link}'>here</a> to confirm your email</h1>");

            TempData["Message"] = "Confirmation email sent. Please check your inbox.";
            return RedirectToAction(nameof(Login));
        }

        

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.UserNameOrEmail) ??
                       await _userManager.FindByNameAsync(vm.UserNameOrEmail);

            if (user == null)
            {
                TempData["Message"] = "Please check your email.";
                return RedirectToAction(nameof(Login));
            }

          
            var recent = await _context.ApplicationUserOTPs
                .CountAsync(o => o.ApplicationUserId == user.Id &&
                                 o.CreateAt > DateTime.UtcNow.AddHours(-1));

            if (recent >= 5)
            {
                TempData["Message"] = "Too many requests. Try again later.";
                return RedirectToAction(nameof(Login));
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            _context.ApplicationUserOTPs.Add(new ApplicationUserOTP
            {
                OTP = otp,
                CreateAt = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMinutes(10),
                IsValid = true,
                ApplicationUserId = user.Id
            });
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(user.Email!, "Your reset code",
                $"<h1>Your verification code is: {otp}</h1><p>It is valid for 10 minutes.</p>");

            TempData["Message"] = "Please check your email.";
            return RedirectToAction(nameof(ValidateOTP), new { userId = user.Id });
        }

        [HttpGet]
        public IActionResult ValidateOTP(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            return View(new ValidateOTPVM { UserId = userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null)
                return NotFound();

            var otp = await _context.ApplicationUserOTPs
                .Where(o => o.ApplicationUserId == vm.UserId &&
                            o.IsValid &&
                            o.ValidTo > DateTime.UtcNow)
                .OrderByDescending(o => o.CreateAt)
                .FirstOrDefaultAsync();

            if (otp == null || otp.OTP != vm.OTP)
            {
                ModelState.AddModelError("", "Invalid or expired code");
                return View(vm);
            }

           
            otp.IsValid = false;
            await _context.SaveChangesAsync();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction(nameof(ResetPassword), new { userId = user.Id, token });
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return NotFound();

            return View(new ResetPasswordVM { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            TempData["Message"] = "Password changed successfully. You can login now.";
            return RedirectToAction(nameof(Login));
        }
    }
}