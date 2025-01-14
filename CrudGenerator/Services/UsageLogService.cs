using CrudGenerator.Data;
using CrudGenerator.Models;

namespace CrudGenerator.Services
{
    public class UsageLogService : IUsageLogService
    {
        private readonly AppDbContext _context;

        public UsageLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogUsageAsync(UsageLog log)
        {
            _context.UsageLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
