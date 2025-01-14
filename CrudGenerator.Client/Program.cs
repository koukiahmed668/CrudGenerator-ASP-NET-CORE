using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace CrudGenerator.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var baseAddress = builder.HostEnvironment.IsDevelopment()
                ? new Uri("https://localhost:7286")
                : new Uri("https://crudgenerator-asp-net-core.onrender.com");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = baseAddress });

            AppContext.SetSwitch("System.Globalization.PredefinedCulturesOnly", false);
            AppContext.SetSwitch("System.Globalization.EnableIcu", true);

            await builder.Build().RunAsync();
        }
    }
}
