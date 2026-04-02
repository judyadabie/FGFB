using FGFB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Web;

namespace FGFB.Pages.Contest
{
    public class ContestStandingsModel : PageModel
    {
        public List<StandingRow> standings = new List<StandingRow>();

        public void ShowDetail(int id)
        {
            //HttpUtility.ParseQueryString(Uri).Get("entryid");
        }

        
        public void OnGet()
        {
            
            SqlConnection cn = new SqlConnection("Data Source=tcp:s25.winhost.com;Initial Catalog=DB_123152_nfldb;User ID=DB_123152_nfldb_user;Password=Blitzen07;Integrated Security=False;TrustServerCertificate=True;");
            try
            {
                
                cn.Open();
                SqlCommand cmd = new SqlCommand("GetStandings", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter parm = new SqlParameter("@Season", SqlDbType.Int);
                parm.Value = 2025;
                parm.Direction = ParameterDirection.Input;
                cmd.Parameters.Add(parm);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable results = new DataTable("searchresults");
                try
                {
                    da.Fill(results);
                    foreach(DataRow dr in results.Rows)
                    {
                        StandingRow standingRow = new StandingRow();
                        standingRow.EntryName = dr.Field<string>("EntryName");
                        standingRow.EntryId = dr.Field<int>("entryid");
                        standingRow.SeasonScore = dr.Field<decimal>("SeasonScore");
                        standingRow.currentRank = dr.Field<int>("ranking");
                        standingRow.status = dr.Field<string>("status");
                        standingRow.PlayOffWeek1 = dr.Field<decimal>("PlayOffWeek1");
                        standingRow.PlayOffWeek2 = dr.Field<decimal>("PlayOffWeek2");
                        standingRow.PlayOffWeek3 = dr.Field<decimal>("PlayOffWeek3");
                        standingRow.PlayOffWeek4 = dr.Field<decimal>("PlayOffWeek4");
                        standingRow.FinalsScore = standingRow.PlayOffWeek4 + standingRow.PlayOffWeek3;
                        if (standingRow.status == null)
                            standingRow.status = "Alive";
                        standings.Add(standingRow);
                        
                    }

                }
                catch (Exception ex)
                {

                }
            }
            catch (Exception ex)
            {
            }

        }
    }
}
