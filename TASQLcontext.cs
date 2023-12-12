using FastMember;
using Microsoft.EntityFrameworkCore;
using System.Data.Odbc;
using System.Reflection;

namespace SQLStrategyProject
{
    public static class Repo
    {
        private static IRepository Rep { get; set; }
        static Repo()
        {
            Rep = new ODBCRepository();
        }
        public static void SetStrategy(IRepository repository)
        {
            Rep = repository;
        }
        public static List<T>? GetAll<T>(string instanceName) where T : new()
        {
            return Rep.GetAll<T>(instanceName);
        }
        public static T? GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions) where T : class, new()
        {
            return Rep.GetSingle<T>(instanceName, whereConditions);
        }
        public static void Insert<T>(string instanceName, T instance) where T : class
        {
            Rep.Insert(instanceName, instance);
        }
        public static void Remove<T>(string instanceName, T instance) where T : class
        {
            Rep.Remove(instanceName, instance);
        }
        public static void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            Rep.Update<T, I>(instance, whereConditions, valuesToUpd);
        }
    }

    public interface IRepository
    {
        public List<T>? GetAll<T>(string instanceName) where T : new();
        public T? GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions) where T : class, new();
        public void Insert<T>(string instanceName, T instance) where T : class;
        public void Remove<T>(string instanceName, T instance) where T : class;
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class;
    }

    public class TASQLRepository : IRepository
    {
        static TASQLcontext Database { get; set; }
        static TASQLRepository()
        {
            Database ??= new();
        }
        public List<T>? GetAll<T>(string instanceName) where T : new() => (Database[instanceName] as IQueryable<T>)?.ToList();
        public T? GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions) where T : class, new()
        {
            var db = Database[instanceName] as DbSet<T>;
            IQueryable<T>? filteredDb = db?.AsQueryable();
            {
                foreach (var (propSql, valSql, sqlOp) in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, propSql, valSql, sqlOp);
                }
                var flist = filteredDb?.ToList();
            }
            return filteredDb?.FirstOrDefault();

            IQueryable<T>? GetWhereResult(IQueryable<T>? objQuerry, string tableColname, string tableValue, SQLwhereOperations oper)
            {
                return objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval() == tableValue).AsQueryable();
            }
        }

        public void Insert<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>)?.Add(instance);
            Database.SaveChanges();
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>)?.Remove(instance);
            Database.SaveChanges();
        }
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            var db = Database[instance] as DbSet<I>;
            IQueryable<I>? filteredDb = db?.AsQueryable();
            {
                foreach (var (propSql, valSql, sqlOp) in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, propSql, valSql, sqlOp);
                }
                var flist = filteredDb?.ToList();
            }

            foreach (var v in valuesToUpd)
            {
                foreach (var f in filteredDb ?? Enumerable.Empty<I>())
                {
                    var propertyInfo = f.GetType().GetProperty(v.Item1, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    propertyInfo?.SetValue(f, v.Item2, null);
                    _ = Database.SaveChanges();
                }
            }

            IQueryable<I>? GetWhereResult(IQueryable<I>? objQuerry, string tableColname, string tableValue, SQLwhereOperations oper)
            {
                return objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval() == tableValue).AsQueryable();
            }
        }
    }

    public class ODBCRepository : IRepository
    {
        static OdbcConnection DbConnection { get; set; }
        static ODBCRepository()
        {
            OdbcConnectionStringBuilder builder = new() { Driver = "ODBC Driver 17 for SQL Server" };
            builder.Add("Server", "172.16.60.128");
            builder.Add("Initial Catalog", "Test_A");
            builder.Add("Uid", "TAndrei");
            builder.Add("Pwd", "tasql");
            DbConnection = new(builder.ConnectionString);
            DbConnection.Open();
        }
        public List<T> GetAll<T>(string instanceName) where T : new()
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = $"SELECT * FROM {instanceName}";
            OdbcDataReader DbReader = DbCommand.ExecuteReader();
            {
                List<T> RetVal = new();
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        var Props = Entity.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        PropDict = Props.ToDictionary(p => p.Name.ToUpper(), p => p);
                        while (DbReader.Read())
                        {
                            T newObject = new();
                            for (int Index = 0; Index < DbReader.FieldCount; Index++)
                            {
                                if (PropDict.ContainsKey(DbReader.GetName(Index).ToUpper()))
                                {
                                    var Info = PropDict[DbReader.GetName(Index).ToUpper()];
                                    if ((Info != null) && Info.CanWrite)
                                    {
                                        var Val = DbReader.GetValue(Index);
                                        Info.SetValue(newObject, (Val == DBNull.Value) ? null : Val, null);
                                    }
                                }
                            }
                            RetVal.Add(newObject);
                        }
                    }
                    else
                    {
                        RetVal = new();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                return RetVal;
            }
        }
        public T? GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions) where T : class, new()
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = $"SELECT TOP 1 * FROM {instanceName} WHERE {GetWhereConditionStr(whereConditions)}";
            OdbcDataReader DbReader = DbCommand.ExecuteReader();
            {
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        Type type = typeof(T);
                        var accessor = TypeAccessor.Create(type);
                        var members = accessor.GetMembers();
                        var t = new T();

                        for (int i = 0; i < DbReader.FieldCount; i++)
                        {
                            if (!DbReader.IsDBNull(i))
                            {
                                string fieldName = DbReader.GetName(i);
                                if (members.Any(m => string.Equals(m.Name, fieldName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    accessor[t, fieldName] = DbReader.GetValue(i);
                                }
                            }
                        }
                        return t;
                    }
                    else
                    {
                        return new();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public void Insert<T>(string instanceName, T instance) where T : class
        {
            CommonExecute(
                instanceName,
                instance,
                $"INSERT INTO {instanceName} ({string.Join(", ", GetFields(instance))}) VALUES ({string.Join(", ", GetFieldValues(instance))})"
                );
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            CommonExecute(
                instanceName,
                instance,
                $"DELETE FROM {instanceName} WHERE {Converter(GetFields(instance), GetFieldValues(instance))}"
                );
        }
        private List<string> GetFieldValues<T>(T instance) where T : class
        {
            var fieldValues = GetType(instance.GetType(), instance);
            if (!fieldValues.Any())
            {
                fieldValues = GetType(instance.GetType().BaseType, instance);
            }
            return fieldValues;
        }
        private void CommonExecute<T>(string instanceName, T instance, string command) where T : class
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();
        }
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            string command = $"UPDATE {instance} SET {ConcatValuesForUpdate(valuesToUpd)} WHERE {GetWhereConditionStr(whereConditions)}";
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();
        }
        private static List<string> GetFields<T>(T _)
        {
            return typeof(T)
                .GetProperties()
                .Select(x =>
                {
                    var dbAttribute = x.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
                    return (dbAttribute == null || dbAttribute.Name == null) ? x.Name : dbAttribute.Name;
                })
                .Where(x => x != null)
                .ToList();
        }
        private static string ConcatValuesForUpdate<T>(List<(string, T)> valuesToUpd)
        {
            List<string> outputList = new();
            foreach (var v in valuesToUpd)
            {
                outputList.Add(v.Item1 + " = " + "'" + v.Item2.ConvertSQLval() + "'");
            }
            return string.Join(", ", outputList);
        }
        private static string GetWhereConditionStr(List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions)
        {
            List<string> outputList = new();
            foreach (var (propSql, valSql, sqlOp) in whereConditions)
            {
                outputList.Add(propSql + EnumOperToStr(sqlOp) + "'" + valSql + "'");
            }
            return string.Join(" and ", outputList);
        }
        private static string EnumOperToStr(SQLwhereOperations sqlOp)
        {
            switch (sqlOp)
            {
                case SQLwhereOperations.Equals: { return "="; }
                case SQLwhereOperations.Less: { return "<"; }
                case SQLwhereOperations.More: { return ">"; }
                case SQLwhereOperations.StartsWith:
                case SQLwhereOperations.EndsWith: { return "LIKE '%'"; }
                case SQLwhereOperations.Contains: { return "LIKE '%%'"; }
                default: return "";
            }
        }
        private static List<string>? GetType<T>(Type? type, T instance) where T : class
        {
            return type?
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field =>
                {
                    return field.GetValue(instance) == null ? "NULL" : "'" +
                    field.GetValue(instance).ConvertSQLval() + "'";
                })
                .ToList();
        }
        private static string Converter(List<string> fields, List<string> values)
        {
            string output = string.Empty;
            for (int i = 0; i < fields.Count; i++)
            {
                if (values[i] == "NULL") continue;
                if (i > 0) output += " AND ";
                output += fields[i] + "=" + values[i];
            }
            return output;
        }
    }

    internal class TASQLcontext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "Data Source=172.16.60.128;Initial Catalog=Test_A;User ID=TAndrei;Password=tasql;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
            {
                _ = sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null
                );
            });
        }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Req> Request { get; set; }
        public virtual DbSet<Year_Plan> Year_Plan { get; set; }
        public virtual DbSet<Revisor> Revisors { get; set; }
        public virtual DbSet<RegionYp> RegionsYP { get; set; }
        public virtual DbSet<VneplanRecord> Vneplans { get; set; }
        public virtual DbSet<CashAktRecord> CashAktRecord { get; set; }
        public virtual DbSet<BSOAktRecord> BSOAktRecord { get; set; }
        public virtual DbSet<AdvanceAktRecord> AdvanceAktRecord { get; set; }
        public virtual DbSet<TreeOper> TreeOper { get; set; }
        public virtual DbSet<TreePosl> TreePosl { get; set; }
        public virtual DbSet<IdsOper> IdsOper { get; set; }
        public virtual DbSet<IdsPosl> IdsPosl { get; set; }
        public virtual DbSet<CertificateShort> CertificateShort { get; set; }
        public virtual DbSet<CertificRecord> CertificRecord { get; set; }
        public virtual DbSet<CertificRecordRev> CertificRecordRev { get; set; }
        public virtual DbSet<CertRecordPunkt> CertRecordPunkt { get; set; }

        public object? this[string propertyName]
        {
            get
            {
                Type myType = typeof(TASQLcontext);
                PropertyInfo? myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo?.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(TASQLcontext);
                PropertyInfo? myPropInfo = myType.GetProperty(propertyName);
                myPropInfo?.SetValue(this, value, null);
            }
        }
    }
    public static class Validator
    {
        public static bool IsEmail(this string text)
        {
            return text != null && new System.Text.RegularExpressions.Regex("^\\S+@\\S+\\.\\S+$").IsMatch(text);
        }
        public static string? ConvertSQLval<T>(this T value)
        {
            bool isDecimal = decimal.TryParse(value?.ToString(), out decimal dec);
            bool isBool = bool.TryParse(value?.ToString(), out bool bo);
            return isDecimal ? dec.ToString().Replace(',', '.') : (isBool ? bo ? "1" : "0" : value?.ToString());
        }
    }
    public enum SQLwhereOperations
    {
        Equals,
        More,
        Less,
        StartsWith,
        EndsWith,
        Contains
    }
}
