using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Projet.Models;

public partial class ProjetContext : DbContext
{
    // commande utile : Scaffold-DbContext "Server=(localdb)\mssqllocaldb;Database=MonProjetL3_DB;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context ProjetContext -Force
    public ProjetContext()
    {
    }

    public ProjetContext(DbContextOptions<ProjetContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Competence> Competences { get; set; }

    public virtual DbSet<CompetenceAcquise> CompetenceAcquises { get; set; }

    public virtual DbSet<CompetenceSouhaitee> CompetenceSouhaitees { get; set; }

    public virtual DbSet<Offre> Offres { get; set; }

    public virtual DbSet<ParametreScoring> ParametreScorings { get; set; }

    public virtual DbSet<Personne> Personnes { get; set; }

    public virtual DbSet<Poste> Postes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MonProjetL3_DB;Trusted_Connection=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Competen__3214EC077FB5CF27");

            entity.ToTable("Competence");

            entity.HasIndex(e => e.Nom, "UQ__Competen__C7D1C61E70A7CAB6").IsUnique();

            entity.Property(e => e.Nom).HasMaxLength(100);
        });

        modelBuilder.Entity<CompetenceAcquise>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Competen__3214EC076EFB2F5B");

            entity.ToTable("CompetenceAcquise");

            entity.Property(e => e.Niveau).HasDefaultValue(1);

            entity.HasOne(d => d.Competence).WithMany(p => p.CompetenceAcquises)
                .HasForeignKey(d => d.CompetenceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Competenc__Compe__5DCAEF64");

            entity.HasOne(d => d.Personne).WithMany(p => p.CompetenceAcquises)
                .HasForeignKey(d => d.PersonneId)
                .HasConstraintName("FK__Competenc__Perso__5CD6CB2B");
        });

        modelBuilder.Entity<CompetenceSouhaitee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Competen__3214EC07CE71D8DB");

            entity.ToTable("CompetenceSouhaitee");

            entity.Property(e => e.NiveauRequis).HasDefaultValue(1);

            entity.HasOne(d => d.Competence).WithMany(p => p.CompetenceSouhaitees)
                .HasForeignKey(d => d.CompetenceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Competenc__Compe__59063A47");

            entity.HasOne(d => d.Offre).WithMany(p => p.CompetenceSouhaitees)
                .HasForeignKey(d => d.OffreId)
                .HasConstraintName("FK__Competenc__Offre__5812160E");
        });

        modelBuilder.Entity<Offre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Offre__3214EC07C6FDC028");

            entity.ToTable("Offre");

            entity.Property(e => e.CodePostalCible).HasMaxLength(10);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Titre).HasMaxLength(200);
            entity.Property(e => e.VilleCible).HasMaxLength(100);

            entity.HasOne(d => d.Poste).WithMany(p => p.Offres)
                .HasForeignKey(d => d.PosteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Offre__PosteId__5070F446");
        });

        modelBuilder.Entity<ParametreScoring>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Parametr__3214EC0790BD927F");

            entity.ToTable("ParametreScoring");

            entity.HasIndex(e => e.OffreId, "UQ__Parametr__540A13133B54325B").IsUnique();

            entity.Property(e => e.ExclureSiExperienceManquante).HasDefaultValue(false);
            entity.Property(e => e.ExclureSiVilleDiff).HasDefaultValue(false);
            entity.Property(e => e.PoidsCompetences).HasDefaultValue(50);
            entity.Property(e => e.PoidsExperience).HasDefaultValue(30);
            entity.Property(e => e.PoidsLocalisation).HasDefaultValue(20);

            entity.HasOne(d => d.Offre).WithOne(p => p.ParametreScoring)
                .HasForeignKey<ParametreScoring>(d => d.OffreId)
                .HasConstraintName("FK__Parametre__Offre__66603565");
        });

        modelBuilder.Entity<Personne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Personne__3214EC07C70B9BFC");

            entity.ToTable("Personne");

            entity.HasIndex(e => e.Email, "UQ__Personne__A9D10534B1605CA4").IsUnique();

            entity.Property(e => e.AnneesExperienceTotal).HasDefaultValue(0);
            entity.Property(e => e.CodePostal).HasMaxLength(10);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Nom).HasMaxLength(100);
            entity.Property(e => e.Prenom).HasMaxLength(100);
            entity.Property(e => e.Ville).HasMaxLength(100);
        });

        modelBuilder.Entity<Poste>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Poste__3214EC07DF0C6F2E");

            entity.ToTable("Poste");

            entity.Property(e => e.Intitule).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
