using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EMSfIIoT_API.DbContexts;
using EMSfIIoT_API.Entities;
using EMSfIIoT_API.Helpers;

namespace EMSfIIoT_API.Services
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
        Task<IEnumerable<User>> GetAll();
    }

    public class UserService : IUserService
    {
        private readonly ApiDbContext _context;

        public UserService(ApiDbContext context)
        {
            _context = context;
        }
        
        /*
        private List<User> _users = new List<User>
        {
            new User { Id = 1, FirstName = "Pedro", LastName = "Rendeiro", Username = "NodeRed", Password = "T>H<BdJ(M(r8Cusb" },
            new User { Id = 2, FirstName = "Pedro", LastName = "Rendeiro", Username = "PedroRendeiro", Password = "WfgYsf&5657" },
            new User { Id = 3, FirstName = "Bosch", LastName = "Connection", Username = "BoschIoTSuite", Password = "eJSwrgC6X8QTnd9EffJgyqUqdVaxrNL8" }
        };
        */

        public async Task<User> Authenticate(string username, string password)
        {
            User user = _context.Users.SingleOrDefault(x => x.Username == username);

            byte[] passwordHash = HashValue(password + user.Salt.ToString().ToUpper());            

            var result = await Task.Run(() => user.PasswordHash.SequenceEqual(passwordHash));

            // return null if user not found
            if (!result)
                return null;

            // authentication successful so return user details without password
            return user.WithoutPassword();
        }

        public async Task<User> CreateUser(UserDTO userDto)
        {
            Guid salt = Guid.NewGuid();

            User user = new User
            {
                Username = userDto.Username,
                Salt = salt,
                PasswordHash = HashValue(userDto.Password + salt.ToString().ToUpper())
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.WithoutPassword();
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await Task.Run(() => _context.Users.AsEnumerable().WithoutPasswords());
        }

        public static byte[] HashValue(string s)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(s);

            var sha1 = SHA512.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);
            sha1.Clear();

            return hashBytes;
        }
    }
}