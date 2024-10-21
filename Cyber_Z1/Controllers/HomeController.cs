using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cyber_Z1.Models;

namespace Cyber_Z1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
        return View();
    }

    public IActionResult Privacy()
    {
        HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}