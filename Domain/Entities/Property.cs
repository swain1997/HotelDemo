using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities
{
    public class Property : AuditableEntity
    {
        public int Id { get; set; }

        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string? Phone { get; set; }
        public required string? Address { get; set; }
        public required string? City { get; set; }
        public required string? PostalCode { get; set; }
        public required string CountryCode { get; set; }

        public TimeOnly DefaultCheckInTime { get; set; }
        public TimeOnly DefaultCheckOutTime { get; set; }

        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Guest> Guests { get; set; } = new List<Guest>();


    }
}
