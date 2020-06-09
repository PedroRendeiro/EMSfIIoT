using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using EMSfIIoT_API.Services;
using EMSfIIoT_API.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using SharedAPI;

namespace EMSfIIoT_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = (UserService)userService;
        }

        [HttpGet]
        [Authorize(Policy = "read")]
        public async Task<IActionResult> GetAll()
        {
            var administrators = await GraphApiConnector.UpdateAdministrators();

            if (!administrators.Contains(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var users = await _userService.GetAll();
            return Ok(users);
        }

        /// <summary>Create a new user</summary>
        [HttpPost]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> CreateUser([FromBody, BindRequired]UserDTO userDTO)
        {
            var administrators = await GraphApiConnector.UpdateAdministrators();

            if (!administrators.Contains(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            var user = await _userService.CreateUser(userDTO);
            
            return Ok(user);
        }
    }
}