using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Aide.Claims.Models;

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

    public virtual DbSet<claim> claim { get; set; }

    public virtual DbSet<claim_document> claim_document { get; set; }

    public virtual DbSet<claim_document_type> claim_document_type { get; set; }

    public virtual DbSet<claim_probatory_document> claim_probatory_document { get; set; }

    public virtual DbSet<claim_probatory_document_media> claim_probatory_document_media { get; set; }

    public virtual DbSet<claim_signature> claim_signature { get; set; }

    public virtual DbSet<claim_status> claim_status { get; set; }

    public virtual DbSet<claim_type> claim_type { get; set; }

    public virtual DbSet<document> document { get; set; }

    public virtual DbSet<document_type> document_type { get; set; }

    public virtual DbSet<media> media { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(_connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.32-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<claim>(entity =>
        {
            entity.HasKey(e => e.claim_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.HasIndex(e => e.claim_status_id, "idx_claim_claim_status_id");

            entity.Property(e => e.claim_number).HasMaxLength(50);
            entity.Property(e => e.customer_full_name)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.external_order_number).HasMaxLength(50);
            entity.Property(e => e.policy_number).HasMaxLength(50);
            entity.Property(e => e.policy_subsection).HasMaxLength(50);
            entity.Property(e => e.report_number).HasMaxLength(50);
            entity.Property(e => e.source).HasMaxLength(5);
        });

        modelBuilder.Entity<claim_document>(entity =>
        {
            entity.HasKey(e => e.claim_document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.HasIndex(e => e.claim_id, "IX_CLAIM_ID");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<claim_document_type>(entity =>
        {
            entity.HasKey(e => e.claim_document_type_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<claim_probatory_document>(entity =>
        {
            entity.HasKey(e => e.claim_probatory_document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.HasIndex(e => e.claim_id, "IX_CLAIM_ID");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<claim_probatory_document_media>(entity =>
        {
            entity.HasKey(e => e.claim_probatory_document_media_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.HasIndex(e => e.claim_probatory_document_id, "IX_CLAIM_PROBATORY_DOCUMENT_ID");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<claim_signature>(entity =>
        {
            entity.HasKey(e => e.claim_signature_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.HasIndex(e => e.claim_id, "IX_CLAIM_ID");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.local_date).HasColumnType("datetime");
            entity.Property(e => e.local_timezone)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.signature).IsRequired();
        });

        modelBuilder.Entity<claim_status>(entity =>
        {
            entity.HasKey(e => e.claim_status_id).HasName("PRIMARY");

            entity.Property(e => e.claim_status_id).ValueGeneratedNever();
            entity.Property(e => e.claim_status_name)
                .IsRequired()
                .HasMaxLength(45);
        });

        modelBuilder.Entity<claim_type>(entity =>
        {
            entity.HasKey(e => e.claim_type_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.claim_type_id).ValueGeneratedNever();
            entity.Property(e => e.claim_type_name)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<document>(entity =>
        {
            entity.HasKey(e => e.document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.checksum_md5).HasMaxLength(32);
            entity.Property(e => e.checksum_sha1).HasMaxLength(40);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.filename)
                .IsRequired()
                .HasMaxLength(1024);
            entity.Property(e => e.metadata_alt).HasMaxLength(250);
            entity.Property(e => e.metadata_copyright).HasMaxLength(250);
            entity.Property(e => e.metadata_title).HasMaxLength(250);
            entity.Property(e => e.mime_type).HasMaxLength(50);
            entity.Property(e => e.url).HasMaxLength(1024);
        });

        modelBuilder.Entity<document_type>(entity =>
        {
            entity.HasKey(e => e.document_type_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.document_type_id).ValueGeneratedNever();
            entity.Property(e => e.accepted_file_extensions)
                .IsRequired()
                .HasMaxLength(250);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.document_type_name)
                .IsRequired()
                .HasMaxLength(150);
        });

        modelBuilder.Entity<media>(entity =>
        {
            entity.HasKey(e => e.media_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.checksum_md5).HasMaxLength(32);
            entity.Property(e => e.checksum_sha1).HasMaxLength(40);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.filename)
                .IsRequired()
                .HasMaxLength(1024);
            entity.Property(e => e.metadata_alt).HasMaxLength(250);
            entity.Property(e => e.metadata_copyright).HasMaxLength(250);
            entity.Property(e => e.metadata_title).HasMaxLength(250);
            entity.Property(e => e.mime_type).HasMaxLength(50);
            entity.Property(e => e.url).HasMaxLength(1024);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
