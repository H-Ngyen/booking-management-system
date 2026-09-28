using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Helpers;

public class AppendAuthorizeToSummaryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Lấy Authorize từ method
        var methodAttributes = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>();

        // Lấy Authorize từ controller
        var controllerAttributes = context.MethodInfo.DeclaringType!
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>();

        var authorizeAttributes = methodAttributes
            .Union(controllerAttributes)
            .Distinct();

        if (authorizeAttributes.Any())
        {
            var roles = authorizeAttributes
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .Select(a => a.Roles)
                .ToList();

            if (roles.Any())
            {
                var roleInfo = $"<b>[Roles: {string.Join(", ", roles)}]</b>";
                operation.Description = $"{roleInfo}<br/>{operation.Description}";
            }
            else
            {
                operation.Description = $"<b>[Authorized User]</b><br/>{operation.Description}";
            }
        }
    }
}