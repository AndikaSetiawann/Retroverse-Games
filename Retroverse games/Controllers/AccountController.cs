using Microsoft.AspNetCore.Mvc;
using RetroVerseGaming.Models;
using RetroVerseGaming.Services;
using MongoDB.Driver;
using System.Linq;
using System.IO;

namespace RetroVerseGaming.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IWebHostEnvironment _env;

        public AccountController(MongoDBService mongoDBService, IWebHostEnvironment env)
        {
            _mongoDBService = mongoDBService;
            _env = env;
        }

        // DEV: Seed one admin quickly (local use only)
        // GET: /Account/SeedAdmin?key=dev
        [HttpGet]
        public async Task<IActionResult> SeedAdmin(string key = "")
        {
            if (key != "dev") return NotFound();

            var existing = await _mongoDBService.Users
                .Find(u => u.Role == "Admin")
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Update password to requested one
                var newHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                var filter = Builders<User>.Filter.Eq(u => u.Id, existing.Id);
                var update = Builders<User>.Update.Set(u => u.Password, newHash);
                await _mongoDBService.Users.UpdateOneAsync(filter, update);
                return Content("Updated admin password to admin123 for: " + existing.Email);
            }

            var admin = new User
            {
                Username = "admin",
                FullName = "Administrator",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.Now
            };

            await _mongoDBService.Users.InsertOneAsync(admin);
            return Content("Seeded admin: admin@example.com / admin123");
        }

        // DEV: Seed a demo customer to test ordering
        // GET: /Account/SeedCustomer?key=dev
        [HttpGet]
        public async Task<IActionResult> SeedCustomer(string key = "")
        {
            if (key != "dev") return NotFound();

            var email = "user@example.com";
            var existing = await _mongoDBService.Users
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return Content("Customer already exists: user@example.com / user123");
            }

            var customer = new User
            {
                Username = "user",
                FullName = "Demo Customer",
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "Customer",
                CreatedAt = DateTime.Now
            };

            await _mongoDBService.Users.InsertOneAsync(customer);
            return Content("Seeded customer: user@example.com / user123");
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            // If already logged in, redirect based on role
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(role))
            {
                if (role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else
                    return RedirectToAction("Index", "Home"); // GANTI DARI Catalog ke Index
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            Console.WriteLine($"=== LOGIN DEBUG START ===");
            Console.WriteLine($"Email: {email}");
            
            var user = await _mongoDBService.Users
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();

            // RECOVERY MECHANISM: Auto-seed if demo users are missing
            if (user == null)
            {
                if (email == "user@example.com" && password == "user123")
                {
                    Console.WriteLine("Auto-seeding missing customer...");
                    user = new User
                    {
                        Username = "user",
                        FullName = "Demo Customer",
                        Email = email,
                        Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                        Role = "Customer"
                    };
                    await _mongoDBService.Users.InsertOneAsync(user);
                }
                else if (email == "admin@example.com" && password == "admin123")
                {
                     Console.WriteLine("Auto-seeding missing admin...");
                     user = new User
                     {
                         Username = "admin",
                         FullName = "Administrator",
                         Email = email,
                         Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                         Role = "Admin"
                     };
                     await _mongoDBService.Users.InsertOneAsync(user);
                }
            }

            Console.WriteLine($"User found: {user?.Email}");
            Console.WriteLine($"User role: {user?.Role}");

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                Console.WriteLine("Password verified!");

                // AUTO-FIX: Force Admin Role for default admin email if it's currently Customer
                if (user.Email == "admin@example.com" && user.Role != "Admin")
                {
                    Console.WriteLine("!!! AUTO-FIXING ADMIN ROLE !!!");
                    user.Role = "Admin";
                    var filterAutoFix = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                    var updateAutoFix = Builders<User>.Update.Set(u => u.Role, "Admin");
                    await _mongoDBService.Users.UpdateOneAsync(filterAutoFix, updateAutoFix);
                }
                
                // Set session
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserEmail", user.Email);
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    HttpContext.Session.SetString("UserAvatar", user.ProfilePictureUrl);
                }

                // Verify session was set
                var sessionRole = HttpContext.Session.GetString("UserRole");
                Console.WriteLine($"Session role after set: {sessionRole}");

                if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("REDIRECTING TO ADMIN DASHBOARD");
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    Console.WriteLine("REDIRECTING TO CUSTOMER CATALOG");
                    return RedirectToAction("Catalog", "Customer");
                }
            }

            Console.WriteLine("LOGIN FAILED");
            ViewBag.Error = "Email atau password salah!";
            return View();
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            // If already logged in, redirect to catalog
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Catalog", "Customer");
            }

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            // Check if email already exists
            var existingUser = await _mongoDBService.Users
                .Find(u => u.Email == user.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                ViewBag.Error = "Email sudah terdaftar! Gunakan email lain.";
                return View();
            }

            // Check if username already exists
            var existingUsername = await _mongoDBService.Users
                .Find(u => u.Username == user.Username)
                .FirstOrDefaultAsync();

            if (existingUsername != null)
            {
                ViewBag.Error = "Username sudah digunakan! Pilih username lain.";
                return View();
            }

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.Role = "Customer"; // Default role
            user.CreatedAt = DateTime.Now;

            await _mongoDBService.Users.InsertOneAsync(user);

            TempData["Success"] = "Registrasi berhasil! Silakan login.";
            return RedirectToAction("Login");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _mongoDBService.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return RedirectToAction("Logout");
            }
                
            // Fetch owned games for "Recent Activity" / Library showcase
            var orders = await _mongoDBService.Orders
                .Find(o => o.CustomerId == userId)
                .SortByDescending(o => o.OrderDate)
                .ToListAsync();
                
            var gameIds = orders.SelectMany(o => o.Items.Select(i => i.GameId)).Distinct().ToList();
            var ownedGames = new List<Game>();
            
            if (gameIds.Any())
            {
                var filter = Builders<Game>.Filter.In(g => g.Id, gameIds);
                ownedGames = await _mongoDBService.Games.Find(filter).ToListAsync();
            }
            
            ViewBag.OwnedGames = ownedGames;
            ViewBag.GameCount = ownedGames.Count;

            return View(user);
        }

        // POST: Account/UpdateProfile
        // POST: Account/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User user, IFormFile? profilePicture)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Get current user data
            var currentUser = await _mongoDBService.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            // Handle Profile Picture Upload
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePictureUrl = "/img/profiles/" + uniqueFileName;
                
                // Update session for immediate UI update if we store avatar in session
                HttpContext.Session.SetString("UserAvatar", user.ProfilePictureUrl);
            }
            else
            {
                // Keep existing picture if no new one uploaded
                user.ProfilePictureUrl = currentUser.ProfilePictureUrl;
            }

            // Update only allowed fields on the EXISTING user object
            // This prevents overwriting Wishlist or other fields with null/empty
            currentUser.FullName = user.FullName;
            currentUser.PhoneNumber = user.PhoneNumber;
            currentUser.Address = user.Address;
            // Username/Email usually shouldn't be changed here or requires more validation, keeping as is or ignoring for now
            // currentUser.Username = user.Username; 
            
            // ProfilePictureUrl is already handled above (currentUser.ProfilePictureUrl = ...) if specific logic was applied, 
            // but here we just ensured 'user' (the param) had it set.
            // Let's make sure we apply it to 'currentUser'
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                currentUser.ProfilePictureUrl = user.ProfilePictureUrl;
            }

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            await _mongoDBService.Users.ReplaceOneAsync(filter, currentUser);

            // Update session
            HttpContext.Session.SetString("UserName", currentUser.FullName);

            TempData["Success"] = "Profile berhasil diupdate!";
            return RedirectToAction("Profile");
        }

        // POST: Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _mongoDBService.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                TempData["Error"] = "Password lama salah!";
                return RedirectToAction("Profile");
            }

            // Update password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.Password, hashedPassword);
            await _mongoDBService.Users.UpdateOneAsync(filter, update);

            TempData["Success"] = "Password berhasil diubah!";
            return RedirectToAction("Profile");
        }
    }
}
