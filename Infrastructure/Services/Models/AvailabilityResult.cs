namespace HotelDemo.Infrastructure.Services.Models;

public class AvailabilityResult
{
    public required int PropertyId { get; init; }
    public required DateOnly Start { get; init; }
    public required int Days { get; init; }
    public List<DateOnly> Dates { get; init; } = new();
    public List<RoomTypeAvailability> RoomTypes { get; init; } = new();

    public (int[] avail, int[] total) Totals()
    {
        var avail = new int[Days];
        var total = new int[Days];
        foreach (var rt in RoomTypes)
        {
            for (int i = 0; i < Days; i++)
            {
                avail[i] += rt.Available[i];
                total[i] += rt.TotalPerDay[i];
            }
        }
        return (avail, total);
    }
}

public class RoomTypeAvailability
{
    public int RoomTypeId { get; init; }
    public string RoomTypeCode { get; init; } = "";
    public string RoomTypeName { get; init; } = "";
    public int[] TotalPerDay { get; init; } = Array.Empty<int>();   // πόσα δωμάτια υπάρχουν
    public int[] Available { get; init; } = Array.Empty<int>();     // πόσα μένουν διαθέσιμα
}
