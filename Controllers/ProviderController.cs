using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Controllers
{
    public class ProviderController : Controller
    {
        // الصفحة الرئيسية لمقدم الخدمة
        public IActionResult Dashboard()
        {
            return View();
        }

        // صفحة الطلبات المتاحة
        public IActionResult AvailableRequests()
        {
            return View();
        }

        // صفحة تاريخ العمليات
        public IActionResult History()
        {
            return View();
        }
    }
}