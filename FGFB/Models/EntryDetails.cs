namespace FGFB.Models
{
    public class EntryDetails
    {
        public List<Player> team {  get; set; }
   
        public int entryId { get; set; }
        public string name { get; set; }
        public int CurrentWeek {  get; set; }
        public decimal totalScore { get; set; }
        public string status { get; set; }

        public int currentRank { get; set; }
    }

    public class Player
    {
        public string playerName { get; set; }
        public string Tier { get; set; }
        public string playerTeam { get; set; }
        public int playerId { get; set; }
        public bool rookie { get; set; }

        public List<Scores> WeeklyScores {get; set;}
    }
    public class Scores
    {
        public int week { get; set; }
        public decimal pprscore { get; set; }
       
        public decimal bonus { get; set; }
        public decimal totalScore { get; set; }
        public bool winner { get; set; }
    }
}
