using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMSfIIoT_API.Entities
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        
        [Required]
        public string Username { get; set; }
        
        public byte[] PasswordHash { get; set; }
        
        public Guid? Salt { get; set; }
    }

    public class UserDTO
    {

        [Required]
        public string Username { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class AppUser
    {
        public string Username { get; set; }
        [Required]
        public string DisplayName { get; set; }
        [DataType(DataType.EmailAddress), Required]
        public string EmailAddress { get; set; }
    }
}