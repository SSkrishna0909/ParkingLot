using ParkingLot.Models;

namespace ParkingLot.Services
{
    public interface IParkingService
    {
        Task<ParkingResult> CheckInAsync(string tagNumber);
        Task<ParkingResult> CheckOutAsync(string tagNumber);
        Task<AreaBViewModel> GetAreaBDataAsync();
        Task<ParkingStats> GetStatsAsync();
    }
}
