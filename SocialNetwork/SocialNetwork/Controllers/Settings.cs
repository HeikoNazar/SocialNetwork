using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetwork.Controllers
{
    [Authorize]
    public class Settings : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
