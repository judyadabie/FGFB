using FGFB.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FGFB.Data
{
    public class StandingsRepository : IStandingsRepository
    {
        private readonly string _connectionString;


        public StandingsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
        }


        public async Task<IReadOnlyList<StandingRow>> GetStandingsAsync(int season, CancellationToken ct = default)
        {
            var results = new List<StandingRow>();


            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);


            await using var cmd = new SqlCommand("GetStandings", conn)
            {
                CommandType = CommandType.StoredProcedure
            };


            // IMPORTANT: using provided parameter name @Seasaon
            cmd.Parameters.Add(new SqlParameter("@Seasaon", SqlDbType.Int) { Value = season });


            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var ordSeasonScore = reader.GetOrdinal("SeasonScore");
            var ordEntryName = reader.GetOrdinal("EntryName");
            var ordEntryId = reader.GetOrdinal("entryid"); // why: proc returns lowercase name


            while (await reader.ReadAsync(ct))
            {
                var row = new StandingRow
                {
                    SeasonScore = !reader.IsDBNull(ordSeasonScore) ? reader.GetInt32(ordSeasonScore) : 0,
                    EntryName = !reader.IsDBNull(ordEntryName) ? reader.GetString(ordEntryName) : string.Empty,
                    EntryId = !reader.IsDBNull(ordEntryId) ? reader.GetInt32(ordEntryId) : 0
                };
                results.Add(row);
            }


            return results;
        }
    }
}
