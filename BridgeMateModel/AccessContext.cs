using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DBF.BridgeMateModel
{
    public partial class AccessContext : DbContext
    {
        public AccessContext()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public AccessContext(DbContextOptions<AccessContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BiddingDatum>        BiddingData          { get; set; }
        public virtual DbSet<Client>              Clients              { get; set; }
        public virtual DbSet<HandEvaluation>      HandEvaluations      { get; set; }
        public virtual DbSet<HandRecord>          HandRecords          { get; set; }
        public virtual DbSet<IntermediateDatum>   IntermediateData     { get; set; }
        public virtual DbSet<LastEntryId>         LastEntryIds         { get; set; }
        public virtual DbSet<PlayDatum>           PlayData             { get; set; }
        public virtual DbSet<PlayerName>          PlayerNames          { get; set; }
        public virtual DbSet<PlayerNumber>        PlayerNumbers        { get; set; }
        public virtual DbSet<ReceivedDataCount>   ReceivedDataCounts   { get; set; }
        public virtual DbSet<ReceivedDataGrouped> ReceivedDataGroupeds { get; set; }
        public virtual DbSet<ReceivedDatum>       ReceivedData         { get; set; }
        public virtual DbSet<ResultCountBoard>    ResultCountBoards    { get; set; }
        public virtual DbSet<ResultCountRound>    ResultCountRounds    { get; set; }
        public virtual DbSet<RoundDatum>          RoundData            { get; set; }
        public virtual DbSet<ScoreUpload>         ScoreUploads         { get; set; }
        public virtual DbSet<Section>             Sections             { get; set; }
        public virtual DbSet<Session>             Sessions             { get; set; }
        public virtual DbSet<Setting>             Settings             { get; set; }
        public virtual DbSet<Table>               Tables               { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseAccess("DataSource=C:\\BC3\\BridgeMate\\2172\\BMDB_Section_508.bws;Readonly=True;");
                //optionsBuilder.UseAccess("DataSource=D:\\Downloads\\BMDB_Section_1222.bws;Readonly=True");

            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("Danish_Norwegian_CI_AS");
            modelBuilder.Entity<BiddingDatum>(entity =>
                                              {
                                                  entity.HasNoKey();
                                                  entity.ToTable("BiddingData", "Access");                                                  
                                              });

            modelBuilder.Entity<Client>(entity =>
                                        {
                                            entity.HasNoKey();
                                            entity.ToTable("Clients", "Access");                                         
                                        });

            modelBuilder.Entity<HandEvaluation>(entity =>
                                                {
                                                    entity.HasNoKey();
                                                    entity.ToTable("HandEvaluation", "Access");
                                                    entity.Property(e => e.Board).HasColumnType("smallint");
                                                    entity.Property(e => e.EastClubs).HasColumnType("smallint");
                                                    entity.Property(e => e.EastDiamonds).HasColumnType("smallint");
                                                    entity.Property(e => e.EastHcp).HasColumnType("smallint");
                                                    entity.Property(e => e.EastHearts).HasColumnType("smallint");
                                                    entity.Property(e => e.EastNotrump).HasColumnType("smallint");
                                                    entity.Property(e => e.EastSpades).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthClubs).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthDiamonds).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthHcp).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthHearts).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthNotrump).HasColumnType("smallint");
                                                    entity.Property(e => e.NorthSpades).HasColumnType("smallint");
                                                    entity.Property(e => e.Section).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthClubs).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthDiamonds).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthHcp).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthHearts).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthNotrump).HasColumnType("smallint");
                                                    entity.Property(e => e.SouthSpades).HasColumnType("smallint");
                                                    entity.Property(e => e.WestClubs).HasColumnType("smallint");
                                                    entity.Property(e => e.WestDiamonds).HasColumnType("smallint");
                                                    entity.Property(e => e.WestHcp).HasColumnType("smallint");
                                                    entity.Property(e => e.WestHearts).HasColumnType("smallint");
                                                    entity.Property(e => e.WestNotrump).HasColumnType("smallint");
                                                    entity.Property(e => e.WestSpades).HasColumnType("smallint");
                                                });

            modelBuilder.Entity<HandRecord>(entity =>
                                            {
                                                entity.HasNoKey();
                                                entity.ToTable("HandRecord", "Access");
                                                entity.Property(e => e.Board).HasColumnType("smallint");
                                                entity.Property(e => e.EastClubs).HasColumnType("varchar(6)");
                                                entity.Property(e => e.EastDiamonds).HasColumnType("varchar(6)");
                                                entity.Property(e => e.EastHearts).HasColumnType("varchar(6)");
                                                entity.Property(e => e.EastSpades).HasColumnType("varchar(6)");
                                                entity.Property(e => e.NorthClubs).HasColumnType("varchar(6)");
                                                entity.Property(e => e.NorthDiamonds).HasColumnType("varchar(6)");
                                                entity.Property(e => e.NorthHearts).HasColumnType("varchar(6)");
                                                entity.Property(e => e.NorthSpades).HasColumnType("varchar(6)");
                                                entity.Property(e => e.Section).HasColumnType("smallint");
                                                entity.Property(e => e.SouthClubs).HasColumnType("varchar(6)");
                                                entity.Property(e => e.SouthDiamonds).HasColumnType("varchar(6)");
                                                entity.Property(e => e.SouthHearts).HasColumnType("varchar(6)");
                                                entity.Property(e => e.SouthSpades).HasColumnType("varchar(6)");
                                                entity.Property(e => e.WestClubs).HasColumnType("varchar(6)");
                                                entity.Property(e => e.WestDiamonds).HasColumnType("varchar(6)");
                                                entity.Property(e => e.WestHearts).HasColumnType("varchar(6)");
                                                entity.Property(e => e.WestSpades).HasColumnType("varchar(6)");
                                            });

            modelBuilder.Entity<IntermediateDatum>(entity =>
                                                   {
                                                       entity.HasNoKey();
                                                       entity.ToTable("IntermediateData", "Access");
                                                       entity.Property(e => e.Board).HasColumnType("smallint");
                                                       entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                       entity.Property(e => e.DateLog).HasColumnType("datetime");
                                                       entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                       entity.Property(e => e.Erased).HasColumnType("bool");
                                                       entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
                                                       entity.Property(e => e.Id)
                                                             .HasColumnType("int")
                                                             .ValueGeneratedOnAddOrUpdate()
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
                                                       entity.Property(e => e.Table).HasColumnType("smallint");
                                                       entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                                   });

            modelBuilder.Entity<LastEntryId>(entity =>
                                             {
                                                 entity.HasNoKey();
                                                 entity.ToTable("LastEntryID", "Access");
                                             });

            modelBuilder.Entity<PlayDatum>(entity =>
                                           {
                                               entity.HasNoKey();
                                               entity.ToTable("PlayData", "Access");
                                               entity.Property(e => e.Board).HasColumnType("smallint");
                                               entity.Property(e => e.Card).HasColumnType("varchar(5)");
                                               entity.Property(e => e.Counter).HasColumnType("smallint");
                                               entity.Property(e => e.DateLog).HasColumnType("datetime");
                                               entity.Property(e => e.Direction).HasColumnType("varchar(1)");
                                               entity.Property(e => e.Erased).HasColumnType("bool");
                                               entity.Property(e => e.Id)
                                                     .HasColumnType("int")
                                                     .ValueGeneratedOnAddOrUpdate()
                                                     .HasColumnName("ID");
                                               entity.Property(e => e.Round).HasColumnType("smallint");
                                               entity.Property(e => e.Section).HasColumnType("smallint");
                                               entity.Property(e => e.Table).HasColumnType("smallint");
                                               entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                           });

            modelBuilder.Entity<PlayerName>(entity =>
                                            {
                                                entity.HasNoKey();
                                                entity.ToTable("PlayerNames", "Access");
                                                entity.HasIndex(e => e.Id, "IDIndex");
                                                entity.HasIndex(e => e.StrId, "strIDIndex");                                          
                                            });

            modelBuilder.Entity<PlayerNumber>(entity =>
                                              {
                                                  entity.HasNoKey();
                                                  entity.ToTable("PlayerNumbers", "Access");
                                              });

            modelBuilder.Entity<ReceivedDataCount>(entity =>
                                                   {
                                                       entity.HasNoKey();
                                                       entity.ToTable("ReceivedDataCount", "Access");
                                                       entity.Property(e => e.Board).HasColumnType("smallint");
                                                       entity.Property(e => e.Round).HasColumnType("smallint");
                                                       entity.Property(e => e.Section).HasColumnType("smallint");
                                                       entity.Property(e => e.Table).HasColumnType("smallint");
                                                   });

            modelBuilder.Entity<ReceivedDataGrouped>(entity =>
                                                     {
                                                         entity.HasNoKey();
                                                         entity.ToTable("ReceivedDataGrouped", "Access");
                                                         entity.Property(e => e.Board).HasColumnType("smallint");
                                                         entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                         entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                         entity.Property(e => e.Erased).HasColumnType("bool");
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
                                                         entity.Property(e => e.Remarks).HasColumnType("varchar(127)");
                                                         entity.Property(e => e.Result).HasColumnType("varchar(5)");
                                                         entity.Property(e => e.Round).HasColumnType("smallint");
                                                         entity.Property(e => e.Section).HasColumnType("smallint");
                                                         entity.Property(e => e.Table).HasColumnType("smallint");
                                                     });

            modelBuilder.Entity<ReceivedDatum>(entity =>
                                               {
                                                   entity.HasNoKey();
                                                   entity.ToTable("ReceivedData", "Access");
                                                   entity.Property(e => e.Board).HasColumnType("smallint");
                                                   entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                   entity.Property(e => e.DateLog).HasColumnType("datetime");
                                                   entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                   entity.Property(e => e.Erased).HasColumnType("bool");
                                                   entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
                                                   entity.Property(e => e.Id)
                                                         .HasColumnType("int")
                                                         .ValueGeneratedOnAddOrUpdate()
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
                                                   entity.Property(e => e.Table).HasColumnType("smallint");
                                                   entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                               });

            modelBuilder.Entity<ResultCountBoard>(entity =>
                                                  {
                                                      entity.HasNoKey();
                                                      entity.ToTable("ResultCountBoard", "Access");
                                                  });

            modelBuilder.Entity<ResultCountRound>(entity =>
                                                  {
                                                      entity.HasNoKey();
                                                      entity.ToTable("ResultCountRound", "Access");
                                                  });

            modelBuilder.Entity<RoundDatum>(entity =>
                                            {
                                                entity.HasNoKey();
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
                                                entity.Property(e => e.Table).HasColumnType("smallint");
                                            });

            modelBuilder.Entity<ScoreUpload>(entity =>
                                             {
                                                 entity.HasNoKey();
                                                 entity.ToTable("ScoreUpload", "Access");
                                                 entity.Property(e => e.Board).HasColumnType("smallint");
                                                 entity.Property(e => e.Contract).HasColumnType("varchar(5)");
                                                 entity.Property(e => e.DateLog).HasColumnType("datetime");
                                                 entity.Property(e => e.Declarer).HasColumnType("smallint");
                                                 entity.Property(e => e.Erased).HasColumnType("bool");
                                                 entity.Property(e => e.ExternalUpdate).HasColumnType("bool");
                                                 entity.Property(e => e.Id)
                                                       .HasColumnType("int")
                                                       .ValueGeneratedOnAddOrUpdate()
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
                                                 entity.Property(e => e.Table).HasColumnType("smallint");
                                                 entity.Property(e => e.TimeLog).HasColumnType("datetime");
                                             });

            modelBuilder.Entity<Section>(entity =>
                                         {
                                             entity.HasNoKey();
                                             entity.ToTable("Section", "Access");                                        
                                         });

            modelBuilder.Entity<Session>(entity =>
                                         {
                                             entity.HasNoKey();
                                             entity.ToTable("Session", "Access");
                                         });

            modelBuilder.Entity<Setting>(entity =>
                                         {
                                             entity.HasNoKey();
                                             entity.ToTable("Settings", "Access");
                                             entity.Property(e => e.Bm2nameSource)
                                                   .HasColumnType("smallint")
                                                   .HasColumnName("BM2NameSource");
                                             entity.Property(e => e.Bm2numberEntryEachRound)
                                                   .HasColumnType("bool")
                                                   .HasColumnName("BM2NumberEntryEachRound");
                                             entity.Property(e => e.Bm2numberEntryPreloadValues)
                                                   .HasColumnType("bool")
                                                   .HasColumnName("BM2NumberEntryPreloadValues");
                                             entity.Property(e => e.Bm2showPlayerNames)
                                                   .HasColumnType("smallint")
                                                   .HasColumnName("BM2ShowPlayerNames");
                                             entity.Property(e => e.LeadCard).HasColumnType("bool");
                                             entity.Property(e => e.MemberNumbers).HasColumnType("bool");
                                             entity.Property(e => e.MemberNumbersNoBlankEntry).HasColumnType("bool");
                                             entity.Property(e => e.Section).HasColumnType("smallint");
                                             entity.Property(e => e.ShowPairNumbers).HasColumnType("bool");
                                         });

            modelBuilder.Entity<Table>(entity =>
                                       {
                                           entity.ToTable("Tables", "Access");
                                       });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        // Read-Only access to the database!
        public override int SaveChanges()
        {
            throw new InvalidOperationException("Denne context er readonly.");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Denne context er readonly.");
        }
    }
}
