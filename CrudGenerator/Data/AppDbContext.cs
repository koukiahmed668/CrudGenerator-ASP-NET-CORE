using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CrudGenerator.Models;
using System.Text.Json;

namespace CrudGenerator.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UsageLog> UsageLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define UsageLog entity and ensure Id is the primary key
            modelBuilder.Entity<UsageLog>()
                .HasKey(u => u.Id); // Ensure Id is the primary key

            modelBuilder.Entity<UsageLog>()
               .Property(u => u.Id)
               .ValueGeneratedOnAdd();

            // Use ValueConverter to convert List<string> to a JSON string and vice versa
            var listStringConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),  // Serialize List<string> to JSON string
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) // Deserialize JSON string to List<string>
            );

            // Apply the converter to the properties
            modelBuilder.Entity<UsageLog>()
                .Property(u => u.GeneratedModels)
                .HasConversion(listStringConverter);

            modelBuilder.Entity<UsageLog>()
                .Property(u => u.Roles)
                .HasConversion(listStringConverter);
        }


    }
}
