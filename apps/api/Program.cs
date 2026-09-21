using API.Extensions;
using API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.LoadEnv();

builder.AddPresentation();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if(app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();