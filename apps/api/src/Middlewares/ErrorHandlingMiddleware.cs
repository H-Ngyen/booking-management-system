using API.Exceptions;

namespace API.Middlewares;

public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = ex.Message, code = ex.Code });

            logger.LogWarning(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex ,ex.Message);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Something went wrong", code = "INTERNAL_ERROR" });
        }
    }
}