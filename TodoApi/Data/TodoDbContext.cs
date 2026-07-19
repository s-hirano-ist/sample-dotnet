using Microsoft.EntityFrameworkCore;

// DbContextは、EF Coreでデータベースとやり取りする中心のクラスです。
// TodoDbContextには、このアプリで使うテーブルの情報を書きます。
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }

    // DbSet<TodoItem> は、TodoItemテーブルに対応するプロパティです。
    // LINQで検索したり、Add/Removeで保存対象を変更したりできます。
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(todo => todo.Id);

            entity.Property(todo => todo.Title)
                .IsRequired()
                .HasMaxLength(TodoValidation.MaxTitleLength);

            entity.Property(todo => todo.IsDone)
                .IsRequired();

            entity.Property(todo => todo.CreatedAt)
                .IsRequired();

            // 一覧で使うフィルターと並び替えに合わせてインデックスを作ります。
            // 複合インデックスは、IsDoneで絞り込みCreatedAtで並べる検索を想定しています。
            entity.HasIndex(todo => new { todo.IsDone, todo.CreatedAt, todo.Id })
                .HasDatabaseName("IX_Todos_IsDone_CreatedAt_Id");

            entity.HasIndex(todo => new { todo.CreatedAt, todo.Id })
                .HasDatabaseName("IX_Todos_CreatedAt_Id");

            entity.HasIndex(todo => new { todo.Title, todo.Id })
                .HasDatabaseName("IX_Todos_Title_Id");
        });
    }
}
