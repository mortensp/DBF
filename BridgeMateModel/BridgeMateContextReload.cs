using System.ComponentModel;
using System.Data;
using System.Data.Odbc;

//using System.Data.Odbc;
//using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel;

public partial class BridgeMateContext : DbContext, IDisposable
{
    private string         _connectionString;
    private OdbcConnection _connection;

    #region Constructors
        public BridgeMateContext()
        {
            openConection(DatabaseName);
            this.ReloadAll(_connection);
        }

        public BridgeMateContext(string dbPath)
        {
            openConection(dbPath);
            this.ReloadAll(_connection);
        }
    #endregion

    public string DatabaseName { get; private set; }// = "F:\\2172\\BMDB_Section_1245.bws";

    #region DbSets - Tables in the database
        public virtual DbSet<Client>           Clients          { get; set; }
        public virtual DbSet<IntermediateData> IntermediateData { get; set; }
        public virtual DbSet<PlayerName>       PlayerNames      { get; set; }
        public virtual DbSet<PlayerNumber>     PlayerNumbers    { get; set; }
        public virtual DbSet<ReceivedData>     ReceivedData     { get; set; }
        public virtual DbSet<RoundData>        RoundData        { get; set; }
        public virtual DbSet<Section>          Sections         { get; set; }
        public virtual DbSet<Session>          Sessions         { get; set; }
        public virtual DbSet<Table>            Tables           { get; set; }

        //public virtual DbSet<BiddingData>         BiddingData          { get; set; }
        //public virtual DbSet<HandEvaluation>      HandEvaluations      { get; set; }
        //public virtual DbSet<HandRecord>          HandRecords          { get; set; }
        //public virtual DbSet<LastEntryId>         LastEntryIds         { get; set; }
        //public virtual DbSet<PlayData>            PlayData             { get; set; }
        //public virtual DbSet<ReceivedDataCount>   ReceivedDataCounts   { get; set; }
        //public virtual DbSet<ReceivedDataGrouped> ReceivedDataGroupeds { get; set; }
        //public virtual DbSet<ResultCountBoard>    ResultCountBoards    { get; set; }
        //public virtual DbSet<ResultCountRound>    ResultCountRounds    { get; set; }
        //public virtual DbSet<ScoreUpload>         ScoreUploads         { get; set; }
        //public virtual DbSet<Setting>             Settings             { get; set; }
    #endregion

    public void Reload()
    {
        this.ReloadAll(_connection);
    }

    #region Database connection and configuration
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // If no provider is configured, register a lightweight InMemory provider so EF can build
            // the model and the context can be used for read-only operations. The actual data is
            // populated from the external OleDbConnection via ReloadAll, so the provider is only
            // required to satisfy EF internals.
            if (!optionsBuilder.IsConfigured)
                try
                {
                    optionsBuilder.UseInMemoryDatabase($"BridgeMate_{Path.GetFileNameWithoutExtension(DatabaseName)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Warning: failed to configure InMemory provider: {ex.Message}");
                    throw;
                }

            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            //
            //optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);
            //optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation("Danish_Norwegian_CI_AS");

            //modelBuilder.Entity<BiddingData>(entity =>
            //                                 {
            //                                   //entity.HasNoKey();
            //                                     entity.ToTable("BiddingData", "Access");
            //                                 });
            modelBuilder.Entity<Client>(entity =>
                                        {
                                            //entity.HasNoKey();
                                            entity.ToTable("Clients", "Access");
                                        });

            //modelBuilder.Entity<HandEvaluation>(entity =>
            //                                    {
            //                                      //entity.HasNoKey();
            //                                        entity.ToTable("HandEvaluation", "Access");
            //                                        entity.Property(e => e.Board).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastClubs).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastDiamonds).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastHcp).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastHearts).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastNotrump).HasColumnType("smallint");
            //                                        entity.Property(e => e.EastSpades).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthClubs).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthDiamonds).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthHcp).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthHearts).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthNotrump).HasColumnType("smallint");
            //                                        entity.Property(e => e.NorthSpades).HasColumnType("smallint");
            //                                        entity.Property(e => e.Section).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthClubs).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthDiamonds).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthHcp).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthHearts).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthNotrump).HasColumnType("smallint");
            //                                        entity.Property(e => e.SouthSpades).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestClubs).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestDiamonds).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestHcp).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestHearts).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestNotrump).HasColumnType("smallint");
            //                                        entity.Property(e => e.WestSpades).HasColumnType("smallint");
            //                                    });

