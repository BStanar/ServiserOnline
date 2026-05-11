using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ServiserOnline.Models;

public class AuditTrace : BaseModel
{
    public DateTime CreatedDate { get; set; }
    public TimeSpan Duration { get; set; }
    public string Controller { get; set; }
    public string Action { get; set; }
    public string Data { get; set; }
    public string User { get; set; }
}

public class BusinessArea : BaseModel
{
    [Required]
    [StringLength(250)]
    public string Name { get; set; }
}

public class CaseContactType : BaseModel
{
    [Display(Name = "Tip prijema intervencije ")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Name { get; set; }
}

public class Client : BaseModel
{
    public List<BusinessArea> BusinessArea { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Korisnik")]
    public string Name { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Registarski broj kompanije")]
    public string CompanyNumber { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "PDV broj")]
    public string VATNumber { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Addresa")]
    public string MainAddress1 { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Addresa")]
    public string MainAddress2 { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Mjesto")]
    public string City { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Postanski broj")]
    public string PostCode { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Telefon")]
    public string MainTelephone1 { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Telefon")]
    public string MainTelephone2 { get; set; }

    [StringLength(50)]
    public string Fax { get; set; }

    [StringLength(150)]
    public string Email { get; set; }

    [Display(Name = "Datum pocetka ugovora")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimeContractStart { get; set; }

    [Display(Name = "Datum zavrsetka ugovora")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimeContractEnd { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Broj ugovora")]
    public string ContractNumber { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Kontakt osoba")]
    public string MainContactPerson { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Pozicija kontakt osobe")]
    public string MainContactPersonRole { get; set; }

    public virtual ICollection<ContactPersonClient> Contacts { get; set; }
    public virtual ICollection<ClientLocation> Locations { get; set; }
}

public class ClientLocation : BaseModel
{
    public Guid ClientID { get; set; }
    public virtual Client Client { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Addressa")]
    public string Address { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Naziv lokacije")]
    public string LocationName { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Mjesto")]
    public string City { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Postanski broj")]
    public string PostCode { get; set; }

    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Telefon")]
    public string Telephone1 { get; set; }

    [Display(Name = "Telefon")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Telephone2 { get; set; }

    [Display(Name = "Opis")]
    [StringLength(550, ErrorMessage = "Maksimum 550 karaktera.")]
    public string Description { get; set; }

    public virtual ICollection<DeviceInLocation> DevicesInLocation { get; set; }
}

public class ContactPersonClient : BaseModel
{
    public Guid ClientID { get; set; }
    public virtual Client Client { get; set; }

    [Display(Name = "Pozicija u firmi")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string ContactPersonRole { get; set; }

    [Display(Name = "Ime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Name { get; set; }

    [Display(Name = "Prezime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Surname { get; set; }

    public string FullName => Name + " " + Surname;

    [Display(Name = "Telefon")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Telephone { get; set; }

    [Display(Name = "Mobilni")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Mobile { get; set; }

    [Display(Name = "Email")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Email { get; set; }
}

public class ContactPersonManufacturer : BaseModel
{
    public Guid ManufacturerID { get; set; }
    public virtual Manufacturer Manufacturer { get; set; }

    [Display(Name = "Pozicija u firmi")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string ContactPersonRole { get; set; }

    [Display(Name = "Ime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Name { get; set; }

    [Display(Name = "Prezime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Surname { get; set; }

    [Display(Name = "Telefon")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Telephone { get; set; }

    [Display(Name = "Mobilni")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Mobile { get; set; }

    [Display(Name = "Email")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Email { get; set; }
}

public class Device : BaseModel
{
    [Display(Name = "Model")]
    public ProductModel Model { get; set; }

    public Guid ModelID { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Serijski broj")]
    public string SerialNumber { get; set; }

    public virtual ICollection<DeviceInLocation> DeviceInLocations { get; set; }
}

public class DeviceInLocation : BaseModel
{
    [Display(Name = "Lokacija")]
    public virtual ClientLocation Location { get; set; }

    public Guid LocationID { get; set; }

    public Guid? SelectedClientID { get; set; }

    [Display(Name = "Datum Instalacije")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime DateOfInstalation { get; set; }

    [Display(Name = "Datum Isteka Garancije")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime DateOfGuarantieEnd { get; set; }

    [Display(Name = "Proizvodjac")]
    public Manufacturer Manufacturer { get; set; }

    [Display(Name = "Model")]
    public ProductModel Model { get; set; }

    [Display(Name = "Uredjaj")]
    public Device Device { get; set; }

    [Display(Name = "Komentar")]
    [StringLength(500, ErrorMessage = "Maksimum 500 karaktera.")]
    public string DeviceInLocationDescription { get; set; }
}

public class GuaranteeType : BaseModel
{
    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime - naziv garancije")]
    public string TypeName { get; set; }
}

public class InterventionType : BaseModel
{
    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime - naziv intervencije")]
    public string Name { get; set; }
}

public class Manufacturer : BaseModel
{
    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime - naziv")]
    public string Name { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Zemlja")]
    public string Country { get; set; }

    [StringLength(850, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Opis")]
    public string Description { get; set; }

    [Display(Name = "Broj uredjaja")]
    public int DeviceNo => Models?.Sum(m => m.DeviceNo) ?? 0;

    public virtual ICollection<ProductModel> Models { get; set; }
    public virtual ICollection<ContactPersonManufacturer> Contacts { get; set; }
}

public class PaymentOption : BaseModel
{
    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime - naziv")]
    public string OptionName { get; set; }
}

public class ProductModel : BaseModel
{
    [Display(Name = "Proizvodjac")]
    public Manufacturer Manufacturer { get; set; }

    public Guid ManufacturerID { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime - naziv")]
    public string Name { get; set; }

    [DataType(DataType.MultilineText)]
    [StringLength(1150)]
    [Display(Name = "Opis")]
    public string Description { get; set; }

    [Display(Name = "Broj uredjaja")]
    public int DeviceNo => Devices?.Count ?? 0;

    public virtual ICollection<Device> Devices { get; set; }
    public virtual ICollection<SparePart> SpareParts { get; set; }
}

public class ProductStatus : BaseModel
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Status uredjaja")]
    public string StatusName { get; set; }

    [StringLength(50)]
    [Display(Name = "Boja Engl.")]
    public string Color { get; set; }
}

public class Serviser : BaseModel
{
    [Display(Name = "Ime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Name { get; set; }

    [Display(Name = "Prezime")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Surname { get; set; }

    [Display(Name = "Telefon")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Telephone { get; set; }

    [Display(Name = "Mobilni")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Mobile { get; set; }

    [Display(Name = "Email")]
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    public string Email { get; set; }
}

public class SparePart : BaseModel
{
    [Display(Name = "Model")]
    public ProductModel Model { get; set; }

    public Guid ModelID { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Ime")]
    public string Name { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Serijski broj")]
    public string SerialNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Kataloški broj")]
    public string CatalogNumber { get; set; }

    [Display(Name = "Lager")]
    public double StockAmount { get; set; }

    [Display(Name = "Cijena")]
    public double Price { get; set; }
}

public class SparePartInCase : BaseModel
{
    public virtual SparePart SparePart { get; set; }
    public Guid SparePartID { get; set; }

    [Display(Name = "Kolicina")]
    public double Amount { get; set; }

    [Display(Name = "Ukupna Cijena")]
    public double TotalPrice => Amount * (SparePart?.Price ?? 0);

    [Display(Name = "Napomena")]
    public string Note { get; set; }
}

public class Case : BaseModel
{
    [StringLength(50, ErrorMessage = "Maksimum 50 karaktera.")]
    [Display(Name = "Servisni izvjestaj br.")]
    public string CaseServisNumber { get; set; }

    public int AutoIncrement { get; set; }

    [Display(Name = "Datum kreiranja")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimeCaseOpened { get; set; }

    [Display(Name = "Datum zavrsetka")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimeServiced { get; set; }

    [Display(Name = "Datum izvestaja")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimeOfReport { get; set; }

    [Display(Name = "Planirani Datum")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateTimePlanned { get; set; }

    [Display(Name = "Mjesec")]
    public string Month => DateTimeServiced?.ToString("MMMM", new CultureInfo("bs-Latn-BA"));

    [Display(Name = "Godina")]
    public string Year => DateTimeServiced?.ToString("yyyy");

    [Display(Name = "Datum posjete")]
    public string Day => DateTimeServiced?.ToString("dd");

    [Display(Name = "Model instrumenta")]
    public List<string> DeviceModels => Devices?.Select(o => o.Model?.Name).ToList();

    [Display(Name = "Serijski broj")]
    public List<string> DeviceSerials => Devices?.Select(o => o.Device?.SerialNumber).ToList();

    [Display(Name = "Sati puta")]
    public double HoursOfTravel { get; set; }

    [Display(Name = "Sati rada")]
    public double HoursOfWork { get; set; }

    [Display(Name = "Vrsta usluge")]
    public InterventionType InterventionType { get; set; }

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Ime servisera")]
    public string ServicePerson { get; set; }

    public Client Client { get; set; }

    [Display(Name = "Korisnik usluga")]
    public string ClientName => Client?.Name;

    public Case ContinuedFromCase { get; set; }

    [Display(Name = "Nastavak od br. izvjestaja")]
    public string ContinueFromNo => ContinuedFromCase?.CaseServisNumber ?? string.Empty;

    [Display(Name = "Nastavak od datum")]
    public string ContinueFromDate => ContinuedFromCase?.DateTimeServiced?.ToString() ?? string.Empty;

    public List<ClientLocation> Locations { get; set; }

    [Display(Name = "Odjel")]
    public List<string> LocationName => Locations?.Select(o => o.LocationName).ToList();

    [Display(Name = "Ulica i broj")]
    public List<string> LocationAddress => Locations?.Select(o => o.Address).ToList();

    [Display(Name = "Mjesto")]
    public List<string> LocationCity => Locations?.Select(o => o.City).ToList();

    [StringLength(150, ErrorMessage = "Maksimum 150 karaktera.")]
    [Display(Name = "Prisutna osoba")]
    public string AttendignPerson { get; set; }

    [StringLength(350, ErrorMessage = "Maksimum 350 karaktera.")]
    [Display(Name = "Broj ugovora / narudzbenice")]
    public string ContractNo { get; set; }

    [StringLength(500, ErrorMessage = "Maksimum 500 karaktera.")]
    [Display(Name = "Opis usluge")]
    public string ServiceDescription { get; set; }

    [StringLength(2500, ErrorMessage = "Maksimum 2500 karaktera.")]
    [Display(Name = "Opis izvrsene intervencije")]
    public string SInterventionDescription { get; set; }

    [Display(Name = "Rezervni dijelovi")]
    public List<SparePartInCase> SpareParts { get; set; }

    [StringLength(500, ErrorMessage = "Maksimum 500 karaktera.")]
    [Display(Name = "Nedovrseno")]
    public string NotFinishedDescription { get; set; }

    [Display(Name = "Intervencija zavrsena")]
    public bool Finished { get; set; }

    public bool OppositeFinished => !Finished;

    [StringLength(500, ErrorMessage = "Maksimum 500 karaktera.")]
    [Display(Name = "Uputstvo za naplatu")]
    public string PaymentInstruction { get; set; }

    public PayWhen PayWhen { get; set; }

    [Display(Name = "Naplati Odmah")]
    public bool PayNow => PayWhen == PayWhen.PayNow;

    [Display(Name = "Naplati kasnije")]
    public bool PayLater => PayWhen == PayWhen.PayLater;

    [Display(Name = "Bez naplate")]
    public bool NoPay => PayWhen == PayWhen.NoPay;

    [Display(Name = "Uredjaji")]
    public List<DeviceInLocation> Devices { get; set; }

    public GuaranteeType GuaranteeType { get; set; }
    public CaseContactType ContactType { get; set; }
    public PaymentOption PaymentOption { get; set; }
    public CaseStatus CaseStatus { get; set; }

    public byte[] ServiserSignature { get; set; }
    public byte[] ClientSignature { get; set; }

    [StringLength(500, ErrorMessage = "Maksimum 500 karaktera.")]
    [Display(Name = "Opis na prijemu")]
    public string AcceptingDescription { get; set; }

    public bool Deleted { get; set; }
}
