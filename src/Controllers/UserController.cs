using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using projekt.src.Controllers.Dto.UserDto;
using projekt.src.Data;
using projekt.src.Exceptions;
using projekt.src.Models.Users;

namespace projekt.src.Controllers;

[ApiController]
[Route("[controller]")]
[EnableCors("AllowAnyOrigin")]
public class UsersController : ControllerBase
{
    private readonly ApiDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly UserService _userService;

    public UsersController(
        ApiDbContext dbContext,
        IConfiguration configuration,
        UserService userService
    )
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _userService = userService;
    }

    [HttpPost("register", Name="Create new user")]
    public async Task<IActionResult>Signup([FromBody] RegisterDto userDto)
    {
        var email = new Email(userDto.Email);
        var name = new Name(userDto.Name);
        var lastname = new LastName(userDto.Lastname);
        var password = new Password(userDto.Password);
        var confirmPassword = new Password(userDto.ConfirmPassword);
        var address = string.IsNullOrEmpty(userDto.Address) ? null : new Address(userDto.Address);
        var location = string.IsNullOrEmpty(userDto.Location) ? null : new Address(userDto.Location);
        var postcode = string.IsNullOrEmpty(userDto.PostCode) ? null : new PostCode(userDto.PostCode);
        var phone = string.IsNullOrEmpty(userDto.Phone) ? null : new Phone(userDto.Phone);

        var exists = await _dbContext.Users.AnyAsync(x=>x.Email == email);
        if(exists) throw new CustomException("User with this email already exists.");

        var hashedPassword = new Password(password.CalculateMD5Hash(password.Value));
        
        var user = Models.Users.User.NewUser(
            email,
            name,
            lastname,
            hashedPassword,
            address,
            location,
            postcode,
            phone,
            DateTime.UtcNow
        );

        _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return Ok(user);
    }

    [HttpPost("login", Name="Login")]
    public async Task<IActionResult> Login(LoginDto userDto){
        var email = new Email(userDto.Email);
        var password = new Password(userDto.Password);

        var hashedPassword = new Password(password.CalculateMD5Hash(password.Value.Trim()));

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == email && x.Password == hashedPassword);

        if(user != null){
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.Id.Value.ToString()),
                new Claim("Email", user.Email.Value.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.MaxValue,
                signingCredentials: signIn
            );

            string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(tokenValue);   
        }
        return Unauthorized();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(){
        var user = await _userService.CurrentUser(User);
        return Ok(user);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetById([FromQuery] Guid userId){
        var id = new UserId(userId);
        var user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.Id == id);
        if(user is null) throw new CustomException("User not found.");
        return Ok(user);
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromBody] bool confirm){
        if(!confirm) throw new CustomException("Deleting account canceled.");

        var user = await _userService.CurrentUser(User);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return Ok("Account deleted successfully.");
    }
}


