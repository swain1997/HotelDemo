using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities;

public class BookingGuest : AuditableEntity
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int? BookingRoomId { get; set; }             // optional link to specific room
    public int GuestId { get; set; }

    public bool IsLeadGuest { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Booking? Booking { get; set; }
    public BookingRoom? BookingRoom { get; set; }
    public Guest? Guest { get; set; }
}
