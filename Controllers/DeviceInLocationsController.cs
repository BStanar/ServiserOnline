using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiserOnline.Infrastructure;
using ServiserOnline.Models;

namespace ServiserOnline.Controllers;

[Authorize]
public class DeviceInLocationsController : Controller
{
    private readonly ApplicationDbContext _db;
    public DeviceInLocationsController(ApplicationDbContext db) => _db = db;

    public IActionResult Index(Guid? SelectedLocation = null, Guid? SelectedClient = null)
    {
        var clients = _db.ClientLocation
            .Where(m => m.Client.ID == SelectedClient)
            .OrderBy(m => m.LocationName).ToList();
        ViewBag.SelectedLocation = new SelectList(clients, "ID", "LocationName", SelectedLocation);
        ViewBag.CreateID = SelectedLocation.GetValueOrDefault();
        ViewBag.CreateIDClient = SelectedClient;

        var res = SelectedLocation.HasValue
            ? _db.DeviceInLocations.Where(d => d.Location.ID == SelectedLocation.Value)
                .OrderBy(d => d.DateOfInstalation)
                .Include(d => d.Location).Include(d => d.Model).Include(d => d.Device)
            : _db.DeviceInLocations.OrderBy(d => d.DateOfInstalation)
                .Include(d => d.Location).Include(d => d.Model).Include(d => d.Device);

        return View(res.ToList());
    }

