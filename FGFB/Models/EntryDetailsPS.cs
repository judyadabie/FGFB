namespace FGFB.Models
{
    public class EntryDetailsPS
    {
        public List<Player> team {  get; set; }
   
        public int entryId { get; set; }
        public string name { get; set; }
        public int CurrentWeek {  get; set; }
        public decimal totalScore { get; set; }
        public string status { get; set; }

        public int currentRank { get; set; }
    }

    
}
