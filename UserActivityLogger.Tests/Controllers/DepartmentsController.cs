using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Attributes;
using UserActivityLogger.Constants;

namespace UserActivityLogger.Tests.Controllers;

public class DepartmentsController : Controller
{
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(ILogger<DepartmentsController> logger)
    {
        _logger = logger;
    }



    [LogUserActivity(ConstPredefinedLogMessage.UserVisitedPage)]
    public IActionResult Index()
    {
        return View();
    }

    
    [LogUserActivity(ConstPredefinedLogMessage.UserVisitedPage)]
    public IActionResult Create()
    {
        return View();
    }


    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserCreatedResource)]
    public IActionResult CreateDept()
    {
        return Ok("Done");
    }


    [LogUserActivity(ConstPredefinedLogMessage.UserVisitedPage)]
    public IActionResult Edit(int id)
    {
        return View();
    }

    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserUpdatedResource)]
    public IActionResult EditDept(int id)
    {
        return Ok("Done");
    }


    [LogUserActivity(ConstPredefinedLogMessage.UserVisitedPage)]
    public IActionResult Delete(int id)
    {
        return View();
    }

    [HttpPost]
    [LogUserActivity(ConstPredefinedLogMessage.UserDeletedResource)]
    public IActionResult DeleteDept(int id)
    {
        return Ok("Done");
    }

}
