using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiserOnline.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Cases");
    }

    public IActionResult About()
    {
        ViewBag.Message = "Your application description page.";
        return View();
    }

    public IActionResult Contact()
    {
        ViewBag.Message = "Your contact page.";
        return View();
    }
}
