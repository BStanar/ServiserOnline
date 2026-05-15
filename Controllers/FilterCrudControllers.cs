using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;
using X.PagedList.Extensions;

namespace ServiserOnline.Controllers;

[Authorize]
public class ManufacturersController : Controller
{
    private readonly ApplicationDbContext _db;
    public ManufacturersController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
    {
        ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "Name_desc" : "";
        if (searchString != null) page = 1;
        else searchString = currentFilter;
        ViewBag.CurrentFilter = searchString;

        IQueryable<Manufacturer> model = _db.Manufacturer
            .Include(m => m.Models).ThenInclude(pm => pm.Devices);

        if (!string.IsNullOrEmpty(searchString))
            model = model.Where(s => s.Name.ToUpper().Contains(searchString.ToUpper())
                || s.Country.ToUpper().Contains(searchString.ToUpper()));

        model = sortOrder == "Name_desc"
            ? model.OrderByDescending(s => s.Name)
            : model.OrderBy(s => s.Name);

        ViewBag.TotalDevices = model.AsEnumerable().Sum(m => m.DeviceNo);
        return View(model.ToPagedList(page ?? 1, 15));
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Manufacturer.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,Country")] Manufacturer item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.Manufacturer.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInline(Guid ModelID, string SerialNumber)
    {
        SerialNumber = (SerialNumber ?? string.Empty).Trim();

        if (ModelID == Guid.Empty)
            return BadRequest(new { message = "Model je obavezan." });

        if (string.IsNullOrWhiteSpace(SerialNumber))
            return BadRequest(new { message = "Serijski broj je obavezan." });

        var modelExists = await _db.ProductModel.AnyAsync(m => m.ID == ModelID);
        if (!modelExists)
            return BadRequest(new { message = "Odabrani model nije ispravan." });

        var exists = await _db.Devices.AnyAsync(d => d.SerialNumber == SerialNumber);
        if (exists)
            return BadRequest(new { message = "Uređaj sa ovim serijskim brojem već postoji." });

        var device = new Device
        {
            ID = Guid.NewGuid(),
            ModelID = ModelID,
            SerialNumber = SerialNumber
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Json(new
        {
            id = device.ID,
            serialNumber = device.SerialNumber
        });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Manufacturer.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name,Description,Country")] Manufacturer item)
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
        var item = await _db.Manufacturer.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.Manufacturer.FindAsync(id);
        if (item == null) return NotFound();
        _db.Manufacturer.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class ClientsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ClientsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
    {
        ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "Name_desc" : "";
        if (searchString != null) page = 1;
        else searchString = currentFilter;
        ViewBag.CurrentFilter = searchString;

        IQueryable<Client> model = _db.Client;
        if (!string.IsNullOrEmpty(searchString))
            model = model.Where(s => s.Name.ToUpper().Contains(searchString.ToUpper())
                || s.City.ToUpper().Contains(searchString.ToUpper()));

        model = sortOrder == "Name_desc"
            ? model.OrderByDescending(s => s.Name)
            : model.OrderBy(s => s.Name);

        return View(model.ToPagedList(page ?? 1, 15));
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Client.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create() => View();

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,CompanyNumber,VATNumber,MainAddress1,MainAddress2,City,PostCode,MainTelephone1,MainTelephone2,Fax,Email,MainContactPerson,MainContactPersonRole,DateTimeContractStart,DateTimeContractEnd,ContractNumber")] Client item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.Client.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Client.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,Name,CompanyNumber,VATNumber,MainAddress1,MainAddress2,City,PostCode,MainTelephone1,MainTelephone2,Fax,Email,MainContactPerson,MainContactPersonRole,DateTimeContractStart,DateTimeContractEnd,ContractNumber")] Client item)
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
        var item = await _db.Client.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.Client.FindAsync(id);
        if (item == null) return NotFound();
        _db.Client.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class ProductModelsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductModelsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedManufacturer = null, string searchString = null)
    {
        ViewBag.SelectedManufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", SelectedManufacturer);
        ViewBag.CreateID = SelectedManufacturer.GetValueOrDefault();
        ViewBag.CurrentFilter = searchString;

        var query = _db.ProductModel
            .Include(d => d.Manufacturer)
            .Include(d => d.Devices)
            .Include(d => d.SpareParts)
            .AsQueryable();

        if (SelectedManufacturer.HasValue)
            query = query.Where(d => d.Manufacturer.ID == SelectedManufacturer.Value);

        if (!string.IsNullOrEmpty(searchString))
            query = query.Where(d => d.Name.Contains(searchString) || d.Description.Contains(searchString));

        return View(query.OrderBy(d => d.Manufacturer.Name).ThenBy(d => d.Name).ToList());
    }
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ProductModel.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedManufacturer = null)
    {
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", SelectedManufacturer);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ManufacturerID,ID,Name,Description")] ProductModel item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.ProductModel.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ProductModel.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", item.ManufacturerID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ManufacturerID,ID,Name,Description")] ProductModel item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ProductModel.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.ProductModel.FindAsync(id);
        if (item == null) return NotFound();
        _db.ProductModel.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class DevicesController : Controller
{
    private readonly ApplicationDbContext _db;

    public DevicesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Replace existing Index in DevicesController (FilterCrudControllers.cs)

    public IActionResult Index(Guid? SelectedProductModel = null, string searchString = null)
    {
        ViewBag.SelectedProductModel = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", SelectedProductModel);
        ViewBag.CreateID = SelectedProductModel.GetValueOrDefault();
        ViewBag.CurrentFilter = searchString;

        var query = _db.Devices
            .Include(d => d.Model).ThenInclude(m => m.Manufacturer)
            .Include(d => d.DeviceInLocations)
                .ThenInclude(dil => dil.Location)
                .ThenInclude(l => l.Client)
            .AsQueryable();

        if (SelectedProductModel.HasValue)
            query = query.Where(d => d.Model.ID == SelectedProductModel.Value);

        if (!string.IsNullOrEmpty(searchString))
            query = query.Where(d => d.SerialNumber.Contains(searchString));

        var devices = query
            .OrderBy(d => d.Model.Manufacturer.Name)
            .ThenBy(d => d.Model.Name)
            .ThenBy(d => d.SerialNumber)
            .ToList();

        // Service counts grouped by (SerialNumber, ModelID) so all duplicate
        // Device rows for the same physical instrument share one count
        var serialModelPairs = devices
            .Select(d => new { d.SerialNumber, d.ModelID })
            .Distinct()
            .ToList();

        var serials = serialModelPairs.Select(p => p.SerialNumber).Distinct().ToList();

        var groupedCounts = _db.CaseDevices
            .Where(cd => serials.Contains(cd.DeviceInLocation.Device.SerialNumber))
            .GroupBy(cd => new
            {
                cd.DeviceInLocation.Device.SerialNumber,
                cd.DeviceInLocation.Device.ModelID
            })
            .Select(g => new { g.Key.SerialNumber, g.Key.ModelID, Count = g.Count() })
            .ToList();

        var serviceCounts = devices.ToDictionary(
            d => d.ID,
            d => groupedCounts
                .FirstOrDefault(c => c.SerialNumber == d.SerialNumber && c.ModelID == d.ModelID)?.Count ?? 0
        );

        ViewBag.ServiceCounts = serviceCounts;

        return View(devices);
    }
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue)
            return BadRequest();

        var item = await _db.Devices
            .Include(d => d.Model)
                .ThenInclude(m => m.Manufacturer)
            .Include(d => d.DeviceInLocations)
                .ThenInclude(dil => dil.Location)
                    .ThenInclude(l => l.Client)
            .FirstOrDefaultAsync(d => d.ID == id.Value);

        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedProductModel = null)
    {
        return RedirectToAction(nameof(Index));
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,ModelID,SerialNumber")] Device item)
    {
        ModelState.Remove(nameof(Device.Model));
        ModelState.Remove(nameof(Device.DeviceInLocations));

        item.SerialNumber = item.SerialNumber?.Trim();

        if (item.ModelID == Guid.Empty)
        {
            ModelState.AddModelError(nameof(Device.ModelID), "Model je obavezan.");
        }

        if (string.IsNullOrWhiteSpace(item.SerialNumber))
        {
            ModelState.AddModelError(nameof(Device.SerialNumber), "Serijski broj je obavezan.");
        }

        var modelExists = await _db.ProductModel.AnyAsync(m => m.ID == item.ModelID);
        if (!modelExists)
        {
            ModelState.AddModelError(nameof(Device.ModelID), "Odabrani model nije pronađen.");
        }

        var duplicate = await _db.Devices.AnyAsync(d => d.SerialNumber == item.SerialNumber);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(Device.SerialNumber), "Uređaj sa ovim serijskim brojem već postoji.");
        }

        if (!ModelState.IsValid)
        {
            TempData["DeviceError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return RedirectToAction(nameof(Index));
        }

        item.ID = Guid.NewGuid();

        _db.Devices.Add(item);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(Guid? id)
    {
        return RedirectToAction(nameof(Index));
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,ModelID,SerialNumber")] Device item)
    {
        ModelState.Remove(nameof(Device.Model));
        ModelState.Remove(nameof(Device.DeviceInLocations));

        item.SerialNumber = item.SerialNumber?.Trim();

        if (item.ID == Guid.Empty)
        {
            return BadRequest();
        }

        if (item.ModelID == Guid.Empty)
        {
            ModelState.AddModelError(nameof(Device.ModelID), "Model je obavezan.");
        }

        if (string.IsNullOrWhiteSpace(item.SerialNumber))
        {
            ModelState.AddModelError(nameof(Device.SerialNumber), "Serijski broj je obavezan.");
        }

        var existing = await _db.Devices.FindAsync(item.ID);
        if (existing == null)
        {
            return NotFound();
        }

        var modelExists = await _db.ProductModel.AnyAsync(m => m.ID == item.ModelID);
        if (!modelExists)
        {
            ModelState.AddModelError(nameof(Device.ModelID), "Odabrani model nije pronađen.");
        }

        var duplicate = await _db.Devices.AnyAsync(d =>
            d.ID != item.ID &&
            d.SerialNumber == item.SerialNumber);

        if (duplicate)
        {
            ModelState.AddModelError(nameof(Device.SerialNumber), "Uređaj sa ovim serijskim brojem već postoji.");
        }

        if (!ModelState.IsValid)
        {
            TempData["DeviceError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return RedirectToAction(nameof(Index));
        }

        existing.ModelID = item.ModelID;
        existing.SerialNumber = item.SerialNumber;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(Guid? id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.Devices
            .Include(d => d.DeviceInLocations)
            .FirstOrDefaultAsync(d => d.ID == id);

        if (item == null)
        {
            return NotFound();
        }

        var hasLocationAssignments = await _db.DeviceInLocations
            .AnyAsync(dil => dil.Device.ID == id);

        if (hasLocationAssignments)
        {
            TempData["DeviceError"] = "Uređaj je dodijeljen lokaciji. Prvo ukloni dodjelu sa lokacije.";
            return RedirectToAction(nameof(Index));
        }

        var hasServiceHistory = await _db.CaseDevices
            .AnyAsync(cd => cd.DeviceInLocation.Device.ID == id);

        if (hasServiceHistory)
        {
            TempData["DeviceError"] = "Uređaj ima servisnu historiju i ne može se brisati.";
            return RedirectToAction(nameof(Index));
        }

        _db.Devices.Remove(item);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInline(Guid ModelID, string SerialNumber)
    {
        SerialNumber = (SerialNumber ?? string.Empty).Trim();

        if (ModelID == Guid.Empty)
        {
            return BadRequest(new { message = "Model je obavezan." });
        }

        if (string.IsNullOrWhiteSpace(SerialNumber))
        {
            return BadRequest(new { message = "Serijski broj je obavezan." });
        }

        var modelExists = await _db.ProductModel.AnyAsync(m => m.ID == ModelID);
        if (!modelExists)
        {
            return BadRequest(new { message = "Odabrani model nije ispravan." });
        }

        var exists = await _db.Devices.AnyAsync(d => d.SerialNumber == SerialNumber);
        if (exists)
        {
            return BadRequest(new { message = "Uređaj sa ovim serijskim brojem već postoji." });
        }

        var device = new Device
        {
            ID = Guid.NewGuid(),
            ModelID = ModelID,
            SerialNumber = SerialNumber
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Json(new
        {
            id = device.ID,
            serialNumber = device.SerialNumber
        });
    }

    public IActionResult ServiceHistory(Guid? id)
    {
        if (!id.HasValue) return BadRequest();

        var device = _db.Devices
            .Where(d => d.ID == id)
            .Include(d => d.Model).ThenInclude(m => m.Manufacturer)
            .SingleOrDefault();
        if (device == null) return NotFound();

        var sn = device.SerialNumber;
        var modelId = device.ModelID;

        // All cases that touched ANY Device row with this SerialNumber + ModelID
        // (same physical instrument across all duplicate Device rows)
        var history = _db.CaseDevices
            .Where(cd => cd.DeviceInLocation.Device.SerialNumber == sn
                      && cd.DeviceInLocation.Device.ModelID == modelId)
            .Include(cd => cd.Case).ThenInclude(c => c.Client)
            .Include(cd => cd.Case).ThenInclude(c => c.InterventionType)
            .Include(cd => cd.DeviceInLocation).ThenInclude(dil => dil.Location).ThenInclude(l => l.Client)
            .OrderByDescending(cd => cd.Case.DateTimeServiced)
            .ToList();

        // Most recent installation for this physical instrument
        var currentInstallation = _db.DeviceInLocations
            .Where(dil => dil.Device.SerialNumber == sn && dil.Device.ModelID == modelId)
            .Include(dil => dil.Location).ThenInclude(l => l.Client)
            .OrderByDescending(dil => dil.DateOfInstalation)
            .FirstOrDefault();

        return View(new DeviceServiceHistoryViewModel
        {
            Device = device,
            CurrentInstallation = currentInstallation,
            History = history
        });
    }

    private List<SelectListItem> BuildDeviceModelOptions(Guid? selectedModelId = null)
    {
        var models = _db.ProductModel
            .Include(m => m.Manufacturer)
            .AsNoTracking()
            .OrderBy(m => m.Manufacturer.Name)
            .ThenBy(m => m.Name)
            .ToList()
            .Where(m => !IsGeneralSparePartsModel(m))
            .ToList();

        var groups = new Dictionary<Guid, SelectListGroup>();

        var result = new List<SelectListItem>();

        foreach (var model in models)
        {
            var manufacturerId = model.ManufacturerID;
            var manufacturerName = model.Manufacturer?.Name ?? "Bez proizvođača";

            if (!groups.TryGetValue(manufacturerId, out var group))
            {
                group = new SelectListGroup { Name = manufacturerName };
                groups[manufacturerId] = group;
            }

            result.Add(new SelectListItem
            {
                Value = model.ID.ToString(),
                Text = model.Name,
                Selected = selectedModelId.HasValue && selectedModelId.Value == model.ID,
                Group = group
            });
        }

        return result;
    }

    private static bool IsGeneralSparePartsModel(ProductModel model)
    {
        var text = $"{model.Name} {model.Manufacturer?.Name}".ToLowerInvariant();

        return text.Contains("opci rezervni dijelovi") ||
               text.Contains("opći rezervni dijelovi") ||
               text.Contains("opsti rezervni dijelovi") ||
               text.Contains("opšti rezervni dijelovi") ||
               text.Contains("general spare parts");
    }
}

[Authorize]
public class SparePartsController : Controller
{
    private readonly ApplicationDbContext _db;
    public SparePartsController(ApplicationDbContext db) => _db = db;



    public IActionResult Index(Guid? SelectedProductModel = null, string searchString = null, string ActiveTab = "all")
    {
        // treat Guid.Empty as no filter (form posts empty Guid when nothing selected)
        if (SelectedProductModel == Guid.Empty) SelectedProductModel = null;
        ActiveTab = ActiveTab ?? "all";

        var allModels = _db.ProductModel
            .Include(m => m.Manufacturer)
            .OrderBy(m => m.Manufacturer.Name).ThenBy(m => m.Name)
            .AsNoTracking()
            .ToList();

        var mfrGroups = allModels
            .GroupBy(m => m.Manufacturer.Name)
            .ToDictionary(g => g.Key, g => new SelectListGroup { Name = g.Key });

        ViewBag.SelectedProductModel = allModels.Select(m => new SelectListItem
        {
            Value = m.ID.ToString(),
            Text = m.Name,
            Selected = m.ID == SelectedProductModel,
            Group = mfrGroups[m.Manufacturer.Name]
        });

        ViewBag.ModelMfrMap = allModels.ToDictionary(
            m => m.ID.ToString(),
            m => m.ManufacturerID.ToString());

        ViewBag.CreateID = SelectedProductModel.GetValueOrDefault();
        ViewBag.CurrentFilter = searchString;
        ViewBag.ActiveTab = ActiveTab;
        ViewBag.Manufacturers = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");

        // base query: model + search filter, but NOT tab filter (tab counts must be pre-tab)
        var baseQuery = _db.SpareParts
            .Include(d => d.Model).ThenInclude(m => m.Manufacturer)
            .AsNoTracking()
            .AsQueryable();

        if (SelectedProductModel.HasValue)
            baseQuery = baseQuery.Where(d => d.ModelID == SelectedProductModel.Value);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var s = searchString.ToUpper();
            baseQuery = baseQuery.Where(d =>
                d.Name.ToUpper().Contains(s) ||
                d.SerialNumber.ToUpper().Contains(s) ||
                d.CatalogNumber.ToUpper().Contains(s));
        }

        // counts for header and tab badges, computed BEFORE tab filter
        ViewBag.TotalAll = baseQuery.Count();
        ViewBag.TotalOut = baseQuery.Count(d => d.StockAmount == 0);
        ViewBag.TotalLow = baseQuery.Count(d => d.StockAmount > 0 && d.StockAmount <= 3);
        ViewBag.MfrCount = baseQuery.Select(d => d.Model.ManufacturerID).Distinct().Count();
        ViewBag.ModelCount = baseQuery.Select(d => d.ModelID).Distinct().Count();

        var res = baseQuery;
        if (ActiveTab == "out")
            res = res.Where(d => d.StockAmount == 0);
        else if (ActiveTab == "low")
            res = res.Where(d => d.StockAmount > 0 && d.StockAmount <= 3);

        return View(res
            .OrderBy(d => d.Model.Manufacturer.Name)
            .ThenBy(d => d.Model.Name)
            .ThenBy(d => d.Name)
            .ToList());
    }
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.SpareParts.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedProductModel = null)
    {
        ViewBag.ModelID = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", SelectedProductModel);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ModelID,ID,Name,SerialNumber,CatalogNumber,StockAmount,Price")] SparePart item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.SpareParts.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ModelID = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name");
        return View(item);
    }

    public IActionResult Edit(Guid? id)
    {

        if (!id.HasValue) return BadRequest();
        var item = _db.SpareParts.Where(c => c.ID == id).Include(c => c.Model).SingleOrDefault();
        if (item == null) return NotFound();
        ViewBag.Manufacturers = new SelectList(
           _db.Manufacturer.OrderBy(m => m.Name), "ID", "Name",
           item.Model?.ManufacturerID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ModelID,ID,Name,SerialNumber,CatalogNumber,StockAmount,Price")] SparePart item)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Manufacturers = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
            return View(item);
        }
        var existing = await _db.SpareParts.FindAsync(item.ID);
        if (existing == null) return NotFound();
        existing.ModelID = item.ModelID;
        existing.Name = item.Name;
        existing.SerialNumber = item.SerialNumber;
        existing.CatalogNumber = item.CatalogNumber;
        existing.StockAmount = item.StockAmount;
        existing.Price = item.Price;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.SpareParts.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.SpareParts.FindAsync(id);
        if (item == null) return NotFound();
        _db.SpareParts.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateStockDto dto)
    {
        var item = await _db.SpareParts.FindAsync(dto.Id);
        if (item == null) return NotFound();
        item.StockAmount = Math.Max(0, item.StockAmount + dto.Delta);
        await _db.SaveChangesAsync();
        return Json(new { stock = item.StockAmount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice([FromBody] UpdatePriceDto dto)
    {
        var item = await _db.SpareParts.FindAsync(dto.Id);
        if (item == null) return NotFound();
        if (dto.Price < 0) return BadRequest();
        item.Price = dto.Price;
        await _db.SaveChangesAsync();
        return Json(new { price = item.Price });
    }

    public class UpdateStockDto { public Guid Id { get; set; } public int Delta { get; set; } }
    public class UpdatePriceDto { public Guid Id { get; set; } public double Price { get; set; } }
}

[Authorize]
public class ClientLocationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClientLocationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index(Guid? SelectedClient = null, string searchString = null)
    {
        ViewBag.SelectedClient = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", SelectedClient);
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", SelectedClient);
        ViewBag.Manufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        ViewBag.CreateID = SelectedClient.GetValueOrDefault();
        ViewBag.CurrentFilter = searchString;

        var query = _db.ClientLocation
            .Include(l => l.Client)
            .AsQueryable();

        if (SelectedClient.HasValue)
        {
            query = query.Where(l => l.ClientID == SelectedClient.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();

            query = query.Where(l =>
                (l.Client != null && l.Client.Name.Contains(term)) ||
                (l.LocationName != null && l.LocationName.Contains(term)) ||
                (l.City != null && l.City.Contains(term)) ||
                (l.Address != null && l.Address.Contains(term)));
        }

        var locations = query
            .OrderBy(l => l.Client.Name)
            .ThenBy(l => l.LocationName)
            .ToList();

        var locationIds = locations.Select(l => l.ID).ToList();

        var devicesByLocation = _db.DeviceInLocations
            .Where(d => locationIds.Contains(d.LocationID))
            .Include(d => d.Location)
            .Include(d => d.Manufacturer)
            .Include(d => d.Model)
            .Include(d => d.Device)
            .OrderBy(d => d.Device.SerialNumber)
            .ToList()
            .GroupBy(d => d.LocationID)
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.DevicesByLocation = devicesByLocation;

        return View(locations);
    }

    public IActionResult Create(Guid? SelectedClient = null)
    {
        return RedirectToAction(nameof(Index), new { SelectedClient });
    }

    [Log]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("ID,ClientID,Address,LocationName,City,PostCode,Telephone1,Telephone2,Description")] ClientLocation item,
        string searchString = null)
    {
        ModelState.Remove(nameof(ClientLocation.Client));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Lokacija nije kreirana. Provjeri obavezna polja.";

            return RedirectToAction(nameof(Index), new
            {
                SelectedClient = item.ClientID,
                searchString
            });
        }

        item.ID = Guid.NewGuid();

        _db.ClientLocation.Add(item);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = item.ClientID,
            searchString
        });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue)
        {
            return BadRequest();
        }

        var item = await _db.ClientLocation.FindAsync(id.Value);

        if (item == null)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = item.ClientID
        });
    }

    [Log]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind("ID,ClientID,Address,LocationName,City,PostCode,Telephone1,Telephone2,Description")] ClientLocation item,
        string searchString = null)
    {
        ModelState.Remove(nameof(ClientLocation.Client));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Lokacija nije izmijenjena. Provjeri obavezna polja.";

            return RedirectToAction(nameof(Index), new
            {
                SelectedClient = item.ClientID,
                searchString
            });
        }

        var existing = await _db.ClientLocation.FindAsync(item.ID);

        if (existing == null)
        {
            return NotFound();
        }

        existing.ClientID = item.ClientID;
        existing.LocationName = item.LocationName;
        existing.City = item.City;
        existing.PostCode = item.PostCode;
        existing.Address = item.Address;
        existing.Telephone1 = item.Telephone1;
        existing.Telephone2 = item.Telephone2;
        existing.Description = item.Description;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = item.ClientID,
            searchString
        });
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue)
        {
            return BadRequest();
        }

        var item = await _db.ClientLocation
            .Include(l => l.Client)
            .FirstOrDefaultAsync(l => l.ID == id.Value);

        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue)
        {
            return BadRequest();
        }

        var item = await _db.ClientLocation.FindAsync(id.Value);

        if (item == null)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = item.ClientID
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, Guid? SelectedClient = null, string searchString = null)
    {
        var item = await _db.ClientLocation.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        var selectedClient = SelectedClient ?? item.ClientID;

        var hasDevices = await _db.DeviceInLocations
            .AnyAsync(x => x.LocationID == id);

        if (hasDevices)
        {
            TempData["Error"] = "Lokacija ima dodijeljene uređaje. Prvo ukloni uređaje sa lokacije.";

            return RedirectToAction(nameof(Index), new
            {
                SelectedClient = selectedClient,
                searchString
            });
        }

        _db.ClientLocation.Remove(item);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = selectedClient,
            searchString
        });
    }
}

