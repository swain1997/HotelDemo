using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities;

public class RoomType : AuditableEntity
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    public required string Code { get; set; }            // unique per Property
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int BaseOccupancy { get; set; }               // typically 2
    public int MaxOccupancy { get; set; }
    public required string BedConfiguration { get; set; } // e.g. "1xDouble"
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }

    
    public Property? Property { get; set; }
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
