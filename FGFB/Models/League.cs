using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FGFB.Models
{
    [Table("Leagues")]
    public class League
    {
        [Key]
        [Column("league_id")]
        public long LeagueId { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("status")]
        public string Status { get; set; } = "";

        [Column("total_rosters")]
        public int TotalRosters { get; set; }

        [Column("season")]
        public int Season { get; set; }

        [Column("bestball")]
        public bool BestBall { get; set; }

        [Column("moneyleague")]
        public bool? MoneyLeague { get; set; }

        [Column("draftDate")]
        public DateTime? DraftDate { get; set; }

        [Column("draftOrderSet")]
        public bool? DraftOrderSet { get; set; }

        [Column("autostart")]
        public bool? AutoStart { get; set; }

        [Column("seasonyear")]
        public int? SeasonYear { get; set; }

        [Column("leaguetype")]
        public string? LeagueType { get; set; }

        [Column("entryfee", TypeName = "money")]
        public decimal? EntryFee { get; set; }

        [Column("joinlink")]
        public string? JoinLink { get; set; }
    }
}