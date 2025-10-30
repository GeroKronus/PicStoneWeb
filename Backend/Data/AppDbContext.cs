using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Data
{
    /// <summary>
    /// Contexto do Entity Framework para SQL Server
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<FotoMobile> FotosMobile { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela FotosMobile
            modelBuilder.Entity<FotoMobile>(entity =>
            {
                entity.ToTable("FotosMobile");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomeArquivo).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Material).HasMaxLength(100).IsRequired(false); // Opcional
                entity.Property(e => e.Bloco).HasMaxLength(50).IsRequired(false); // Opcional
                entity.Property(e => e.Lote).HasMaxLength(50).IsRequired(false); // Opcional (compatibilidade)
                entity.Property(e => e.Chapa).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Processo).HasMaxLength(50).IsRequired(false); // Opcional (compatibilidade)
                entity.Property(e => e.Usuario).HasMaxLength(100);
                entity.Property(e => e.CaminhoArquivo).HasMaxLength(500);
            });

            // Configuração da tabela Usuarios
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.NomeCompleto).HasMaxLength(200);
                entity.HasIndex(e => e.Username).IsUnique();
            });
        }
    }
}
