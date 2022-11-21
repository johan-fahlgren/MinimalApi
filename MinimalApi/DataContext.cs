using Microsoft.EntityFrameworkCore;
using MinimalApi.Model;

namespace MinimalApi
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options) { }

        public DbSet<Todo> Todos => Set<Todo>();
        public DbSet<User> Users => Set<User>();
    }
}
