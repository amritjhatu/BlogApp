using Microsoft.AspNetCore.Mvc;
using BlogWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BlogWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Ganss.Xss;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;  // Inject UserManager

    public HomeController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var currentDate = DateTime.UtcNow;

        var articles = _context.Articles
            .Include(a => a.Contributor)  // Eager load the Contributor navigation property
            .Where(a => a.StartDate <= currentDate && a.EndDate >= currentDate)
            .OrderByDescending(a => a.CreateDate)
            .ToList();

        return View(articles);
    }

    public IActionResult Details(int id)
    {
        var article = _context.Articles
            .Include(a => a.Contributor)  // Eagerly load
            .FirstOrDefault(a => a.ArticleId == id);

        if (article == null)
        {
            return NotFound();
        }

        return View(article);
    }

    // Allow both Contributors and Admins to access this action
    [Authorize(Roles = "Contributor,Admin")]
    public IActionResult Create()
    {
        var model = new Article { Body = null }; // Initialize the Body property to null
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Contributor,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article article)
    {   
        if (User.Identity == null)
        {
            return Forbid();
        }
        article.ContributorUsername = User.Identity.Name;
        ModelState.Remove("ContributorUsername");  // Remove from validation if necessary
        
        if (ModelState.IsValid)
        {
            if (article.StartDate.HasValue && article.EndDate.HasValue && article.EndDate < article.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
                return View(article);
            }

            // Sanitize the article Body content to prevent XSS
            var sanitizer = new HtmlSanitizer();
            article.Body = sanitizer.Sanitize(article.Body ?? string.Empty);  // Fallback to empty string if Body is null
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            article.Contributor = user;
            article.ContributorUsername = user.UserName!;
            article.CreateDate = DateTime.UtcNow;

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        else
        {
            // Print out the model errors to see what might be invalid
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Error: {error.ErrorMessage}");
            }
        }

        return View(article);
    }

    [HttpGet]
    [Authorize(Roles = "Contributor,Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _context.Articles.FindAsync(id);

        if (article == null)
        {
            return NotFound();
        }

        // Check if the user identity is null
        if (User.Identity?.Name == null)
        {
            return Forbid();
        }

        // Ensure the user is either the contributor or an admin
        if (article.ContributorUsername != User.Identity.Name && !User.IsInRole("Admin"))
        {
            var user = await _userManager.GetUserAsync(User);
            var username = user?.UserName;
            var contributorUsername = article.ContributorUsername;
            Console.WriteLine(username);
            Console.WriteLine(contributorUsername);
            Console.WriteLine("User is not the contributor or an admin");
            return Forbid();
        }

        return View(article);
    }

    [HttpPost]
    [Authorize(Roles = "Contributor,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Article article)
    {
        if (id != article.ArticleId)
        {
            return NotFound();
        }

        var existingArticle = await _context.Articles
            .Include(a => a.Contributor)  // Eagerly load
            .FirstOrDefaultAsync(a => a.ArticleId == id);

        // Check if the article exists
        if (existingArticle == null)
        {
            return NotFound();
        }

        // Check if the user identity is null
        if (User.Identity?.Name == null)
        {
            return Forbid();
        }

        // Ensure the user is either the contributor or an admin
        if (existingArticle.Contributor?.UserName != User.Identity.Name && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Automatically set ContributorUsername to the logged-in user's name
        article.ContributorUsername = User.Identity.Name;  // Set on the passed article object
        Console.WriteLine(article.ContributorUsername);
        ModelState.Remove("ContributorUsername");  // Remove the ContributorUsername from the ModelState

        if (ModelState.IsValid)
        {
            // Validation for dates
            if (article.StartDate.HasValue && article.EndDate.HasValue && article.EndDate < article.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
                return View(article);
            }

            // Sanitize the article Body content to prevent XSS
            var sanitizer = new HtmlSanitizer();
            article.Body = sanitizer.Sanitize(article.Body ?? string.Empty);  // Fallback to empty string if Body is null

            // Update article properties
            existingArticle.Title = article.Title;
            existingArticle.Body = article.Body;
            existingArticle.StartDate = article.StartDate;
            existingArticle.EndDate = article.EndDate;

            // Save changes to database
            _context.Update(existingArticle);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        return View(article);
    }

    [HttpPost]
    [Authorize(Roles = "Contributor,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
