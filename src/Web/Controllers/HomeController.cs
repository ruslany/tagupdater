using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TagUpdater.Web.Models;
using TagUpdater.Web.Services;

namespace TagUpdater.Web.Controllers;

// Controllers
[Authorize]
public class HomeController(ResourceManagerService resourceManagerService) : Controller
{
    private readonly ResourceManagerService _resourceManagerService = resourceManagerService;

    public IActionResult Index()
    {
        return View(new ResourceTagUpdateModel(){ 
            ResourceId = "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{provider}/{resourceType}/{resourceName}",
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
        
        var success = await _resourceManagerService.UpdateResourceTagsAsync(model.ResourceId, tags);
        
        if (success)
        {
            ViewBag.SuccessMessage = "Resource tags updated successfully!";
        }
        else
        {
            ViewBag.ErrorMessage = "Failed to update resource tags. Please check the resource ID and your permissions.";
        }
        
        return View(model);
    }
    
    public IActionResult Privacy()
    {
        return View();
    }
    
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
