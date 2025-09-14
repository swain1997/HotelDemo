using HotelDemo.Domain.Entities;
using HotelDemo.Domain.Enums;
using HotelDemo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    public BookingService(AppDbContext db) => _db = db;

    public async Task<string> GenerateNextCodeAsync(int propertyId, int year)
    {
        var count = await _db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.BookedAt.Year == year);
        return $"B-{year}-{count + 1:0000}";
    }

    public async Task<Booking> CreateAsync(
        int propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        int adults,
        int children,
        int infants,
        string createdByUserId,
        int? leadGuestId = null)
    {
        if (checkIn >= checkOut) throw new InvalidOperationException("Check-in must be earlier than check-out.");
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var code = await GenerateNextCodeAsync(propertyId, DateTime.UtcNow.Year);

        var booking = new Booking
        {
            PropertyId = propertyId,
            Code = code,
            Status = BookingStatus.Tentative,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Nights = nights,
            Adults = adults,
            Children = children,
            Infants = infants,
            LeadGuestId = leadGuestId,
            TotalAmount = 0m,
            BookedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    // ---------------- Details ----------------
    public async Task UpdateDetailsAsync(int bookingId, DateOnly checkIn, DateOnly checkOut,
        int adults, int children, int infants, string? contactEmail, string? contactPhone, string? notes, BookingStatus status)
    {
        if (checkIn >= checkOut) throw new InvalidOperationException("Check-in must be earlier than check-out.");
        var b = await _db.Bookings.FindAsync(bookingId) ?? throw new InvalidOperationException("Booking not found.");
        b.CheckInDate = checkIn;
        b.CheckOutDate = checkOut;
        b.Nights = checkOut.DayNumber - checkIn.DayNumber;
        b.Adults = adults;
        b.Children = children;
        b.Infants = infants;
        b.ContactEmail = contactEmail;
        b.ContactPhone = contactPhone;
        b.Notes = notes;
        b.Status = status;

        // sync child lines dates? (απλό demo: κρατάμε τα πεδία των γραμμών ως έχουν)
        await _db.SaveChangesAsync();

        await RecalculateTotalsAsync(bookingId);
    }

    // ---------------- Rooms ----------------
    public async Task<BookingRoom> AddRoomAsync(int bookingId, int roomTypeId, int? roomId = null)
    {
        var b = await _db.Bookings.FindAsync(bookingId) ?? throw new InvalidOperationException("Booking not found.");
        var nights = b.Nights;

        decimal price = 0m;
        if (roomId.HasValue)
        {
            var room = await _db.Rooms.Where(r => r.Id == roomId.Value).Select(r => new { r.BasePricePerNight }).FirstOrDefaultAsync();
            if (room != null) price = room.BasePricePerNight;
        }

        var line = new BookingRoom
        {
            BookingId = bookingId,
            RoomTypeId = roomTypeId,
            RoomId = roomId,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            Nights = nights,
            Adults = Math.Max(1, b.Adults), // demo
            Children = 0,
            Infants = 0,
            LineTotal = price * nights
        };

        _db.BookingRooms.Add(line);
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(bookingId);
        return line;
    }

    public async Task AssignRoomAsync(int bookingRoomId, int? roomId)
    {
        var line = await _db.BookingRooms.Include(l => l.Booking).FirstOrDefaultAsync(l => l.Id == bookingRoomId)
            ?? throw new InvalidOperationException("BookingRoom not found.");

        line.RoomId = roomId;

        decimal price = 0m;
        if (roomId.HasValue)
        {
            var room = await _db.Rooms.Where(r => r.Id == roomId.Value).Select(r => new { r.BasePricePerNight }).FirstOrDefaultAsync();
            if (room != null) price = room.BasePricePerNight;
        }
        line.LineTotal = price * line.Nights;

        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(line.BookingId);
    }

    public async Task RemoveRoomAsync(int bookingRoomId)
    {
        var line = await _db.BookingRooms.FindAsync(bookingRoomId) ?? throw new InvalidOperationException("Line not found.");
        var bookingId = line.BookingId;
        _db.BookingRooms.Remove(line);
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(bookingId);
    }

    // ---------------- Guests ----------------
    public async Task<Guest> AddGuestQuickAsync(int propertyId, string firstName, string lastName)
    {
        var g = new Guest
        {
            PropertyId = propertyId,
            FirstName = firstName,
            LastName = lastName
        };
        _db.Guests.Add(g);
        await _db.SaveChangesAsync();
        return g;
    }

    public async Task<BookingGuest> AttachGuestAsync(int bookingId, int guestId, bool isLead)
    {
        var b = await _db.Bookings.FindAsync(bookingId) ?? throw new InvalidOperationException("Booking not found.");

        var bg = new BookingGuest
        {
            BookingId = bookingId,
            GuestId = guestId,
            IsLeadGuest = isLead
        };
        _db.BookingGuests.Add(bg);

        if (isLead)
        {
            // clear others & set lead on booking
            var others = _db.BookingGuests.Where(x => x.BookingId == bookingId && x.GuestId != guestId);
            await others.ForEachAsync(x => x.IsLeadGuest = false);

            b.LeadGuestId = guestId;
        }

        await _db.SaveChangesAsync();
        return bg;
    }

    public async Task RemoveGuestAsync(int bookingGuestId)
    {
        var bg = await _db.BookingGuests.FindAsync(bookingGuestId) ?? throw new InvalidOperationException("BookingGuest not found.");
        var bookingId = bg.BookingId;
        _db.BookingGuests.Remove(bg);
        await _db.SaveChangesAsync();

        // if it was lead, unset on booking
        var b = await _db.Bookings.FindAsync(bookingId);
        if (b != null && b.LeadGuestId == bg.GuestId)
        {
            b.LeadGuestId = null;
            await _db.SaveChangesAsync();
        }
    }

    // ---------------- Payments ----------------
    public async Task<Payment> AddPaymentAsync(int bookingId, int propertyId, string method, decimal amount, string createdByUserId, DateTimeOffset? receivedAt = null)
    {
        var p = new Payment
        {
            BookingId = bookingId,
            PropertyId = propertyId,
            Method = method,
            Amount = amount,
            ReceivedAt = receivedAt ?? DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId
        };
        _db.Payments.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    // ---------------- Totals ----------------
    public async Task RecalculateTotalsAsync(int bookingId)
    {
        var b = await _db.Bookings
            .Include(x => x.BookingRooms)
            .FirstOrDefaultAsync(x => x.Id == bookingId) ?? throw new InvalidOperationException("Booking not found.");

        // Recalc nights on lines (αν ταιριάζουν με dates)
        foreach (var line in b.BookingRooms)
        {
            line.Nights = line.CheckOutDate.DayNumber - line.CheckInDate.DayNumber;

            decimal price = 0m;
            if (line.RoomId.HasValue)
            {
                var roomPrice = await _db.Rooms.Where(r => r.Id == line.RoomId.Value)
                    .Select(r => (decimal?)r.BasePricePerNight)
                    .FirstOrDefaultAsync();
                price = roomPrice ?? 0m;
            }
            line.LineTotal = price * line.Nights;
        }

        b.TotalAmount = b.BookingRooms.Sum(l => l.LineTotal);
        await _db.SaveChangesAsync();
    }
}
