using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace ServiserOnline.Controllers;

[Authorize(Roles = "Admin")]
public class AuditTracesController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuditTracesController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(int? page) =>
        View(_db.AuditTrace
            .OrderByDescending(o => o.CreatedDate)
            .ToPagedList(page ?? 1, 100));

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.AuditTrace.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.AuditTrace.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.AuditTrace.FindAsync(id);
        _db.AuditTrace.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
