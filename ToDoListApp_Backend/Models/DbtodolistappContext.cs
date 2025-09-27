using Microsoft.EntityFrameworkCore;

namespace ToDoListApp_Backend.Models;

public partial class DbtodolistappContext : DbContext
{
    public DbtodolistappContext()
    {
    }

    public DbtodolistappContext(DbContextOptions<DbtodolistappContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<Todo> Todos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PRIMARY");

            entity.ToTable("tags");

            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.CognitoSub)
                .HasMaxLength(50)
                .HasColumnName("cognito_sub");
            entity.Property(e => e.TagName)
                .HasMaxLength(50)
                .HasColumnName("tag_name");
        });

        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.TodoId).HasName("PRIMARY");

            entity.ToTable("todos");

            entity.Property(e => e.TodoId).HasColumnName("todo_id");
            entity.Property(e => e.CognitoSub)
                .HasMaxLength(50)
                .HasColumnName("cognito_sub");
            entity.Property(e => e.CreateAt)
                .HasColumnType("datetime")
                .HasColumnName("create_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("due_date");
            entity.Property(e => e.IsDone).HasColumnName("is_done");
            entity.Property(e => e.UpdateAt)
                .HasColumnType("datetime")
                .HasColumnName("update_at");

            entity.HasMany(d => d.Tags).WithMany(p => p.Todos)
                .UsingEntity<Dictionary<string, object>>(
                    "TodoTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_tag"),
                    l => l.HasOne<Todo>().WithMany()
                        .HasForeignKey("TodoId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_todo"),
                    j =>
                    {
                        j.HasKey("TodoId", "TagId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("todo_tag");
                        j.HasIndex(new[] { "TagId" }, "fk_tag");
                        j.IndexerProperty<int>("TodoId").HasColumnName("todo_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
