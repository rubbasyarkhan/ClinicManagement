﻿using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
