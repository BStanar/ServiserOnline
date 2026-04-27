using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;

namespace ServiserOnline.Controllers;

[Authorize]
public class AcceptCaseController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AcceptCaseController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Yellow → Orange: set planned date and accept
    public IActionResult Index(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)
            .SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Yellow) return RedirectToAction("Index", "Cases");

        ViewBag.ContinuedFromCase = new SelectList(
            _db.Case.Where(x => x.ID != CaseID && x.Client.ID == c.Client.ID && x.CaseStatus == CaseStatus.LightGreen)
                .OrderByDescending(x => x.DateTimeCaseOpened), "ID", "CaseServisNumber") ?? new SelectList(Enumerable.Empty<object>());
        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.Client)
            .SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Yellow) return RedirectToAction("Index", "Cases");

        if (string.IsNullOrEmpty(collection["DateTimePlanned"]))
            return AddCaseErrorView(c, "Planirani datum je obavezan");

        try { c.DateTimePlanned = DateTime.Parse(collection["DateTimePlanned"]); }
        catch { return AddCaseErrorView(c, "Planirani datum nije u redu"); }

        if (!string.IsNullOrEmpty(collection["ContinuedFromCase"]))
        {
            try { c.ContinuedFromCase = _db.Case.Single(l => l.ID == Guid.Parse(collection["ContinuedFromCase"])); }
            catch { return AddCaseErrorView(c, "Prethodni slucaj nije u redu"); }
        }

        c.CaseStatus = CaseStatus.Orange;
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Cases");
    }

    // Orange / LightGreen → Green or LightGreen: fill in the service report
    public IActionResult OnSite(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)
            .SingleOrDefault();

        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Orange && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Index", "Cases");

        if (string.IsNullOrEmpty(c.CaseServisNumber))
        {
            int maxAutoInc = (_db.Case.Any() ? _db.Case.Max(p => p.AutoIncrement) : 0) + 1;
            string maxStr = maxAutoInc.ToString();
            string year = maxStr.Length >= 4 ? maxStr.Substring(0, 4) : DateTime.Now.Year.ToString();
            string number = maxStr.Length > 4 ? maxStr.Substring(4) : "001";

            if (DateTime.Parse(year + "-01-01").Year < DateTime.Now.Year)
            {
                year = DateTime.Now.Year.ToString();
                number = "001";
                c.AutoIncrement = int.Parse(year + number);
            }

            int.TryParse(number, out int intNum);
            int.TryParse(year + number, out int yearNum);
            c.AutoIncrement = yearNum;
            c.CaseServisNumber = year + "-" + intNum.ToString("000");
        }

        c.DateTimeServiced = DateTime.Now;

        ViewBag.ContinuedFromCase = new SelectList(
            _db.Case.Where(x => x.ID != CaseID && x.Client.ID == c.Client.ID && x.CaseStatus == CaseStatus.LightGreen)
                .OrderByDescending(x => x.DateTimeCaseOpened), "ID", "CaseServisNumber") ?? new SelectList(Enumerable.Empty<object>());
        ViewBag.Attending = new SelectList(
            _db.ContactPersonClients.Where(x => x.Client.ID == c.Client.ID).OrderBy(x => x.Surname),
            "FullName", "FullName", c.AttendignPerson);
        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OnSite(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id).Include(ca => ca.Client).SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Orange && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Index", "Cases");

        c.CaseServisNumber = collection["CaseServisNumber"];
        c.AttendignPerson = collection["Attending"];
        c.ServiceDescription = collection["ServiceDescription"];
        c.SInterventionDescription = collection["SInterventionDescription"];
        c.NotFinishedDescription = collection["NotFinishedDescription"];
        c.PaymentInstruction = collection["PaymentInstruction"];
        c.ContractNo = collection["ContractNo"];

        try { c.DateTimeServiced = DateTime.Parse(collection["DateTimeServiced"]); }
        catch { return AddCaseErrorView(c, "Datum intervencije nije u redu"); }

        try { c.AutoIncrement = int.Parse(collection["AutoIncrement"]); }
        catch { return AddCaseErrorView(c, "Auto Inc nije u redu"); }

        try { c.HoursOfTravel = int.Parse(collection["HoursOfTravel"]); }
        catch { return AddCaseErrorView(c, "Sati putovanja nisu u redu"); }

        try { c.HoursOfWork = int.Parse(collection["HoursOfWork"]); }
        catch { return AddCaseErrorView(c, "Sati rada nisu u redu"); }

        if (!string.IsNullOrEmpty(collection["ContinuedFromCase"]))
        {
            try { c.ContinuedFromCase = _db.Case.Single(l => l.ID == Guid.Parse(collection["ContinuedFromCase"])); }
            catch { return AddCaseErrorView(c, "Prethodni slucaj nije u redu"); }
        }

        try
        {
            string user = User.Identity.Name;
            var dbUser = _db.Serviser.SingleOrDefault(u => u.Email == user);
            if (dbUser?.Name != null && dbUser?.Surname != null)
                c.ServicePerson = dbUser.Name + " " + dbUser.Surname;
        }
        catch { return AddCaseErrorView(c, "Serviser nije u redu"); }

        if (collection["Finished"] == "false")
        {
            c.CaseStatus = CaseStatus.LightGreen;
            c.Finished = false;
        }
        else
        {
            c.CaseStatus = CaseStatus.Green;
            c.Finished = true;
        }

        Enum.TryParse<PayWhen>(collection["PayWhen"], out var payWhen);
        c.PayWhen = payWhen;

        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Cases");
    }

    // Green / LightGreen / LightBlue: view and print the report
    public async Task<IActionResult> Print(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.Locations)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model).ThenInclude(m => m.Manufacturer)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model).ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
        if (c == null) return NotFound();

        c.DateTimeOfReport = DateTime.Now;
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return View(c);
    }

    // Green → LightBlue: mark as invoiced
    public IActionResult Invoice(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model).ThenInclude(m => m.Manufacturer)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model).ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Green) return RedirectToAction("Index", "Cases");

        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Invoice(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.Client)
            .SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Green) return RedirectToAction("Index", "Cases");

        c.ContractNo = collection["ContractNo"];
        c.CaseStatus = CaseStatus.LightBlue;
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Cases");
    }
    private IActionResult AddCaseErrorView(Case c, string v)
    {
        ViewBag.ContinuedFromCase = new SelectList(
            _db.Case.Where(x => x.ID != c.ID && x.Client.ID == c.Client.ID && x.CaseStatus == CaseStatus.LightGreen)
                .OrderByDescending(x => x.DateTimeCaseOpened), "ID", "CaseServisNumber") ?? new SelectList(Enumerable.Empty<object>());
        ViewBag.Attending = new SelectList(
            _db.ContactPersonClients.Where(x => x.Client.ID == c.Client.ID).OrderBy(x => x.Surname),
            "FullName", "FullName");
        ModelState.AddModelError("", v);
        return View(c);
    }
}
