using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class CustomerController : BasePublicController
    {
        private readonly ICustomerService _customerService;
        private readonly IAuthenticationService _authenticationService;

        public CustomerController(
            IWorkContext workContext,
            ICustomerService customerService,
            IAuthenticationService authenticationService) : base(workContext)
        {
            _customerService = customerService;
            _authenticationService = authenticationService;
        }

        // GET: /Customer/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Customer/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Email and password are required";
                return View();
            }

            var isValid = await _customerService.ValidateCustomerAsync(email, password);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "Invalid email or password";
                return View();
            }

            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found";
                return View();
            }

            await _authenticationService.SignInAsync(customer, rememberMe);
            TempData["SuccessMessage"] = $"Welcome back, {customer.Email}!";
            return RedirectToAction("Info");
        }

        // GET: /Customer/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Customer/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Email and password are required";
                return View();
            }

            var existingCustomer = await _customerService.GetCustomerByEmailAsync(email);
            if (existingCustomer != null)
            {
                TempData["ErrorMessage"] = "Email already registered";
                return View();
            }

            var customer = new Core.Domain.Customers.Customer
            {
                Email = email,
                Username = email,
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
                CustomerGuid = Guid.NewGuid()
            };

            await _customerService.InsertCustomerAsync(customer);
            await _authenticationService.SignInAsync(customer, false);
            TempData["SuccessMessage"] = "Registration successful! Welcome to nopCommerce!";
            return RedirectToAction("Info");
        }

        // GET: /Customer/Logout
        public async Task<IActionResult> Logout()
        {
            await _authenticationService.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Customer/Info
        public IActionResult Info()
        {
            return View();
        }

        // GET: /Customer/Orders
        public IActionResult Orders()
        {
            return Content("Customer Orders");
        }
    }
}
