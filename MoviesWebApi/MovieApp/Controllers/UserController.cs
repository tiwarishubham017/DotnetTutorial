using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MovieApp.Models;
using MovieApp.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MovieApp.Controllers
{
    [Route("api/users")]
    public class UserController:Controller
    {
        private readonly UserService _userSer;
        public UserController(UserService userSer)
        {
            _userSer = userSer;
        }
        /// <summary>
        /// Getting token. Required for accessing api
        /// </summary>
        [HttpPost("gettoken")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            IActionResult response = Unauthorized();
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Pwd))
                return response;

            var dbUser = await _userSer.GetUser(user.Username,user.Pwd);
            if (dbUser != null)
            {
                var tokenString = GenerateJSONWebToken(dbUser);
                response = Ok(new { token = tokenString });
            }

            return response;
        }
        /// <summary>
        /// Adding users to the system
        /// </summary>
        [HttpPost("adduser")]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Pwd))
                return Ok(new { data = "Username or Password is blank",success=0 });

            var response = await _userSer.AddUser(user);
            return Ok(new { data = response.Item1, success = response.Item2 });
        }
        private string GenerateJSONWebToken(User user)
        {
            var roles =  _userSer.GetUserRoleMapping().Result;
            var userRoles = roles.Where(x => x.UserID == user.ID);
            string s = "";
            foreach( var role in userRoles)
            {
                s = s + role.RoleID +",";
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("123456789ABCDEFGHIJHK!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
        new Claim("userroles", s),
        new Claim("userid",user.ID.ToString())
        };
            var token = new JwtSecurityToken("subhendu.com",
              "subhendu.com",
              claims,
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
