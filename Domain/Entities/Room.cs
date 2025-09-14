using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities;

public class Room : AuditableEntity
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int RoomTypeId { get; set; }

    public required string Code { get; set; }             // unique per Property
    public decimal BasePricePerNight { get; set; }        // decimal(10,2) στο Fluent API
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Property? Property { get; set; }
    public RoomType? RoomType { get; set; }
    public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
}
