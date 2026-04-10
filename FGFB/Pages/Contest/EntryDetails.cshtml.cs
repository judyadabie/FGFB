using FGFB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace FGFB.Pages.Contest
{
    public class EntryDetailsModel : PageModel
    {
        public EntryDetails entry = new EntryDetails();
        public void OnGet()
        {
            
            string id = HttpContext.Request.Query["entryid"];
            string name = HttpContext.Request.Query["entryName"];
            string rank = HttpContext.Request.Query["rank"];
            string status = HttpContext.Request.Query["status"];

            SqlConnection cn = new SqlConnection("Data Source=tcp:s25.winhost.com;Initial Catalog=DB_123152_nfldb;User ID=DB_123152_nfldb_user;Password=Blitzen07;Integrated Security=False;TrustServerCertificate=True;");
            try
            {

                cn.Open();
                SqlCommand cmd = new SqlCommand("GetEntryDetails", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter parm = new SqlParameter("@Season", SqlDbType.Int);
                parm.Value = 2026;
                parm.Direction = ParameterDirection.Input;
                cmd.Parameters.Add(parm);
                parm = new SqlParameter("@Entryid", SqlDbType.Int);
                parm.Value = id;
                parm.Direction = ParameterDirection.Input;
                cmd.Parameters.Add(parm);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable results = new DataTable("searchresults");
                List<Player> playerlist = new List<Player>();
                try
                {
                    da.Fill(results);
                    
                    entry.entryId = Convert.ToInt16( id);
                    entry.name = name;
                    entry.currentRank = Convert.ToInt16(rank);
                    entry.CurrentWeek = 0;
                    entry.totalScore = 0;
                    entry.status = status;
                    foreach (DataRow dr in results.Rows)
                    {
                        if(entry.CurrentWeek == 0)
                            entry.CurrentWeek = Convert.ToInt32(dr.Field<int>("currentWeek"));
                        if (entry.totalScore == 0)
                            entry.totalScore = dr.Field<decimal>("totalScore");
                        Player p = new Player();
                        p.playerName = dr.Field<string>("Name");
                        p.playerId = dr.Field<int>("playerId");
                        p.Tier = dr.Field<string>("Tier");
                        p.rookie = dr.Field<bool>("rookie");
                        p.playerTeam = dr.Field<string>("Team");

                        //Get Weekly Scores
                        List<Scores> scores = new List<Scores>();
                        cmd = new SqlCommand("GetWeeklyScoresContest", cn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        parm = new SqlParameter("@Season", SqlDbType.Int);
                        parm.Value = 2026;
                        parm.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(parm);
                        parm = new SqlParameter("@playerId", SqlDbType.Int);
                        parm.Value = p.playerId;
                        parm.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(parm);
                        SqlDataAdapter da2 = new SqlDataAdapter(cmd);
                        DataTable results2 = new DataTable("searchresults2");
                        da2.Fill(results2);
                        foreach (DataRow s in results2.Rows)
                        {
                            Scores score = new Scores();
                            score.totalScore = s.Field<decimal>("totalscore");
                            score.pprscore = s.Field<decimal>("pprscore");
                            score.bonus = s.Field<decimal>("rookieBonus");
                            score.week = s.Field<int>("week");
                            scores.Add(score);
                        }


                        p.WeeklyScores =scores;
                        playerlist.Add(p);
                    }
                    

                }
                catch (Exception ex)
                {

                }
                entry.team = SetWinners(playerlist, entry.CurrentWeek); 
            }
            catch (Exception ex)
            {
            }

        }

        private List<Player> SetWinners(List<Player> players, int currentWeek)
        {
            //compare QBs
            Player p1 = players.Find(f => f.Tier == "QB1-A");
            Player p2 = players.Find(f => f.Tier == "QB1-B");
            Player p3 = players.Find(f => f.Tier == "QB1-C");
           
            if (currentWeek > 17)
                currentWeek = 17;
            for (int i = 1; i <= currentWeek; i++)
            {
                if((p1.WeeklyScores.FirstOrDefault(s=> s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s=>s.week== i ).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s=> s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }
            //compare RBs
            p1 = players.Find(f => f.Tier == "RB1-A");
            p2 = players.Find(f => f.Tier == "RB1-B");
            p3 = players.Find(f => f.Tier == "RB1-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }
            //compare RB2s
            p1 = players.Find(f => f.Tier == "RB2-A");
            p2 = players.Find(f => f.Tier == "RB2-B");
            p3 = players.Find(f => f.Tier == "RB2-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }

            //compare WR1s
            p1 = players.Find(f => f.Tier == "WR1-A");
            p2 = players.Find(f => f.Tier == "WR1-B");
            p3 = players.Find(f => f.Tier == "WR1-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }
            //compare WR2s
            p1 = players.Find(f => f.Tier == "WR2-A");
            p2 = players.Find(f => f.Tier == "WR2-B");
            p3 = players.Find(f => f.Tier == "WR2-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }
            //compare TE1s
            p1 = players.Find(f => f.Tier == "TE1-A");
            p2 = players.Find(f => f.Tier == "TE1-B");
            p3 = players.Find(f => f.Tier == "TE1-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }
            //compare DST
            p1 = players.Find(f => f.Tier == "DEF-A");
            p2 = players.Find(f => f.Tier == "DEF-B");
            p3 = players.Find(f => f.Tier == "DEF-C");
            for (int i = 1; i <= currentWeek; i++)
            {
                if ((p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p1.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else if ((p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p1.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore) &&
                    (p2.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore >= p3.WeeklyScores.FirstOrDefault(s => s.week == i).totalScore))
                {
                    p2.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;
                }
                else p3.WeeklyScores.FirstOrDefault(s => s.week == i).winner = true;



            }


            return players;
        }


    }
    
}
