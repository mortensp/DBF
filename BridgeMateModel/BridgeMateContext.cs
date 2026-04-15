using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel;

public class BridgeMateContext : DbContext
{
    #region Constructors
        public BridgeMateContext()
        {
        }

        public BridgeMateContext(string dbPath)
        {
            DatabasePath = dbPath;
        }
    #endregion

    public         string                  DatabasePath     { get; internal init; }

    public virtual DbSet<Client>           Clients          { get; set; }
    public virtual DbSet<Custom_DBF>       Custom_DBF       { get; set; }
    public virtual DbSet<IntermediateData> IntermediateData { get; set; }
    public virtual DbSet<PlayerName>       PlayerNames      { get; set; }
    public virtual DbSet<PlayerNumber>     PlayerNumbers    { get; set; }
    public virtual DbSet<ReceivedData>     ReceivedData     { get; set; }
    public virtual DbSet<RoundData>        RoundData        { get; set; }
    public virtual DbSet<Section>          Sections         { get; set; }
    public virtual DbSet<Session>          Sessions         { get; set; }
    public virtual DbSet<Table>            Tables           { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string connStr = "Driver={Microsoft Access Driver (*.mdb, *.accdb)};"
                       + $"Dbq={DatabasePath};";

        _ = options.UseJetOdbc(connStr);
        //.LogTo(Console.WriteLine);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity => { });
        modelBuilder.Entity<Custom_DBF>(entity => { });
        modelBuilder.Entity<IntermediateData>(entity => { });
        modelBuilder.Entity<PlayerName>(entity => { });
        modelBuilder.Entity<PlayerNumber>(entity => { });
        modelBuilder.Entity<ReceivedData>(entity => { });
            modelBuilder.Entity<RoundData>(entity =>
            {
                entity.Property(e => e.TableNo).HasColumnType("smallint");

                // Configure foreign key relationship to Section based on Section.Id
                entity.HasOne(rd => rd.SectionEntity)
                      .WithMany(s => s.Rounds)
                      .HasForeignKey(rd => rd.Section)
                      .HasPrincipalKey(s => s.Id)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });
        modelBuilder.Entity<Section>(entity => { entity.ToTable("Section"); });
        modelBuilder.Entity<Session>(entity => { entity.ToTable("Session"); });
        modelBuilder.Entity<Table>(entity => { });
    }
}