            //modelBuilder.Entity<HandRecord>(entity =>
            //                                {
            //                                  //entity.HasNoKey();
            //                                    entity.ToTable("HandRecord", "Access");
            //                                    entity.Property(e => e.Board).HasColumnType("smallint");
            //                                    entity.Property(e => e.EastClubs).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.EastDiamonds).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.EastHearts).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.EastSpades).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.NorthClubs).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.NorthDiamonds).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.NorthHearts).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.NorthSpades).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.Section).HasColumnType("smallint");
            //                                    entity.Property(e => e.SouthClubs).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.SouthDiamonds).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.SouthHearts).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.SouthSpades).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.WestClubs).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.WestDiamonds).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.WestHearts).HasColumnType("varchar(6)");
            //                                    entity.Property(e => e.WestSpades).HasColumnType("varchar(6)");
            //                                });
            modelBuilder.Entity<IntermediateData>(entity =>
                                                  {
                                                      //entity.HasNoKey();
                                                      entity.ToTable("IntermediateData", "Access");
                                                      //entity.Property(e => e.Board).HasColumnType("smallint");
                                                      //entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                      //entity.Property(e => e.DateLog).HasColumnType("datetime");
                                                      //entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                      //entity.Property(e => e.Erased).HasColumnType("bool");
                                                      //entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
                                                      //entity.Property(e => e.Id)
                                                      //      .HasColumnType("int")
                                                      //      //.ValueGeneratedOnAddOrUpdate()
                                                      //      .HasColumnName("ID");
                                                      //entity.Property(e => e.LeadCard).HasColumnType("varchar(5)");
                                                      //entity.Property(e => e.NsEw)
                                                      //      .HasColumnType("varchar(1)")
                                                      //      .HasColumnName("NS/EW");
                                                      //entity.Property(e => e.PairEw)
                                                      //      .HasColumnType("smallint")
                                                      //      .HasColumnName("PairEW");
                                                      //entity.Property(e => e.PairNs)
                                                      //      .HasColumnType("smallint")
                                                      //      .HasColumnName("PairNS");
                                                      //entity.Property(e => e.Processed).HasColumnType("bool");
                                                      //entity.Property(e => e.Processed1).HasColumnType("bool");
                                                      //entity.Property(e => e.Processed2).HasColumnType("bool");
                                                      //entity.Property(e => e.Processed3).HasColumnType("bool");
                                                      //entity.Property(e => e.Processed4).HasColumnType("bool");
                                                      //entity.Property(e => e.Remarks).HasColumnType("varchar(127)");
                                                      //entity.Property(e => e.Result).HasColumnType("varchar(5)");
                                                      //entity.Property(e => e.Round).HasColumnType("smallint");
                                                      //entity.Property(e => e.Section).HasColumnType("smallint");
                                                      //entity.Property(e => e.SuspiciousContract).HasColumnType("smallint");
                                                      //entity.Property(e => e.TableNo).HasColumnType("smallint");
                                                      //entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                                  });

            //modelBuilder.Entity<LastEntryId>(entity =>
            //                                 {
            //                                   //entity.HasNoKey();
            //                                     entity.ToTable("LastEntryID", "Access");
            //                                 });

            //modelBuilder.Entity<PlayData>(entity =>
            //                              {
            //                                //entity.HasNoKey();
            //                                  entity.ToTable("PlayData", "Access");
            //                                  entity.Property(e => e.Board).HasColumnType("smallint");
            //                                  entity.Property(e => e.Card).HasColumnType("varchar(5)");
            //                                  entity.Property(e => e.Counter).HasColumnType("smallint");
            //                                  entity.Property(e => e.DateLog).HasColumnType("datetime");
            //                                  entity.Property(e => e.Direction).HasColumnType("varchar(1)");
            //                                  entity.Property(e => e.Erased).HasColumnType("bool");
            //                                  entity.Property(e => e.Id)
            //                                        .HasColumnType("int")
            //                                      //.ValueGeneratedOnAddOrUpdate()
            //                                        .HasColumnName("ID");
            //                                  entity.Property(e => e.Round).HasColumnType("smallint");
            //                                  entity.Property(e => e.Section).HasColumnType("smallint");
            //                                  entity.Property(e => e.TableNo).HasColumnType("smallint");
            //                                  entity.Property(e => e.TimeLog).HasColumnType("datetime");
            //                              });
            modelBuilder.Entity<PlayerName>(entity =>
                                            {
                                                //entity.HasNoKey();
                                                //entity.HasKey(e => e.Id);
                                                //entity.Property(e => e.Id)
                                                //.HasColumnType("int")
                                                //.ValueGeneratedNever()
                                                //.HasColumnName("ID");
                                                entity.ToTable("PlayerNames", "Access");
                                                entity.HasIndex(e => e.Id, "IDIndex");
                                                entity.HasIndex(e => e.StrId, "strIDIndex");
                                            });

            modelBuilder.Entity<PlayerNumber>(entity =>
                                              {
                                                  //entity.HasNoKey();
                                                  entity.ToTable("PlayerNumbers", "Access");
                                              });

            //modelBuilder.Entity<ReceivedDataCount>(entity =>
            //                                       {
            //                                         //entity.HasNoKey();
            //                                           entity.ToTable("ReceivedDataCount", "Access");
            //                                           entity.Property(e => e.Board).HasColumnType("smallint");
            //                                           entity.Property(e => e.Round).HasColumnType("smallint");
            //                                           entity.Property(e => e.Section).HasColumnType("smallint");
            //                                           entity.Property(e => e.TableNo).HasColumnType("smallint");
            //                                       });

            //modelBuilder.Entity<ReceivedDataGrouped>(entity =>
            //                                         {
            //                                           //entity.HasNoKey();
            //                                             entity.ToTable("ReceivedDataGrouped", "Access");
            //                                             entity.Property(e => e.Board).HasColumnType("smallint");
            //                                             entity.Property(e => e.Contract).HasColumnType("varchar(5)");
            //                                             entity.Property(e => e.Declarer).HasColumnType("smallint");
            //                                             entity.Property(e => e.Erased).HasColumnType("bool");
            //                                             entity.Property(e => e.LeadCard).HasColumnType("varchar(5)");
            //                                             entity.Property(e => e.NsEw)
            //                                                   .HasColumnType("varchar(1)")
            //                                                   .HasColumnName("NS/EW");
            //                                             entity.Property(e => e.PairEw)
            //                                                   .HasColumnType("smallint")
            //                                                   .HasColumnName("PairEW");
            //                                             entity.Property(e => e.PairNs)
            //                                                   .HasColumnType("smallint")
            //                                                   .HasColumnName("PairNS");
            //                                             entity.Property(e => e.Remarks).HasColumnType("varchar(127)");
            //                                             entity.Property(e => e.Result).HasColumnType("varchar(5)");
            //                                             entity.Property(e => e.Round).HasColumnType("smallint");
            //                                             entity.Property(e => e.Section).HasColumnType("smallint");
            //                                             entity.Property(e => e.TableNo).HasColumnType("smallint");
            //                                         });
            modelBuilder.Entity<ReceivedData>(entity =>
                                              {
                                                  //entity.HasNoKey();
                                                  entity.ToTable("ReceivedData", "Access");
                                                  entity.Property(e => e.Board).HasColumnType("smallint");
                                                  entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                  entity.Property(e => e.DateLog).HasColumnType("datetime");
                                                  entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                  entity.Property(e => e.Erased).HasColumnType("bool");
                                                  entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
                                                  entity.Property(e => e.Id)
                                                        .HasColumnType("int")
                                                        //.ValueGeneratedOnAddOrUpdate()
                                                        .HasColumnName("ID");
                                                  entity.Property(e => e.LeadCard).HasColumnType("varchar(5)");
                                                  entity.Property(e => e.NsEw)
                                                        .HasColumnType("varchar(1)")
                                                        .HasColumnName("NS/EW");
                                                  entity.Property(e => e.PairEw)
                                                        .HasColumnType("smallint")
                                                        .HasColumnName("PairEW");
                                                  entity.Property(e => e.PairNs)
                                                        .HasColumnType("smallint")
                                                        .HasColumnName("PairNS");
                                                  entity.Property(e => e.Processed).HasColumnType("bool");
                                                  entity.Property(e => e.Processed1).HasColumnType("bool");
                                                  entity.Property(e => e.Processed2).HasColumnType("bool");
                                                  entity.Property(e => e.Processed3).HasColumnType("bool");
                                                  entity.Property(e => e.Processed4).HasColumnType("bool");
                                                  entity.Property(e => e.Remarks).HasColumnType("varchar(127)");
                                                  entity.Property(e => e.Result).HasColumnType("varchar(5)");
                                                  entity.Property(e => e.Round).HasColumnType("smallint");
                                                  entity.Property(e => e.Section).HasColumnType("smallint");
                                                  entity.Property(e => e.SuspiciousContract).HasColumnType("smallint");
                                                  entity.Property(e => e.TableNo).HasColumnType("smallint");
                                                  entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                              });

            //modelBuilder.Entity<ResultCountBoard>(entity =>
            //                                      {
            //                                        //entity.HasNoKey();
            //                                          entity.ToTable("ResultCountBoard", "Access");
            //                                      });

            //modelBuilder.Entity<ResultCountRound>(entity =>
            //                                      {
            //                                        //entity.HasNoKey();
            //                                          entity.ToTable("ResultCountRound", "Access");
            //                                      });
            modelBuilder.Entity<RoundData>(entity =>
                                           {
                                               //entity.HasNoKey();
                                               entity.ToTable("RoundData", "Access");
                                               entity.Property(e => e.CustomBoards).HasColumnType("varchar(127)");
                                               entity.Property(e => e.Ewpair)
                                                     .HasColumnType("smallint")
                                                     .HasColumnName("EWPair");
                                               entity.Property(e => e.HighBoard).HasColumnType("smallint");
                                               entity.Property(e => e.LowBoard).HasColumnType("smallint");
                                               entity.Property(e => e.Nspair)
                                                     .HasColumnType("smallint")
                                                     .HasColumnName("NSPair");
                                               entity.Property(e => e.Round).HasColumnType("smallint");
                                               entity.Property(e => e.Section).HasColumnType("smallint");
                                               entity.Property(e => e.TableNo).HasColumnType("smallint");
                                           });

            //modelBuilder.Entity<ScoreUpload>(entity =>
            //                                 {
            //                                   //entity.HasNoKey();
            //                                     entity.ToTable("ScoreUpload", "Access");
            //                                     entity.Property(e => e.Board).HasColumnType("smallint");
            //                                     entity.Property(e => e.Contract).HasColumnType("varchar(5)");
            //                                     entity.Property(e => e.DateLog).HasColumnType("datetime");
            //                                     entity.Property(e => e.Declarer).HasColumnType("smallint");
            //                                     entity.Property(e => e.Erased).HasColumnType("bool");
            //                                     entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
            //                                     entity.Property(e => e.Id)
            //                                           .HasColumnType("int")
            //                                           .ValueGeneratedOnAddOrUpdate()
            //                                           .HasColumnName("ID");
            //                                     entity.Property(e => e.LeadCard).HasColumnType("varchar(5)");
            //                                     entity.Property(e => e.NsEw)
            //                                           .HasColumnType("varchar(1)")
            //                                           .HasColumnName("NS/EW");
            //                                     entity.Property(e => e.PairEw)
            //                                           .HasColumnType("smallint")
            //                                           .HasColumnName("PairEW");
            //                                     entity.Property(e => e.PairNs)
            //                                           .HasColumnType("smallint")
            //                                           .HasColumnName("PairNS");
            //                                     entity.Property(e => e.Processed).HasColumnType("bool");
            //                                     entity.Property(e => e.Processed1).HasColumnType("bool");
            //                                     entity.Property(e => e.Processed2).HasColumnType("bool");
            //                                     entity.Property(e => e.Processed3).HasColumnType("bool");
            //                                     entity.Property(e => e.Processed4).HasColumnType("bool");
            //                                     entity.Property(e => e.Remarks).HasColumnType("varchar(127)");
            //                                     entity.Property(e => e.Result).HasColumnType("varchar(5)");
            //                                     entity.Property(e => e.Round).HasColumnType("smallint");
            //                                     entity.Property(e => e.Section).HasColumnType("smallint");
            //                                     entity.Property(e => e.SuspiciousContract).HasColumnType("smallint");
            //                                     entity.Property(e => e.TableNo).HasColumnType("smallint");
            //                                     entity.Property(e => e.TimeLog).HasColumnType("datetime");
            //                                 });
            modelBuilder.Entity<Section>(entity =>
                                         {
                                             //entity.HasNoKey();
                                             entity.ToTable("Section", "Access");
                                         });

            modelBuilder.Entity<Session>(entity =>
                                         {
                                             //entity.HasNoKey();
                                             entity.ToTable("Session", "Access");
                                         });

            //modelBuilder.Entity<Setting>(entity =>
            //                             {
            //                               //entity.HasNoKey();
            //                                 entity.ToTable("Settings", "Access");
            //                                 entity.Property(e => e.Bm2nameSource)
            //                                       .HasColumnType("smallint")
            //                                       .HasColumnName("BM2NameSource");
            //                                 entity.Property(e => e.Bm2numberEntryEachRound)
            //                                       .HasColumnType("bool")
            //                                       .HasColumnName("BM2NumberEntryEachRound");
            //                                 entity.Property(e => e.Bm2numberEntryPreloadValues)
            //                                       .HasColumnType("bool")
            //                                       .HasColumnName("BM2NumberEntryPreloadValues");
            //                                 entity.Property(e => e.Bm2showPlayerNames)
            //                                       .HasColumnType("smallint")
            //                                       .HasColumnName("BM2ShowPlayerNames");
            //                                 entity.Property(e => e.LeadCard).HasColumnType("bool");
            //                                 entity.Property(e => e.MemberNumbers).HasColumnType("bool");
            //                                 entity.Property(e => e.MemberNumbersNoBlankEntry).HasColumnType("bool");
            //                                 entity.Property(e => e.Section).HasColumnType("smallint");
            //                                 entity.Property(e => e.ShowPairNumbers).HasColumnType("bool");
            //                             });
            modelBuilder.Entity<Table>(entity =>
                                       {
                                           entity.ToTable("Tables", "Access");
                                       });

            //OnModelCreatingPartial(modelBuilder);
        }

        //partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    #endregion

    // Read-Only access to the database!
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new int SaveChanges()
    {
        throw new InvalidOperationException("Denne context er readonly.");
    }

    private new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Denne context er readonly.");
    }


