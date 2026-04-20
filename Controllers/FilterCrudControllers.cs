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

    public IActionResult Index(Guid? SelectedManufacturer = null)
    {
        ViewBag.SelectedManufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", SelectedManufacturer);
        ViewBag.CreateID = SelectedManufacturer.GetValueOrDefault();

        var res = SelectedManufacturer.HasValue
            ? _db.ProductModel.Where(d => d.Manufacturer.ID == SelectedManufacturer.Value).OrderBy(d => d.Name).Include(d => d.Manufacturer)
            : _db.ProductModel.OrderBy(d => d.Name).Include(d => d.Manufacturer);

        return View(res.ToList());
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
        _db.ProductModel.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class DevicesController : Controller
{
    private readonly ApplicationDbContext _db;
    public DevicesController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedProductModel = null)
    {
        ViewBag.SelectedProductModel = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", SelectedProductModel);
        ViewBag.CreateID = SelectedProductModel.GetValueOrDefault();

        var res = SelectedProductModel.HasValue
            ? _db.Devices.Where(d => d.Model.ID == SelectedProductModel.Value).OrderBy(d => d.SerialNumber).Include(d => d.Model)
            : _db.Devices.OrderBy(d => d.SerialNumber).Include(d => d.Model);

        return View(res.ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Devices.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedProductModel = null)
    {
        ViewBag.ModelID = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", SelectedProductModel);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,ModelID,SerialNumber")] Device item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.Devices.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ModelID = new SelectList(_db.ProductModel, "ID", "Name", item.ModelID);
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Devices.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ModelID = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", item.ModelID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,ModelID,SerialNumber")] Device item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ModelID = new SelectList(_db.ProductModel, "ID", "Name", item.ModelID);
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.Devices.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.Devices.FindAsync(id);
        _db.Devices.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class SparePartsController : Controller
{
    private readonly ApplicationDbContext _db;
    public SparePartsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedProductModel = null)
    {
        ViewBag.SelectedProductModel = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", SelectedProductModel);
        ViewBag.CreateID = SelectedProductModel.GetValueOrDefault();

        var res = SelectedProductModel.HasValue
            ? _db.SpareParts.Where(d => d.Model.ID == SelectedProductModel.Value).OrderBy(d => d.Name).Include(d => d.Model)
            : _db.SpareParts.OrderBy(d => d.Name).Include(d => d.Model);

        return View(res.ToList());
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
    public async Task<IActionResult> Create([Bind("Model,ModelID,ID,Name,SerialNumber,StockAmount,Price")] SparePart item)
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
        ViewBag.Model = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", item.Model?.ID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ModelID,ID,Name,SerialNumber,StockAmount,Price")] SparePart item)
    {
        var model = _db.ProductModel.Find(item.ModelID);
        item.Model = model;
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.Model = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", item.ModelID);
        return View(item);
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
        _db.SpareParts.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}

[Authorize]
public class ClientLocationsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ClientLocationsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedClient = null)
    {
        ViewBag.SelectedClient = new SelectList(_db.Client.OrderBy(m => m.Name), "ID", "Name", SelectedClient);
        ViewBag.CreateID = SelectedClient.GetValueOrDefault();

        var res = SelectedClient.HasValue
            ? _db.ClientLocation.Where(d => d.Client.ID == SelectedClient.Value).OrderBy(d => d.LocationName).Include(d => d.Client)
            : _db.ClientLocation.OrderBy(d => d.LocationName).Include(d => d.Client);

        return View(res.ToList());
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ClientLocation.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedClient = null)
    {
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", SelectedClient);
        return View();
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,ClientID,Address,LocationName,City,PostCode,Telephone1,Telephone2,Description")] ClientLocation item)
    {
        if (ModelState.IsValid)
        {
            item.ID = Guid.NewGuid();
            _db.ClientLocation.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", item.ClientID);
        return View(item);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ClientLocation.FindAsync(id);
        if (item == null) return NotFound();
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", item.ClientID);
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,ClientID,Address,LocationName,City,PostCode,Telephone1,Telephone2,Description")] ClientLocation item)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        ViewBag.ClientID = new SelectList(_db.Client.OrderBy(c => c.Name), "ID", "Name", item.ClientID);
        return View(item);
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue) return BadRequest();
        var item = await _db.ClientLocation.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.ClientLocation.FindAsync(id);
        _db.ClientLocation.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
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

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.ContactPersonClients.FindAsync(id);
        _db.ContactPersonClients.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
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
        _db.ContactPersonManufacturers.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
