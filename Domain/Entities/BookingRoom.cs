using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities
{
    public class BookingRoom : AuditableEntity
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomTypeId { get; set; }
        public int? RoomId { get; set; }                    // optional until assignment

        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Nights { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }

        public decimal LineTotal { get; set; }              // decimal(10,2)
        public string? Notes { get; set; }

        // Navigation
        public Booking? Booking { get; set; }
        public RoomType? RoomType { get; set; }
        public Room? Room { get; set; }

        public ICollection<BookingGuest> BookingGuests { get; set; } = new List<BookingGuest>();
    }
}
