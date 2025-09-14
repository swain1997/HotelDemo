using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities;

public class Payment : AuditableEntity
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int BookingId { get; set; }

    public required string Method { get; set; }         // Cash, CardPOS, CardOnline, BankTransfer
    public decimal Amount { get; set; }                 // positive; sign semantics via Type (απλή demo: μόνο εισπράξεις)
    public DateTimeOffset ReceivedAt { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    // Navigation
    public Property? Property { get; set; }
    public Booking? Booking { get; set; }
}
