using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Dapper;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DBF.BridgeMateModel;

/// <summary>
/// Provides extension methods for reloading all tracked entities in an Entity Framework Core DbContext from a database
/// connection.
/// </summary>
/// <remarks>These extensions are intended for scenarios where the in-memory state of tracked entities needs to be
/// refreshed to match the current state in the database. The methods operate on all entity types defined in the
/// DbContext model and update or add entities as needed. Use with caution in environments where concurrent changes may
/// occur, as this operation may overwrite local changes.</remarks>
/// <usage>
/// using (var connection = new SqlConnection(connString))
///{
///    connection.Open();
///    dbContext.ReloadAll(connection);
///}
/// </usage>
public static class EfReloadExtensions
{
    // ------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------
    public static void ReloadAll(this DbContext db, IDbConnection connection)
    {
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var table   = entityType.GetTableName();

            if (table == null)
                continue;

            var sql     = $"SELECT * FROM {table}";
            var newList = connection.Query(clrType, sql).ToList();

            var oldList = GetLocalEntities(db, clrType);

            var keyProps = entityType.FindPrimaryKey()?.Properties;

            if (keyProps == null)
                continue;

            var buildKey = BuildKeyFunc(clrType, keyProps);

            MergeEntities(db, oldList, newList, buildKey);
        }
    }

    // ------------------------------------------------------------
    // GET LOCAL ENTITIES
    // ------------------------------------------------------------
    private static IList GetLocalEntities(DbContext db, Type clrType)
    {
        //var set          = db.Set(clrType);
          var set = GetQueryableSet(db, clrType);

        var localProp    = set.GetType().GetProperty("Local");
        var localView    = localProp.GetValue(set);
        var toListMethod = localView.GetType().GetMethod("ToList");
        return (IList)toListMethod.Invoke(localView, null);
    }

    // ------------------------------------------------------------
    // BUILD COMPOSITE KEY FUNCTION
    // ------------------------------------------------------------
    private static Func<object, string> BuildKeyFunc(Type clrType, IReadOnlyList<IProperty> keyProps)
    {
        var propInfos = keyProps
                       .Select(k => clrType.GetProperty(k.Name))
                       .ToArray();

        return entity =>
               {
                   var values = propInfos
                               .Select(p => p.GetValue(entity)?.ToString() ?? "")
                               .ToArray();

                   return string.Join("::", values);
               };
    }

    // ------------------------------------------------------------
    // MERGE ENGINE
    // ------------------------------------------------------------
    private static void MergeEntities(
                                       DbContext db
                                     , IList oldList
                                     , IList newList
                                     , Func<object, string> buildKey)
    {
        var oldDict = oldList.Cast<object>().ToDictionary(buildKey);
        var newDict = newList.Cast<object>().ToDictionary(buildKey);

        foreach (var kv in newDict)
            if (!oldDict.TryGetValue(kv.Key, out var oldEntity))
                db.Add(kv.Value);
            else
                CopyValues(oldEntity, kv.Value);
    }

    // ------------------------------------------------------------
    // COPY VALUES FROM NEW ENTITY TO EXISTING EF ENTITY
    // ------------------------------------------------------------
    private static void CopyValues(object target, object source)
    {
        var type  = target.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite);

        foreach (var prop in props)
        {
            var value = prop.GetValue(source);
            prop.SetValue(target, value);
        }
    }

    private static IQueryable GetQueryableSet(DbContext db, Type clrType)
    {
        var method = typeof(DbContext)
        .GetMethods()
        .First(m => m.Name == "Set" && m.IsGenericMethod);

        var generic = method.MakeGenericMethod(clrType);
        return (IQueryable)generic.Invoke(db, null);
    }
}
