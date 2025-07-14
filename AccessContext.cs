using Microsoft.EntityFrameworkCore;

namespace DBF.ViewModels
{
    class AccessContext : DbContext
    {
        public AccessContext() { }

        #region Database Properties
            public virtual DbSet<PlayerName> PlayerNames { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------
            // Build Model -
            // -------------
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<PlayerName>(entity =>
                                            {
                                                entity.HasKey(e => e.Id);
                                            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = @"Offline=False;DataSource=C:\BC3\BridgeMate\2172\BMDB_Section_1222.bws;";
            optionsBuilder.UseCDataAccess(connectionString);
                    //optionsBuilder.LogTo(Console.WriteLine);
           

            optionsBuilder.UseLazyLoadingProxies();
#if (DEBUG || PRODTEST)
            //optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
#endif
            //optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.LazyLoadOnDisposedContextWarning));
        }
    }
}

