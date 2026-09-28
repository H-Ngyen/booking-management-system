using API.Data;
using API.Extensions;
using API.Middlewares;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.LoadEnv();

builder.AddPresentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS policy "web" (registered in SignalRExtensions): order matters, keep visible here.
app.UseCors("web");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapSignalRHubs();

app.Run();

public partial class Program { }
