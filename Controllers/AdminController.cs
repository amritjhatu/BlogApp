using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BlogWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlogWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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

//---------------------------------------------------------------------
        // GET: ManageUser/Details/username
        public async Task<IActionResult> Details(string username)
        {
            if (username == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: ManageUser/Edit/username
        public async Task<IActionResult> Edit(string username)
        {
            if (username == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: ManageUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string username, 
            [Bind("FirstName,LastName,isApproved,Id,UserName,"+
            "NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,"+
            "PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,"+
            "PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,"+
            "LockoutEnabled,AccessFailedCount")] User user)
        {
            if (username != user.UserName)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserName))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageUsers));
            }
            return View(user);
        }
        private bool UserExists(string username)
        {
            return _context.Users.Any(e => e.UserName == username);
        }
    }
}
