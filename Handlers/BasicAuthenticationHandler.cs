using BookStoresWebAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BookStoresWebAPI.Handlers
{
    
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly BookStoresDBContext _context;
        
        //Constructor for handling Basic Authentication
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            BookStoresDBContext context)
            : base(options, logger, encoder, clock)
        {
            _context = context;
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return (AuthenticateResult.Fail("Authorization Header was not found"));

            try
            {
                var authenticationHeaderValue = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var bytes = Convert.FromBase64String(authenticationHeaderValue.Parameter);
                string[] credentials = Encoding.UTF8.GetString(bytes).Split(":");
                string emailAddress = credentials[0];
                string password = credentials[1];

                User user = _context.Users.Where(user => user.EmailAddress == emailAddress && user.Password == password).FirstOrDefault();

                if (user == null)
                    AuthenticateResult.Fail("Invalid Email or Password");
                else
                {
                    //to verify identity claims
                    var claims = new[] { new Claim(ClaimTypes.Name, user.EmailAddress) };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }
            }
            catch (Exception)
            {

                return await Task.FromResult(AuthenticateResult.Fail("Error has Occured"));
            }

            

            return await Task.FromResult(AuthenticateResult.Fail(""));
        }
    }
}
