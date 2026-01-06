using RetroVerseGaming.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();


// Register MongoDB Service
builder.Services.AddSingleton<MongoDBService>();

// Add Session untuk login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// COMMENT AUTHENTICATION SEMENTARA
// app.UseAuthentication();
// app.UseAuthorization();

app.UseRouting();
app.UseSession(); // PASTIKAN INI ADA

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
