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
    public class UserService
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        public UserService(IOptions<AppSettings> appSettings, IMemoryCache cache)
        {
            _connectionString = appSettings.Value.ConnectionString;
            _cache = cache;
        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            using (var conn = GetConnection())
            {
                return await conn.QueryAsync<User>("select * from users").ConfigureAwait(false);
            }
        }
        public async Task<User> GetUser(string username,string pwd)
        {
            var users = await GetUsers();
            return users.FirstOrDefault(x => string.Equals(x.Username, username) && string.Equals(x.Pwd, pwd));
        }
        public async Task<User> GetUserByID(int userid)
        {
            var users = await GetUsers();
            return users.FirstOrDefault(x => x.ID==userid);
        }
        public async Task<IEnumerable<Role>> GetRoles()
        {
            return await _cache.GetOrCreateAsync("role", async c =>
            {
                c.SetAbsoluteExpiration(TimeSpan.FromHours(4));
                return await GetRolesFromDB();
            });
        }

        public async Task<(string,int)> AddUser(User user)
        {
            var users = await GetUsers();
            var extUser = users.FirstOrDefault(x=>string.Equals(x.Username,user.Username,StringComparison.InvariantCultureIgnoreCase));
            if (extUser != null)
                return ("Username already exists", 0);
            using(var conn = GetConnection())
            {
                var sql = @"insert into users(username,pwd) values(@username,@pwd)
                    declare @userid int,@roleid int
                    select @userid=id from users where username=@username
                    select @roleid=id from userroles where name='user'
                    insert into userrolemapping(roleid,userid) values(@roleid,@userid);
                    ";
                await conn.ExecuteAsync(sql, new { user.Username,user.Pwd}).ConfigureAwait(false);
                return ("User added successfully",1);
            }
        }

        public async Task<IEnumerable<Role>> GetRolesFromDB()
        {
            using (var conn = GetConnection())
            {
                return await conn.QueryAsync<Role>("select * from userroles").ConfigureAwait(false);
            }
        }
        public async Task<IEnumerable<UserRoleMapping>> GetUserRoleMapping()
        {
            using (var conn = GetConnection())
            {
                return await conn.QueryAsync<UserRoleMapping>("select * from userrolemapping").ConfigureAwait(false);
            }
        }
        public async Task<bool> IsAdmin(string rolesCsv)
        {
            var roles = await GetRoles();
            var adminid = roles.FirstOrDefault(x => string.Equals(x.Name,"admin", StringComparison.CurrentCultureIgnoreCase))?.ID;
            if (rolesCsv.Contains(adminid.ToString()))
                return true;
            return false;
        }
        public async Task<bool> IsUser(string rolesCsv)
        {
            var roles = await GetRoles();
            var adminid = roles.FirstOrDefault(x => string.Equals(x.Name, "user", StringComparison.CurrentCultureIgnoreCase))?.ID;
            if (rolesCsv.Contains(adminid.ToString()))
                return true;
            return false;
        }
    }
}
