using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// TodoPersistenceServiceCollectionExtensionsは、DBプロバイダーごとのDI登録を隠します。
public static class TodoPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddTodoPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseConfigurationDefaults.Section))
            .ValidateOnStart();

        var databaseOptions = configuration
            .GetSection(DatabaseConfigurationDefaults.Section)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();
        var connectionString = configuration.GetConnectionString(
            ConfigurationDefaults.TodoDatabaseConnection
        );

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{ConfigurationDefaults.TodoDatabaseConnection} must be configured."
            );
        }

        if (string.Equals(databaseOptions.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<TodoDbContext>(options => options.UseSqlite(connectionString));
        }
        else
        {
            services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));
        }

        return services;
    }
}
