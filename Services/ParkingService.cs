using Microsoft.Data.Sqlite;
using ParkingLot.Models;

namespace ParkingLot.Services
{
    public class ParkingService : IParkingService
    {
        private readonly string _connectionString;
        private readonly int _totalSpots;
        private readonly decimal _hourlyFee;

        public ParkingService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _totalSpots = configuration.GetValue<int>("ParkingSettings:TotalSpots");
            _hourlyFee = configuration.GetValue<decimal>("ParkingSettings:HourlyFee");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ParkingRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TagNumber TEXT NOT NULL,
                    CheckIn TEXT NOT NULL,
                    CheckOut TEXT NULL,
                    AmountCharged REAL NULL
                );
                CREATE INDEX IF NOT EXISTS IX_Tag ON ParkingRecords (TagNumber);
                CREATE INDEX IF NOT EXISTS IX_CheckOut ON ParkingRecords (CheckOut);";
            cmd.ExecuteNonQuery();
        }

        public async Task<ParkingResult> CheckInAsync(string tagNumber)
        {
            tagNumber = tagNumber.Trim().ToUpper();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM ParkingRecords WHERE CheckOut IS NULL";
            var occupied = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            if (occupied >= _totalSpots)
                return new ParkingResult { Success = false, Message = "No spots available." };

            var existsCmd = conn.CreateCommand();
            existsCmd.CommandText = "SELECT COUNT(*) FROM ParkingRecords WHERE TagNumber = $tag AND CheckOut IS NULL";
            existsCmd.Parameters.AddWithValue("$tag", tagNumber);
            var exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());

            if (exists > 0)
                return new ParkingResult { Success = false, Message = "Car is already in the parking lot." };

            var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO ParkingRecords (TagNumber, CheckIn) VALUES ($tag, $checkIn)";
            insertCmd.Parameters.AddWithValue("$tag", tagNumber);
            insertCmd.Parameters.AddWithValue("$checkIn", DateTime.Now.ToString("o"));
            await insertCmd.ExecuteNonQueryAsync();

            return new ParkingResult { Success = true, Message = "Car checked in successfully." };
        }

        public async Task<ParkingResult> CheckOutAsync(string tagNumber)
        {
            tagNumber = tagNumber.Trim().ToUpper();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var selectCmd = conn.CreateCommand();
            selectCmd.CommandText = "SELECT Id, CheckIn FROM ParkingRecords WHERE TagNumber = $tag AND CheckOut IS NULL";
            selectCmd.Parameters.AddWithValue("$tag", tagNumber);

            using var reader = await selectCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return new ParkingResult { Success = false, Message = "Car is not registered in the parking lot." };

            var id = reader.GetInt32(0);
            var checkIn = DateTime.Parse(reader.GetString(1));
            await reader.CloseAsync();

            var checkOut = DateTime.Now;
            var duration = checkOut - checkIn;
            var hoursCharged = (int)Math.Ceiling(duration.TotalHours);
            if (hoursCharged < 1) hoursCharged = 1;
            var amount = hoursCharged * _hourlyFee;

            var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE ParkingRecords SET CheckOut = $checkOut, AmountCharged = $amount WHERE Id = $id";
            updateCmd.Parameters.AddWithValue("$checkOut", checkOut.ToString("o"));
            updateCmd.Parameters.AddWithValue("$amount", (double)amount);
            updateCmd.Parameters.AddWithValue("$id", id);
            await updateCmd.ExecuteNonQueryAsync();

            return new ParkingResult { Success = true, Message = $"Total amount charged: ${amount:F2}", AmountCharged = amount };
        }

        public async Task<AreaBViewModel> GetAreaBDataAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TagNumber, CheckIn FROM ParkingRecords WHERE CheckOut IS NULL ORDER BY CheckIn";

            var parkedCars = new List<ParkedCarViewModel>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var checkIn = DateTime.Parse(reader.GetString(1));
                var elapsed = DateTime.Now - checkIn;
                var hours = (int)Math.Floor(elapsed.TotalHours);
                string elapsedStr;
                if (hours > 0)
                    elapsedStr = hours == 1 ? "1 hour" : $"{hours} hours";
                else
                    elapsedStr = elapsed.Minutes == 1 ? "1 minute" : $"{elapsed.Minutes} minutes";

                parkedCars.Add(new ParkedCarViewModel
                {
                    TagNumber = reader.GetString(0),
                    CheckIn = checkIn.ToString("hh:mmtt"),
                    ElapsedTime = elapsedStr
                });
            }

            return new AreaBViewModel
            {
                TotalSpots = _totalSpots,
                HourlyFee = _hourlyFee,
                AvailableSpots = _totalSpots - parkedCars.Count,
                SpotsTaken = parkedCars.Count,
                ParkedCars = parkedCars
            };
        }

        public async Task<ParkingStats> GetStatsAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var occupiedCmd = conn.CreateCommand();
            occupiedCmd.CommandText = "SELECT COUNT(*) FROM ParkingRecords WHERE CheckOut IS NULL";
            var occupied = Convert.ToInt32(await occupiedCmd.ExecuteScalarAsync());

            var todayCmd = conn.CreateCommand();
            todayCmd.CommandText = "SELECT IFNULL(SUM(AmountCharged), 0) FROM ParkingRecords WHERE date(CheckOut) = date('now')";
            var todayRevenue = Convert.ToDecimal(await todayCmd.ExecuteScalarAsync());

            var thirtyDaysAgo = DateTime.Now.AddDays(-30).ToString("o");

            var avgCarsCmd = conn.CreateCommand();
            avgCarsCmd.CommandText = @"
                SELECT IFNULL(AVG(CAST(DailyCars AS REAL)), 0)
                FROM (
                    SELECT date(CheckIn) AS Day, COUNT(*) AS DailyCars
                    FROM ParkingRecords
                    WHERE CheckIn >= $start
                    GROUP BY date(CheckIn)
                )";
            avgCarsCmd.Parameters.AddWithValue("$start", thirtyDaysAgo);
            var avgCars = Convert.ToDouble(await avgCarsCmd.ExecuteScalarAsync());

            var avgRevCmd = conn.CreateCommand();
            avgRevCmd.CommandText = @"
                SELECT IFNULL(AVG(DailyRev), 0)
                FROM (
                    SELECT date(CheckOut) AS Day, SUM(AmountCharged) AS DailyRev
                    FROM ParkingRecords
                    WHERE CheckOut >= $start AND AmountCharged IS NOT NULL
                    GROUP BY date(CheckOut)
                )";
            avgRevCmd.Parameters.AddWithValue("$start", thirtyDaysAgo);
            var avgRev = Convert.ToDouble(await avgRevCmd.ExecuteScalarAsync());

            return new ParkingStats
            {
                AvailableSpots = _totalSpots - occupied,
                TodayRevenue = todayRevenue,
                AvgCarsPerDay = Math.Round(avgCars, 1),
                AvgRevenuePerDay = Math.Round(avgRev, 2)
            };
        }
    }
}
