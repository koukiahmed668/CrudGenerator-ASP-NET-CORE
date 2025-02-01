using CrudGenerator.Models;

namespace CrudGenerator.Interfaces
{
    public interface IUsageLogService
    {
        Task LogUsageAsync(UsageLog log);
    }



}
