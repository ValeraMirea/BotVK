using Microsoft.EntityFrameworkCore;

namespace Bot.DataBase
{
    public class ApplicationContext : DbContext
    {
        public DbSet<VkUser> VkUsers { get; set; }

        public ApplicationContext()
        {
            Database.EnsureCreated(); // проверяем наличие таблиц базы данных
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=VkBot;Username=postgres;Password=1111");
        }
    }
}