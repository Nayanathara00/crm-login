using WebApplication1.Helpers;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using WebApplication1.Models.Dto;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerContext _customerContext;
        public CustomerController(CustomerContext customerContext)
        {
            _customerContext = customerContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            if (_customerContext.Customers == null)
            {
                return NotFound();
            }
            return await _customerContext.Customers.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            if (_customerContext.Customers == null)
            {
                return NotFound();
            }
            var customer = await _customerContext.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return customer;
        }

        [HttpPost]

        public async Task<IActionResult> RegisterUser([FromBody] Customer customerObj)
        {
            if (customerObj == null)
            {
                return BadRequest();
            }

            //Check username
            if (await CheckUserNameExistAsync(customerObj.UserName))
            {
                return BadRequest(new { Message = "Username already exists!" });
            }


            //Check email
            if (await CheckEmailExistAsync(customerObj.Email))
            {
                return BadRequest(new { Message = "Email already exists!" });
            }


            //Check password strength
            var pass = CheckPasswordStrength(customerObj.Password);
            if (!string.IsNullOrEmpty(pass))
            {
                return BadRequest(new { Message = pass.ToString() });
            }

            customerObj.Password = PasswordHasher.HashPassword(customerObj.Password);
            customerObj.Token = "";
            await _customerContext.Customers.AddAsync(customerObj);
            await _customerContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "Customer added successfully!"
            });
        }
        private Task<bool> CheckUserNameExistAsync(string username)
            => _customerContext.Customers.AnyAsync(x => x.UserName == username);

        private Task<bool> CheckEmailExistAsync(string email)
            => _customerContext.Customers.AnyAsync(x => x.Email == email);

        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();
            if (password.Length < 8)
            {
                sb.Append("Minimum password length should be 8." + Environment.NewLine);
            }
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]")
                && Regex.IsMatch(password, "[0-9]")))
            {
                sb.Append("Password should be Alphanumeric." + Environment.NewLine);
            }
            if (!Regex.IsMatch(password, "[<,>,@,!,#,$,%,^,&,8,(,),_,+,\\[,\\],{,},?,:,;,|,',\\,.,/,~,`,-,=]"))
            {
                sb.Append("Password should contain special chars." + Environment.NewLine);
            }
            return sb.ToString();
        }

        [HttpPut("{id}")]

        public async Task<ActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.ID)
            {
                return BadRequest();
            }
            _customerContext.Entry(customer).State = EntityState.Modified;
            try
            {
                await _customerContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            if (_customerContext.Customers == null)
            {
                return NotFound();
            }
            var customer = await _customerContext.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            _customerContext.Customers.Remove(customer);
            await _customerContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("authenticate")]

        public async Task<IActionResult> Authenticate([FromBody] Customer customerObj)
        {
            if (customerObj == null)
            {
                return BadRequest();
            }
            var customer = await _customerContext.Customers
                .FirstOrDefaultAsync(x => x.UserName == customerObj.UserName);
            if (customer == null)
            {
                return NotFound(new { Message = "User not found!" });
            }
            if (!PasswordHasher.VerifyPassword(customerObj.Password, customer.Password))
            {
                return BadRequest(new { Message = "Password is incorrect." });
            }

            customer.Token = CreateJwt(customer);
            var newAccessToken = customer.Token;
            var newRefreshToken = CreateRefreshToken();
            customer.RefreshToken = newRefreshToken;
            customer.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
            await _customerContext.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {

                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        private string CreateJwt(Customer customer)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, $"{customer.UserName}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        private string CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            var tokenInUser = _customerContext.Customers
                .Any(a => a.RefreshToken == refreshToken);
            if (tokenInUser)
            {
                return CreateRefreshToken();
            }
            return refreshToken;
        }

        private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principle = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("This is invalid token.");
            }
            return principle;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenApiDto tokenApiDto)
        {
            if (tokenApiDto is null)
            {
                return BadRequest("Invalid Client Requset");
            }
            string accessToken = tokenApiDto.AccessToken;
            string refreshToken = tokenApiDto.RefreshToken;
            var principle = GetPrincipleFromExpiredToken(accessToken);
            var username = principle.Identity.Name;
            var user = await _customerContext.Customers.FirstOrDefaultAsync(u => u.UserName == username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return BadRequest("Invalid Request");
            }
            var newAccessToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            await _customerContext.SaveChangesAsync();
            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            });
        }
    }
}


