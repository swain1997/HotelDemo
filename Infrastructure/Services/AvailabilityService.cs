using HotelDemo.Infrastructure.Data;
using HotelDemo.Infrastructure.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelDemo.Infrastructure.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _db;
    public AvailabilityService(AppDbContext db) => _db = db;

    public async Task<AvailabilityResult> GetAvailabilityAsync(int propertyId, DateOnly startDate, int days)
    {
        var endDate = startDate.AddDays(days);
        var dates = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();

        // Room types & total rooms per type
        var roomTypes = await _db.RoomTypes
            .Where(rt => rt.PropertyId == propertyId && rt.IsActive)
            .OrderBy(rt => rt.DisplayOrder).ThenBy(rt => rt.Name)
            .Select(rt => new
            {
                rt.Id,
                rt.Code,
                rt.Name,
                Total = _db.Rooms.Count(r => r.PropertyId == propertyId && r.RoomTypeId == rt.Id && r.IsActive)
            })
            .ToListAsync();

        // BookingRooms που επικαλύπτουν το διάστημα (εξαιρούμε Cancelled/NoShow bookings)
        var booked = await _db.BookingRooms
            .Where(br =>
                br.Booking!.PropertyId == propertyId &&
                br.Booking.Status != HotelDemo.Domain.Enums.BookingStatus.Cancelled &&
                br.Booking.Status != HotelDemo.Domain.Enums.BookingStatus.NoShow &&
                br.CheckInDate < endDate && br.CheckOutDate > startDate)
            .Select(br => new { br.RoomTypeId, br.CheckInDate, br.CheckOutDate })
            .ToListAsync();

        var result = new AvailabilityResult
        {
            PropertyId = propertyId,
            Start = startDate,
            Days = days,
            Dates = dates
        };

        foreach (var rt in roomTypes)
        {
            var totalPerDay = Enumerable.Repeat(rt.Total, days).ToArray();
            var usedPerDay = new int[days];

            foreach (var br in booked.Where(x => x.RoomTypeId == rt.Id))
            {
                var d = br.CheckInDate < startDate ? startDate : br.CheckInDate;
                var until = br.CheckOutDate > endDate ? endDate : br.CheckOutDate;
                for (var day = d; day < until; day = day.AddDays(1))
                {
                    var idx = day.DayNumber - startDate.DayNumber; // safe index
                    if (idx >= 0 && idx < days) usedPerDay[idx]++;
                }
            }

            var available = totalPerDay.Zip(usedPerDay, (t, u) => Math.Max(0, t - u)).ToArray();

            result.RoomTypes.Add(new RoomTypeAvailability
            {
                RoomTypeId = rt.Id,
                RoomTypeCode = rt.Code,
                RoomTypeName = rt.Name,
                TotalPerDay = totalPerDay,
                Available = available
            });
        }

        return result;
    }
}
