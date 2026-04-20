using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;

namespace ServiserOnline.Controllers;

[Authorize]
public class BusinessAreasController : Controller
{
    private readonly ApplicationDbContext _db;
    public BusinessAreasController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.BusinessArea.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.BusinessArea.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name")] BusinessArea item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.BusinessArea.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.BusinessArea.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name")] BusinessArea item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.BusinessArea.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.BusinessArea.FindAsync(id);
        _db.BusinessArea.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class CaseContactTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    public CaseContactTypesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.CaseContactTypes.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.CaseContactTypes.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name")] CaseContactType item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.CaseContactTypes.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.CaseContactTypes.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name")] CaseContactType item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.CaseContactTypes.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.CaseContactTypes.FindAsync(id);
        _db.CaseContactTypes.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class GuaranteeTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    public GuaranteeTypesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.GuaranteeType.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.GuaranteeType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,TypeName")] GuaranteeType item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.GuaranteeType.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.GuaranteeType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,TypeName")] GuaranteeType item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.GuaranteeType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.GuaranteeType.FindAsync(id);
        _db.GuaranteeType.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class InterventionTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    public InterventionTypesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.InterventionType.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.InterventionType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name")] InterventionType item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.InterventionType.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.InterventionType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name")] InterventionType item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.InterventionType.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.InterventionType.FindAsync(id);
        _db.InterventionType.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class PaymentOptionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PaymentOptionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.PaymentOption.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.PaymentOption.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,OptionName")] PaymentOption item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.PaymentOption.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.PaymentOption.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,OptionName")] PaymentOption item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.PaymentOption.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.PaymentOption.FindAsync(id);
        _db.PaymentOption.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class ServisersController : Controller
{
    private readonly ApplicationDbContext _db;
    public ServisersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Serviser.ToListAsync());

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Serviser.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Surname,Telephone,Mobile,Email")] Serviser item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.Serviser.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Serviser.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name,Surname,Telephone,Mobile,Email")] Serviser item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Serviser.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.Serviser.FindAsync(id);
        _db.Serviser.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize(Roles = "Admin")]
public class AuditTracesController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuditTracesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.AuditTrace.OrderByDescending(o => o.CreatedDate).ToListAsync());

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
