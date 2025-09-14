using HotelDemo.Infrastructure.Services.Models;

namespace HotelDemo.Infrastructure.Services;

public interface IAvailabilityService
{
    Task<AvailabilityResult> GetAvailabilityAsync(int propertyId, DateOnly startDate, int days);
}
