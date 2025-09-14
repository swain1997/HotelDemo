using System.Reflection;
using HotelDemo.Domain.Entities;
using HotelDemo.Domain.Entities.Common;
using HotelDemo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
    public DbSet<BookingGuest> BookingGuests => Set<BookingGuest>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --------------------
        // PROPERTIES
        // --------------------
        modelBuilder.Entity<Property>(b =>
        {
            b.ToTable("Properties");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired().HasMaxLength(20);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Email).IsRequired().HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Address).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);

            b.Property(x => x.DefaultCheckInTime).HasColumnType("time");
            b.Property(x => x.DefaultCheckOutTime).HasColumnType("time");

            b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Properties_Code");
            b.HasIndex(x => x.CountryCode);

            ConfigureAuditable(b);
        });

        // --------------------
        // ROOM TYPES
        // --------------------
        modelBuilder.Entity<RoomType>(b =>
        {
            b.ToTable("RoomTypes");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired().HasMaxLength(20);
            b.Property(x => x.Name).IsRequired().HasMaxLength(150);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.BedConfiguration).IsRequired().HasMaxLength(100);

            b.HasOne(x => x.Property)
             .WithMany(p => p.RoomTypes)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.PropertyId, x.Code }).IsUnique();
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.DisplayOrder).HasDefaultValue(0);

            ConfigureAuditable(b);
        });

        // --------------------
        // ROOMS
        // --------------------
        modelBuilder.Entity<Room>(b =>
        {
            b.ToTable("Rooms");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired().HasMaxLength(20);
            b.Property(x => x.BasePricePerNight).HasPrecision(10, 2);
            b.Property(x => x.Notes);

            b.HasOne(x => x.Property)
             .WithMany(p => p.Rooms)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.RoomType)
             .WithMany(rt => rt.Rooms)
             .HasForeignKey(x => x.RoomTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.PropertyId, x.Code })
             .IsUnique()
             .HasDatabaseName("UX_Rooms_Property_Code");

            b.HasIndex(x => x.RoomTypeId);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            ConfigureAuditable(b);
        });

        // --------------------
        // GUESTS
        // --------------------
        modelBuilder.Entity<Guest>(b =>
        {
            b.ToTable("Guests");
            b.HasKey(x => x.Id);

            b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            b.Property(x => x.LastName).IsRequired().HasMaxLength(100);

            b.Property(x => x.DateOfBirth).HasColumnType("date");
            b.Property(x => x.NationalityCode).HasMaxLength(2);

            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);

            b.Property(x => x.AddressLine).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.Property(x => x.CountryCode).HasMaxLength(2);

            b.Property(x => x.DocumentType).HasMaxLength(30);
            b.Property(x => x.DocumentNumber).HasMaxLength(50);

            b.HasOne(x => x.Property)
             .WithMany(p => p.Guests)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.PropertyId, x.LastName, x.FirstName })
             .HasDatabaseName("IX_Guests_PropertyId_LastName_FirstName");

            b.HasIndex(x => x.Email).HasDatabaseName("IX_Guests_Email");
            b.HasIndex(x => x.Phone).HasDatabaseName("IX_Guests_Phone");

            ConfigureAuditable(b);
        });

        // --------------------
        // BOOKINGS
        // --------------------
        modelBuilder.Entity<Booking>(b =>
        {
            b.ToTable("Bookings");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired().HasMaxLength(30);
            b.Property(x => x.Status)
                .HasConversion<string>()        // store enum as nvarchar
                .HasMaxLength(20);

            b.Property(x => x.CheckInDate).HasColumnType("date");
            b.Property(x => x.CheckOutDate).HasColumnType("date");
            b.Property(x => x.Nights);

            b.Property(x => x.Adults);
            b.Property(x => x.Children);
            b.Property(x => x.Infants);

            b.Property(x => x.ContactEmail).HasMaxLength(200);
            b.Property(x => x.ContactPhone).HasMaxLength(50);

            b.Property(x => x.TotalAmount).HasPrecision(10, 2);

            b.Property(x => x.CancelledByUserId).HasMaxLength(450);
            b.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(450);

            b.HasOne(x => x.Property)
             .WithMany(p => p.Bookings)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.LeadGuest)
             .WithMany(g => g.LeadBookings)
             .HasForeignKey(x => x.LeadGuestId)
             .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            b.HasIndex(x => new { x.PropertyId, x.Code })
             .IsUnique()
             .HasDatabaseName("UX_Bookings_Property_Code");

            b.HasIndex(x => new { x.PropertyId, x.CheckInDate });
            b.HasIndex(x => new { x.PropertyId, x.Status });

            // Check constraint: CheckInDate < CheckOutDate
            b.ToTable(tb =>
                tb.HasCheckConstraint("CK_Bookings_CheckDates", "[CheckInDate] < [CheckOutDate]")
            );

            ConfigureAuditable(b);
        });

        // --------------------
        // BOOKING ROOMS
        // --------------------
        modelBuilder.Entity<BookingRoom>(b =>
        {
            b.ToTable("BookingRooms");
            b.HasKey(x => x.Id);

            b.Property(x => x.CheckInDate).HasColumnType("date");
            b.Property(x => x.CheckOutDate).HasColumnType("date");
            b.Property(x => x.LineTotal).HasPrecision(10, 2);

            b.HasOne(x => x.Booking)
             .WithMany(p => p.BookingRooms)
             .HasForeignKey(x => x.BookingId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.RoomType)
             .WithMany()
             .HasForeignKey(x => x.RoomTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Room)
             .WithMany(r => r.BookingRooms)
             .HasForeignKey(x => x.RoomId)
             .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            b.HasIndex(x => x.BookingId);
            b.HasIndex(x => new { x.RoomId, x.CheckInDate });

            b.ToTable(tb =>
                tb.HasCheckConstraint("CK_BookingRooms_CheckDates", "[CheckInDate] < [CheckOutDate]")
            );

            ConfigureAuditable(b);
        });

        // --------------------
        // BOOKING GUESTS
        // --------------------
        modelBuilder.Entity<BookingGuest>(b =>
        {
            b.ToTable("BookingGuests");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Booking)
             .WithMany(p => p.BookingGuests)
             .HasForeignKey(x => x.BookingId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.BookingRoom)
             .WithMany(r => r.BookingGuests)
             .HasForeignKey(x => x.BookingRoomId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Guest)
             .WithMany(g => g.BookingGuests)
             .HasForeignKey(x => x.GuestId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.BookingId);
            b.HasIndex(x => x.BookingRoomId);
            b.HasIndex(x => x.GuestId);

            ConfigureAuditable(b);
        });

        // --------------------
        // PAYMENTS
        // --------------------
        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.HasKey(x => x.Id);

            b.Property(x => x.Method).IsRequired().HasMaxLength(20);
            b.Property(x => x.Amount).HasPrecision(10, 2);
            b.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(450);

            b.HasOne(x => x.Property)
             .WithMany()
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Booking)
             .WithMany(p => p.Payments)
             .HasForeignKey(x => x.BookingId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.BookingId).HasDatabaseName("IX_Payments_BookingId");
            b.HasIndex(x => x.PropertyId).HasDatabaseName("IX_Payments_PropertyId");

            ConfigureAuditable(b);
        });
    }

    // ----------------------------------------------------
    // Helper: κοινή ρύθμιση Auditable (CreatedAt/UpdatedAt)
    // ----------------------------------------------------
    private static void ConfigureAuditable<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> b)
        where TEntity : AuditableEntity
    {
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
    }

    // Αυτόματο γέμισμα CreatedAt / UpdatedAt
    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false; // μην αλλάζεις CreatedAt
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
