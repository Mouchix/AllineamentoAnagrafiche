using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AllineamentoAnagrafiche.Models;

public partial class AnagraficheContext : DbContext
{
    public AnagraficheContext()
    {
    }

    public AnagraficheContext(DbContextOptions<AnagraficheContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TAutorizzazioni> TAutorizzazionis { get; set; }

    public virtual DbSet<TComuni> TComunis { get; set; }

    public virtual DbSet<TLogMessaggi> TLogMessaggis { get; set; }

    public virtual DbSet<TProvince> TProvinces { get; set; }

    public virtual DbSet<TRegioni> TRegionis { get; set; }

    public virtual DbSet<TTipoAutorizzazioni> TTipoAutorizzazionis { get; set; }

    public virtual DbSet<TUtenti> TUtentis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TAutorizzazioni>(entity =>
        {
            entity.HasKey(e => e.AutorizzazioneCodice).HasName("PK__T_AUTORI__E0708A62CA57B9D8");

            entity.ToTable("T_AUTORIZZAZIONI");

            entity.Property(e => e.AutorizzazioneCodice).HasColumnName("AUTORIZZAZIONE_CODICE");
            entity.Property(e => e.MetodoCodice).HasColumnName("METODO_CODICE");
            entity.Property(e => e.UserCodice).HasColumnName("USER_CODICE");

            entity.HasOne(d => d.MetodoCodiceNavigation).WithMany(p => p.TAutorizzazionis)
                .HasForeignKey(d => d.MetodoCodice)
                .HasConstraintName("FK__T_AUTORIZ__METOD__7B5B524B");

            entity.HasOne(d => d.UserCodiceNavigation).WithMany(p => p.TAutorizzazionis)
                .HasForeignKey(d => d.UserCodice)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__T_AUTORIZ__USER___7A672E12");
        });

        modelBuilder.Entity<TComuni>(entity =>
        {
            entity.HasKey(e => e.ComCodice).HasName("PK__T_COMUNI__0BE93EF370156AEA");

            entity.ToTable("T_COMUNI");

            entity.HasIndex(e => e.ComIstat, "UQ__T_COMUNI__123474E44AD5A61E").IsUnique();

            entity.Property(e => e.ComCodice).HasColumnName("COM_CODICE");
            entity.Property(e => e.ComDescrizione)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("COM_DESCRIZIONE");
            entity.Property(e => e.ComFineValidita)
                .HasColumnType("datetime")
                .HasColumnName("COM_FINE_VALIDITA");
            entity.Property(e => e.ComInizioValidita)
                .HasColumnType("datetime")
                .HasColumnName("COM_INIZIO_VALIDITA");
            entity.Property(e => e.ComIstat)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("COM_ISTAT");
            entity.Property(e => e.ComProCodice).HasColumnName("COM_PRO_CODICE");

            entity.HasOne(d => d.ComProCodiceNavigation).WithMany(p => p.TComunis)
                .HasForeignKey(d => d.ComProCodice)
                .HasConstraintName("FK__T_COMUNI__COM_PR__52593CB8");
        });

        modelBuilder.Entity<TLogMessaggi>(entity =>
        {
            entity.HasKey(e => e.LogCodice).HasName("PK__T_LOG_ME__4372482A587A5D1C");

            entity.ToTable("T_LOG_MESSAGGI");

            entity.Property(e => e.LogCodice).HasColumnName("LOG_CODICE");
            entity.Property(e => e.LogDataOra)
                .HasColumnType("datetime")
                .HasColumnName("LOG_DATA_ORA");
            entity.Property(e => e.LogMessaggioRequest)
                .IsUnicode(false)
                .HasColumnName("LOG_MESSAGGIO_REQUEST");
            entity.Property(e => e.LogMessaggioResponse)
                .IsUnicode(false)
                .HasColumnName("LOG_MESSAGGIO_RESPONSE");
            entity.Property(e => e.LogMetodo).HasColumnName("LOG_METODO");
            entity.Property(e => e.LogUser).HasColumnName("LOG_USER");

            entity.HasOne(d => d.LogMetodoNavigation).WithMany(p => p.TLogMessaggis)
                .HasForeignKey(d => d.LogMetodo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__T_LOG_MES__LOG_M__02084FDA");

            entity.HasOne(d => d.LogUserNavigation).WithMany(p => p.TLogMessaggis)
                .HasForeignKey(d => d.LogUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__T_LOG_MES__LOG_U__01142BA1");
        });

        modelBuilder.Entity<TProvince>(entity =>
        {
            entity.HasKey(e => e.ProCodice).HasName("PK__T_PROVIN__A4730B06E7B1A95F");

            entity.ToTable("T_PROVINCE");

            entity.HasIndex(e => e.ProIstat, "UQ__T_PROVIN__9D8B362C16AF45B9").IsUnique();

            entity.Property(e => e.ProCodice).HasColumnName("PRO_CODICE");
            entity.Property(e => e.ProDescrizione)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("PRO_DESCRIZIONE");
            entity.Property(e => e.ProFineValidita)
                .HasColumnType("datetime")
                .HasColumnName("PRO_FINE_VALIDITA");
            entity.Property(e => e.ProInizioValidita)
                .HasColumnType("datetime")
                .HasColumnName("PRO_INIZIO_VALIDITA");
            entity.Property(e => e.ProIstat)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PRO_ISTAT");
            entity.Property(e => e.ProRegCodice).HasColumnName("PRO_REG_CODICE");

            entity.HasOne(d => d.ProRegCodiceNavigation).WithMany(p => p.TProvinces)
                .HasForeignKey(d => d.ProRegCodice)
                .HasConstraintName("FK__T_PROVINC__PRO_R__4E88ABD4");
        });

        modelBuilder.Entity<TRegioni>(entity =>
        {
            entity.HasKey(e => e.RegCodice).HasName("PK__T_REGION__E2D380FF0923D9CF");

            entity.ToTable("T_REGIONI");

            entity.HasIndex(e => e.RegIstat, "UQ__T_REGION__CA51D94FF67671A8").IsUnique();

            entity.Property(e => e.RegCodice).HasColumnName("REG_CODICE");
            entity.Property(e => e.RegDescrizione)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("REG_DESCRIZIONE");
            entity.Property(e => e.RegFineValidita)
                .HasColumnType("datetime")
                .HasColumnName("REG_FINE_VALIDITA");
            entity.Property(e => e.RegInizioValidita)
                .HasColumnType("datetime")
                .HasColumnName("REG_INIZIO_VALIDITA");
            entity.Property(e => e.RegIstat)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("REG_ISTAT");
        });

        modelBuilder.Entity<TTipoAutorizzazioni>(entity =>
        {
            entity.HasKey(e => e.TipoAutorizzazioneCodice).HasName("PK__T_TIPO_A__E57FE79FEF1151BB");

            entity.ToTable("T_TIPO_AUTORIZZAZIONI");

            entity.HasIndex(e => e.NomeMetodo, "UQ__T_TIPO_A__CA22017C6162ECA7").IsUnique();

            entity.Property(e => e.TipoAutorizzazioneCodice).HasColumnName("TIPO_AUTORIZZAZIONE_CODICE");
            entity.Property(e => e.NomeMetodo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NOME_METODO");
        });

        modelBuilder.Entity<TUtenti>(entity =>
        {
            entity.HasKey(e => e.UserCodice).HasName("PK__USERS__7CAC83A4BAD7B225");

            entity.ToTable("T_UTENTI");

            entity.Property(e => e.UserCodice).HasColumnName("USER_CODICE");
            entity.Property(e => e.PasswordHash).HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.Salt).HasColumnName("SALT");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("USERNAME");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
