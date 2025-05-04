using Microsoft.EntityFrameworkCore;
using ModelLibrary.Model;

namespace pdfEngineAPI.Data
{
    public class PdfDbContext : DbContext
    {
        public DbSet<FileMetaData> FileMetaData { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }
        public DbSet<FileContent> FileContent { get; set; }

        public PdfDbContext(DbContextOptions<PdfDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileMetaData>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<FileMetaData>()
                .HasOne(f => f.FileContent)
                .WithOne()
                .HasForeignKey<FileContent>(fc => fc.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentChunk>()
                .HasKey(dc => dc.Id);

            modelBuilder.Entity<Embedding>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<DocumentChunk>()
                .HasMany(dc => dc.Embeddings) 
                .WithOne(e => e.Chunk) 
                .HasForeignKey(e => e.ChunkId) // Foreign key to DocumentChunk
                .OnDelete(DeleteBehavior.Cascade); // Cascading delete 

            modelBuilder.Entity<DocumentChunk>()
                .HasOne(dc => dc.Document) 
                .WithMany(f => f.DocumentChunks) 
                .HasForeignKey(dc => dc.DocumentId) // Foreign key to FileMetaData
                .OnDelete(DeleteBehavior.Cascade); // Cascading delete 

            modelBuilder.Entity<Embedding>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd(); 
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=pdfDatabase.db");
            }

            // Enable FK support / is probably on
            optionsBuilder.UseSqlite("Data Source=pdfDatabase.db", sqliteOptions => sqliteOptions.CommandTimeout(60));
            base.OnConfiguring(optionsBuilder);
        }

        public override int SaveChanges()
        {
            // Ensure foreign keys are enabled
            this.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            return base.SaveChanges();
        }
    }
}
