using HomeServices.Data;
using HomeServices.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Identity Services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequiredLength = 4;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // مهمة جداً

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 4. الترتيب الصحيح للميدل وير
app.UseAuthentication(); // لازم قبل الـ Authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // مهمة جداً

// 5. الـ Seeding للكاشات (اختياري حسب كودك)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category { Name = "السباكة", Description = "خدمات تصليح السباكة" },
            new Category { Name = "الكهرباء", Description = "خدمات الكهرباء المنزلية" }
        );
        context.SaveChanges();
    }
}

app.Run();