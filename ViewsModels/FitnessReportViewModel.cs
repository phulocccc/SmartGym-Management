using SmartGymManagement.Models;

namespace SmartGymManagement.ViewModels
{
    public class FitnessReportViewModel
    {
        public BMIRecord CurrentRecord { get; set; } = null!;

        public List<BMIRecord> History { get; set; }
            = new List<BMIRecord>();
    }
}