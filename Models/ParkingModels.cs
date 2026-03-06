namespace ParkingLot.Models
{
    public class ParkingRecord
    {
        public int Id { get; set; }
        public string TagNumber { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal? AmountCharged { get; set; }
    }

    public class ParkingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? AmountCharged { get; set; }
    }

    public class ParkingStats
    {
        public int AvailableSpots { get; set; }
        public decimal TodayRevenue { get; set; }
        public double AvgCarsPerDay { get; set; }
        public double AvgRevenuePerDay { get; set; }
    }

    public class AreaBViewModel
    {
        public int TotalSpots { get; set; }
        public decimal HourlyFee { get; set; }
        public int AvailableSpots { get; set; }
        public int SpotsTaken { get; set; }
        public List<ParkedCarViewModel> ParkedCars { get; set; } = new();
    }

    public class ParkedCarViewModel
    {
        public string TagNumber { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string ElapsedTime { get; set; } = string.Empty;
    }
}
