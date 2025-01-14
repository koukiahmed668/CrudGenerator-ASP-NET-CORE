using CrudGenerator.Models;

namespace CrudGenerator.Services
{
    public interface IUsageLogService
    {
        Task LogUsageAsync(UsageLog log);
    }

   

}
