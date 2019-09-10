using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MovieApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MovieApp.Services
{
    public class MovieService
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        public MovieService(IOptions<AppSettings> appSettings, IMemoryCache cache)
        {
            _connectionString = appSettings.Value.ConnectionString;
            _cache = cache;
        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<IEnumerable<Movie>> GetMovies()
        {
            using (var conn = GetConnection())
            {
                var movies= await conn.QueryAsync<Movie>("select * from Movies").ConfigureAwait(false);
                var categories = await GetCategories();
                foreach( var movie in movies)
                {
                    var cName = categories.FirstOrDefault(x => x.ID == movie.Category)?.Name;
                    movie.CategoryName = cName;
                }
                return movies;
            }
        }
        public async Task<IEnumerable<Category>> GetCategories()
        {
            return await _cache.GetOrCreateAsync("cateory", async c =>
            {
                c.SetAbsoluteExpiration(TimeSpan.FromHours(4));
                return await GetCategoriesFromDB();
            });
        }

        public async Task<(string,int)> AddMovie(Movie movie)
        {
            var movies = await GetMovies();
            var extMovie = movies.FirstOrDefault(x => string.Equals(x.Name, movie.Name));
            if (extMovie != null)
                return ("A movie already exists with this name",0);
            var sql = "INSERT into movies(name,title,Thumbnail,Category,ReleaseYear) values(@name,@title,@thumbnail,@category,@Releaseyear)";
            using (var conn = GetConnection())
            {
                await conn.ExecuteAsync(sql, new { movie.Name,movie.Title,movie.Thumbnail,movie.Category,movie.ReleaseYear}).ConfigureAwait(false);
                return ("Movie added successfully",1);
            }
        }

        public async Task<IEnumerable<Category>> GetCategoriesFromDB()
        {
            using (var conn = GetConnection())
            {
                return await conn.QueryAsync<Category>("select * from category").ConfigureAwait(false);
            }
        }
        public async Task<IEnumerable<MovieRating>> GetMovieRatings()
        {
            using (var conn = GetConnection())
            {
                return await conn.QueryAsync<MovieRating>("select * from movierating").ConfigureAwait(false);
            }
        }

        public async Task<(string, int)> UpdateMovie(Movie movie)
        {
            var movies = await GetMovies();
            var extMovie = movies.FirstOrDefault(x =>x.ID==movie.ID);
            if (extMovie == null)
                return ("Movie does not exists.", 0);
            var sql = "update movies set name=@name,title=@title,Thumbnail=@thumbnail,Category=@category,ReleaseYear=@Releaseyear where id=@id";
            using (var conn = GetConnection())
            {
                await conn.ExecuteAsync(sql, new { movie.Name, movie.Title, movie.Thumbnail, movie.Category, movie.ReleaseYear,movie.ID }).ConfigureAwait(false);
                return ("Movie Updated successfully", 1);
            }
        }

        public async Task<(string, int)> AddRating(MovieRating rating)
        {
            var ratings = await GetMovieRatings();
            var ratingsMovie = ratings.Where(x => x.MovieID == rating.MovieID).ToList();
            var userrating = ratingsMovie.FirstOrDefault(x => x.UserID == rating.UserID);
            if (userrating != null)
            {
                var sql = "update movierating set userrating=@userrating where id=@id";
                using (var conn = GetConnection())
                {
                    await conn.ExecuteAsync(sql, new { userrating.ID, rating.UserRating }).ConfigureAwait(false);
                    //return ("Rating added successfully", 1);
                }
            }
            else
            {
                var sql = "insert into movierating(userid,movieid,userrating) values(@userid,@movieid,@userrating)";
                using (var conn = GetConnection())
                {
                    await conn.ExecuteAsync(sql, new { rating.UserRating, rating.UserID, rating.MovieID }).ConfigureAwait(false);
                    //return ("Rating Updated successfully", 1);
                }
            }
            var updatedRatings = await GetMovieRatings();
            var newMovieRatings = updatedRatings.Where(x => x.MovieID == rating.MovieID);
            if (newMovieRatings.Count() > 0)
            {
                var sum = 0;
                foreach (var r in newMovieRatings)
                {
                    sum = sum + r.UserRating;
                }
                double avg = (double)sum / newMovieRatings.Count();
                using (var conn = GetConnection())
                {
                    var sql = "update movies set AverageRating=@avg where id=@movieid";
                    await conn.ExecuteAsync(sql, new { avg,rating.MovieID }).ConfigureAwait(false);
                    //return ("Rating Updated successfully", 1);
                }
            }
            return ("Rating Updated successfully", 1);
        }
    }
}