    public async Task<IActionResult> Details(Guid? id, Guid? SelectedClient = null)
    {
        ViewBag.CreateIDClient = SelectedClient;
        if (!id.HasValue) return BadRequest();
        var item = await _db.DeviceInLocations.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    public IActionResult Create(Guid? SelectedLocation = null, Guid? SelectedClient = null)
    {
        var model = new DeviceInLocation
        {
            DateOfInstalation = DateTime.Now,
            DateOfGuarantieEnd = DateTime.Now.AddYears(2),
            SelectedClientID = SelectedClient
        };
        ViewBag.Manufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        ViewBag.LocationID = new SelectList(
            _db.ClientLocation.Where(m => m.Client.ID == SelectedClient).OrderBy(m => m.LocationName),
            "ID", "LocationName", SelectedLocation);
        return View(model);
    }

    public IActionResult GetModelsList(Guid SelectedManufacturer)
    {
        var models = _db.ProductModel
            .Where(m => m.Manufacturer.ID == SelectedManufacturer)
            .OrderBy(m => m.Name)
            .Select(m => new { id = m.ID, name = m.Name }).ToList();
        return Json(models);
    }

    public IActionResult GetDevicesList(Guid SelectedModel)
    {
        var allDevices = _db.Devices.Where(m => m.Model.ID == SelectedModel).OrderBy(m => m.SerialNumber).ToList();
        var usedDevices = _db.DeviceInLocations.Select(d => d.Device).ToList();
        var available = allDevices.Except(usedDevices)
            .Select(d => new { id = d.ID, serialNumber = d.SerialNumber }).ToList();
        return Json(available);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormCollection collection)
    {
        var dil = new DeviceInLocation();
        var locid = Guid.Parse(collection["LocationID"]);

        try
        {
            dil.LocationID = locid;
            dil.Location = _db.ClientLocation.Single(l => l.ID == locid);

            try
            {
                dil.DateOfGuarantieEnd = DateTime.Parse(collection["DateOfGuarantieEnd"]);
                dil.DateOfInstalation = DateTime.Parse(collection["DateOfInstalation"]);
            }
            catch { return CreateErrorView(dil, "Datumi nisu ispravni"); }

            dil.DeviceInLocationDescription = collection["DeviceInLocationDescription"];

            try { dil.Manufacturer = _db.Manufacturer.Single(l => l.ID == Guid.Parse(collection["Manufacturer"])); }
            catch { return CreateErrorView(dil, "Proizvodjac nije ispravan"); }

            try { dil.Model = _db.ProductModel.Single(l => l.ID == Guid.Parse(collection["Model"])); }
            catch { return CreateErrorView(dil, "Model nije ispravan"); }

            try { dil.Device = _db.Devices.Single(l => l.ID == Guid.Parse(collection["Device"])); }
            catch { return CreateErrorView(dil, "Uredjaj nije ispravan"); }

            var modelDevices = _db.ProductModel.Where(l => l.ID == dil.Model.ID).Include(m => m.Devices).Single();
            if (!modelDevices.Devices.Contains(dil.Device))
                return CreateErrorView(dil, "Uredjaj ne pripada modelu");

            dil.ID = Guid.NewGuid();
            dil.SelectedClientID = Guid.Parse(collection["SelectedClientID"]);
            _db.DeviceInLocations.Add(dil);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", new { SelectedLocation = locid, SelectedClient = dil.SelectedClientID });
        }
        catch { return CreateErrorView(dil, "Kreiranje nije uspelo"); }
    }

    public IActionResult Edit(Guid? id, Guid? SelectedClient = null)
    {
        if (!id.HasValue) return BadRequest();
        var item = _db.DeviceInLocations.Where(c => c.ID == id)
            .Include(c => c.Location).Include(c => c.Device)
            .Include(c => c.Model).Include(c => c.Manufacturer)
            .SingleOrDefault();
        if (item == null) return NotFound();

        ViewBag.CreateID = item.LocationID;  // add this
        ViewBag.LocationID = new SelectList(
            _db.ClientLocation.Where(m => m.Client.ID == SelectedClient).OrderBy(m => m.LocationName),
            "ID", "LocationName", item.LocationID);
        ViewBag.Manufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name", item.Manufacturer?.ID);
        ViewBag.Model = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", item.Model?.ID);
        ViewBag.Device = new SelectList(_db.Devices.OrderBy(m => m.SerialNumber), "ID", "SerialNumber", item.Device?.ID);

        item.SelectedClientID = SelectedClient;
        return View(item);
    }

    [Log, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(IFormCollection collection)
    {
        var ID = Guid.Parse(collection["ID"]);
        var dil = _db.DeviceInLocations.Where(d => d.ID == ID).Single();

        try
        {
            try { dil.Location = _db.ClientLocation.Single(l => l.ID == Guid.Parse(collection["LocationID"])); dil.LocationID = dil.Location.ID; }
            catch { return EditErrorView(dil, "Lokacija nije ispravna"); }

            try
            {
                dil.DateOfGuarantieEnd = DateTime.Parse(collection["DateOfGuarantieEnd"]);
                dil.DateOfInstalation = DateTime.Parse(collection["DateOfInstalation"]);
            }
            catch { return EditErrorView(dil, "Datumi nisu ispravni"); }

            dil.DeviceInLocationDescription = collection["DeviceInLocationDescription"];

            try { dil.Model = _db.ProductModel.Single(l => l.ID == Guid.Parse(collection["Model"])); }
            catch { return EditErrorView(dil, "Model nije ispravan"); }

            try { dil.Device = _db.Devices.Single(l => l.ID == Guid.Parse(collection["Device"])); }
            catch { return EditErrorView(dil, "Uredjaj nije ispravan"); }

            var modelDevices = _db.ProductModel.Where(l => l.ID == dil.Model.ID).Include(m => m.Devices).Single();
            if (!modelDevices.Devices.Contains(dil.Device))
                return EditErrorView(dil, "Uredjaj ne pripada modelu");

            _db.Entry(dil).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", new { SelectedLocation = dil.Location.ID, SelectedClient = dil.SelectedClientID });
        }
        catch { return EditErrorView(dil, "Snimanje podataka nije uspelo"); }
    }

    public IActionResult Delete(Guid? id, Guid? SelectedClient = null)
    {
        if (!id.HasValue) return BadRequest();
        var item = _db.DeviceInLocations.Where(d => d.ID == id)
            .Include(d => d.Location)
            .Include(d => d.Device)
            .Include(d => d.Model)
            .SingleOrDefault();
        if (item == null) return NotFound();

        ViewBag.CreateID = item.LocationID;  // add this

        item.SelectedClientID = SelectedClient;
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var item = await _db.DeviceInLocations.FindAsync(id);
        _db.DeviceInLocations.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", new { SelectedLocation = item.LocationID, SelectedClient = item.SelectedClientID });
    }

    private IActionResult CreateErrorView(DeviceInLocation dil, string error)
    {
        ModelState.AddModelError("", error);
        ViewBag.LocationID = new SelectList(
            _db.ClientLocation.Where(m => m.Client.ID == dil.SelectedClientID).OrderBy(m => m.LocationName),
            "ID", "LocationName", dil.LocationID);
        ViewBag.Manufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        return View("Create", dil);
    }

    private IActionResult EditErrorView(DeviceInLocation dil, string error)
    {
        ModelState.AddModelError("", error);
        ViewBag.LocationID = new SelectList(_db.ClientLocation, "ID", "LocationName", dil.LocationID);
        ViewBag.Manufacturer = new SelectList(_db.Manufacturer.OrderBy(m => m.Name), "ID", "Name");
        ViewBag.Model = new SelectList(_db.ProductModel.OrderBy(m => m.Name), "ID", "Name", dil.Model?.ID);
        ViewBag.Device = new SelectList(_db.Devices.OrderBy(m => m.SerialNumber), "ID", "SerialNumber", dil.Device?.ID);
        return View("Edit", dil);
    }
}
