using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// dotnet ef migrations add など、コマンドラインからマイグレーションを作るときに使うFactoryです。
// アプリ本体を起動しなくても、TodoDbContextの作り方が分かるようにします。
public class TodoDbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TodoDbContext>();
        optionsBuilder.UseSqlite("Data Source=todo.db");

        return new TodoDbContext(optionsBuilder.Options);
    }
}
