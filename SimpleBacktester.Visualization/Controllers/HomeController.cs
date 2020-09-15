using Microsoft.AspNetCore.Mvc;

namespace SimpleBacktester.Visualization.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