    //public void ReadSchema(string tableName)
    //{
    //    string       connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={DatabaseName};";
    //    using var    connection       = new OleDbConnection(connectionString);
    //    connection.Open();

    //    DataTable       schemaTable;
    //    using var       command = new OleDbCommand($"SELECT * FROM {tableName}", connection);
    //    using var       reader  = command.ExecuteReader();
    //    schemaTable             = reader.GetSchemaTable();
    //    Debugger.Break();
    //}

    //public void ListTables()
    //{
   
    //    DataTable tables = _connection.GetSchema("Tables");

    //    foreach (DataRow row in tables.Rows)
    //    {
    //        string tableType = row["TABLE_TYPE"]?.ToString();

    //        if (tableType == "TABLE")
    //        {
    //            string tableName = row["TABLE_NAME"]?.ToString();
    //            Debug.WriteLine(tableName);
    //        }
    //    }

    //    Debugger.Break();
    //}

    //internal void LoadTable<T>(DbSet<T> dbset) where T : class
    //{
    //    string tableName = typeof(T).Name + 's';
    //    // Find DbSet property og modeltype
    //    var prop = GetType().GetProperty(tableName, BindingFlags.Public | BindingFlags.Instance);
    //    if (prop == null)
    //        throw new ArgumentException($"DbSet property '{tableName}' not found.");

