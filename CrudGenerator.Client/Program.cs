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

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7286") });

            AppContext.SetSwitch("System.Globalization.PredefinedCulturesOnly", false);
            AppContext.SetSwitch("System.Globalization.EnableIcu", true);




            await builder.Build().RunAsync();
        }
    }
}
