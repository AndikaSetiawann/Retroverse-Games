using Microsoft.AspNetCore.Mvc;
using RetroVerseGaming.Models;
using RetroVerseGaming.Services;
using MongoDB.Driver;

namespace RetroVerseGaming.Controllers
{
    // [Authorize] // COMMENT INI DULU
    public class AdminController : Controller
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IWebHostEnvironment _env;

        public AdminController(MongoDBService mongoDBService, IWebHostEnvironment env)
        {
            _mongoDBService = mongoDBService;
            _env = env;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        // GET: Admin/Dashboard
        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            
            // Get stats
            ViewBag.TotalGames = await _mongoDBService.Games.CountDocumentsAsync(_ => true);
            ViewBag.TotalUsers = await _mongoDBService.Users.CountDocumentsAsync(_ => true);
            
            var orders = await _mongoDBService.Orders.Find(_ => true).ToListAsync();
            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = orders.Count(o => o.Status != "Completed");

            return View();
        }

        // ===== GAMES MANAGEMENT =====

        // GET: Admin/Games
        public async Task<IActionResult> Games()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var games = await _mongoDBService.Games.Find(_ => true).ToListAsync();
            return View(games);
        }

        // GET: Admin/CreateGame
        public IActionResult CreateGame()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Admin/CreateGame
        [HttpPost]
        public async Task<IActionResult> CreateGame(Game game, IFormFile? imageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            game.CreatedAt = DateTime.Now;

            // Handle Image Upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var gamesImgDir = Path.Combine(_env.WebRootPath, "img", "games");
                if (!Directory.Exists(gamesImgDir))
                    Directory.CreateDirectory(gamesImgDir);

                var uniqueName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(gamesImgDir, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                game.ImageUrl = "/img/games/" + uniqueName;
            }
            // else: if user provided a URL string in the form, game.ImageUrl is already set

            await _mongoDBService.Games.InsertOneAsync(game);

            TempData["Success"] = "Game berhasil ditambahkan!";
            return RedirectToAction("Games");
        }

        // GET: Admin/EditGame/id
        public async Task<IActionResult> EditGame(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var game = await _mongoDBService.Games.Find(g => g.Id == id).FirstOrDefaultAsync();
            return View(game);
        }

        // POST: Admin/EditGame/id
        [HttpPost]
        public async Task<IActionResult> EditGame(string id, Game game, IFormFile? imageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // Handle Image Upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var gamesImgDir = Path.Combine(_env.WebRootPath, "img", "games");
                if (!Directory.Exists(gamesImgDir))
                    Directory.CreateDirectory(gamesImgDir);

                var uniqueName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(gamesImgDir, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                game.ImageUrl = "/img/games/" + uniqueName;
            }
            else
            {
                // If no new file uploaded, keep existing ImageUrl from hidden field or DB
                // Note: MVC binding handles the form field `ImageUrl`, but if it was disabled/missing we'd need to fetch existing.
                // Assuming form has it. If empty, maybe user cleared it.
                // Best practice: Fetch existing to be safe if form doesn't send it back fully, or rely on hidden input.
            }

            game.Id = id;
            var filter = Builders<Game>.Filter.Eq(g => g.Id, id);
            await _mongoDBService.Games.ReplaceOneAsync(filter, game);

            TempData["Success"] = "Game berhasil diupdate!";
            return RedirectToAction("Games");
        }

        // GET: Admin/DeleteGame/id
        public async Task<IActionResult> DeleteGame(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var filter = Builders<Game>.Filter.Eq(g => g.Id, id);
            await _mongoDBService.Games.DeleteOneAsync(filter);

            TempData["Success"] = "Game berhasil dihapus!";
            return RedirectToAction("Games");
        }

        // ===== ORDERS MANAGEMENT =====

        // GET: Admin/Orders
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var orders = await _mongoDBService.Orders.Find(_ => true).SortByDescending(o => o.OrderDate).ToListAsync();

            // Get customer names for each order
            foreach (var order in orders)
            {
                var customer = await _mongoDBService.Users.Find(u => u.Id == order.CustomerId).FirstOrDefaultAsync();
                ViewData[$"CustomerName_{order.Id}"] = customer?.FullName ?? "Unknown";
            }

            return View(orders);
        }

        // POST: Admin/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
            var update = Builders<Order>.Update.Set(o => o.Status, status);
            await _mongoDBService.Orders.UpdateOneAsync(filter, update);

            TempData["Success"] = "Status order berhasil diupdate!";
            return RedirectToAction("Orders");
        }

        // GET: Admin/OrderDetails/id
        public async Task<IActionResult> OrderDetails(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var order = await _mongoDBService.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            var customer = await _mongoDBService.Users.Find(u => u.Id == order.CustomerId).FirstOrDefaultAsync();

            ViewBag.CustomerName = customer?.FullName;
            ViewBag.CustomerEmail = customer?.Email;
            ViewBag.CustomerPhone = customer?.PhoneNumber;

            return View(order);
        }

        // ===== USERS MANAGEMENT =====

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var users = await _mongoDBService.Users.Find(_ => true).ToListAsync();
            return View(users);
        }

        // GET: Admin/DeleteUser/id
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            await _mongoDBService.Users.DeleteOneAsync(filter);

            TempData["Success"] = "User berhasil dihapus!";
            return RedirectToAction("Users");
        }

        // POST: Admin/ToggleUserRole
        [HttpPost]
        public async Task<IActionResult> ToggleUserRole(string userId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var newRole = user.Role == "Admin" ? "Customer" : "Admin";

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.Role, newRole);
            await _mongoDBService.Users.UpdateOneAsync(filter, update);

            TempData["Success"] = $"Role berhasil diubah menjadi {newRole}!";
            return RedirectToAction("Users");
        }

        // DEV: Seed one demo toy (stored in Games collection)
        // GET: /Admin/SeedDemoGame?key=dev
        [HttpGet]
        public async Task<IActionResult> SeedDemoGame(string key = "")
        {
            if (key != "dev") return NotFound();

            var title = "Robot Builder Set";
            var existing = await _mongoDBService.Games.Find(g => g.Title == title).FirstOrDefaultAsync();
            if (existing != null)
            {
                return Content("Demo item already exists: " + existing.Title);
            }

            var game = new Game
            {
                Title = title,
                Platform = "Limited Edition",
                Publisher = "HappyToys",
                Developer = "HappyToys Factory",
                Genre = "STEM",
                Price = 199000,
                Stock = 25,
                Description = "Set rakit robot edukatif dengan 120+ komponen. Cocok untuk usia 7+.",
                ImageUrl = "https://images.unsplash.com/photo-1601758124096-1a1c90b3f3fd?w=800&q=80",
                ReleaseDate = DateTime.Now.AddMonths(-1),
                Rating = "7+",
                CreatedAt = DateTime.Now
            };

            await _mongoDBService.Games.InsertOneAsync(game);
            return Content("Seeded demo item: " + game.Title);
        }

        // DEV: Seed one PC game with image
        // GET: /Admin/SeedPCGame?key=dev
        [HttpGet]
        public async Task<IActionResult> SeedPCGame(string key = "")
        {
            if (key != "dev") return NotFound();

            var title = "Cyber Quest PC";
            var existing = await _mongoDBService.Games.Find(g => g.Title == title).FirstOrDefaultAsync();
            if (existing != null)
            {
                return Content("PC game already exists: " + existing.Title);
            }

            var game = new Game
            {
                Title = title,
                Platform = "PC",
                Publisher = "HyperPixel",
                Developer = "HyperPixel Studio",
                Genre = "Action",
                Price = 249000,
                Stock = 40,
                Description = "Petualangan aksi futuristik di kota neon. Optimized for PC.",
                ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1000&q=80",
                ReleaseDate = DateTime.Now.AddMonths(-2),
                Rating = "12+",
                CreatedAt = DateTime.Now
            };

            await _mongoDBService.Games.InsertOneAsync(game);
            return Content("Seeded PC game: " + game.Title);
        }

        // DEV: Update image URL by title (for quick fixes)
        // GET: /Admin/UpdateImage?key=dev&title=...&url=...
        [HttpGet]
        public async Task<IActionResult> UpdateImage(string key = "", string title = "", string url = "")
        {
            if (key != "dev") return NotFound();
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)) return BadRequest("title and url required");

            var filter = Builders<Game>.Filter.Eq(g => g.Title, title);
            var update = Builders<Game>.Update.Set(g => g.ImageUrl, url);
            var result = await _mongoDBService.Games.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0) return NotFound("Game not found: " + title);
            return Content("Updated image for: " + title);
        }

        // DEV: Cache external image to wwwroot/img and update ImageUrl to local file
        // GET: /Admin/CacheImage?key=dev&title=...&url=...&file=mk1.jpg
        [HttpGet]
        public async Task<IActionResult> CacheImage(string key = "", string title = "", string url = "", string file = "")
        {
            if (key != "dev") return NotFound();
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(file))
                return BadRequest("title, url, file required");

            // Download
            using var http = new HttpClient();
            byte[] bytes;
            try
            {
                bytes = await http.GetByteArrayAsync(url);
            }
            catch
            {
                return BadRequest("failed to download");
            }

            // Save
            var imgDir = Path.Combine(_env.WebRootPath, "img");
            if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);
            var savePath = Path.Combine(imgDir, file);
            await System.IO.File.WriteAllBytesAsync(savePath, bytes);

            // Update DB to local path
            var localUrl = "/img/" + file;
            var filter = Builders<Game>.Filter.Eq(g => g.Title, title);
            var update = Builders<Game>.Update.Set(g => g.ImageUrl, localUrl);
            var result = await _mongoDBService.Games.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0) return NotFound("Game not found: " + title);

            return Content("Cached and updated: " + localUrl);
        }

        // DEV: Seed Mortal Kombat 1 for PS5 and PC
        // GET: /Admin/SeedMK1?key=dev
        [HttpGet]
        public async Task<IActionResult> SeedMK1(string key = "")
        {
            if (key != "dev") return NotFound();

            var mkImage = "/img/mk1.jpg"; // assume cached via CacheImage

            var items = new List<Game>
            {
                new Game
                {
                    Title = "Mortal Kombat 1",
                    Platform = "PlayStation 5",
                    Publisher = "Warner Bros.",
                    Developer = "NetherRealm Studios",
                    Genre = "Fighting",
                    Price = 799000,
                    Stock = 20,
                    Description = "Pertarungan sinematik brutal generasi baru di PS5.",
                    ImageUrl = mkImage,
                    ReleaseDate = new DateTime(2023, 9, 19),
                    Rating = "18+",
                    CreatedAt = DateTime.Now
                },
                new Game
                {
                    Title = "Mortal Kombat 1",
                    Platform = "PC",
                    Publisher = "Warner Bros.",
                    Developer = "NetherRealm Studios",
                    Genre = "Fighting",
                    Price = 749000,
                    Stock = 30,
                    Description = "Pertarungan sinematik brutal versi PC (optimized).",
                    ImageUrl = mkImage,
                    ReleaseDate = new DateTime(2023, 9, 19),
                    Rating = "18+",
                    CreatedAt = DateTime.Now
                }
            };

            int inserted = 0;
            foreach (var game in items)
            {
                var exists = await _mongoDBService.Games.Find(g => g.Title == game.Title && g.Platform == game.Platform).FirstOrDefaultAsync();
                if (exists == null)
                {
                    await _mongoDBService.Games.InsertOneAsync(game);
                    inserted++;
                }
            }

            return Content($"Seed MK1 done. Inserted: {inserted}");
        }
    }
}



