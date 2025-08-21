using api.Data;
using api.Services.Enrichment;
using api.Services.RickandMorty;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<IRickandMortyClient, RickandMortyClient>(client =>
{
    client.BaseAddress = new Uri("https://rickandmortyapi.com/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add services to the container.
builder.Services.AddScoped<IEnrichmentService, EnrichmentService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

app.MapControllers();

app.UseStaticFiles();

app.Run();