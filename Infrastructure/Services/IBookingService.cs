using HotelDemo.Domain.Entities;

namespace HotelDemo.Infrastructure.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(
        int propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        int adults,
        int children,
        int infants,
        string createdByUserId,
        int? leadGuestId = null);

    Task<string> GenerateNextCodeAsync(int propertyId, int year);

    // --- Details ---
    Task UpdateDetailsAsync(int bookingId, DateOnly checkIn, DateOnly checkOut,
        int adults, int children, int infants,
        string? contactEmail, string? contactPhone, string? notes,
        Domain.Enums.BookingStatus status);

    // --- Rooms ---
    Task<BookingRoom> AddRoomAsync(int bookingId, int roomTypeId, int? roomId = null);
    Task AssignRoomAsync(int bookingRoomId, int? roomId);
    Task RemoveRoomAsync(int bookingRoomId);

    // --- Guests ---
    Task<Guest> AddGuestQuickAsync(int propertyId, string firstName, string lastName);
    Task<BookingGuest> AttachGuestAsync(int bookingId, int guestId, bool isLead);
    Task RemoveGuestAsync(int bookingGuestId);

    // --- Payments ---
    Task<Payment> AddPaymentAsync(int bookingId, int propertyId, string method, decimal amount, string createdByUserId, DateTimeOffset? receivedAt = null);

    // --- Totals ---
    Task RecalculateTotalsAsync(int bookingId);
}
