using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieApp.Models;
using MovieApp.Services;

namespace MovieApp.Controllers
{
    [Route("api/movies")]
    public class MovieController:Controller
    {
        private readonly MovieService _movSer;
        private readonly UserService _userSer;
        public MovieController(MovieService movser,UserService userSer)
        {
            _movSer = movser;
            _userSer = userSer;
        }
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetMovies()
        {
            var movies = await _movSer.GetMovies();
            return Json(movies);
        }

        [HttpPost("addmovie")]
        [Authorize]
        public async Task<IActionResult> AddMovies([FromBody] Movie movie)
        {
            var user=HttpContext.User;
            if (user == null)
                return Unauthorized();
            var userrole=user.Claims.FirstOrDefault(x => x.Type.Equals("userroles"))?.Value;
            if (!(await _userSer.IsAdmin(userrole)))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(movie.Name))
            {
                return Ok(new { data = "Movie name is blank. Cant add Movie",success=0 });
            }
            var response =await _movSer.AddMovie(movie);
            return Ok(new { data = response.Item1,success=response.Item2 });
        }
        [HttpPost("updatemovie")]
        [Authorize]
        public async Task<IActionResult> UpdateMovies([FromBody] Movie movie)
        {
            var user = HttpContext.User;
            if (user == null)
                return Unauthorized();
            var userrole = user.Claims.FirstOrDefault(x => x.Type.Equals("userroles"))?.Value;
            if (!(await _userSer.IsAdmin(userrole)))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(movie.Name))
            {
                return Ok(new { data = "Movie name is blank. Cant Update Movie", success = 0 });
            }
            var response = await _movSer.UpdateMovie(movie);
            return Ok(new { data = response.Item1, success = response.Item2 });
        }
        [HttpPost("addrating")]
        [Authorize]
        public async Task<IActionResult> AddRating([FromBody]MovieRating rating)
        {
            var user = HttpContext.User;
            if (user == null)
                return Unauthorized();
            var userrole = user.Claims.FirstOrDefault(x => x.Type.Equals("userroles"))?.Value;
            var userid = user.Claims.FirstOrDefault(x => x.Type.Equals("userid"))?.Value;
            if (!(await _userSer.IsUser(userrole)) || string.IsNullOrWhiteSpace(userid))
                return Unauthorized();
            rating.UserID = Convert.ToInt32(userid);
            var response = await _movSer.AddRating(rating);
            return Ok(new { data = response.Item1, success = response.Item2 });
        }
    }
}
