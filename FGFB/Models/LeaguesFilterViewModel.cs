using System;
using System.Collections.Generic;

namespace FGFB.Models
{
    public class LeaguesFilterViewModel
    {
        public decimal? MaxEntryFee { get; set; }
        public DateTime? DraftDateFrom { get; set; }
        public bool BestBallOnly { get; set; }
        public int SeasonYear { get; set; } = 2026;

        public List<League> Leagues { get; set; } = new();
    }
}