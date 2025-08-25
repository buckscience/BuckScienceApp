using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Web.Controllers
{
    public class PhotosController : Controller
    {
        public IActionResult Upload()
        {
            return View();
        }
    }
}