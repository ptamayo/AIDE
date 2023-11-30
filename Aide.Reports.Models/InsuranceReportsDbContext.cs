using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Aide.Reports.Models;

public partial class InsuranceReportsDbContext : DbContext
{
    private readonly string _connectionString;

    public InsuranceReportsDbContext() { }

    public InsuranceReportsDbContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public InsuranceReportsDbContext(DbContextOptions<InsuranceReportsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<vw_dashboard1_claims_report> vw_dashboard1_claims_report { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(_connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.26-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<vw_dashboard1_claims_report>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_dashboard1_claims_report");

            entity.Property(e => e.claim_number)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.claim_status_name)
                .IsRequired()
                .HasMaxLength(11)
                .HasDefaultValueSql("''");
            entity.Property(e => e.claim_type_name)
                .IsRequired()
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.customer_full_name)
                .IsRequired()
                .HasMaxLength(150)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.date_created).HasColumnType("datetime");
            entity.Property(e => e.external_order_number)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.insurance_company_name)
                .IsRequired()
                .HasMaxLength(250)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.policy_number)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.policy_subsection)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.report_number)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.signature_date_created).HasColumnType("datetime");
            entity.Property(e => e.store_name)
                .IsRequired()
                .HasMaxLength(250)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.store_sap_number)
                .HasMaxLength(15)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
