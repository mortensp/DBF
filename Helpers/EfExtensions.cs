using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DBF.Helpers;

public static class EfExtensions
{
    public static void RefreshAll<TEntity>(this DbContext db)
        where TEntity : class
    {
        var set = db.Set<TEntity>();

        // 1. Hent nye rækker (tilføjer kun nye entities)
        set.Load();

        // 2. Refresh alle eksisterende entities + navigation properties
        var visited = new HashSet<object>();

        foreach (var entity in set.Local)
            RefreshEntityGraph(db, entity, visited);
    }

    public static void RefreshAllTrackedEntities(this DbContext db)
    {
        var entityTypes = db.Model.GetEntityTypes();

        foreach (var type in entityTypes)
        {
            var clrType = type.ClrType;

            // Kald RefreshAll<TEntity>() via reflection
            var method = typeof(EfExtensions)
            .GetMethod(nameof(EfExtensions.RefreshAll))
            .MakeGenericMethod(clrType);

            method.Invoke(null, new object[] { db });
        }
    }

    #region Private helper methods
        private static void RefreshEntityGraph(DbContext db, object entity, HashSet<object> visited)
        {
            if (entity == null || visited.Contains(entity))
                return;

            visited.Add(entity);

            // Reload selve entity'en
            db.Entry(entity).Reload();

            var entry = db.Entry(entity);

            // 1. Reference navigation properties
            foreach (var reference in entry.References)
            {
                if (reference.Metadata.IsEagerLoaded(db))
                {
                    reference.Load();
                    RefreshEntityGraph(db, reference.CurrentValue, visited);
                }
            }

            // 2. Collection navigation properties
            foreach (var collection in entry.Collections)
            {
                if (collection.Metadata.IsEagerLoaded(db))
                {
                    collection.Load();

                    foreach (var child in (IEnumerable<object>)collection.CurrentValue)
                        RefreshEntityGraph(db, child, visited);
                }
            }
        }

        // Helper: afgør om navigationen er konfigureret til eager loading
        private static bool IsEagerLoaded(this INavigationBase navigation, DbContext db)
        {
            // Hvis navigationen er inkluderet via modelkonfiguration (HasOne/WithMany + AutoInclude)
            return navigation is INavigation nav && nav.IsEagerLoaded;
        }
    #endregion
}
