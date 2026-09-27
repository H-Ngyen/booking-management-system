using API.DTOs;
using API.Entities;
using API.Exceptions;
using API.Interfaces;
using API.Interfaces.Repositories;
using API.Interfaces.Services;
using AutoMapper;

namespace API.Services;

public class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IMapper mapper,
    IUserContext userContext) : IAuthService
{
    public async Task<string> Login(LoginRequest request)
    {
        User? user = await userRepository.GetUserByUserName(request.UserName)
            ?? throw new UnauthorizedException("Tên người dùng hoặc mật khẩu không hợp lệ");
        
        bool isVerify = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if(!isVerify) 
            throw new UnauthorizedException("Tên người dùng hoặc mật khẩu không hợp lệ");

        CurrentUser currentUser = mapper.Map<CurrentUser>(user);
        string token = tokenService.CreateToken(currentUser);

        return token;
    }

    public async Task<UserDto> Me()
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User? user = await userRepository.GetUserById(currentUser.Id) 
            ?? throw new NotFoundException("Không tìm thấy người dùng");

        UserDto result = mapper.Map<UserDto>(user); 
        return result; 
    }
}