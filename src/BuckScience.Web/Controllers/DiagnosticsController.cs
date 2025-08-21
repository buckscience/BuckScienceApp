using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize] // same auth as your real actions
public class DiagnosticsController : Controller
{
    [HttpGet("/diag/content")]
    public IActionResult ContentOnly() => Content("ok");

    [HttpGet("/diag/view")]
    public IActionResult ViewOnly() => View();
}