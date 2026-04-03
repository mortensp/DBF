using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.BridgeMateModel;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
/// 
public static class EfReloadExtensionsIL
{
    private static readonly Dictionary<Type, Action<object, object>>     _copyCache    = new();
    private static readonly Dictionary<Type, Func<object, object, bool>> _compareCache = new();

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
            var copier   = GetOrCreateCopier(clrType);
            var comparer = GetOrCreateComparer(clrType);

            MergeEntities(db, oldList, newList, buildKey, copier, comparer);
        }
    }

    // ------------------------------------------------------------
    // GET LOCAL ENTITIES
    // ------------------------------------------------------------
    private static IList GetLocalEntities(DbContext db, Type clrType)
    {
        //var set = db.Set(clrType);
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
        var propInfos = keyProps.Select(k => clrType.GetProperty(k.Name)).ToArray();

        return entity =>
               {
                   var values = propInfos
                               .Select(p => p.GetValue(entity)?.ToString() ?? "")
                               .ToArray();

                   return string.Join("::", values);
               };
    }

    // ------------------------------------------------------------
    // MERGE ENGINE WITH CHANGE DETECTION
    // ------------------------------------------------------------
    private static void MergeEntities(
                                       DbContext db
                                     , IList oldList
                                     , IList newList
                                     , Func<object, string> buildKey
                                     , Action<object, object> copier
                                     , Func<object, object, bool> comparer)
    {
        var oldDict = oldList.Cast<object>().ToDictionary(buildKey);
        var newDict = newList.Cast<object>().ToDictionary(buildKey);

        foreach (var (key, newEntity) in newDict)
        {
            if (!oldDict.TryGetValue(key, out var oldEntity))
                db.Add(newEntity);
            else
            {
                if (!comparer(oldEntity, newEntity))
                    copier(oldEntity, newEntity);
            }
        }
    }

    // ------------------------------------------------------------
    // IL-EMITTED COPY FUNCTION
    // ------------------------------------------------------------
    private static Action<object, object> GetOrCreateCopier(Type type)
    {
        if (_copyCache.TryGetValue(type, out var copier))
            return copier;

        var dm = new DynamicMethod(
                                    $"Copy_{type.Name}"
                                  , null
                                  , new[] { typeof(object), typeof(object) }
                                  , type.Module
                                  , true);

        var il = dm.GetILGenerator();

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.CanRead)
                        .ToArray();

        foreach (var prop in props)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, type);

            il.Emit(OpCodes.Callvirt, prop.GetMethod);
            il.Emit(OpCodes.Callvirt, prop.SetMethod);
        }

        il.Emit(OpCodes.Ret);

        copier           = (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
        _copyCache[type] = copier;

        return copier;
    }

    // ------------------------------------------------------------
    // IL-EMITTED COMPARER (RETURNS TRUE IF ENTITIES ARE EQUAL)
    // ------------------------------------------------------------
    private static Func<object, object, bool> GetOrCreateComparer(Type type)
    {
        if (_compareCache.TryGetValue(type, out var comparer))
            return comparer;

        var dm = new DynamicMethod(
                                    $"Compare_{type.Name}"
                                  , typeof(bool)
                                  , new[] { typeof(object), typeof(object) }
                                  , type.Module
                                  , true);

        var il    = dm.GetILGenerator();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead)
                        .ToArray();

        foreach (var prop in props)
        {
            var endIfEqual = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Callvirt, prop.GetMethod);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Callvirt, prop.GetMethod);

            if (prop.PropertyType.IsValueType)
                il.Emit(OpCodes.Beq_S, endIfEqual);
            else
            {
                var equalsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) });
                il.Emit(OpCodes.Call, equalsMethod);
                il.Emit(OpCodes.Brtrue_S, endIfEqual);
            }

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(endIfEqual);
        }

        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);

        comparer            = (Func<object, object, bool>)dm.CreateDelegate(typeof(Func<object, object, bool>));
        _compareCache[type] = comparer;

        return comparer;
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
