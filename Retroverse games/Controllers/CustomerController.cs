using Microsoft.AspNetCore.Mvc;
using RetroVerseGaming.Models;
using RetroVerseGaming.Services;
using MongoDB.Driver;

namespace RetroVerseGaming.Controllers
{
    public class CustomerController : Controller
    {
        private readonly MongoDBService _mongoDBService;

        public CustomerController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        // Check if user is logged in
        private bool IsLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return !string.IsNullOrEmpty(userId);
        }

        // Get current user ID
        private string GetUserId()
        {
            return HttpContext.Session.GetString("UserId") ?? string.Empty;
        }

        // ===== CATALOG =====
        // GET: Customer/Home
        public IActionResult Home()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Index", "Home");

            return View();
        }

        // GET: Customer/Catalog
        public async Task<IActionResult> Catalog(string genre = "", string search = "")
        {
            var filter = Builders<Game>.Filter.Empty;

            // Filter by genre
            if (!string.IsNullOrEmpty(genre))
            {
                filter = Builders<Game>.Filter.Eq(g => g.Genre, genre);
            }

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                var titleFilter = Builders<Game>.Filter.Regex(g => g.Title, new MongoDB.Bson.BsonRegularExpression(search, "i"));
                var publisherFilter = Builders<Game>.Filter.Regex(g => g.Publisher, new MongoDB.Bson.BsonRegularExpression(search, "i"));
                filter = filter & (titleFilter | publisherFilter);
            }

            var games = await _mongoDBService.Games.Find(filter).ToListAsync();

            ViewBag.Genres = await _mongoDBService.Games.Distinct<string>("Genre", Builders<Game>.Filter.Empty).ToListAsync();
            ViewBag.SelectedGenre = genre;
            ViewBag.SearchQuery = search;

            // Get owned game IDs for logged in user
            var ownedGameIds = new List<string>();
            var wishlistedGameIds = new List<string>();
            if (IsLoggedIn())
            {
                var userId = GetUserId();
                var orders = await _mongoDBService.Orders.Find(o => o.CustomerId == userId).ToListAsync();
                ownedGameIds = orders.SelectMany(o => o.Items.Select(i => i.GameId)).Distinct().ToList();
                
                var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                if (user.Wishlist != null) wishlistedGameIds = user.Wishlist;
            }
            ViewBag.OwnedGameIds = ownedGameIds;
            ViewBag.WishlistedGameIds = wishlistedGameIds;

            return View(games);
        }

        // GET: Customer/GameDetail/id
        public async Task<IActionResult> GameDetail(string id)
        {
            var game = await _mongoDBService.Games.Find(g => g.Id == id).FirstOrDefaultAsync();

            if (game == null)
            {
                return RedirectToAction("Catalog");
            }

            // Check if user owns this game
            ViewBag.IsOwned = false;
            if (IsLoggedIn())
            {
                var userId = GetUserId();
                var orders = await _mongoDBService.Orders.Find(o => o.CustomerId == userId).ToListAsync();
                var ownedGameIds = orders.SelectMany(o => o.Items.Select(i => i.GameId)).Distinct().ToList();
                ViewBag.IsOwned = ownedGameIds.Contains(game.Id);

                // Check wishlist
                var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                ViewBag.IsWishlisted = user.Wishlist != null && user.Wishlist.Contains(game.Id);
            }

            return View(game);
        }

        // ===== CART =====

        // GET: Customer/Cart
        public async Task<IActionResult> Cart()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart == null)
            {
                cart = new Cart { CustomerId = userId, Items = new List<CartItem>() };
            }

            return View(cart);
        }

        // POST: Customer/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string gameId, int quantity = 1)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var game = await _mongoDBService.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync();

            if (game == null || game.Stock < quantity)
            {
                TempData["Error"] = "Stok tidak mencukupi!";
                return RedirectToAction("GameDetail", new { id = gameId });
            }

            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart == null)
            {
                // Create new cart
                cart = new Cart
                {
                    CustomerId = userId,
                    Items = new List<CartItem>
                    {
                        new CartItem
                        {
                            GameId = gameId,
                            GameTitle = game.Title,
                            Quantity = quantity,
                            Price = game.Price,
                            ImageUrl = game.ImageUrl
                        }
                    }
                };
                await _mongoDBService.Carts.InsertOneAsync(cart);
            }
            else
            {
                // Update existing cart
                var existingItem = cart.Items.FirstOrDefault(i => i.GameId == gameId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        GameId = gameId,
                        GameTitle = game.Title,
                        Quantity = quantity,
                        Price = game.Price,
                        ImageUrl = game.ImageUrl
                    });
                }

                cart.UpdatedAt = DateTime.Now;
                var filter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
                await _mongoDBService.Carts.ReplaceOneAsync(filter, cart);
            }

            TempData["Success"] = "Game berhasil ditambahkan ke keranjang!";
            return RedirectToAction("Cart");
        }

        // GET: Customer/AddToCart (fallback when POST blocked)
        [HttpGet]
        public Task<IActionResult> AddToCart(string gameId)
        {
            // Reuse POST handler with default quantity from query (?quantity=)
            var qtyString = HttpContext.Request.Query["quantity"].ToString();
            int qty = 1;
            int.TryParse(qtyString, out qty);
            return AddToCart(gameId, Math.Max(1, qty));
        }

        // POST: Customer/UpdateCartItem
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(string gameId, int quantity)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.GameId == gameId);
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Items.Remove(item);
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }

                    cart.UpdatedAt = DateTime.Now;
                    var filter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
                    await _mongoDBService.Carts.ReplaceOneAsync(filter, cart);
                }
            }

            return RedirectToAction("Cart");
        }

        // GET: Customer/RemoveFromCart/gameId
        public async Task<IActionResult> RemoveFromCart(string gameId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart != null)
            {
                cart.Items.RemoveAll(i => i.GameId == gameId);
                cart.UpdatedAt = DateTime.Now;

                var filter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
                await _mongoDBService.Carts.ReplaceOneAsync(filter, cart);

                TempData["Success"] = "Item berhasil dihapus dari keranjang!";
            }

            return RedirectToAction("Cart");
        }

        // ===== CHECKOUT =====

        // GET: Customer/Checkout
        public async Task<IActionResult> Checkout()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] = "Keranjang kosong!";
                return RedirectToAction("Cart");
            }

            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            ViewBag.User = user;

            return View(cart);
        }

        // POST: Customer/PlaceOrder
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod, string phoneNumber = "", string cardNumber = "", string expiryDate = "", string cvv = "", string cardholderName = "")
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var cart = await _mongoDBService.Carts.Find(c => c.CustomerId == userId).FirstOrDefaultAsync();

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] = "Cart is empty!";
                return RedirectToAction("Cart");
            }

            if (string.IsNullOrEmpty(paymentMethod))
            {
                TempData["Error"] = "Please select a payment method!";
                return RedirectToAction("Checkout");
            }

            // Calculate total
            decimal totalAmount = 0;
            foreach (var item in cart.Items)
            {
                totalAmount += item.Price * item.Quantity;
            }

            // Simulate payment processing
            string paymentInfo = "";
            string vaNumber = "";
            if (paymentMethod == "Dana" || paymentMethod == "OVO" || paymentMethod == "GoPay")
            {
                paymentInfo = $"{paymentMethod} - {phoneNumber}";
            }
            else if (paymentMethod == "BCA" || paymentMethod == "Mandiri" || paymentMethod == "BNI")
            {
                // Generate virtual account number (simulation) - 10 digits
                Random random = new Random();
                // Generate 10-digit VA number by combining two parts
                int part1 = random.Next(10000, 99999); // 5 digits
                int part2 = random.Next(10000, 99999); // 5 digits
                vaNumber = $"{part1}{part2}";
                paymentInfo = $"{paymentMethod} VA: {vaNumber}";
            }
            else if (paymentMethod == "CreditCard")
            {
                paymentInfo = $"Card ending in {cardNumber.Substring(Math.Max(0, cardNumber.Length - 4))}";
            }

            // Create order
            var order = new Order
            {
                CustomerId = userId,
                Items = cart.Items.Select(i => new OrderItem
                {
                    GameId = i.GameId,
                    GameTitle = i.GameTitle,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                TotalAmount = totalAmount,
                Status = "Completed", // Digital game - auto complete after purchase
                ShippingAddress = shippingAddress ?? "Digital Delivery",
                PaymentMethod = paymentMethod,
                PaymentInfo = paymentInfo,
                OrderDate = DateTime.Now
            };

            await _mongoDBService.Orders.InsertOneAsync(order);

            // Digital games - no stock management needed
            // Games are added to library immediately

            // Clear cart
            var cartFilter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
            await _mongoDBService.Carts.DeleteOneAsync(cartFilter);

            // Get user info for WhatsApp message
            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var userName = user?.FullName ?? "Customer";
            var userEmail = user?.Email ?? "";

            // Build game list for WhatsApp message
            var gameList = string.Join(", ", cart.Items.Select(i => i.GameTitle));

            // WhatsApp message
            var waMessage = $"Halo Admin RetroVerse!%0A%0A" +
                           $"Saya *{userName}* ({userEmail}) ingin konfirmasi pembayaran:%0A%0A" +
                           $"📦 *Order ID:* {order.Id.Substring(0, 8)}%0A" +
                           $"🎮 *Games:* {gameList}%0A" +
                           $"💰 *Total:* Rp {totalAmount:N0}%0A" +
                           $"💳 *Metode:* {paymentMethod}%0A%0A" +
                           $"Mohon konfirmasi pembayaran saya. Terima kasih!";

            // Store WhatsApp URL in TempData for redirect
            var waUrl = $"https://wa.me/6281388209195?text={waMessage}";
            TempData["WhatsAppUrl"] = waUrl;
            TempData["Success"] = "Order berhasil! Silakan konfirmasi pembayaran via WhatsApp.";
            
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        // GET: Customer/OrderConfirmation/orderId
        public async Task<IActionResult> OrderConfirmation(string orderId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var order = await _mongoDBService.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
            if (order == null)
                return RedirectToAction("Library");

            // Get user info
            var userId = GetUserId();
            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            
            // Build WhatsApp URL
            var gameList = string.Join(", ", order.Items.Select(i => i.GameTitle));
            var waMessage = $"Halo Admin RetroVerse!%0A%0A" +
                           $"Saya *{user?.FullName}* ({user?.Email}) ingin konfirmasi pembayaran:%0A%0A" +
                           $"📦 *Order ID:* {order.Id.Substring(0, 8)}%0A" +
                           $"🎮 *Games:* {gameList}%0A" +
                           $"💰 *Total:* Rp {order.TotalAmount:N0}%0A" +
                           $"💳 *Metode:* {order.PaymentMethod}%0A%0A" +
                           $"Mohon konfirmasi pembayaran saya. Terima kasih!";

            ViewBag.WhatsAppUrl = $"https://wa.me/6281388209195?text={waMessage}";
            ViewBag.User = user;
            
            return View(order);
        }

        // GET: Customer/OrderSuccess/orderId
        public async Task<IActionResult> OrderSuccess(string orderId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var order = await _mongoDBService.Orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
            return View(order);
        }

        // ===== MY ORDERS =====

        // GET: Customer/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var orders = await _mongoDBService.Orders
                .Find(o => o.CustomerId == userId)
                .SortByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Customer/MyOrderDetail/id
        public async Task<IActionResult> MyOrderDetail(string id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var order = await _mongoDBService.Orders.Find(o => o.Id == id && o.CustomerId == userId).FirstOrDefaultAsync();

            if (order == null)
            {
                return RedirectToAction("MyOrders");
            }

            return View(order);

        }

        // ===== LIBRARY =====

        // GET: Customer/Library
        public async Task<IActionResult> Library()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            
            // Get all orders for this user (matching Catalog logic)
            var completedOrders = await _mongoDBService.Orders
                .Find(o => o.CustomerId == userId)
                .ToListAsync();

            // Extract all game IDs from orders
            var ownedGameIds = new HashSet<string>();
            foreach (var order in completedOrders)
            {
                foreach (var item in order.Items)
                {
                    ownedGameIds.Add(item.GameId);
                }
            }

            // Get game details for owned games
            var ownedGames = new List<Game>();
            foreach (var gameId in ownedGameIds)
            {
                var game = await _mongoDBService.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
                if (game != null)
                {
                    ownedGames.Add(game);
                }
            }

            ViewBag.OwnedGames = ownedGames;
            return View(ownedGames);
        }

        // GET: Customer/Download/gameId
        public async Task<IActionResult> Download(string gameId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            
            // Verify user owns this game
            var completedOrders = await _mongoDBService.Orders
                .Find(o => o.CustomerId == userId)
                .ToListAsync();

            bool ownsGame = false;
            foreach (var order in completedOrders)
            {
                if (order.Items.Any(i => i.GameId == gameId))
                {
                    ownsGame = true;
                    break;
                }
            }

            if (!ownsGame)
            {
                TempData["Error"] = "Anda belum membeli game ini!";
                return RedirectToAction("Library");
            }

            var game = await _mongoDBService.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync();
            if (game == null)
            {
                TempData["Error"] = "Game tidak ditemukan!";
                return RedirectToAction("Library");
            }

            // Simulate download (in real app, this would serve the actual game file)
            TempData["Success"] = $"Download {game.Title} dimulai! (Simulasi)";
            return RedirectToAction("Library");
        }

        // ===== WISHLIST =====

        // GET: Customer/Wishlist
        public async Task<IActionResult> Wishlist()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            var wishlistGames = new List<Game>();
            if (user.Wishlist != null && user.Wishlist.Count > 0)
            {
                var filter = Builders<Game>.Filter.In(g => g.Id, user.Wishlist);
                wishlistGames = await _mongoDBService.Games.Find(filter).ToListAsync();
            }

            return View(wishlistGames);
        }

        // POST: Customer/AddToWishlist
        [HttpPost]
        public async Task<IActionResult> AddToWishlist(string gameId)
        {
            if (!IsLoggedIn())
                return Json(new { success = false, message = "Please login first" });

            var userId = GetUserId();
            var update = Builders<User>.Update.AddToSet(u => u.Wishlist, gameId);
            await _mongoDBService.Users.UpdateOneAsync(u => u.Id == userId, update);

            return Json(new { success = true, message = "Added to wishlist" });
        }

        // POST: Customer/RemoveFromWishlist
        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(string gameId)
        {
            if (!IsLoggedIn())
                return Json(new { success = false, message = "Please login first" });

            var userId = GetUserId();
            var update = Builders<User>.Update.Pull(u => u.Wishlist, gameId);
            await _mongoDBService.Users.UpdateOneAsync(u => u.Id == userId, update);

            return Json(new { success = true, message = "Removed from wishlist" });
        }

        // GET: Customer/ToggleWishlist/gameId (Fallback)
        public async Task<IActionResult> ToggleWishlist(string gameId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            var user = await _mongoDBService.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user.Wishlist != null && user.Wishlist.Contains(gameId))
            {
                var pull = Builders<User>.Update.Pull(u => u.Wishlist, gameId);
                await _mongoDBService.Users.UpdateOneAsync(u => u.Id == userId, pull);
                TempData["Success"] = "Removed from wishlist";
            }
            else
            {
                var push = Builders<User>.Update.AddToSet(u => u.Wishlist, gameId);
                await _mongoDBService.Users.UpdateOneAsync(u => u.Id == userId, push);
                TempData["Success"] = "Added to wishlist";
            }

            return RedirectToAction("GameDetail", new { id = gameId });
        }
    }
}

