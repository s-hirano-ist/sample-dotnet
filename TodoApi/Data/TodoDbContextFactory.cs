using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// dotnet ef migrations addなど、コマンドラインからMigrationを作るときに使うFactoryです。
// 本番用MigrationはPostgreSQLを基準に生成します。
public class TodoDbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TodoDbContext>();
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__TodoDatabase"
        ) ?? "Host=localhost;Port=5432;Database=todo;Username=todo;Password=todo";

        optionsBuilder.UseNpgsql(connectionString);

        return new TodoDbContext(optionsBuilder.Options);
    }
}
