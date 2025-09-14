using HotelDemo.Domain.Entities.Common;

namespace HotelDemo.Domain.Entities;

public class Guest : AuditableEntity
{
    public int Id { get; set; }
    public int PropertyId { get; set; }                 // “ανήκει” στο κατάλυμα

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public string? NationalityCode { get; set; }        // ISO 3166-1 alpha-2 (π.χ. "GR")

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }            // χώρα κατοικίας (ISO alpha-2)

    public string? DocumentType { get; set; }           // Passport, ID, ...
    public string? DocumentNumber { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public Property? Property { get; set; }
    public ICollection<BookingGuest> BookingGuests { get; set; } = new List<BookingGuest>();
    public ICollection<Booking> LeadBookings { get; set; } = new List<Booking>(); // ως LeadGuest
}
