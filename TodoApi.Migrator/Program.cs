using Microsoft.EntityFrameworkCore;

var connectionString = GetConnectionString(args);

var options = new DbContextOptionsBuilder<TodoDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var dbContext = new TodoDbContext(options);
await dbContext.Database.MigrateAsync();

static string GetConnectionString(string[] args)
{
    var connectionOptionIndex = Array.IndexOf(args, "--connection");

    if (connectionOptionIndex >= 0 && connectionOptionIndex + 1 < args.Length)
    {
        return args[connectionOptionIndex + 1];
    }

    return Environment.GetEnvironmentVariable("ConnectionStrings__TodoDatabase")
        ?? throw new InvalidOperationException(
            "ConnectionStrings__TodoDatabase or --connection must be configured."
        );
}
