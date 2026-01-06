using Microsoft.AspNetCore.Mvc;
using RetroVerseGaming.Models;
using RetroVerseGaming.Services;
using MongoDB.Driver;

namespace RetroVerseGaming.Controllers
{
    public class GamesController : Controller
    {
        private readonly MongoDBService _mongoDBService;

        public GamesController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        // GET: Games (List all games)
        public async Task<IActionResult> Index()
        {
            var games = await _mongoDBService.Games.Find(_ => true).ToListAsync();
            return View(games);
        }

        // GET: Games/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Games/Create
        [HttpPost]
        public async Task<IActionResult> Create(Game game)
        {
            game.CreatedAt = DateTime.Now;
            await _mongoDBService.Games.InsertOneAsync(game);
            return RedirectToAction("Index");
        }

        // GET: Games/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var game = await _mongoDBService.Games
                .Find(g => g.Id == id)
                .FirstOrDefaultAsync();
            return View(game);
        }

        // POST: Games/Edit/id
        [HttpPost]
        public async Task<IActionResult> Edit(string id, Game game)
        {
            var filter = Builders<Game>.Filter.Eq(g => g.Id, id);
            await _mongoDBService.Games.ReplaceOneAsync(filter, game);
            return RedirectToAction("Index");
        }

        // GET: Games/Delete/id
        public async Task<IActionResult> Delete(string id)
        {
            var filter = Builders<Game>.Filter.Eq(g => g.Id, id);
            await _mongoDBService.Games.DeleteOneAsync(filter);
            return RedirectToAction("Index");
        }

        // GET: Games/Details/id
        public async Task<IActionResult> Details(string id)
        {
            var game = await _mongoDBService.Games
                .Find(g => g.Id == id)
                .FirstOrDefaultAsync();
            return View(game);
        }
    }
}