using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace BookStoresWebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly BookStoresDBContext _context;
        private readonly JWTSettings _jwtsettings;

        public UsersController(BookStoresDBContext context, IOptions<JWTSettings> jwtsettings)
        {
            _context = context;
            _jwtsettings = jwtsettings.Value;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        //for getting User with email & password
        //GET: api/Users/GetAuthenticatedUser        
        [HttpGet("GetAuthenticatedUser")]
        public async Task<ActionResult<User>> GetAuthenticatedUser()
        {
            string emailAddress = HttpContext.User.Identity.Name;

            var authenticatedUser = await _context.Users
                                     .Where(user => user.EmailAddress == emailAddress)
                                     .FirstOrDefaultAsync();

            authenticatedUser.Password = null;


            if (authenticatedUser == null)
            {
                return NotFound();
            }

            return authenticatedUser;
        }

        //For Login
        //GET: api/Users/Login
        [HttpGet("Login")]
        public async Task<ActionResult<UserWithToken>> Login([FromBody] User user)
        {
            user = await _context.Users
                         .Include(u => u.Role)
                         .Where(u => u.EmailAddress == user.EmailAddress
                           && u.Password == user.Password)
                          .FirstOrDefaultAsync();

            UserWithToken userWithToken = null;

            if (user != null)
            {
                RefreshToken refreshToken = GenerateRefreshToken();
                user.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                userWithToken = new UserWithToken(user);
                userWithToken.RefreshToken = refreshToken.Token;
            }

            if (userWithToken == null)
            {
                return NotFound();

            }

            //sign your token here...
            //var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
            //var tokenDescriptor = new SecurityTokenDescriptor
            //{
            //    Subject = new ClaimsIdentity(new Claim[]
            //    {
            //            new Claim(ClaimTypes.Name, user.EmailAddress)
            //    }),

            //    Expires = DateTime.UtcNow.AddSeconds(60),
            //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
            //    SecurityAlgorithms.HmacSha256Signature)

            //};
            //var token = tokenHandler.CreateToken(tokenDescriptor);
            //userWithToken.AccessToken = tokenHandler.WriteToken(token);

            //userWithToken.AccessToken = GenerateAccessToken(user.UserId);

            return userWithToken;
        }

        //The method for Generating RefreshToken
        private RefreshToken GenerateRefreshToken()
        {
            RefreshToken refreshToken = new RefreshToken();

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
            }
            refreshToken.ExpiryDate = DateTime.UtcNow.AddMinutes(60);

            return refreshToken;
        }

        //Method for generating AccessToken
        //private string GenerateAccessToken(int userId)
        //{
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new Claim[]
        //        {
        //                new Claim(ClaimTypes.Name, user.EmailAddress)
        //        }),

        //        Expires = DateTime.UtcNow.AddSeconds(60),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
        //        SecurityAlgorithms.HmacSha256Signature)

        //    };
        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    userWithToken.AccessToken = tokenHandler.WriteToken(token);
        //}





    private bool ValidateRefreshToken(User user, string refreshToken)
        {

            RefreshToken refreshTokenUser = _context.RefreshTokens.Where(rt => rt.Token == refreshToken)
                                           .OrderByDescending(rt => rt.ExpiryDate)
                                           .FirstOrDefault();
            if (refreshTokenUser != null && refreshTokenUser.UserId == user.UserId
                && refreshTokenUser.ExpiryDate > DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }

        //private async Task<User> GetUserFromAccessToken(string accesstoken)
        //{
        //    try
        //    {

        //        var tokenHandler = new JwtSecurityTokenHandler();
        //        var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);

        //        var tokenValidationParameters = new TokenValidationParameters
        //        {
        //            ValidateIssuerSigningKey = true,
        //            IssuerSigningKey = new SymmetricSecurityKey(key),
        //            ValidateIssuer = false,
        //            ValidateAudience = false
        //        };

        //        SecurityToken securityToken;
        //        var principle = tokenHandler.ValidateToken(accesstoken, tokenValidationParameters, out securityToken);

        //        JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

        //        if(jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            var userId = principle.FindFirst(ClaimTypes.Name)?.Value;

        //            return await _context.Users.Include(u => u.Role)
        //                                 .Where(u => u.UserId == Convert.ToInt32(userId)).FirstOrDefaultAsync();
        //        }

        //    }
        //    catch (Exception)
        //    {

        //        return new User();
        //    }

        //    return new User();
        //}



        // PUT: api/Users/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
