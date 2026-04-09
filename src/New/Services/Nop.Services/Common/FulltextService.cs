using Microsoft.EntityFrameworkCore;
using Nop.Data;

namespace Nop.Services.Common;

/// <summary>
/// Full-text search service — SQL Server specific.
/// Uses stored procedures (FullText_IsSupported, FullText_Enable, FullText_Disable)
/// that must be created during database installation.
/// </summary>
public class FulltextService : IFulltextService
{
    private readonly NopDbContext _dbContext;

    public FulltextService(NopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<bool> IsFullTextSupportedAsync()
    {
        try
        {
            var result = await _dbContext.Database
                .SqlQueryRaw<int>("EXEC [FullText_IsSupported]")
                .ToListAsync();
            return result.FirstOrDefault() > 0;
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task EnableFullTextAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("EXEC [FullText_Enable]");
    }

    public virtual async Task DisableFullTextAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("EXEC [FullText_Disable]");
    }
}
