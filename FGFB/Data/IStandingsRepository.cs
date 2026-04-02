using FGFB.Models;



namespace FGFB.Data
{
    public interface IStandingsRepository
    {
        Task<IReadOnlyList<StandingRow>> GetStandingsAsync(int season, CancellationToken ct = default);
    }
}