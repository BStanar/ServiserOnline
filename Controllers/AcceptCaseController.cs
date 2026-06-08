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
    public IActionResult AddSpares(Guid CaseID)
    {
        return Redirect(Url.Action("Details", "Cases", new { id = CaseID }) + "#details-spares-entry");
    }
    public AcceptCaseController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }
    // Green / LightGreen → LightBlue: confirm invoicing
    public IActionResult Invoice(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model).ThenInclude(m => m.Manufacturer)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Location)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Model).ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();

        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Green && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Details", "Cases", new { id = CaseID });

        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Invoice(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["ID"], out var id))
            return BadRequest();

        var c = _db.Case.Where(ca => ca.ID == id)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.Client)
            .SingleOrDefault();

        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Green && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Details", "Cases", new { id });

        c.ContractNo = collection["ContractNo"];
        c.CaseStatus = CaseStatus.LightBlue;

        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return RedirectToAction("Details", "Cases", new { id });
    }

    // Yellow → Orange: set planned date and accept
    public IActionResult Index(Guid CaseID)
    {
        var c = _db.Case.Where(ca => ca.ID == CaseID)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Model)
                    .ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Yellow) return RedirectToAction("Index", "Cases");

        BuildAcceptSelectLists(c);
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

        c.ContinuedFromCase = null;
        if (!string.IsNullOrWhiteSpace(collection["ContinuedFromCase"]))
        {
            if (!Guid.TryParse(collection["ContinuedFromCase"], out var continuedId))
                return AddCaseErrorView(c, "Prethodni slučaj nije u redu");

            var clientId = c.Client?.ID ?? Guid.Empty;
            var previousCase = _db.Case
                .Include(x => x.Client)
                .SingleOrDefault(x =>
                    x.ID == continuedId &&
                    x.ID != c.ID &&
                    x.CaseStatus == CaseStatus.LightGreen &&
                    !x.Deleted &&
                    x.Client.ID == clientId);

            if (previousCase == null)
                return AddCaseErrorView(c, "Nastavak se može vezati samo za neuspješno završen nalog istog korisnika");

            c.ContinuedFromCase = previousCase;
        }

        c.CaseStatus = CaseStatus.Orange;
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Cases");
    }

    // GET: Green → ekran za rezervne dijelove prije fakturisanja

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSparePart(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["CaseID"], out var caseId))
            return BadRequest();

        var c = LoadOnSiteCase(caseId);
        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Green && c.CaseStatus != CaseStatus.LightBlue)
            return RedirectToAction("Index", "Cases");

        if (!Guid.TryParse(collection["SparePartID"], out var spareId))
            return AddSparesErrorView(c, "Rezervni dio nije ispravan");

        if (!TryParseWholeNumber(collection["Amount"], out var amount) || amount < 1)
            return AddSparesErrorView(c, "Količina mora biti cijeli broj veći od 0.");

        var sparePart = _db.SpareParts
            .Include(s => s.Model)
            .SingleOrDefault(s => s.ID == spareId);

        if (sparePart == null)
            return AddSparesErrorView(c, "Rezervni dio nije pronađen");

        if (!IsSpareAllowedForCase(caseId, sparePart))
            return AddSparesErrorView(c, "Rezervni dio ne pripada uređajima iz ovog naloga");

        var available = (int)Math.Floor(sparePart.StockAmount);
        if (available < 1)
            return AddSparesErrorView(c, "Odabrani rezervni dio nije dostupan na stanju.");

        if (amount > available)
            return AddSparesErrorView(c, $"Na stanju je dostupno samo {available} kom.");

        var item = new SparePartInCase
        {
            ID = Guid.NewGuid(),
            SparePartID = sparePart.ID,
            Amount = amount,
            Note = collection["Note"]
        };

        _db.SparePartsInCase.Add(item);
        _db.Entry(item).Property("Case_ID").CurrentValue = caseId;

        sparePart.StockAmount -= amount;
        await _db.SaveChangesAsync();

        return RedirectToAction("AddSpares", new { CaseID = caseId });
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSparePart(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["CaseID"], out var caseId) ||
            !Guid.TryParse(collection["SpareInCaseID"], out var spareInCaseId))
            return BadRequest();

        var item = _db.SparePartsInCase
            .SingleOrDefault(x => x.ID == spareInCaseId && EF.Property<Guid?>(x, "Case_ID") == caseId);

        if (item != null)
        {
            var sparePart = _db.SpareParts.SingleOrDefault(s => s.ID == item.SparePartID);
            if (sparePart != null)
                sparePart.StockAmount += item.Amount;

            _db.SparePartsInCase.Remove(item);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("AddSpares", new { CaseID = caseId });
    }

    private IActionResult AddSparesErrorView(Case c, string error)
    {
        // reload navigations if stripped
        if (c.Client == null)
            c = _db.Case.Where(ca => ca.ID == c.ID)
                .Include(ca => ca.Client)
                .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model)
                .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Model)
                .Single();
        ModelState.AddModelError("", error);
        return View("AddSpares", c);
    }

    // Orange / LightGreen → Green or LightGreen: fill in the service report
    public IActionResult OnSite(Guid CaseID)
    {
        var c = LoadOnSiteCase(CaseID);

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

        BuildOnSiteSelectLists(c);
        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OnSite(IFormCollection collection)
    {
        var id = Guid.Parse(collection["ID"]);
        var c = _db.Case.Where(ca => ca.ID == id).Include(ca => ca.Client).SingleOrDefault();
        if (c == null) return NotFound();
        if (c.CaseStatus != CaseStatus.Orange && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Details", "Cases", new { id = c.ID });

        c.CaseServisNumber = collection["CaseServisNumber"];
        c.AttendignPerson = collection["Attending"];
        c.ServiceDescription = collection["ServiceDescription"];
        c.SInterventionDescription = collection["SInterventionDescription"];
        c.NotFinishedDescription = collection["NotFinishedDescription"];
        c.PaymentInstruction = collection["PaymentInstruction"];
        c.ContractNo = collection["ContractNo"];

        try { c.DateTimeServiced = DateTime.Parse(collection["DateTimeServiced"]); }
        catch { return AddOnSiteErrorView(c, "Datum intervencije nije u redu"); }

        try { c.AutoIncrement = int.Parse(collection["AutoIncrement"]); }
        catch { return AddOnSiteErrorView(c, "Auto Inc nije u redu"); }

        if (!TryParseWholeNumber(collection["HoursOfTravel"], out var hoursOfTravel) || hoursOfTravel < 0)
            return AddOnSiteErrorView(c, "Sati putovanja moraju biti cijeli broj.");
        c.HoursOfTravel = hoursOfTravel;

        if (!TryParseWholeNumber(collection["HoursOfWork"], out var hoursOfWork) || hoursOfWork < 0)
            return AddOnSiteErrorView(c, "Sati rada moraju biti cijeli broj.");
        c.HoursOfWork = hoursOfWork;

        c.ContinuedFromCase = null;
        if (!string.IsNullOrWhiteSpace(collection["ContinuedFromCase"]))
        {
            if (!Guid.TryParse(collection["ContinuedFromCase"], out var continuedId))
                return AddOnSiteErrorView(c, "Prethodni slučaj nije u redu");

            var clientId = c.Client?.ID ?? Guid.Empty;
            var previousCase = _db.Case
                .Include(x => x.Client)
                .SingleOrDefault(x =>
                    x.ID == continuedId &&
                    x.ID != c.ID &&
                    x.CaseStatus == CaseStatus.LightGreen &&
                    !x.Deleted &&
                    x.Client.ID == clientId);

            if (previousCase == null)
                return AddOnSiteErrorView(c, "Nastavak se može vezati samo za neuspješno završen nalog istog korisnika");

            c.ContinuedFromCase = previousCase;
        }

        try
        {
            string user = User.Identity.Name;
            var dbUser = _db.Serviser.SingleOrDefault(u => u.Email == user);
            if (dbUser?.Name != null && dbUser?.Surname != null)
                c.ServicePerson = dbUser.Name + " " + dbUser.Surname;
        }
        catch { return AddOnSiteErrorView(c, "Serviser nije u redu"); }

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
        return RedirectToAction("Details", "Cases", new { id = c.ID });
    }

    [HttpGet]
    public IActionResult GetOnSiteSpares(Guid caseId, Guid modelId)
    {
        var allowedModelIds = GetCaseModelIds(caseId);
        if (!allowedModelIds.Contains(modelId))
            return Json(Array.Empty<object>());

        var manufacturerId = _db.ProductModel
            .Where(m => m.ID == modelId)
            .Select(m => m.ManufacturerID)
            .SingleOrDefault();

        var spares = _db.SpareParts
            .Include(s => s.Model)
            .Where(s => s.StockAmount >= 1 &&
                (s.ModelID == modelId ||
                 (s.Model.IsGeneral && s.Model.ManufacturerID == manufacturerId)))
            .OrderBy(s => s.Model.IsGeneral)
            .ThenBy(s => s.Model.Name)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                id = s.ID,
                label = s.Model.Name + " · " + s.Name +
                        " / " + (string.IsNullOrEmpty(s.CatalogNumber) ? s.SerialNumber : s.CatalogNumber) +
                        " / Stanje: " + Math.Floor(s.StockAmount),
                stockAmount = Math.Floor(s.StockAmount)
            })
            .ToList();

        return Json(spares);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOnSiteSparePart(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["CaseID"], out var caseId))
            return BadRequest();

        var c = LoadOnSiteCase(caseId);
        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Orange && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Index", "Cases");

        if (!Guid.TryParse(collection["ModelID"], out var modelId))
            return AddOnSiteErrorView(c, "Model uređaja nije ispravan");

        if (!Guid.TryParse(collection["SparePartID"], out var spareId))
            return AddOnSiteErrorView(c, "Rezervni dio nije ispravan");

        if (!TryParseWholeNumber(collection["Amount"], out var amount) || amount < 1)
            return AddOnSiteErrorView(c, "Količina mora biti cijeli broj veći od 0.");

        if (!GetCaseModelIds(caseId).Contains(modelId))
            return AddOnSiteErrorView(c, "Model nije dio ovog radnog naloga");

        var sparePart = _db.SpareParts
            .Include(s => s.Model)
            .SingleOrDefault(s => s.ID == spareId);

        if (sparePart == null)
            return AddOnSiteErrorView(c, "Rezervni dio nije pronađen");

        if (!IsSpareAllowedForCaseAndSelectedModel(caseId, modelId, sparePart))
            return AddOnSiteErrorView(c, "Rezervni dio nije dozvoljen za izabrani model");

        var available = (int)Math.Floor(sparePart.StockAmount);
        if (available < 1)
            return AddOnSiteErrorView(c, "Odabrani rezervni dio nije dostupan na stanju.");

        if (amount > available)
            return AddOnSiteErrorView(c, $"Na stanju je dostupno samo {available} kom.");

        var item = new SparePartInCase
        {
            ID = Guid.NewGuid(),
            SparePartID = sparePart.ID,
            Amount = amount,
            Note = collection["Note"]
        };

        _db.SparePartsInCase.Add(item);
        _db.Entry(item).Property("Case_ID").CurrentValue = caseId;

        sparePart.StockAmount -= amount;
        await _db.SaveChangesAsync();

        return RedirectToAction("OnSite", new { CaseID = caseId });
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveOnSiteSparePart(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["CaseID"], out var caseId) ||
            !Guid.TryParse(collection["SpareInCaseID"], out var spareInCaseId))
            return BadRequest();

        var c = _db.Case.SingleOrDefault(x => x.ID == caseId);
        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Orange && c.CaseStatus != CaseStatus.LightGreen)
            return RedirectToAction("Index", "Cases");

        var item = _db.SparePartsInCase
            .SingleOrDefault(x => x.ID == spareInCaseId && EF.Property<Guid?>(x, "Case_ID") == caseId);

        if (item != null)
        {
            var sparePart = _db.SpareParts.SingleOrDefault(s => s.ID == item.SparePartID);
            if (sparePart != null)
                sparePart.StockAmount += item.Amount;

            _db.SparePartsInCase.Remove(item);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("OnSite", new { CaseID = caseId });
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
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Model).ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
        if (c == null) return NotFound();

        c.DateTimeOfReport = DateTime.Now;
        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return View(c);
    }


    private Case LoadOnSiteCase(Guid caseId)
    {
        return _db.Case
            .Where(ca => ca.ID == caseId)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
            .Include(ca => ca.SpareParts)
                .ThenInclude(s => s.SparePart)
                    .ThenInclude(sp => sp.Model)
                        .ThenInclude(m => m.Manufacturer)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Location)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Device)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Manufacturer)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Model)
                        .ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
    }

    private void BuildOnSiteSelectLists(Case c)
    {
        var clientId = c.Client?.ID ?? Guid.Empty;

        var continuedCases = _db.Case
            .Where(x =>
                x.ID != c.ID &&
                x.Client.ID == clientId &&
                x.CaseStatus == CaseStatus.LightGreen &&
                !x.Deleted)
            .OrderByDescending(x => x.DateTimeServiced ?? x.DateTimeCaseOpened)
            .Select(x => new
            {
                x.ID,
                Label = (string.IsNullOrEmpty(x.CaseServisNumber) ? "Bez broja" : x.CaseServisNumber) +
                        " · " + ((x.DateTimeServiced ?? x.DateTimeCaseOpened).HasValue
                            ? (x.DateTimeServiced ?? x.DateTimeCaseOpened).Value.ToString("dd/MM/yyyy")
                            : "bez datuma")
            })
            .ToList();

        ViewBag.ContinuedFromCase = new SelectList(
            continuedCases,
            "ID",
            "Label",
            c.ContinuedFromCase?.ID);

        ViewBag.Attending = new SelectList(
            _db.ContactPersonClients
                .Where(x => x.Client.ID == clientId)
                .OrderBy(x => x.Surname),
            "FullName",
            "FullName",
            c.AttendignPerson);

        var spareModels = c.Devices?
            .Where(cd => cd.DeviceInLocation?.Model != null)
            .Select(cd => cd.DeviceInLocation.Model)
            .GroupBy(m => m.ID)
            .Select(g => g.First())
            .OrderBy(m => m.Name)
            .ToList() ?? new List<ProductModel>();

        ViewBag.OnSiteSpareModels = new SelectList(spareModels, "ID", "Name");
    }

    private IActionResult AddOnSiteErrorView(Case c, string error)
    {
        var fullCase = LoadOnSiteCase(c.ID) ?? c;
        BuildOnSiteSelectLists(fullCase);
        ModelState.AddModelError("", error);
        return View("OnSite", fullCase);
    }

    private List<Guid> GetCaseModelIds(Guid caseId)
    {
        return _db.CaseDevices
            .Where(cd => cd.CaseID == caseId)
            .Include(cd => cd.DeviceInLocation)
            .Where(cd => cd.DeviceInLocation != null)
            .Select(cd => cd.DeviceInLocation.Model.ID)
            .Distinct()
            .ToList();
    }

    private bool IsSpareAllowedForCase(Guid caseId, SparePart sparePart)
    {
        var caseModelIds = GetCaseModelIds(caseId);
        if (caseModelIds.Contains(sparePart.ModelID))
            return true;

        if (sparePart.Model?.IsGeneral != true)
            return false;

        var caseManufacturerIds = _db.ProductModel
            .Where(m => caseModelIds.Contains(m.ID))
            .Select(m => m.ManufacturerID)
            .Distinct()
            .ToList();

        return caseManufacturerIds.Contains(sparePart.Model.ManufacturerID);
    }

    private bool IsSpareAllowedForCaseAndSelectedModel(Guid caseId, Guid selectedModelId, SparePart sparePart)
    {
        if (!GetCaseModelIds(caseId).Contains(selectedModelId))
            return false;

        if (sparePart.ModelID == selectedModelId)
            return true;

        if (sparePart.Model?.IsGeneral != true)
            return false;

        var selectedManufacturerId = _db.ProductModel
            .Where(m => m.ID == selectedModelId)
            .Select(m => m.ManufacturerID)
            .SingleOrDefault();

        return sparePart.Model.ManufacturerID == selectedManufacturerId;
    }

    private bool TryParseWholeNumber(string value, out int amount)
    {
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out amount) ||
               int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out amount);
    }

    private void BuildAcceptSelectLists(Case c)
    {
        var clientId = c.Client?.ID ?? Guid.Empty;

        var continuedCases = _db.Case
            .Where(x =>
                x.ID != c.ID &&
                x.Client.ID == clientId &&
                x.CaseStatus == CaseStatus.LightGreen &&
                !x.Deleted)
            .OrderByDescending(x => x.DateTimeServiced ?? x.DateTimeCaseOpened)
            .Select(x => new
            {
                x.ID,
                Label = (string.IsNullOrEmpty(x.CaseServisNumber) ? "Bez broja" : x.CaseServisNumber) +
                        " · " + ((x.DateTimeServiced ?? x.DateTimeCaseOpened).HasValue
                            ? (x.DateTimeServiced ?? x.DateTimeCaseOpened).Value.ToString("dd/MM/yyyy")
                            : "bez datuma")
            })
            .ToList();

        ViewBag.ContinuedFromCase = new SelectList(
            continuedCases,
            "ID",
            "Label",
            c.ContinuedFromCase?.ID);
    }

    private IActionResult AddCaseErrorView(Case c, string v)
    {
        if (c.Client == null)
        {
            var fullCase = _db.Case
                .Include(x => x.Client)
                .Include(x => x.ContinuedFromCase)
                .SingleOrDefault(x => x.ID == c.ID);
            if (fullCase != null) c = fullCase;
        }

        BuildAcceptSelectLists(c);
        ViewBag.Attending = new SelectList(
            _db.ContactPersonClients.Where(x => x.Client.ID == c.Client.ID).OrderBy(x => x.Surname),
            "FullName", "FullName");
        ModelState.AddModelError("", v);
        return View(c);
    }

}
