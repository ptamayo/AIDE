using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Aide.Notifications.Models;

public partial class AideDbContext : DbContext
{
    private string _connectionString;

    public AideDbContext() { }

    public AideDbContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public AideDbContext(DbContextOptions<AideDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<notification> notification { get; set; }

    public virtual DbSet<notification_user> notification_user { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(_connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.26-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<notification>(entity =>
        {
            entity.HasKey(e => e.notification_id).HasName("PRIMARY");

            entity.HasIndex(e => e.date_created, "IX_DATE_CREATED").IsDescending();

            entity.HasIndex(e => new { e.date_created, e.target }, "IX_DATE_CREATED_AND_TARGET").IsDescending(true, false);

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.message)
                .IsRequired()
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.message_type)
                .IsRequired()
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.source)
                .IsRequired()
                .HasMaxLength(150)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.target)
                .HasMaxLength(150)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
        });

        modelBuilder.Entity<notification_user>(entity =>
        {
            entity.HasKey(e => e.notification_user_id).HasName("PRIMARY");

            entity.HasIndex(e => new { e.user_id, e.notification_id }, "IX_USER_AND_NOTIFICATION_IDS");

            entity.Property(e => e.date_created).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
