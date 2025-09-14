using HotelDemo.Domain.Entities.Common;
using HotelDemo.Domain.Enums;

namespace HotelDemo.Domain.Entities;

public class Booking : AuditableEntity
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    public required string Code { get; set; }           // unique per Property
    public BookingStatus Status { get; set; }           // θα το χαρτογραφήσουμε σε nvarchar(20)

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Nights { get; set; }

    public int Adults { get; set; }
    public int Children { get; set; }
    public int Infants { get; set; }

    public int? LeadGuestId { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public decimal TotalAmount { get; set; }            // decimal(10,2)
    public string? Notes { get; set; }

    public DateTimeOffset BookedAt { get; set; }
    public DateTimeOffset? CheckInAt { get; set; }
    public DateTimeOffset? CheckOutAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancelledByUserId { get; set; }      // FK -> AspNetUsers
    public string CreatedByUserId { get; set; } = null!;

    // Navigation
    public Property? Property { get; set; }
    public Guest? LeadGuest { get; set; }

    public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
    public ICollection<BookingGuest> BookingGuests { get; set; } = new List<BookingGuest>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