    //    var modelType = prop.PropertyType.GetGenericArguments()[0];
    //    var listType = typeof(List<>).MakeGenericType(modelType);

    //    string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=F:\2172\BMDB_Section_1245.bws;";
    //    using var connection = new OleDbConnection(connectionString);
    //    connection.Open();

    //    using var command = new OleDbCommand($"SELECT * FROM [{tableName}]", connection);
    //    using var reader = command.ExecuteReader();
    //    var props = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    //    var allPlayers = PlayerNames.ToList();
    //    //PlayerNames.RemoveRange(allPlayers);

    //    foreach(var player in allPlayers)
    //        PlayerNames.Remove(PlayerNames.Find(player.Id));

    //    //dbset.Remove(dbset.First()); // Tøm DbSet

    //    while (reader.Read())
    //    {
    //        var obj = Activator.CreateInstance(modelType);
    //        foreach (var p in props)
    //        {
    //            var colName = p.Name;
    //            if (!reader.HasColumn(colName)) continue;

    //            var value = reader[colName];
    //            if (value == DBNull.Value) value = null;
    //            p.SetValue(obj, value);
    //        }
    //        //dbset.Add(obj);
    //    }
    //}
    //public IList<object> LoadTableAsObjects(string tableName)
    //{
    //    // Find model type ud fra tableName (DbSet property navn)
    //    var prop = GetType().GetProperty(tableName, BindingFlags.Public | BindingFlags.Instance);