[Authorize]
public class ContactPersonClientsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContactPersonClientsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedClient = null)
    {
        ViewBag.SelectedClient = new SelectList(_db.Client.OrderBy(m => m.Name), "ID", "Name", SelectedClient);
        ViewBag.CreateID = SelectedClient.GetValueOrDefault();

        var res = SelectedClient.HasValue
            ? _db.ContactPersonClients.Where(d => d.Client.ID == SelectedClient.Value).OrderBy(d => d.Name).Include(d => d.Client)
            : _db.ContactPersonClients.OrderBy(d => d.Name).Include(d => d.Client);

        return View(res.ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonClients.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedClient = null)
    {
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", SelectedClient);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,ClientID,ContactPersonRole,Name,Surname,Telephone,Mobile,Email")] ContactPersonClient item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.ContactPersonClients.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ClientID = new SelectList(_db.Client, "ID", "Name", item.ClientID);
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonClients.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ClientID = new SelectList(_db.Client, "ID", "Name", item.ClientID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,ClientID,ContactPersonRole,Name,Surname,Telephone,Mobile,Email")] ContactPersonClient item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ClientID = new SelectList(_db.Client, "ID", "Name", item.ClientID);
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonClients.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, Guid? SelectedClient = null, string searchString = null)
    {
        var item = await _db.ClientLocation
            .FirstOrDefaultAsync(x => x.ID == id);

        if (item == null)
        {
            return NotFound();
        }

        var clientId = SelectedClient ?? item.ClientID;

        var hasDevices = await _db.DeviceInLocations
            .AnyAsync(d => d.LocationID == id);

        if (hasDevices)
        {
            TempData["ClientLocationError"] = "Lokacija ima dodijeljene uređaje. Prvo ukloni uređaje sa lokacije, pa onda obriši lokaciju.";

            return RedirectToAction(nameof(Index), new
            {
                SelectedClient = clientId,
                searchString
            });
        }

        _db.ClientLocation.Remove(item);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new
        {
            SelectedClient = clientId,
            searchString
        });
    }
}

