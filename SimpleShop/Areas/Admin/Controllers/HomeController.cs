using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // 只有 Admin 角色才能存取
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}