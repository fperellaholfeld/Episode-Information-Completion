using api.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UploadHistory> Uploads => Set<UploadHistory>();
        public DbSet<Episode> Episodes => Set<Episode>();
        public DbSet<Character> Characters => Set<Character>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<EpisodeCharacter> EpisodeCharacters => Set<EpisodeCharacter>();
        public DbSet<UploadEpisode> UploadEpisodes => Set<UploadEpisode>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // Configure the Episode entity
            mb.Entity<Episode>().ToTable("Episodes");
            mb.Entity<Episode>().HasKey(e => e.Id);
            mb.Entity<Episode>().Property(e => e.Id).ValueGeneratedNever(); // External API supplies the ID
            mb.Entity<Episode>()
                .Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            mb.Entity<Episode>()
                .Property(e => e.AirDate)
                .IsRequired()
                .HasMaxLength(50);
            mb.Entity<Episode>()
                .Property(e => e.EpisodeCode)
                .IsRequired()
                .HasMaxLength(50);

            //Configure the Character entity
            mb.Entity<Character>().ToTable("Characters");
            mb.Entity<Character>().HasKey(c => c.Id);
            mb.Entity<Character>().Property(c => c.Id).ValueGeneratedNever(); // External API supplies the ID
            mb.Entity<Character>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(255);
            mb.Entity<Character>().HasIndex(c => c.Gender); // Made this an index so that we can quickly get the totals for genders

            //Configure the Location entity
            mb.Entity<Location>().ToTable("Locations");
            mb.Entity<Location>().HasKey(l => l.Id);
            mb.Entity<Location>().Property(l => l.Id).ValueGeneratedNever(); // External API supplies the ID
            mb.Entity<Location>()
                .Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(255);

            // Link the Character and Location entities
            mb.Entity<Character>()
                .HasOne(c => c.Origin)
                .WithMany(l => l.AsOrigin)
                .HasForeignKey(c => c.OriginLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Character>()
                .HasOne(c => c.Location)
                .WithMany(l => l.AsCurrent)
                .HasForeignKey(c => c.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure EpisodeCharacter entity
            mb.Entity<EpisodeCharacter>().ToTable("EpisodeCharacters");
            mb.Entity<EpisodeCharacter>().HasKey(ec => new { ec.EpisodeId, ec.CharacterId });

            // Configure UploadEpisode entity
            mb.Entity<UploadEpisode>().ToTable("UploadEpisodes");
            mb.Entity<UploadEpisode>().HasKey(ue => new { ue.UploadId, ue.EpisodeId });

            // Configure UploadHistory Entity
            mb.Entity<UploadHistory>().ToTable("Uploads");
            mb.Entity<UploadHistory>().HasKey(u => u.Id);
            mb.Entity<UploadHistory>().Property(u => u.FilePath)
                .IsRequired()
                .HasMaxLength(1024);
            mb.Entity<UploadHistory>().Property(u => u.CreatedTimestamp)
                .HasDefaultValueSql("GETUTCDATE()");
            mb.Entity<UploadHistory>().Property(u => u.Status)
                .HasConversion<int>();

        }
    }
}