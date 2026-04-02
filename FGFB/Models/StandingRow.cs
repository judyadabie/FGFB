namespace FGFB.Models
{
    public class StandingRow
    {
        public decimal SeasonScore { get; set; }
        public string EntryName { get; set; } = string.Empty;
        public int EntryId { get; set; }
        public int currentRank { get; set; }
        public decimal PlayOffWeek1 { get; set; }
        public decimal PlayOffWeek2 {  get; set; }
        public decimal PlayOffWeek3 { get; set; }
        public decimal PlayOffWeek4 { get; set; }
        public decimal FinalsScore { get; set; }
        public string status { get; set; }

        public StandingRow() { }
    }
}
