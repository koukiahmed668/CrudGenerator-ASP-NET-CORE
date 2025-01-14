using CrudGenerator.Data;
using CrudGenerator.Services;
using System;
using Microsoft.EntityFrameworkCore;



namespace CrudGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(connectionString)
            );



            // Add services to the container
            builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
            builder.Services.AddScoped<IUsageLogService, UsageLogService>();

            // Configure CORS to allow requests from the Blazor client
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://localhost:7222") // Update with your Blazor frontend URL
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Serve static files (for Blazor WebAssembly client)
            app.UseStaticFiles();  // Ensure static files (like Blazor WebAssembly files) are served

            // Apply CORS policy
            app.UseCors("AllowFrontend");

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.UseDefaultFiles()
             .UseStaticFiles(new StaticFileOptions
             {
                 ServeUnknownFileTypes = true
             })
             .UseRouting();

            // Add fallback for Blazor routing (handle routes like /home, /about, etc.)
            app.MapFallbackToFile("index.html");  // This ensures Blazor WebAssembly routing works

            // Map API controllers
            app.MapControllers();

            app.Run();
        }
    }
}
