using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace ServiserOnline.Controllers;

[Authorize]
public class CasesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CasesController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? page, string status = null)
    {
        ViewBag.CurrentStatus = status;
        if (searchString != null) page = 1;
        else searchString = currentFilter;
        ViewBag.CurrentFilter = searchString;

        var model = _db.Case
            .Include(cl => cl.Client)
            .Include(ca => ca.InterventionType)
            .Where(cs => cs.Deleted != true);

        if (!string.IsNullOrEmpty(status))
            model = model.Where(c => c.CaseStatus.ToString().ToLower() == status);
        else
            model = model.Where(c =>
                c.CaseStatus == CaseStatus.Yellow ||
                c.CaseStatus == CaseStatus.Orange ||
                c.CaseStatus == CaseStatus.Green ||
                c.CaseStatus == CaseStatus.LightGreen);

        if (!string.IsNullOrEmpty(searchString))
            model = model.Where(c =>
                c.Client.Name.Contains(searchString) ||
                (c.CaseServisNumber != null && c.CaseServisNumber.Contains(searchString)));

        model = model
            .OrderBy(cs => cs.CaseStatus)
            .ThenByDescending(c => c.CaseServisNumber)
            .ThenBy(c => c.DateTimePlanned)
            .ThenBy(c => c.DateTimeCaseOpened);

        return View(model.ToPagedList(page ?? 1, 300));
    }

    public IActionResult Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.Client)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)
            .SingleOrDefault();
        return c == null ? NotFound() : View(c);
    }

    public IActionResult Create()
    {
        ViewBag.Client = new SelectList(_db.Client.OrderBy(m => m.Name), "ID", "Name");
        ViewBag.ContinuedFromCase = new SelectList(
            _db.Case.Where(c => c.CaseStatus == CaseStatus.LightGreen).OrderBy(c => c.CaseServisNumber),
            "ID", "CaseServisNumber");
        ViewBag.ContactType = new SelectList(_db.CaseContactTypes.OrderBy(m => m.Name), "ID", "Name");
        ViewBag.InterventionType = new SelectList(_db.InterventionType.OrderBy(m => m.Name), "ID", "Name");

        var model = new Case
        {
            Locations = new List<ClientLocation>(),
            Devices = new List<DeviceInLocation>(),
            ID = Guid.Empty,
            DateTimeCaseOpened = DateTime.Now,
            DateTimeServiced = DateTime.Now
        };
        return View(model);
    }

    public IActionResult GetClientsList()
    {
        var models = _db.Client.OrderBy(m => m.Name)
            .Select(c => new { c.ID, c.Name }).ToList();
        return Json(models);
    }

    public IActionResult GetLocationsList(Guid SelectedClient)
    {
        var locations = _db.ClientLocation
            .Where(m => m.Client.ID == SelectedClient)
            .OrderBy(m => m.LocationName)
            .Select(m => new { m.ID, m.LocationName }).ToList();
        return Json(locations);
    }

    public IActionResult GetDevicesList(Guid SelectedLocation)
    {
        try
        {
            var devices = _db.DeviceInLocations
                .Where(x => x.Location.ID == SelectedLocation)
                .Include(d => d.Device)
                .OrderBy(m => m.DateOfInstalation)
                .Select(m => new { m.ID, m.Device.SerialNumber, m.DateOfInstalation }).ToList();
            return Json(devices);
        }
        catch { return Json(new object[0]); }
    }

    public IActionResult GetSparesList(Guid SelectedModel)
    {
        try
        {
            var spares = SelectedModel == Guid.Empty
                ? _db.SpareParts.OrderBy(m => m.Name).Select(m => new { m.ID, m.Name, m.SerialNumber, m.StockAmount }).ToList()
                : _db.SpareParts.Where(m => m.Model.ID == SelectedModel).OrderBy(m => m.Name)
                    .Select(m => new { m.ID, m.Name, m.SerialNumber, m.StockAmount }).ToList();
            return Json(spares);
        }
        catch { return Json(new object[0]); }
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormCollection collection)
    {
        var c = new Case();

        try { c.ContactType = _db.CaseContactTypes.Find(Guid.Parse(collection["ContactType"])); }
        catch { return AddCaseErrorView(c, "Tip kontakta nije u redu"); }

        try { c.DateTimeCaseOpened = DateTime.Parse(collection["DateTimeCaseOpened"]); }
        catch { return AddCaseErrorView(c, "Datum kontakta nije u redu"); }

        c.ContractNo = collection["ContractNo"];

        try { c.Client = _db.Client.Single(l => l.ID == Guid.Parse(collection["Client"])); }
        catch { return AddCaseErrorView(c, "Korisnik nije u redu"); }

        if (!string.IsNullOrEmpty(collection["ContinuedFromCase"]))
        {
            try { c.ContinuedFromCase = _db.Case.Single(l => l.ID == Guid.Parse(collection["ContinuedFromCase"])); }
            catch { return AddCaseErrorView(c, "Prethodni slucaj nije u redu"); }
        }

        if (!string.IsNullOrEmpty(collection["InterventionType"]))
        {
            try { c.InterventionType = _db.InterventionType.Single(l => l.ID == Guid.Parse(collection["InterventionType"])); }
            catch { return AddCaseErrorView(c, "Vrsta usluge nije u redu"); }
        }

        if (!string.IsNullOrEmpty(collection["AcceptingDescription"]))
            c.AcceptingDescription = collection["AcceptingDescription"];

        c.ID = Guid.NewGuid();
        c.CaseStatus = CaseStatus.Yellow;
        _db.Case.Add(c);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    public IActionResult AddSpare(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)  // add this
            .SingleOrDefault();
        if (c == null) return NotFound();

        // Get model IDs from devices on this case
        var caseModelIds = c.Devices?
            .Select(d => d.Model?.ID)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList() ?? new List<Guid>();

        // Pre-select if only one model, otherwise show all case models first
        Guid? preSelectedModel = caseModelIds.Count == 1 ? caseModelIds[0] : null;

        var models = caseModelIds.Any()
            ? _db.ProductModel
                .Where(m => caseModelIds.Contains(m.ID))
                .OrderBy(m => m.Name).ToList()
            : _db.ProductModel.OrderBy(m => m.Name).ToList();

        models.Insert(0, new ProductModel { Name = "-- Odaberi model --" });
        ViewBag.Models = new SelectList(models, "ID", "Name", preSelectedModel);
        return View(c);
    }
    public async Task<IActionResult> RemoveSpare(Guid? ID, Guid? caseID)
    {
        if (!ID.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == caseID).Include(ca => ca.SpareParts).SingleOrDefault();
        if (c == null) return NotFound();

        var spare = _db.SparePartsInCase.Find(ID);
        var sparePart = _db.SpareParts.Single(l => l.ID == spare.SparePartID);
        sparePart.StockAmount += spare.Amount;
        c.SpareParts.Remove(spare);
        _db.Entry(c).State = EntityState.Modified;
        _db.Entry(sparePart).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("AddSpare", new { id = caseID });
    }

    public async Task<IActionResult> RemoveDevice(Guid? ID, Guid? caseID)
    {
        if (!ID.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == caseID)
            .Include(ca => ca.Devices)
            .Include(ca => ca.Locations)
            .SingleOrDefault();
        if (c == null) return NotFound();

        var device = _db.DeviceInLocations.Where(ca => ca.ID == ID).Include(ca => ca.Location).SingleOrDefault();
        var loc = device.Location;
        c.Devices.Remove(device);
        c.Locations.Remove(loc);
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("AddDevice", new { id = caseID });
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpare(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)  // add this
            .SingleOrDefault();

        int am;
        try { am = int.Parse(collection["Amount"]); }
        catch { return AddSpareErrorView(c, "Kolicina nije u redu"); }

        try
        {
            var spareID = Guid.Parse(collection["Spares"]);
            if (c.SpareParts == null) c.SpareParts = new List<SparePartInCase>();
            var sparePart = _db.SpareParts.Single(l => l.ID == spareID);
            c.SpareParts.Add(new SparePartInCase
            {
                ID = Guid.NewGuid(),
                SparePart = sparePart,
                Amount = am,
                Note = collection["Note"]
            });
            sparePart.StockAmount -= am;
            _db.Entry(c).State = EntityState.Modified;
            _db.Entry(sparePart).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return AddSpareErrorView(c, "Dio je uspjesno dodat ***");
        }
        catch { return AddSpareErrorView(c, "Dio nije u redu"); }
    }

    public IActionResult AddDevice(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)
            .SingleOrDefault();
        if (c == null) return NotFound();

        var locations = _db.ClientLocation
            .Where(m => m.Client.ID == c.Client.ID)
            .OrderBy(m => m.LocationName).ToList();
        locations.Insert(0, new ClientLocation { LocationName = "-- Odaberi lokaciju --" });
        ViewBag.LocationID = new SelectList(locations, "ID", "LocationName");
        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDevice(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.Devices).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(d => d.Model)
            .SingleOrDefault();

        try
        {
            if (c.Locations == null) c.Locations = new List<ClientLocation>();
            c.Locations.Add(_db.ClientLocation.Single(l => l.ID == Guid.Parse(collection["LocationID"])));
        }
        catch { return AddCaseErrorView(c, "Lokacija nije u redu"); }

        try
        {
            if (c.Devices == null) c.Devices = new List<DeviceInLocation>();
            c.Devices.Add(_db.DeviceInLocations
                .Where(l => l.ID == Guid.Parse(collection["Device"]))
                .Include(d => d.Device).Include(d => d.Model).Single());
        }
        catch { return AddCaseErrorView(c, "Uredjaj nije u redu"); }

        if (c.ID == Guid.Empty)
        {
            c.ID = Guid.NewGuid();
            _db.Case.Add(c);
        }
        else
        {
            _db.Entry(c).State = EntityState.Modified;
        }
        await _db.SaveChangesAsync();

        if (collection.ContainsKey("action:AddDeviceReportAsync"))
            return RedirectToAction("OnSite", "AcceptCase", new { CaseID = id });

        return AddCaseErrorView(c, "Uredjaj je uspjesno dodat ***");
    }

    private IActionResult AddCaseErrorView(Case c, string error)
    {
        ModelState.AddModelError("", error);
        var locations = _db.ClientLocation
            .Where(m => m.Client.ID == c.Client.ID)
            .OrderBy(m => m.LocationName).ToList();
        locations.Insert(0, new ClientLocation { LocationName = "-- Odaberi lokaciju --" });
        ViewBag.LocationID = new SelectList(locations, "ID", "LocationName");
        ViewBag.InterventionType = new SelectList(_db.InterventionType.OrderBy(m => m.Name), "ID", "Name");
        return View(c);
    }

    private IActionResult AddSpareErrorView(Case c, string error)
    {
        ModelState.AddModelError("", error);

        var caseModelIds = c.Devices?
            .Select(d => d.Model?.ID)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList() ?? new List<Guid>();

        Guid? preSelected = caseModelIds.Count == 1 ? caseModelIds[0] : null;

        var models = caseModelIds.Any()
            ? _db.ProductModel.Where(m => caseModelIds.Contains(m.ID)).OrderBy(m => m.Name).ToList()
            : _db.ProductModel.OrderBy(m => m.Name).ToList();

        models.Insert(0, new ProductModel { Name = "-- Odaberi model --" });
        ViewBag.Models = new SelectList(models, "ID", "Name", preSelected);
        return View(c);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var c = await _db.Case.FindAsync(id);
        return c == null ? NotFound() : View(c);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var c = await _db.Case.FindAsync(id);
        _db.Case.Remove(c);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
