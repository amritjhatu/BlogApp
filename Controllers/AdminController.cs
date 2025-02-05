using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BlogWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // Action to list users and their roles
        public async Task<IActionResult> ManageUsers()
        {
            // Get all users
            var users = _userManager.Users.ToList();
            
            // Create a list to store the view model instances
            var userRoleViewModels = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                // Get the roles for each user
                var roles = await _userManager.GetRolesAsync(user);
                
                // Add user info and roles to the view model list
                userRoleViewModels.Add(new UserRoleViewModel
                {
                    UserName = user.UserName,
                    Roles = roles.ToList()
                });
            }

            return View(userRoleViewModels);
        }

        // Action to update user roles (toggle Contributor role)
        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(string userName, bool isContributor)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user != null)
            {
                if (isContributor)
                {
                    if (!await _userManager.IsInRoleAsync(user, "Contributor"))
                    {
                        await _userManager.AddToRoleAsync(user, "Contributor");
                    }
                }
                else
                {
                    if (await _userManager.IsInRoleAsync(user, "Contributor"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Contributor");
                    }
                }
            }

            return RedirectToAction(nameof(ManageUsers));
        }
    

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user); // Soft delete is an option also

            if (!result.Succeeded)
            {
                return BadRequest("An error occurred while banning the user.");
            }

            return Ok(); // Return a success status
        }
    }
}
