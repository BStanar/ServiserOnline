using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ServiserOnline.Models;

public class ApplicationUser : IdentityUser
{
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentOption> PaymentOption { get; set; }
    public DbSet<GuaranteeType> GuaranteeType { get; set; }
    public DbSet<BusinessArea> BusinessArea { get; set; }
    public DbSet<ClientLocation> ClientLocation { get; set; }
    public DbSet<ProductModel> ProductModel { get; set; }
    public DbSet<Client> Client { get; set; }
    public DbSet<Manufacturer> Manufacturer { get; set; }
    public DbSet<InterventionType> InterventionType { get; set; }
    public DbSet<Case> Case { get; set; }
    public DbSet<ContactPersonManufacturer> ContactPersonManufacturers { get; set; }
    public DbSet<ContactPersonClient> ContactPersonClients { get; set; }
    public DbSet<SparePart> SpareParts { get; set; }
    public DbSet<SparePartInCase> SparePartsInCase { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceInLocation> DeviceInLocations { get; set; }
    public DbSet<Serviser> Serviser { get; set; }
    public DbSet<CaseContactType> CaseContactTypes { get; set; }
    public DbSet<AuditTrace> AuditTrace { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Table name mappings
        builder.Entity<Case>().ToTable("Cases");
        builder.Entity<Client>().ToTable("Clients");
        builder.Entity<Manufacturer>().ToTable("Manufacturers");
        builder.Entity<ProductModel>().ToTable("ProductModels");
        builder.Entity<ClientLocation>().ToTable("ClientLocations");
        builder.Entity<BusinessArea>().ToTable("BusinessAreas");
        builder.Entity<GuaranteeType>().ToTable("GuaranteeTypes");
        builder.Entity<InterventionType>().ToTable("InterventionTypes");
        builder.Entity<PaymentOption>().ToTable("PaymentOptions");
        builder.Entity<Serviser>().ToTable("Servisers");
        builder.Entity<AuditTrace>().ToTable("AuditTraces");
        builder.Entity<SparePartInCase>().ToTable("SparePartInCases");

        // Cases FK mappings
        builder.Entity<Case>()
            .HasOne(c => c.Client).WithMany()
            .HasForeignKey("Client_ID").OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Case>()
            .HasOne(c => c.ContinuedFromCase).WithMany()
            .HasForeignKey("ContinuedFromCase_ID").OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Case>()
            .HasOne(c => c.InterventionType).WithMany()
            .HasForeignKey("InterventionType_ID");
        builder.Entity<Case>()
            .HasOne(c => c.ContactType).WithMany()
            .HasForeignKey("ContactType_ID");
        builder.Entity<Case>()
            .HasOne(c => c.GuaranteeType).WithMany()
            .HasForeignKey("GuaranteeType_ID");
        builder.Entity<Case>()
            .HasOne(c => c.PaymentOption).WithMany()
            .HasForeignKey("PaymentOption_ID");

        // Case collections - EF6 used shadow FKs


        // DeviceInLocation FK mappings
        builder.Entity<DeviceInLocation>()
    .HasOne(d => d.Device).WithMany(d => d.DeviceInLocations)
    .HasForeignKey("Device_ID");
        builder.Entity<DeviceInLocation>()
            .HasOne(d => d.Manufacturer).WithMany()
            .HasForeignKey("Manufacturer_ID");
        builder.Entity<DeviceInLocation>()
            .HasOne(d => d.Model).WithMany()
            .HasForeignKey("Model_ID");
        builder.Entity<DeviceInLocation>()
            .Property<Guid?>("Case_ID");

        // SparePartInCase -> Case
        builder.Entity<SparePartInCase>()
            .Property<Guid?>("Case_ID");

        // Case -> Locations (Case_ID column on ClientLocations)
        builder.Entity<Case>()
            .HasMany(c => c.Locations)
            .WithOne()
            .HasForeignKey("Case_ID");

        // Case -> Devices (Case_ID column on DeviceInLocations)  
        builder.Entity<Case>()
            .HasMany(c => c.Devices)
            .WithOne()
            .HasForeignKey("Case_ID");

        // Case -> SpareParts (via Case_ID on SparePartInCases)
        builder.Entity<Case>()
            .HasMany(c => c.SpareParts)
            .WithOne()
            .HasForeignKey("Case_ID");

        // BusinessArea -> Client
        builder.Entity<BusinessArea>()
            .Property<Guid?>("Client_ID");

        // Devices -> Case (shadow FK)
        builder.Entity<Device>()
            .Property<Guid?>("Case_ID");

        // Ignore properties not in DB
        builder.Entity<Client>().Ignore(c => c.MainContactPerson);
        builder.Entity<Client>().Ignore(c => c.MainContactPersonRole);

        // Ignore computed properties
        builder.Entity<Case>().Ignore(c => c.Month);
        builder.Entity<Case>().Ignore(c => c.Year);
        builder.Entity<Case>().Ignore(c => c.Day);
        builder.Entity<Case>().Ignore(c => c.DeviceModels);
        builder.Entity<Case>().Ignore(c => c.DeviceSerials);
        builder.Entity<Case>().Ignore(c => c.ClientName);
        builder.Entity<Case>().Ignore(c => c.ContinueFromNo);
        builder.Entity<Case>().Ignore(c => c.ContinueFromDate);
        builder.Entity<Case>().Ignore(c => c.LocationName);
        builder.Entity<Case>().Ignore(c => c.LocationAddress);
        builder.Entity<Case>().Ignore(c => c.LocationCity);
        builder.Entity<Case>().Ignore(c => c.OppositeFinished);
        builder.Entity<Case>().Ignore(c => c.PayNow);
        builder.Entity<Case>().Ignore(c => c.PayLater);
        builder.Entity<Case>().Ignore(c => c.NoPay);

        builder.Entity<Manufacturer>().Ignore(m => m.DeviceNo);
        builder.Entity<ProductModel>().Ignore(m => m.DeviceNo);
        builder.Entity<ContactPersonClient>().Ignore(c => c.FullName);
        builder.Entity<SparePartInCase>().Ignore(s => s.TotalPrice);
    }
}