    //    if (prop == null)
    //        throw new ArgumentException($"DbSet property '{tableName}' not found.");

    //    var modelType = prop.PropertyType.GetGenericArguments()[0];

    //    var          result           = new List<object>();
    //    string       connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={DatabaseName};";
    //    using var    connection       = new OleDbConnection(connectionString);
    //    connection.Open();

    //    using var command = new OleDbCommand($"SELECT * FROM [{tableName}]", connection);
    //    using var reader  = command.ExecuteReader();
    //    var       props   = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    //    while (reader.Read())
    //    {
    //        var obj = Activator.CreateInstance(modelType);

    //        foreach (var p in props)
    //        {
    //            // Prøv at matche property med kolonnenavn
    //            var colName = p.Name;

    //            if (!reader.HasColumn(colName))
    //                continue;

    //            var value = reader[colName];

    //            if (value == DBNull.Value)
    //                value =  null;

    //            p.SetValue(obj, value);
    //        }

    //        result.Add(obj);
    //    }

    //    return result;
    //}

    #region Private Methods
        private void openConection(string databaseName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //_connectionString = $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={DatabaseName};";
            _connectionString = "Driver={Microsoft Access Driver (*.mdb)};"
                              + $"Dbq={databaseName};"
                              + "Uid=Admin;Pwd=;";

            //_connection = new OleDbConnection(_connectionString);
            _connection = new OdbcConnection(_connectionString);
            _connection.Open();
        }

        #region IDisposable Support
            private bool _disposed;

            public override void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _connection?.Dispose();
                }

                catch
                {
                    // swallow exceptions during dispose
                }

                _disposed = true;
                base.Dispose();
                GC.SuppressFinalize(this);
            }
        #endregion
    #endregion#endregion
}
