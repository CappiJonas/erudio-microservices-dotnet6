using Microsoft.EntityFrameworkCore;

namespace GeekShopping.Email.Model.Context
{
    public class SqlServerContext : DbContext
    {
        public SqlServerContext(DbContextOptions<SqlServerContext> options) : base(options) { }

        public DbSet<EmailLog> Emails { get; set; }
    }
}
