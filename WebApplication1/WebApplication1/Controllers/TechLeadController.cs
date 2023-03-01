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
using WebApplication1.Models.Dto;
using System.Security.Cryptography;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechLeadController : ControllerBase
    {
        private readonly TechLeadContext _techLeadContext;
        public TechLeadController(TechLeadContext techLeadContext)
        {
            _techLeadContext = techLeadContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TechLead>>> GetTechLeads()
        {
            if (_techLeadContext.TechLeads == null)
            {
                return NotFound();
            }
            return await _techLeadContext.TechLeads.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TechLead>> GetTechLead(int id)
        {
            if (_techLeadContext.TechLeads == null)
            {
                return NotFound();
            }
            var techLead = await _techLeadContext.TechLeads.FindAsync(id);
            if (techLead == null)
            {
                return NotFound();
            }
            return techLead;
        }

        [HttpPost]

        public async Task<IActionResult> AddTechLead([FromBody] TechLead techLeadObj)
        {
            if (techLeadObj == null)
            {
                return BadRequest();
            }

            //Check username
            if (await CheckUserNameExistAsync(techLeadObj.UserName))
            {
                return BadRequest(new { Message = "Username already exists!" });
            }


            //Check email
            if (await CheckEmailExistAsync(techLeadObj.Email))
            {
                return BadRequest(new { Message = "Email already exists!" });
            }


            //Check password strength
            var pass = CheckPasswordStrength(techLeadObj.Password);
            if (!string.IsNullOrEmpty(pass))
            {
                return BadRequest(new { Message = pass.ToString() });
            }

            techLeadObj.Password = PasswordHasher.HashPassword(techLeadObj.Password);
            techLeadObj.Token = "";
            await _techLeadContext.TechLeads.AddAsync(techLeadObj);
            await _techLeadContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "Customer added successfully!"
            });
        }
        private Task<bool> CheckUserNameExistAsync(string username)
            => _techLeadContext.TechLeads.AnyAsync(x => x.UserName == username);

        private Task<bool> CheckEmailExistAsync(string email)
            => _techLeadContext.TechLeads.AnyAsync(x => x.Email == email);

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

        public async Task<ActionResult> PutTechLead(int id, TechLead techLead)
        {
            if (id != techLead.ID)
            {
                return BadRequest();
            }
            _techLeadContext.Entry(techLead).State = EntityState.Modified;
            try
            {
                await _techLeadContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTechLead(int id)
        {
            if (_techLeadContext.TechLeads == null)
            {
                return NotFound();
            }
            var techLead = await _techLeadContext.TechLeads.FindAsync(id);
            if (techLead == null)
            {
                return NotFound();
            }
            _techLeadContext.TechLeads.Remove(techLead);
            await _techLeadContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("authenticate")]

        public async Task<IActionResult> Authenticate([FromBody] TechLead techLeadobj)
        {
            if (techLeadobj == null)
            {
                return BadRequest();
            }
            var techLead = await _techLeadContext.TechLeads
                .FirstOrDefaultAsync(x => x.UserName == techLeadobj.UserName);
            if (techLead == null)
            {
                return NotFound(new { Message = "User not found!" });
            }
            if (!PasswordHasher.VerifyPassword(techLeadobj.Password, techLead.Password))
            {
                return BadRequest(new { Message = "Password is incorrect." });
            }

            techLead.Token = CreateJwt(techLead);
            var newAccessToken = techLead.Token;
            var newRefreshToken = CreateRefreshToken();
            techLead.RefreshToken = newRefreshToken;
            techLead.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
            await _techLeadContext.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        private string CreateJwt(TechLead techLead)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, $"{techLead.UserName}")
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

            var tokenInUser = _techLeadContext.TechLeads
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
            var user = await _techLeadContext.TechLeads.FirstOrDefaultAsync(u => u.UserName == username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return BadRequest("Invalid Request");
            }
            var newAccessToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            await _techLeadContext.SaveChangesAsync();
            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            });
        }
    }
}
