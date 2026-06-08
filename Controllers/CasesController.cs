using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;
using System.Globalization;
using X.PagedList.Extensions;

namespace ServiserOnline.Controllers;

[Authorize]
public class CasesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CasesController(ApplicationDbContext db) => _db = db;


    // Add these methods to Controllers/CasesController.cs.
    // They replace the old workflow where users had to open /AcceptCase/AddSpares.
    // Requires using Microsoft.EntityFrameworkCore; which is already present in your controller.

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpareFromDetails(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["ID"], out var caseId))
            return BadRequest();

        var c = LoadCaseForDetailsSpares(caseId);
        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Green && c.CaseStatus != CaseStatus.LightGreen)
        {
            TempData["SpareError"] = "Rezervni dijelovi se mogu dodavati samo prije fakturisanja.";
            return RedirectToAction("Details", new { id = caseId });
        }

        if (!Guid.TryParse(collection["SpareModelId"], out var selectedModelId))
        {
            TempData["SpareError"] = "Model uređaja nije ispravan.";
            return RedirectToAction("Details", new { id = caseId });
        }

        if (!Guid.TryParse(collection["Spares"], out var spareId))
        {
            TempData["SpareError"] = "Rezervni dio nije ispravan.";
            return RedirectToAction("Details", new { id = caseId });
        }

        if (!int.TryParse(collection["Amount"], out var amount) || amount < 1)
        {
            TempData["SpareError"] = "Količina mora biti cijeli broj veći od 0.";
            return RedirectToAction("Details", new { id = caseId });
        }

        var allowedModels = c.Devices?
            .Where(cd => cd.DeviceInLocation?.Model != null)
            .Select(cd => cd.DeviceInLocation.Model)
            .GroupBy(m => m.ID)
            .Select(g => g.First())
            .ToList() ?? new List<ProductModel>();

        var allowedModelIds = allowedModels.Select(m => m.ID).ToList();
        var allowedManufacturerIds = allowedModels.Select(m => m.ManufacturerID).Distinct().ToList();

        if (!allowedModelIds.Any())
        {
            TempData["SpareError"] = "U nalogu nema uređaja. Prvo dodaj uređaj u radni nalog.";
            return RedirectToAction("Details", new { id = caseId });
        }

        if (!allowedModelIds.Contains(selectedModelId))
        {
            TempData["SpareError"] = "Izabrani model ne pripada uređajima iz ovog radnog naloga.";
            return RedirectToAction("Details", new { id = caseId });
        }

        var selectedModel = allowedModels.First(m => m.ID == selectedModelId);

        var sparePart = await _db.SpareParts
            .Include(s => s.Model)
            .SingleOrDefaultAsync(s => s.ID == spareId);

        if (sparePart == null)
        {
            TempData["SpareError"] = "Rezervni dio nije pronađen.";
            return RedirectToAction("Details", new { id = caseId });
        }

        var isAllowed =
            sparePart.ModelID == selectedModelId ||
            (sparePart.Model != null &&
             sparePart.Model.IsGeneral &&
             sparePart.Model.ManufacturerID == selectedModel.ManufacturerID &&
             allowedManufacturerIds.Contains(sparePart.Model.ManufacturerID));

        if (!isAllowed)
        {
            TempData["SpareError"] = "Ovaj rezervni dio ne pripada izabranom modelu ili općim dijelovima istog proizvođača.";
            return RedirectToAction("Details", new { id = caseId });
        }

        var available = (int)Math.Floor(sparePart.StockAmount);

        if (available < 1)
        {
            TempData["SpareError"] = "Odabrani rezervni dio nije dostupan na stanju.";
            return RedirectToAction("Details", new { id = caseId });
        }

        if (amount > available)
        {
            TempData["SpareError"] = $"Na stanju je dostupno samo {available} kom.";
            return RedirectToAction("Details", new { id = caseId });
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var item = new SparePartInCase
            {
                ID = Guid.NewGuid(),
                SparePartID = sparePart.ID,
                SparePart = sparePart,
                Amount = amount,
                Note = collection["Note"]
            };

            _db.SparePartsInCase.Add(item);
            _db.Entry(item).Property("Case_ID").CurrentValue = caseId;

            sparePart.StockAmount -= amount;
            _db.Entry(sparePart).State = EntityState.Modified;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["SpareSuccess"] = "Rezervni dio je dodat u radni nalog.";
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["SpareError"] = "Greška pri dodavanju rezervnog dijela.";
        }

        return Redirect(Url.Action("Details", "Cases", new { id = caseId }) + "#details-spares-entry");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSpareFromDetails(Guid? ID, Guid? caseID)
    {
        if (!ID.HasValue || !caseID.HasValue)
            return BadRequest();

        var c = LoadCaseForDetailsSpares(caseID.Value);
        if (c == null) return NotFound();

        if (c.CaseStatus != CaseStatus.Green && c.CaseStatus != CaseStatus.LightGreen)
        {
            TempData["SpareError"] = "Rezervni dijelovi se ne mogu mijenjati nakon fakturisanja.";
            return RedirectToAction("Details", new { id = caseID.Value });
        }

        var item = await _db.SparePartsInCase
            .Include(x => x.SparePart)
            .SingleOrDefaultAsync(x => x.ID == ID.Value);

        if (item == null)
            return Redirect(Url.Action("Details", "Cases", new { id = caseID.Value }) + "#details-spares-entry");

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            if (item.SparePart != null)
            {
                item.SparePart.StockAmount += item.Amount;
                _db.Entry(item.SparePart).State = EntityState.Modified;
            }

            _db.SparePartsInCase.Remove(item);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["SpareSuccess"] = "Rezervni dio je uklonjen iz radnog naloga.";
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["SpareError"] = "Greška pri uklanjanju rezervnog dijela.";
        }

        return Redirect(Url.Action("Details", "Cases", new { id = caseID.Value }) + "#details-spares-entry");
    }

    // Replace your existing GetSparesList with this version.
    // It returns parts for the selected model + general parts for the same manufacturer only.
    public IActionResult GetSparesList(Guid SelectedModel, Guid? caseId = null)
    {
        try
        {
            if (SelectedModel == Guid.Empty)
                return Json(new object[0]);

            var selectedModel = _db.ProductModel
                .AsNoTracking()
                .SingleOrDefault(m => m.ID == SelectedModel);

            if (selectedModel == null)
                return Json(new object[0]);

            if (caseId.HasValue)
            {
                var modelBelongsToCase = _db.CaseDevices
                    .Include(cd => cd.DeviceInLocation)
                        .ThenInclude(d => d.Model)
                    .Any(cd => cd.CaseID == caseId.Value && cd.DeviceInLocation.Model.ID == SelectedModel);

                if (!modelBelongsToCase)
                    return Json(new object[0]);
            }

            var manufacturerId = selectedModel.ManufacturerID;

            var spares = _db.SpareParts
                .AsNoTracking()
                .Include(s => s.Model)
                .Where(s =>
                    s.StockAmount >= 1 &&
                    (
                        s.ModelID == SelectedModel ||
                        (s.Model.IsGeneral && s.Model.ManufacturerID == manufacturerId)
                    ))
                .OrderBy(s => s.Model.IsGeneral)
                .ThenBy(s => s.Model.Name)
                .ThenBy(s => s.Name)
                .Select(s => new
                {
                    id = s.ID,
                    name = s.Name,
                    serialNumber = s.SerialNumber,
                    catalogNumber = s.CatalogNumber,
                    stockAmount = Math.Floor(s.StockAmount),
                    price = s.Price,
                    modelName = s.Model.Name,
                    isGeneral = s.Model.IsGeneral
                })
                .ToList();

            return Json(spares);
        }
        catch
        {
            return Json(new object[0]);
        }
    }

    private Case LoadCaseForDetailsSpares(Guid caseId)
    {
        return _db.Case
            .Where(ca => ca.ID == caseId)
            .Include(ca => ca.Client)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.SpareParts)
                .ThenInclude(s => s.SparePart)
                    .ThenInclude(sp => sp.Model)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Device)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Location)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Model)
                        .ThenInclude(m => m.Manufacturer)
            .SingleOrDefault(ca => ca.ID == caseId);
    }

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
            .Include(ca => ca.SpareParts).ThenInclude(s => s.SparePart).ThenInclude(sp => sp.Model).ThenInclude(m => m.Manufacturer)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Location)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Device)
            .Include(ca => ca.Devices).ThenInclude(cd => cd.DeviceInLocation).ThenInclude(d => d.Model).ThenInclude(m => m.Manufacturer)
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
            Devices = new List<CaseDevice>(),
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

    public IActionResult GetDevicesList(Guid SelectedLocation, Guid? caseId)
    {
        try
        {
            var alreadyAddedIds = caseId.HasValue
                ? _db.CaseDevices
                    .Where(cd => cd.CaseID == caseId.Value)
                    .Select(cd => cd.DeviceInLocationID)
                    .ToList()
                : new List<Guid>();

            var devices = _db.DeviceInLocations
                .Where(x => x.LocationID == SelectedLocation && !alreadyAddedIds.Contains(x.ID))
                .Include(d => d.Device)
                .Include(d => d.Model).ThenInclude(m => m.Manufacturer)
                .Include(d => d.Manufacturer)
                .OrderBy(m => m.Model.Name)
                .ThenBy(m => m.Device.SerialNumber)
                .Select(m => new
                {
                    m.ID,
                    SerialNumber = m.Device != null ? m.Device.SerialNumber : "",
                    m.DateOfInstalation,
                    ModelName = m.Model != null ? m.Model.Name : "",
                    ManufacturerName = m.Manufacturer != null
                        ? m.Manufacturer.Name
                        : (m.Model != null && m.Model.Manufacturer != null ? m.Model.Manufacturer.Name : "")
                })
                .ToList();

            return Json(devices);
        }
        catch { return Json(new object[0]); }
    }


    public IActionResult GetSparesByCase(Guid caseId)
    {
        try
        {
            var caseModels = _db.CaseDevices
                .Where(cd => cd.CaseID == caseId)
                .Include(cd => cd.DeviceInLocation).ThenInclude(d => d.Model)
                .Select(cd => new
                {
                    ModelID = cd.DeviceInLocation.Model.ID,
                    ManufacturerID = cd.DeviceInLocation.Model.ManufacturerID
                })
                .Distinct()
                .ToList();

            var caseModelIds = caseModels.Select(m => m.ModelID).Distinct().ToList();
            var caseManufacturerIds = caseModels.Select(m => m.ManufacturerID).Distinct().ToList();

            var spares = _db.SpareParts
                .AsNoTracking()
                .Include(s => s.Model)
                .Where(s =>
                    s.StockAmount > 0 &&
                    (
                        caseModelIds.Contains(s.ModelID) ||
                        (s.Model.IsGeneral && caseManufacturerIds.Contains(s.Model.ManufacturerID))
                    ))
                .OrderBy(s => s.Model.IsGeneral)
                .ThenBy(s => s.Model.Name)
                .ThenBy(s => s.Name)
                .Select(s => new
                {
                    s.ID,
                    s.Name,
                    s.SerialNumber,
                    s.CatalogNumber,
                    StockAmount = Math.Floor(s.StockAmount),
                    s.Price,
                    ModelName = s.Model.Name,
                    IsGeneral = s.Model.IsGeneral
                })
                .ToList();

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

        var c = LoadCaseForAddSpare(id.Value);
        if (c == null) return NotFound();

        BuildModelsForAddSpare(c);
        return View(c);
    }
    public async Task<IActionResult> RemoveSpare(Guid? ID, Guid? caseID)
    {
        if (!ID.HasValue || !caseID.HasValue) return BadRequest();

        var spare = await _db.SparePartsInCase
            .SingleOrDefaultAsync(s => s.ID == ID.Value && EF.Property<Guid?>(s, "Case_ID") == caseID.Value);

        if (spare == null)
            return RedirectToAction("AddSpare", new { id = caseID });

        var sparePart = await _db.SpareParts.SingleOrDefaultAsync(l => l.ID == spare.SparePartID);
        if (sparePart != null)
            sparePart.StockAmount += spare.Amount;

        _db.SparePartsInCase.Remove(spare);
        await _db.SaveChangesAsync();

        return RedirectToAction("AddSpare", new { id = caseID });
    }


    // ID parameter is DeviceInLocation.ID (matches existing URL contract)
    public async Task<IActionResult> RemoveDevice(Guid? ID, Guid? caseID)
    {
        if (!ID.HasValue) return BadRequest();
        var c = _db.Case.Where(ca => ca.ID == caseID)
            .Include(ca => ca.Devices)
            .Include(ca => ca.Locations)
            .SingleOrDefault();
        if (c == null) return NotFound();

        var dil = _db.DeviceInLocations.Where(d => d.ID == ID).Include(d => d.Location).SingleOrDefault();
        if (dil == null) return NotFound();

        var caseDevice = c.Devices?.FirstOrDefault(cd => cd.DeviceInLocationID == ID);
        if (caseDevice != null) c.Devices.Remove(caseDevice);

        var loc = c.Locations?.FirstOrDefault(l => l.ID == dil.LocationID);
        if (loc != null) c.Locations.Remove(loc);

        _db.Entry(c).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction("AddDevice", new { id = caseID });
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpare(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["ID"], out var caseId))
            return BadRequest();

        var c = LoadCaseForAddSpare(caseId);
        if (c == null) return NotFound();

        if (!int.TryParse(collection["Amount"], out var amount) || amount <= 0)
            return AddSpareErrorView(c, "Količina mora biti cijeli broj veći od 0.");

        if (!Guid.TryParse(collection["Spares"], out var spareId))
            return AddSpareErrorView(c, "Rezervni dio nije odabran.");

        var caseModelIds = c.Devices?
            .Select(cd => cd.DeviceInLocation?.Model?.ID)
            .Where(mid => mid.HasValue)
            .Select(mid => mid.Value)
            .Distinct()
            .ToList() ?? new List<Guid>();

        var caseManufacturerIds = c.Devices?
            .Select(cd => cd.DeviceInLocation?.Model?.ManufacturerID)
            .Where(mid => mid.HasValue)
            .Select(mid => mid.Value)
            .Distinct()
            .ToList() ?? new List<Guid>();

        var sparePart = await _db.SpareParts
            .Include(s => s.Model)
            .SingleOrDefaultAsync(s => s.ID == spareId);

        if (sparePart == null)
            return AddSpareErrorView(c, "Rezervni dio nije pronađen.");

        var allowedForCase = sparePart.Model != null &&
            (
                caseModelIds.Contains(sparePart.ModelID) ||
                (sparePart.Model.IsGeneral && caseManufacturerIds.Contains(sparePart.Model.ManufacturerID))
            );

        if (!allowedForCase)
            return AddSpareErrorView(c, "Rezervni dio ne pripada modelima/proizvođačima uređaja u ovom radnom nalogu.");

        var available = (int)Math.Floor(sparePart.StockAmount);
        if (available < 1)
            return AddSpareErrorView(c, "Odabrani rezervni dio nije dostupan na stanju.");

        if (amount > available)
            return AddSpareErrorView(c, $"Na stanju je dostupno samo {available} kom.");

        var spareInCase = new SparePartInCase
        {
            ID = Guid.NewGuid(),
            SparePartID = sparePart.ID,
            Amount = amount,
            Note = collection["Note"]
        };

        _db.SparePartsInCase.Add(spareInCase);
        _db.Entry(spareInCase).Property("Case_ID").CurrentValue = caseId;

        sparePart.StockAmount -= amount;

        await _db.SaveChangesAsync();

        return RedirectToAction("AddSpare", new { id = caseId });
    }


    public IActionResult DbCheck()
    {
        var conn = _db.Database.GetDbConnection();

        var result = new List<string>();

        using (var command = conn.CreateCommand())
        {
            command.CommandText = @"
            SELECT 
                DB_NAME() AS CurrentDatabase,
                @@SERVERNAME AS ServerName;

            SELECT 
                TABLE_SCHEMA,
                TABLE_NAME,
                COLUMN_NAME,
                DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'ProductModels'
            ORDER BY COLUMN_NAME;
        ";

            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            using var reader = command.ExecuteReader();

            result.Add("=== DATABASE ===");

            while (reader.Read())
            {
                result.Add($"Database: {reader["CurrentDatabase"]}");
                result.Add($"Server: {reader["ServerName"]}");
            }

            reader.NextResult();

            result.Add("");
            result.Add("=== ProductModels columns ===");

            while (reader.Read())
            {
                result.Add($"{reader["TABLE_SCHEMA"]}.{reader["TABLE_NAME"]}.{reader["COLUMN_NAME"]} ({reader["DATA_TYPE"]})");
            }
        }

        return Content(string.Join(Environment.NewLine, result));
    }

    public IActionResult AddDevice(Guid? id)
    {
        if (!id.HasValue) return BadRequest();

        var c = LoadCaseForAddDevice(id.Value);
        if (c == null) return NotFound();

        BuildLocationsForAddDevice(c);
        return View(c);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDevice(IFormCollection collection)
    {
        if (!Guid.TryParse(collection["ID"], out var caseId))
            return BadRequest();

        if (!Guid.TryParse(collection["Device"], out var deviceInLocationId))
        {
            var cForError = LoadCaseForAddDevice(caseId);
            return AddDeviceErrorView(cForError, "Uređaj nije odabran.");
        }

        var c = _db.Case
            .Include(ca => ca.Client)
            .SingleOrDefault(ca => ca.ID == caseId);

        if (c == null)
            return NotFound();

        var dil = _db.DeviceInLocations
            .Include(d => d.Location)
            .Include(d => d.Device)
            .Include(d => d.Model)
            .SingleOrDefault(d => d.ID == deviceInLocationId);

        if (dil == null)
        {
            var cForError = LoadCaseForAddDevice(caseId);
            return AddDeviceErrorView(cForError, "Uređaj nije pronađen.");
        }

        if (dil.Location == null || dil.Location.ClientID != c.Client.ID)
        {
            var cForError = LoadCaseForAddDevice(caseId);
            return AddDeviceErrorView(cForError, "Uređaj ne pripada izabranom korisniku.");
        }

        var alreadyExists = _db.CaseDevices.Any(cd =>
            cd.CaseID == caseId &&
            cd.DeviceInLocationID == deviceInLocationId);

        if (alreadyExists)
        {
            var cForError = LoadCaseForAddDevice(caseId);
            return AddDeviceErrorView(cForError, "Uređaj je već dodan u ovaj radni nalog.");
        }

        _db.CaseDevices.Add(new CaseDevice
        {
            ID = Guid.NewGuid(),
            CaseID = caseId,
            DeviceInLocationID = deviceInLocationId
        });

        await _db.SaveChangesAsync();

        return RedirectToAction("AddDevice", new { id = caseId });
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

    private Case LoadCaseForAddDevice(Guid caseId)
    {
        return _db.Case
            .Include(ca => ca.Client)
            .Include(ca => ca.Locations)
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
            .SingleOrDefault(ca => ca.ID == caseId);
    }

    private Case LoadCaseForAddSpare(Guid caseId)
    {
        return _db.Case
            .Include(ca => ca.Client)
            .Include(ca => ca.SpareParts)
                .ThenInclude(s => s.SparePart)
                    .ThenInclude(sp => sp.Model)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Model)
                        .ThenInclude(m => m.Manufacturer)
            .SingleOrDefault(ca => ca.ID == caseId);
    }

    private void BuildLocationsForAddDevice(Case c)
    {
        var locations = _db.ClientLocation
            .Where(m => m.ClientID == c.Client.ID)
            .OrderBy(m => m.LocationName)
            .ToList();

        ViewBag.LocationID = new SelectList(locations, "ID", "LocationName");
    }

    private IActionResult AddDeviceErrorView(Case c, string error)
    {
        if (c == null) return NotFound();

        ModelState.AddModelError("", error);
        BuildLocationsForAddDevice(c);
        return View("AddDevice", c);
    }

    private void BuildModelsForAddSpare(Case c)
    {
        var caseModelIds = c.Devices?
            .Select(cd => cd.DeviceInLocation?.Model?.ID)
            .Where(mid => mid.HasValue)
            .Select(mid => mid.Value)
            .Distinct()
            .ToList() ?? new List<Guid>();

        Guid? preSelected = caseModelIds.Count == 1 ? caseModelIds[0] : null;

        var models = caseModelIds.Any()
            ? _db.ProductModel
                .Where(m => caseModelIds.Contains(m.ID))
                .OrderBy(m => m.Name)
                .ToList()
            : new List<ProductModel>();

        models.Insert(0, new ProductModel { ID = Guid.Empty, Name = caseModelIds.Any() ? "-- Odaberi model --" : "-- Prvo dodaj uređaj --" });
        ViewBag.Models = new SelectList(models, "ID", "Name", preSelected);
    }

    private IActionResult AddSpareErrorView(Case c, string error)
    {
        if (c == null) return NotFound();

        ModelState.AddModelError("", error);
        BuildModelsForAddSpare(c);
        return View("AddSpare", c);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();

        var c = await _db.Case
            .Include(ca => ca.Client)
            .Include(ca => ca.InterventionType)
            .SingleOrDefaultAsync(ca => ca.ID == id.Value);

        return c == null ? NotFound() : View(c);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var c = await _db.Case.SingleOrDefaultAsync(ca => ca.ID == id);
        if (c == null) return NotFound();

        // Index already filters Deleted != true, so this keeps service history intact.
        c.Deleted = true;
        _db.Entry(c).Property(x => x.Deleted).IsModified = true;

        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }


    [HttpGet]
    public async Task<IActionResult> ReportData(Guid caseId)
    {
        var c = await _db.Case
            .Where(ca => ca.ID == caseId && ca.Deleted != true)
            .Include(ca => ca.Client)
            .Include(ca => ca.ContinuedFromCase)
            .Include(ca => ca.InterventionType)
            .Include(ca => ca.SpareParts)
                .ThenInclude(s => s.SparePart)
                    .ThenInclude(sp => sp.Model)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Device)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Location)
            .Include(ca => ca.Devices)
                .ThenInclude(cd => cd.DeviceInLocation)
                    .ThenInclude(d => d.Model)
                        .ThenInclude(m => m.Manufacturer)
            .SingleOrDefaultAsync();

        if (c == null)
            return NotFound();

        // Same behavior as AcceptCase/Print: when the report is generated, update report date.
        c.DateTimeOfReport = DateTime.Now;
        await _db.SaveChangesAsync();

        static string DateText(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "";

        static string Text(string value)
            => string.IsNullOrWhiteSpace(value) ? "" : value;

        static string NumberText(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.000001)
                return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        var devices = (c.Devices ?? new List<CaseDevice>())
            .Where(cd => cd.DeviceInLocation != null)
            .Select(cd => new
            {
                modelName = Text(cd.DeviceInLocation.Model?.Name),
                manufacturerName = Text(cd.DeviceInLocation.Model?.Manufacturer?.Name),
                serialNumber = Text(cd.DeviceInLocation.Device?.SerialNumber),
                locationName = Text(cd.DeviceInLocation.Location?.LocationName)
            })
            .ToList();

        var spareParts = (c.SpareParts ?? new List<SparePartInCase>())
            .Where(spc => spc.SparePart != null)
            .Select(spc => new
            {
                // Original Print.cshtml used SerialNumber in this column.
                serialNumber = Text(spc.SparePart.SerialNumber),
                catalogNumber = Text(spc.SparePart.CatalogNumber),
                name = Text(spc.SparePart.Name),
                amount = NumberText(spc.Amount),
                note = Text(spc.Note)
            })
            .ToList();

        var address = string.Join(" ", new[]
        {
        c.Client?.MainAddress1,
        c.Client?.MainAddress2
    }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        return Json(new
        {
            caseServisNumber = Text(c.CaseServisNumber),
            dateTimePlanned = DateText(c.DateTimePlanned),
            dateTimeCaseOpened = DateText(c.DateTimeCaseOpened),
            dateTimeServiced = DateText(c.DateTimeServiced),
            dateTimeOfReport = DateText(c.DateTimeOfReport),
            interventionTypeName = Text(c.InterventionType?.Name),

            servicePerson = Text(c.ServicePerson),
            hoursOfTravel = NumberText(c.HoursOfTravel),
            hoursOfWork = NumberText(c.HoursOfWork),

            clientName = Text(c.Client?.Name),
            clientCity = Text(c.Client?.City),
            clientAddress = Text(address),

            attendingPerson = Text(c.AttendignPerson),
            contractNo = Text(c.ContractNo),

            continueFromNo = Text(c.ContinuedFromCase?.CaseServisNumber),
            continuedFromNo = Text(c.ContinuedFromCase?.CaseServisNumber),
            continuedFromDate = DateText(c.ContinuedFromCase?.DateTimeServiced),

            serviceDescription = Text(c.ServiceDescription),
            interventionDescription = Text(c.SInterventionDescription),
            notFinishedDescription = Text(c.NotFinishedDescription),

            finished = c.Finished,
            payWhen = c.PayWhen.ToString(),
            payNow = c.PayWhen == PayWhen.PayNow,
            payLater = c.PayWhen == PayWhen.PayLater,
            noPay = c.PayWhen == PayWhen.NoPay,

            devices,
            spareParts
        });
    }

}
