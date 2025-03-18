using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using TagUpdater.Web.Models;
using TagUpdater.Web.Services;

namespace TagUpdater.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ResourceManagerService _resourceManagerService;
    private readonly ITokenAcquisition _tokenAcquisition;
    
    public HomeController(ResourceManagerService resourceManagerService, ITokenAcquisition tokenAcquisition)
    {
        _resourceManagerService = resourceManagerService;
        _tokenAcquisition = tokenAcquisition;
    }
    
    public IActionResult Index()
    {
        return View(new ResourceTagUpdateModel(){
            ResourceId = "resourceId",
            TagsInput = ""
        });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ResourceTagUpdateModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var tags = model.ParseTags();
        
        if (tags.Count == 0)
        {
            ModelState.AddModelError("TagsInput", "Please enter at least one valid tag in key=value format");
            return View(model);
        }
        
        try
        {
            var success = await _resourceManagerService.UpdateResourceTagsAsync(model.ResourceId, tags);
            
            if (success)
            {
                ViewBag.SuccessMessage = "Resource tags updated successfully!";
            }
            else
            {
                ViewBag.ErrorMessage = "Failed to update resource tags. Please check the resource ID and your permissions.";
            }
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            // Handle consent challenge
            return Challenge();
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error: {ex.Message}";
        }
        
        return View(model);
    }
    
    [AllowAnonymous]
    public IActionResult ConsentRequired()
    {
        return View();
    }
    
    [AllowAnonymous]
    public IActionResult RequestConsent()
    {
        // Trigger a consent request by challenging the user
        return Challenge();
    }
    
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string message)
    {
        var model = new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = message
        };
        return View(model);
    }
}