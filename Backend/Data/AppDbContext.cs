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
        public DbSet<Material> Materiais { get; set; }
        public DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<GeneratedEnvironment> GeneratedEnvironments { get; set; }

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
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.NomeCompleto).HasMaxLength(200);
                entity.Property(e => e.TokenVerificacao).HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasDefaultValue(StatusUsuario.Pendente);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.TokenVerificacao);
            });

            // Configuração da tabela Materiais
            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Materiais");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Ativo).IsRequired();
                entity.Property(e => e.Ordem).IsRequired();
                entity.HasIndex(e => e.Nome).IsUnique();
            });

            // Configuração da tabela UserLogins
            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.ToTable("UserLogins");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UsuarioId).IsRequired();
                entity.Property(e => e.DataHora).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                // Relacionamento com Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Logins)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices para performance
                entity.HasIndex(e => new { e.UsuarioId, e.DataHora });
                entity.HasIndex(e => e.DataHora);
            });

            // Configuração da tabela GeneratedEnvironments
            modelBuilder.Entity<GeneratedEnvironment>(entity =>
            {
                entity.ToTable("GeneratedEnvironments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UsuarioId).IsRequired();
                entity.Property(e => e.DataHora).IsRequired();
                entity.Property(e => e.TipoAmbiente).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Material).HasMaxLength(100);
                entity.Property(e => e.Bloco).HasMaxLength(50);
                entity.Property(e => e.Chapa).HasMaxLength(50);
                entity.Property(e => e.Detalhes).HasMaxLength(500);
                entity.Property(e => e.QuantidadeImagens).IsRequired();

                // Relacionamento com Usuario
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.AmbientesGerados)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices para performance
                entity.HasIndex(e => new { e.UsuarioId, e.DataHora });
                entity.HasIndex(e => e.TipoAmbiente);
                entity.HasIndex(e => e.DataHora);
            });
        }
    }
}
