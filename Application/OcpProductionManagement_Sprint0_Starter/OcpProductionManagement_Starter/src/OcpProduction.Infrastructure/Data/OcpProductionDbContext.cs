using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OcpProduction.Infrastructure.Generated;

namespace OcpProduction.Infrastructure.Data;

public partial class OcpProductionDbContext : DbContext
{
    public OcpProductionDbContext(DbContextOptions<OcpProductionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<LoadingPoint> LoadingPoints { get; set; }

    public virtual DbSet<QualityResult> QualityResults { get; set; }

    public virtual DbSet<StopReason> StopReasons { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    public virtual DbSet<Truck> Trucks { get; set; }

    public virtual DbSet<TruckStop> TruckStops { get; set; }

    public virtual DbSet<WorkShift> WorkShifts { get; set; }

    public virtual DbSet<vw_DriverPerformance> vw_DriverPerformances { get; set; }

    public virtual DbSet<vw_ExecutiveDashboard> vw_ExecutiveDashboards { get; set; }

    public virtual DbSet<vw_GlobalProductionSummary> vw_GlobalProductionSummaries { get; set; }

    public virtual DbSet<vw_QualityAnalysis> vw_QualityAnalyses { get; set; }

    public virtual DbSet<vw_QualityByPeriod> vw_QualityByPeriods { get; set; }

    public virtual DbSet<vw_StopAnalysis> vw_StopAnalyses { get; set; }

    public virtual DbSet<vw_StopsByReason> vw_StopsByReasons { get; set; }

    public virtual DbSet<vw_TripDetail> vw_TripDetails { get; set; }

    public virtual DbSet<vw_TripSummary> vw_TripSummaries { get; set; }

    public virtual DbSet<vw_TripsDailySummary> vw_TripsDailySummaries { get; set; }

    public virtual DbSet<vw_TruckPerformance> vw_TruckPerformances { get; set; }

    public virtual DbSet<vw_TruckStopDetail> vw_TruckStopDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("Destination");

            entity.HasIndex(e => e.DestinationCode, "UQ_Destination_DestinationCode").IsUnique();

            entity.Property(e => e.DestinationCode).HasMaxLength(20);
            entity.Property(e => e.DestinationName).HasMaxLength(100);
            entity.Property(e => e.DestinationType).HasMaxLength(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Destination_IsActive");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("Driver");

            entity.HasIndex(e => e.DriverCode, "UQ_Driver_DriverCode").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Driver_CreatedAt");
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Driver_IsActive");
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active", "DF_Driver_Status");
        });

        modelBuilder.Entity<LoadingPoint>(entity =>
        {
            entity.ToTable("LoadingPoint");

            entity.HasIndex(e => e.LoadingPointCode, "UQ_LoadingPoint_LoadingPointCode").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_LoadingPoint_IsActive");
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointDescription).HasMaxLength(250);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.ZoneName).HasMaxLength(100);
        });

        modelBuilder.Entity<QualityResult>(entity =>
        {
            entity.ToTable("QualityResult");

            entity.HasIndex(e => e.AnalysisDate, "IX_QualityResult_AnalysisDate");

            entity.HasIndex(e => new { e.LoadingPointID, e.AnalysisDate }, "IX_QualityResult_LoadingPointID_AnalysisDate");

            entity.HasIndex(e => e.TripID, "IX_QualityResult_TripID");

            entity.Property(e => e.AnalysisDate).HasPrecision(0);
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_QualityResult_CreatedAt");
            entity.Property(e => e.QualityUnit)
                .HasMaxLength(20)
                .HasDefaultValue("P2O5Percent", "DF_QualityResult_QualityUnit");
            entity.Property(e => e.QualityValue).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending", "DF_QualityResult_Status");
            entity.Property(e => e.TargetValue).HasColumnType("decimal(10, 3)");

            entity.HasOne(d => d.LoadingPoint).WithMany(p => p.QualityResults)
                .HasForeignKey(d => d.LoadingPointID)
                .HasConstraintName("FK_QualityResult_LoadingPoint");

            entity.HasOne(d => d.Trip).WithMany(p => p.QualityResults)
                .HasForeignKey(d => d.TripID)
                .HasConstraintName("FK_QualityResult_Trip");
        });

        modelBuilder.Entity<StopReason>(entity =>
        {
            entity.ToTable("StopReason");

            entity.HasIndex(e => e.ReasonCode, "UQ_StopReason_ReasonCode").IsUnique();

            entity.Property(e => e.Category).HasMaxLength(40);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_StopReason_IsActive");
            entity.Property(e => e.ReasonCode).HasMaxLength(30);
            entity.Property(e => e.ReasonName).HasMaxLength(100);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("Trip");

            entity.HasIndex(e => e.DestinationID, "IX_Trip_DestinationID");

            entity.HasIndex(e => new { e.DriverID, e.TripDate }, "IX_Trip_DriverID_TripDate");

            entity.HasIndex(e => e.LoadingPointID, "IX_Trip_LoadingPointID");

            entity.HasIndex(e => e.ShiftID, "IX_Trip_ShiftID");

            entity.HasIndex(e => e.TripDate, "IX_Trip_TripDate");

            entity.HasIndex(e => new { e.TruckID, e.TripDate }, "IX_Trip_TruckID_TripDate");

            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Trip_CreatedAt");
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Completed", "DF_Trip_Status");
            entity.Property(e => e.Tonnage).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Destination).WithMany(p => p.Trips)
                .HasForeignKey(d => d.DestinationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Destination");

            entity.HasOne(d => d.Driver).WithMany(p => p.Trips)
                .HasForeignKey(d => d.DriverID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Driver");

            entity.HasOne(d => d.LoadingPoint).WithMany(p => p.Trips)
                .HasForeignKey(d => d.LoadingPointID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_LoadingPoint");

            entity.HasOne(d => d.Shift).WithMany(p => p.Trips)
                .HasForeignKey(d => d.ShiftID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_WorkShift");

            entity.HasOne(d => d.Truck).WithMany(p => p.Trips)
                .HasForeignKey(d => d.TruckID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trip_Truck");
        });

        modelBuilder.Entity<Truck>(entity =>
        {
            entity.ToTable("Truck");

            entity.HasIndex(e => e.TruckCode, "UQ_Truck_TruckCode").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Truck_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Truck_IsActive");
            entity.Property(e => e.Model).HasMaxLength(80);
            entity.Property(e => e.RatedPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RegistrationNumber).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Available", "DF_Truck_Status");
            entity.Property(e => e.TruckCode).HasMaxLength(20);
        });

        modelBuilder.Entity<TruckStop>(entity =>
        {
            entity.ToTable("TruckStop");

            entity.HasIndex(e => e.DriverID, "IX_TruckStop_DriverID");

            entity.HasIndex(e => e.ShiftID, "IX_TruckStop_ShiftID");

            entity.HasIndex(e => new { e.StopReasonID, e.StartTime }, "IX_TruckStop_StopReasonID_StartTime");

            entity.HasIndex(e => e.TripID, "IX_TruckStop_TripID");

            entity.HasIndex(e => new { e.TruckID, e.StartTime }, "IX_TruckStop_TruckID_StartTime");

            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_TruckStop_CreatedAt");
            entity.Property(e => e.DurationMinutes).HasComputedColumnSql("(case when [EndTime] IS NULL then NULL else datediff(minute,[StartTime],[EndTime]) end)", true);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.ValidationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending", "DF_TruckStop_ValidationStatus");

            entity.HasOne(d => d.Driver).WithMany(p => p.TruckStops)
                .HasForeignKey(d => d.DriverID)
                .HasConstraintName("FK_TruckStop_Driver");

            entity.HasOne(d => d.Shift).WithMany(p => p.TruckStops)
                .HasForeignKey(d => d.ShiftID)
                .HasConstraintName("FK_TruckStop_WorkShift");

            entity.HasOne(d => d.StopReason).WithMany(p => p.TruckStops)
                .HasForeignKey(d => d.StopReasonID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TruckStop_StopReason");

            entity.HasOne(d => d.Trip).WithMany(p => p.TruckStops)
                .HasForeignKey(d => d.TripID)
                .HasConstraintName("FK_TruckStop_Trip");

            entity.HasOne(d => d.Truck).WithMany(p => p.TruckStops)
                .HasForeignKey(d => d.TruckID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TruckStop_Truck");
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.HasKey(e => e.ShiftID);

            entity.ToTable("WorkShift");

            entity.HasIndex(e => e.ShiftCode, "UQ_WorkShift_ShiftCode").IsUnique();

            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_WorkShift_IsActive");
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasPrecision(0);
        });

        modelBuilder.Entity<vw_DriverPerformance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DriverPerformance");

            entity.Property(e => e.AverageLinkedStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.AveragePayloadTonnes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AveragePayloadUtilizationPercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AverageTripDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CompletionRatePercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.DriverFullName).HasMaxLength(120);
            entity.Property(e => e.DriverStatus).HasMaxLength(20);
            entity.Property(e => e.MaximumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MaximumPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinimumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalTonnage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TripsPerActiveDay).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<vw_ExecutiveDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExecutiveDashboard");

            entity.Property(e => e.AverageP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.AveragePayloadTonnes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AveragePayloadUtilizationPercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AverageStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageTonnagePerActiveDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageTripDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageTripsPerActiveDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CompletionRatePercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaximumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.TotalTonnage).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<vw_GlobalProductionSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_GlobalProductionSummary");

            entity.Property(e => e.AverageP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.AverageStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageTripDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LongTripRate).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.MaximumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.TotalTonnage).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<vw_QualityAnalysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QualityAnalysis");

            entity.Property(e => e.AnalysisDate).HasPrecision(0);
            entity.Property(e => e.DestinationCode).HasMaxLength(20);
            entity.Property(e => e.DestinationName).HasMaxLength(100);
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.DriverFullName).HasMaxLength(120);
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.P2O5Percent).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.QualityStatus).HasMaxLength(20);
            entity.Property(e => e.QualityUnit).HasMaxLength(20);
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.TargetValue).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.TripStatus).HasMaxLength(20);
            entity.Property(e => e.TruckCode).HasMaxLength(20);
        });

        modelBuilder.Entity<vw_QualityByPeriod>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QualityByPeriod");

            entity.Property(e => e.AverageQuality).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.MaximumQuality).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumQuality).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.QualityUnit).HasMaxLength(20);
            entity.Property(e => e.ZoneName).HasMaxLength(100);
        });

        modelBuilder.Entity<vw_StopAnalysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StopAnalysis");

            entity.Property(e => e.DestinationCode).HasMaxLength(20);
            entity.Property(e => e.DestinationName).HasMaxLength(100);
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.DriverFullName).HasMaxLength(120);
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.PlannedUnplannedLabel).HasMaxLength(10);
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StopCategory).HasMaxLength(40);
            entity.Property(e => e.StopEndTime).HasPrecision(0);
            entity.Property(e => e.StopReasonCode).HasMaxLength(30);
            entity.Property(e => e.StopReasonName).HasMaxLength(100);
            entity.Property(e => e.StopStartTime).HasPrecision(0);
            entity.Property(e => e.StopStatus).HasMaxLength(20);
            entity.Property(e => e.TripStatus).HasMaxLength(20);
            entity.Property(e => e.TruckCode).HasMaxLength(20);
        });

        modelBuilder.Entity<vw_StopsByReason>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StopsByReason");

            entity.Property(e => e.AverageStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReasonCategory).HasMaxLength(40);
            entity.Property(e => e.ReasonCode).HasMaxLength(30);
            entity.Property(e => e.ReasonName).HasMaxLength(100);
        });

        modelBuilder.Entity<vw_TripDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TripDetails");

            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.DestinationCode).HasMaxLength(20);
            entity.Property(e => e.DestinationName).HasMaxLength(100);
            entity.Property(e => e.DestinationType).HasMaxLength(30);
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.DriverName).HasMaxLength(120);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointDescription).HasMaxLength(250);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(30);
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Tonnage).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TruckCode).HasMaxLength(20);
            entity.Property(e => e.ZoneName).HasMaxLength(100);
        });

        modelBuilder.Entity<vw_TripSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TripSummary");

            entity.Property(e => e.AverageP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.AverageStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DestinationCode).HasMaxLength(20);
            entity.Property(e => e.DestinationName).HasMaxLength(100);
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.LatestP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.LoadingPointCode).HasMaxLength(20);
            entity.Property(e => e.LoadingPointName).HasMaxLength(100);
            entity.Property(e => e.MaximumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.PayloadUtilizationPercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RatedPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Tonnage).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TruckCode).HasMaxLength(20);
            entity.Property(e => e.TruckModel).HasMaxLength(80);
        });

        modelBuilder.Entity<vw_TripsDailySummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TripsDailySummary");

            entity.Property(e => e.AverageTripDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LongTripRate).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.TotalTonnage).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<vw_TruckPerformance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TruckPerformance");

            entity.Property(e => e.AverageP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.AveragePayloadTonnes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AveragePayloadUtilizationPercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AverageStopDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AverageTripDurationMinutes).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CompletionRatePercent).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaximumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MaximumPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinimumP2O5).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MinimumPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RatedPayloadTonnes).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalTonnage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TripsPerActiveDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TruckCode).HasMaxLength(20);
            entity.Property(e => e.TruckModel).HasMaxLength(80);
            entity.Property(e => e.TruckStatus).HasMaxLength(20);
        });

        modelBuilder.Entity<vw_TruckStopDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TruckStopDetails");

            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.DriverCode).HasMaxLength(20);
            entity.Property(e => e.DriverName).HasMaxLength(120);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.ReasonCategory).HasMaxLength(40);
            entity.Property(e => e.ReasonCode).HasMaxLength(30);
            entity.Property(e => e.ReasonName).HasMaxLength(100);
            entity.Property(e => e.ShiftCode).HasMaxLength(20);
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.TruckCode).HasMaxLength(20);
            entity.Property(e => e.ValidationStatus).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