[Authorize]
public class ContactPersonManufacturersController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContactPersonManufacturersController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedManufacturer = null)
    {
        ViewBag.SelectedManufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", SelectedManufacturer);
        ViewBag.CreateID = SelectedManufacturer.GetValueOrDefault();

        var res = SelectedManufacturer.HasValue
            ? _db.ContactPersonManufacturers.Where(d => d.Manufacturer.ID == SelectedManufacturer.Value).OrderBy(d => d.Name).Include(d => d.Manufacturer)
            : _db.ContactPersonManufacturers.OrderBy(d => d.Name).Include(d => d.Manufacturer);

        return View(res.ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonManufacturers.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedManufacturer = null)
    {
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", SelectedManufacturer);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,ManufacturerID,ContactPersonRole,Name,Surname,Telephone,Mobile,Email")] ContactPersonManufacturer item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.ContactPersonManufacturers.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", item.ManufacturerID);
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonManufacturers.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", item.ManufacturerID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,ManufacturerID,ContactPersonRole,Name,Surname,Telephone,Mobile,Email")] ContactPersonManufacturer item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ManufacturerID = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", item.ManufacturerID);
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ContactPersonManufacturers.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.ContactPersonManufacturers.FindAsync(id);
        if (item == null) return NotFound();
        _db.ContactPersonManufacturers.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}