using API.DTOs;
using API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// api/v1/auth
public class AuthController(IAuthService authService) : BaseApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Login(LoginRequest request)
    {
        return await authService.Login(request);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        return await authService.Me();
    }
}