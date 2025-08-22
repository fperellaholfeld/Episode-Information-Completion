using api.Data;
using api.Services;
using api.Entities;
using api.Services.Background;
using api.Services.Enrichment;
using api.Services.RickandMorty;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddScoped<IEnrichmentService, EnrichmentService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IJobQueue>(new InMemoryJobQueue(capacity: 100));
builder.Services.AddHostedService<UploadProcessingService>();
builder.Services.AddScoped<IEnrichmentService, EnrichmentService>();
builder.Services.AddHttpClient<IRickandMortyClient, RickandMortyClient>(client =>
{
    client.BaseAddress = new Uri("https://rickandmortyapi.com/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Seed placeholder entities (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Locations.Any(l => l.Id == 0))
    {
        db.Locations.Add(new Location { Id = 0, Name = "unknown", Type = "unknown", Dimension = "unknown" });
        db.SaveChanges();
    }
}

app.MapControllers();

app.UseStaticFiles();

app.Run();