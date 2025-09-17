using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize] // same auth as your real actions
public class DiagnosticsController : Controller
{
    [HttpGet("/diag/content")]
    public IActionResult ContentOnly() => Content("ok");

    [HttpGet("/diag/view")]
    public IActionResult ViewOnly() => View();

    [HttpGet("/__ctrl_probe")]
    public IActionResult CtrlProbe()
        => Content("Controller/action executed");

    [HttpGet("/__claims")]
    public IActionResult Claims()
    {
        var claims = User?.Claims
            .Select(c => new { c.Type, c.Value })
            .OrderBy(c => c.Type)
            .ToList() ?? new();
        return Json(new
        {
            IsAuthenticated = User?.Identity?.IsAuthenticated == true,
            Claims = claims
        });
    }
}
