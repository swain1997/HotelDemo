using HotelDemo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Infrastructure.Data;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Αν υπάρχουν ήδη properties, θεωρούμε ότι έχει γίνει seeding
        if (await db.Properties.AnyAsync()) return;

        // ----- Properties -----
        var p1 = new Property
        {
            Code = "P-ATH",
            Name = "Athens Inn",
            Email = "info@athens.local",
            Phone = "+30 210 0000000",
            Address = "Syntagma 1",
            City = "Athens",
            PostalCode = "10557",
            CountryCode = "GR",
            DefaultCheckInTime = new TimeOnly(14, 0),
            DefaultCheckOutTime = new TimeOnly(11, 0)
        };

        var p2 = new Property
        {
            Code = "P-THS",
            Name = "Thess Suites",
            Email = "info@thess.local",
            Phone = "+30 2310 000000",
            Address = "Tsimiski 10",
            City = "Thessaloniki",
            PostalCode = "54624",
            CountryCode = "GR",
            DefaultCheckInTime = new TimeOnly(14, 0),
            DefaultCheckOutTime = new TimeOnly(11, 0)
        };

        db.Properties.AddRange(p1, p2);
        await db.SaveChangesAsync();

        // ----- RoomTypes (per property) -----
        var p1Std = new RoomType
        {
            PropertyId = p1.Id,
            Code = "STD",
            Name = "Standard",
            Description = "Standard room",
            BaseOccupancy = 2,
            MaxOccupancy = 2,
            BedConfiguration = "1xDouble",
            IsActive = true,
            DisplayOrder = 1
        };
        var p1Dlx = new RoomType
        {
            PropertyId = p1.Id,
            Code = "DLX",
            Name = "Deluxe",
            Description = "Deluxe room",
            BaseOccupancy = 2,
            MaxOccupancy = 3,
            BedConfiguration = "1xDouble+Sofa",
            IsActive = true,
            DisplayOrder = 2
        };
        var p2Std = new RoomType
        {
            PropertyId = p2.Id,
            Code = "STD",
            Name = "Standard",
            Description = "Standard room",
            BaseOccupancy = 2,
            MaxOccupancy = 2,
            BedConfiguration = "2xSingle",
            IsActive = true,
            DisplayOrder = 1
        };

        db.RoomTypes.AddRange(p1Std, p1Dlx, p2Std);
        await db.SaveChangesAsync();

        // ----- Rooms -----
        var rooms = new List<Room>
        {
            // P-ATH
            new Room { PropertyId = p1.Id, RoomTypeId = p1Std.Id, Code = "101", BasePricePerNight = 70m, IsActive = true },
            new Room { PropertyId = p1.Id, RoomTypeId = p1Std.Id, Code = "102", BasePricePerNight = 70m, IsActive = true },
            new Room { PropertyId = p1.Id, RoomTypeId = p1Std.Id, Code = "103", BasePricePerNight = 70m, IsActive = true },
            new Room { PropertyId = p1.Id, RoomTypeId = p1Dlx.Id, Code = "201", BasePricePerNight = 95m, IsActive = true },
            new Room { PropertyId = p1.Id, RoomTypeId = p1Dlx.Id, Code = "202", BasePricePerNight = 95m, IsActive = true },
            new Room { PropertyId = p1.Id, RoomTypeId = p1Dlx.Id, Code = "203", BasePricePerNight = 95m, IsActive = true },

            // P-THS
            new Room { PropertyId = p2.Id, RoomTypeId = p2Std.Id, Code = "A1", BasePricePerNight = 60m, IsActive = true },
            new Room { PropertyId = p2.Id, RoomTypeId = p2Std.Id, Code = "A2", BasePricePerNight = 60m, IsActive = true },
            new Room { PropertyId = p2.Id, RoomTypeId = p2Std.Id, Code = "A3", BasePricePerNight = 60m, IsActive = true }
        };

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
    }
}
