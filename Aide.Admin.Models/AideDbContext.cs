using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Models;

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

    public virtual DbSet<insurance_collage> insurance_collage { get; set; }

    public virtual DbSet<insurance_collage_probatory_document> insurance_collage_probatory_document { get; set; }

    public virtual DbSet<insurance_company> insurance_company { get; set; }

    public virtual DbSet<insurance_company_claim_type_settings> insurance_company_claim_type_settings { get; set; }

    public virtual DbSet<insurance_export_probatory_document> insurance_export_probatory_document { get; set; }

    public virtual DbSet<insurance_probatory_document> insurance_probatory_document { get; set; }

    public virtual DbSet<probatory_document> probatory_document { get; set; }

    public virtual DbSet<store> store { get; set; }

    public virtual DbSet<user> user { get; set; }

    public virtual DbSet<user_company> user_company { get; set; }

    public virtual DbSet<user_psw_history> user_psw_history { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(_connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.26-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<insurance_collage>(entity =>
        {
            entity.HasKey(e => e.insurance_collage_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.insurance_collage_name)
                .IsRequired()
                .HasMaxLength(250);
        });

        modelBuilder.Entity<insurance_collage_probatory_document>(entity =>
        {
            entity.HasKey(e => e.insurance_collage_probatory_document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<insurance_company>(entity =>
        {
            entity.HasKey(e => e.insurance_company_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.insurance_company_name)
                .IsRequired()
                .HasMaxLength(250);
        });

        modelBuilder.Entity<insurance_company_claim_type_settings>(entity =>
        {
            entity.HasKey(e => new { e.insurance_company_id, e.claim_type_id })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<insurance_export_probatory_document>(entity =>
        {
            entity.HasKey(e => e.insurance_export_probatory_document_id).HasName("PRIMARY");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<insurance_probatory_document>(entity =>
        {
            entity.HasKey(e => e.insurance_probatory_document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
        });

        modelBuilder.Entity<probatory_document>(entity =>
        {
            entity.HasKey(e => e.probatory_document_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.accepted_file_extensions)
                .IsRequired()
                .HasMaxLength(250);
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.probatory_document_name)
                .IsRequired()
                .HasMaxLength(250);
        });

        modelBuilder.Entity<store>(entity =>
        {
            entity.HasKey(e => e.store_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.store_email).HasMaxLength(100);
            entity.Property(e => e.store_name)
                .IsRequired()
                .HasMaxLength(250);
            entity.Property(e => e.store_sap_number).HasMaxLength(15);
        });

        modelBuilder.Entity<user>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.date_logout).HasColumnType("datetime");
            entity.Property(e => e.date_modified).HasColumnType("datetime");
            entity.Property(e => e.email)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.psw)
                .IsRequired()
                .HasMaxLength(128);
            entity.Property(e => e.time_last_attempt).HasColumnType("datetime");
            entity.Property(e => e.user_first_name)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.user_last_name)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<user_company>(entity =>
        {
            entity.HasKey(e => e.user_company_id).HasName("PRIMARY");

            entity
                .HasCharSet("utf8mb3")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.date_created).HasColumnType("datetime");
        });

        modelBuilder.Entity<user_psw_history>(entity =>
        {
            entity.HasKey(e => e.user_psw_history_id).HasName("PRIMARY");

            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.psw)
                .IsRequired()
                .HasMaxLength(128);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
