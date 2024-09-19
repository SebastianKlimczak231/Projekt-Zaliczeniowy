var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

var builder2 = WebApplication.CreateBuilder(args);

// Ustawienie portu HTTPS
builder.WebHost.UseUrls("https://localhost:5199");

builder.Services.AddControllersWithViews();

var app2 = builder.Build();

if (!app2.Environment.IsDevelopment())
{
    app2.UseExceptionHandler("/Home/Error");
    app2.UseHsts();
}

app2.UseHttpsRedirection();
app2.UseStaticFiles();

app2.UseRouting();

app2.UseAuthorization();

app2.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app2.Run();
