using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Odbc;
using System.Reflection;

namespace SQLStrategyProject
{
    public static class Repo
    {
        private static IRepository rep { get; set; }
        public static void SetStrategy(IRepository repository)
        {
            rep = repository;
        }
        /// <summary>
        ///Обобщенный параметр - Класс в коде. Параметр метода - название 
        ///таблицы SQL (а также одноимённый DbSet для Entity Framework)
        /// </summary>
        public static List<T> GetAll<T>(string instanceName) where T : new()
        {
            return rep.GetAll<T>(instanceName);
        }
        public static void Insert<T>(string instanceName, T instance) where T : class
        {
            rep.Insert<T>(instanceName, instance);
        }
        public static void Remove<T>(string instanceName, T instance) where T : class
        {
            rep.Remove<T>(instanceName, instance);
        }
        public static void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            rep.Update<T, I>(instance, whereConditions, valuesToUpd);
        }
    }

    public interface IRepository
    {
        public List<T> GetAll<T>(string instanceName) where T : new();
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
        public List<T> GetAll<T>(string instanceName) where T : new()
        {
            return (Database[instanceName] as IQueryable<T>).ToList();
        }

        public void Insert<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>).Add(instance);
            Database.SaveChanges();
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>).Remove(instance);
            Database.SaveChanges();
        }
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            var db = (Database[instance] as DbSet<I>);
            IQueryable<I> filteredDb = db.AsQueryable();
            //Фильтруем (базар)
            {
                foreach (var w in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, w.propSql, w.valSql, w.sqlOp);
                }
                IQueryable<I> GetWhereResult(IQueryable<I> objQuerry, string tableColname, string tableValue, SQLwhereOperations oper)
                {
                    var properties = typeof(I).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var property in properties)
                    {
                        if (property.Name == tableColname && property.CanRead)
                            return objQuerry.Where(q => property.GetValue(q, null).ToString() == tableValue);
                    }
                    return objQuerry;
                }
            }

            //foreach (var v in valuesToUpd)
            //{
            //    filteredDb.ExecuteUpdate(f => f.SetProperty(i =>
            //    {
            //        Type myType = typeof(I);
            //        PropertyInfo myPropInfo = myType.GetProperty(v.Item1);
            //        myPropInfo.SetValue(i, v.Item2, null);
            //    }));
            //}
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
                List<T> RetVal = null;
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        RetVal = new List<T>();
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
        public void Insert<T>(string instanceName, T instance) where T : class
        {
            var bindingFlags = BindingFlags.Instance |
                   BindingFlags.NonPublic |
                   BindingFlags.Public;

            var fieldValues = GetType(instance.GetType());

            if (!fieldValues.Any())
            {
                fieldValues = GetType(instance.GetType().BaseType);
            }

            OdbcCommand DbCommand = DbConnection.CreateCommand();
            string command = $"INSERT INTO {instanceName} ({string.Join(", ", GetFields(instance))}) " +
                $"VALUES ({string.Join(", ", fieldValues)})";
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();

            List<string> GetType(Type? type)
            {
                return type
                    .GetFields(bindingFlags)
                    .Select(field =>
                    {
                        bool isDecimal = decimal.TryParse(field.GetValue(instance)?.ToString(), out decimal dec);
                        return field.GetValue(instance) == null ? "NULL" : "'" + 
                        (isDecimal ? dec.ToString().Replace(',', '.') : field.GetValue(instance).ToString()) + "'";
                    })
                    .ToList();
            }
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            //throw new NotImplementedException();
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var fieldValues = GetType(instance.GetType());

            if (!fieldValues.Any())
            {
                fieldValues = GetType(instance.GetType().BaseType);
            }

            OdbcCommand DbCommand = DbConnection.CreateCommand();
            string command = $"DELETE FROM {instanceName} WHERE {Converter(GetFields(instance), fieldValues)}";
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();

            string Converter(List<string> fields, List<string> values)
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

            List<string> GetType(Type? type)
            {
                return type
                    .GetFields(bindingFlags)
                    .Select(field =>
                    {
                        return field.GetValue(instance) == null ? "NULL" : "'" +
                        field.GetValue(instance).ConvertSQLval() + "'";
                    })
                    .ToList();
            }
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
        private string ConcatValuesForUpdate<T>(List<(string, T)> valuesToUpd)
        {
            List<string> outputList = new();
            foreach (var v in valuesToUpd)
            {
                outputList.Add(v.Item1 + " = " + "'" + v.Item2.ConvertSQLval() + "'");
            }
            return string.Join(", ", outputList);
        }
        private string GetWhereConditionStr(List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions)
        {
            List<string> outputList = new();
            foreach (var v in whereConditions)
            {
                outputList.Add(v.propSql + EnumOperToStr(v.sqlOp) + "'" + v.valSql + "'");
            }
            return string.Join(" and ", outputList);
        }
        private static string EnumOperToStr(SQLwhereOperations sqlOp)
        {
            switch(sqlOp)
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
    }

    //Сюда вносить все DbSet по необходимости
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
        //Сертификаты и их классы для обновлённой бд
        public virtual DbSet<CertificateShort> CertificateShort { get; set; }
        public virtual DbSet<CertificRecord> CertificRecord { get; set; }
        public virtual DbSet<CertificRecordRev> CertificRecordRev { get; set; }
        public virtual DbSet<CertRecordPunkt> CertRecordPunkt { get; set; }

        //Метод расширения для получения свойства (контекста) через
        //строку (напр.: название таблицы в БД - "Ops")
        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(TASQLcontext);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(TASQLcontext);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }
    }

    //Пользователь программы
    [Table("Users")]
    public class User 
    {
        [Key]
        public string? Login { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required]
        public byte Role { get; set; }
        [Column("ObjectName")]
        public string? ObjName { get; set; }
        private string? email;
        public string? Email
        {
            get => email;
            set
            {
                if (value.IsEmail())
                {
                    email = value;
                }
            }
        }
    }

    [Table("OPS")]
    public class OPSobject
    {
        [Key]
        public string? Name { get; set; }
    }

    [Table("Request")]
    public class Req
    {
        [Key]
        public string? Login { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required]
        public byte Role { get; set; }
        [Column("ObjectName")]
        public string? ObjName { get; set; }
        [Column("IPadress")]
        public string? IPv4info { get; set; }
        private string? email;
        public string? Email
        {
            get => email;
            set
            {
                if (value.IsEmail())
                {
                    email = value;
                }
            }
        }
    }

    [Table("Year_Plan")]
    [PrimaryKey("Ops_name", "Year")]
    public class Year_Plan
    {
        public string Ops_name { get; set; }
        public string? Srok_doc_1 { get; set; }
        public string? Srok_doc_2 { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Date_doc { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Date_doc_2 { get; set; }
        public string? Srok_oper { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Date_oper { get; set; }
        public string? Srok_posl_1 { get; set; }
        public string? Srok_posl_2 { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Date_posl { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Date_posl_2 { get; set; }
        public int Year { get; set; }
        public int Serial { get; set; }
        public string Region { get; set; }
        public static int GetQuarter(string month)
        {
            return month.ToLower() switch
            {
                "январь" => 1,
                "февраль" => 1,
                "март" => 1,
                "апрель" => 2,
                "май" => 2,
                "июнь" => 2,
                "июль" => 3,
                "август" => 3,
                "сентябрь" => 3,
                "октябрь" => 4,
                "ноябрь" => 4,
                "декабрь" => 4,
                _ => 5,
            };
        }
    }

    [Table("Revisors")]
    [PrimaryKey("ID")]
    public class Revisor
    {
        [Key]
        public int ID { get; set; }
        public string? Ops { get; set; }
        public string? Job_title { get; set; }
        public int Rev_Type_Num { get; set; }
        public int Year { get; set; }
        public bool IsSecondMonth { get; set; }
        //Все 4 метода нужны для переопределения сравнение (теперь по значению + без учёта id)
        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            Revisor o1 = this;
            Revisor? o2 = obj as Revisor;
            bool b = o1.Ops == o2.Ops && o1.Job_title.ToLower() == o2.Job_title.ToLower() && o1.Rev_Type_Num == o2.Rev_Type_Num && o1.Year == o2.Year &&
                o1.IsSecondMonth == o2.IsSecondMonth;
            return b;
        }
        public static bool operator ==(Revisor obj1, Revisor obj2)
        {
            return obj1.Equals(obj2);
        }
        public static bool operator !=(Revisor obj1, Revisor obj2)
        {
            return !obj1.Equals(obj2);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ID, Ops, Job_title, Rev_Type_Num, Year, IsSecondMonth);
        }

        //Метод генерирует id для нового инспектора по принципу "любой свободный по возрастанию или максимальный из имеющихся"
        public static int GenerateId(List<int>? idsToAdd = null)
        {
            List<int> revIDs = new();
            try
            {
                revIDs = new TASQLcontext().Revisors.Select(r => r.ID).ToList();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Операция не выполнена. Внутреннее исключение: {ex.Message}");
            }
            if (idsToAdd != null)
            {
                revIDs.AddRange(idsToAdd);
            }
            if (!revIDs.Any())
            {
                return 1;
            }
            else
            {
                for (int id = 1; id < revIDs.Max() + 1; id++)
                {
                    if (!revIDs.Contains(id))
                    {
                        return id;
                    }
                }
            }
            return revIDs.Max() + 1;
        }
    }
    [Table("RegionsYP")]
    [PrimaryKey("Region", "Year")]
    public class RegionYp
    {
        public string Region { get; set; }
        public int Year { get; set; }
    }

    [Table("Vneplans")]
    [PrimaryKey("Ops_name", "Year", "Date")]
    public class VneplanRecord
    {
        [Required]
        public string Ops_name { get; set; }
        [Required]
        public int Year { get; set; }
        [Column(TypeName = "datetime")]
        [Required]
        public DateTime? Date { get; set; }
        public string Reason { get; set; }
        public string Akt { get; set; }
    }

    [Table("CashAktRecord")]
    [PrimaryKey("Ops_name", "Date", "InFact", "AktTypeId", "PROVERKA")]
    public class CashAktRecord
    {
        public CashAktRecord()
        { }
        public CashAktRecord(string ops, DateTime dt, decimal? rem, decimal? remWcash, decimal? mark, decimal? writ, decimal? bonus, decimal? toloto, decimal? inter,
            decimal? expr, decimal? serv, decimal? lot, decimal? chan, decimal? post, decimal? retail, decimal? tabac, decimal? parcbx, decimal? parcbs,
            decimal? seed, decimal? period, decimal? paid, decimal? paidmel, decimal? euro, decimal? comp, decimal? natsport, decimal? phil, bool f, int aid, int prov)
        {
            Ops_name = ops; Date = dt; Remaining = rem; Cash = remWcash; Marked = mark; WritCorr = writ; BonusPlus = bonus;
            ToLoto = toloto; InterCardWiFi = inter; ExprCardBelTel = expr; ServiceCard = serv; Loter = lot; Chancellery = chan;
            PostGoods = post; Retail = retail; Tabacco = tabac; ParcelBox = parcbx; ParcelBags = parcbs; Seeds = seed; Periodic = period;
            PaidShipping = paid; PaidMelShipping = paidmel; EuroLot = euro; ComplBest = comp; NatSportLot = natsport; PhilatelProd = phil;
            InFact = f; AktTypeId = aid; PROVERKA = prov;
        }
        public CashAktRecord(string ops, DateTime dt, List<decimal?> values, bool f, int aid, int prov) : this(ops, dt, values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15], values[16],
            values[17], values[18], values[19], values[20], values[21], values[22], values[23], f, aid, prov)
        { }

        [Required]
        public string Ops_name { get; set; }
        [Column(TypeName = "datetime")]
        [Required]
        public DateTime Date { get; set; }
        public int PROVERKA { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Remaining { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Cash { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Marked { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? WritCorr { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? BonusPlus { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ToLoto { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? InterCardWiFi { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ExprCardBelTel { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ServiceCard { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Loter { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Chancellery { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PostGoods { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Retail { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Tabacco { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ParcelBox { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ParcelBags { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Seeds { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Periodic { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PaidShipping { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PaidMelShipping { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? EuroLot { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ComplBest { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? NatSportLot { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PhilatelProd { get; set; }
        [Column(TypeName = "bit")]
        [Required]
        public bool InFact { get; set; }
        public int AktTypeId { get; set; }
    }

    [Table("BSOAktRecord")]
    [PrimaryKey("Ops_name", "Date", "InFact", "AktTypeId", "PROVERKA")]
    public class BSOAktRecord
    {
        public BSOAktRecord()
        { }
        public BSOAktRecord(string ops, DateTime dt, bool fact, int aid, decimal? pack, decimal? mail, decimal? smail, decimal? order, decimal? calen, decimal? stamp,
            decimal? quitt, decimal? wayb, decimal? insur, decimal? commsug, int prov)
        {
            Ops_name = ops; Date = dt; InFact = fact; AktTypeId = aid; Package = pack; Mail = mail; SpeedMail = smail; Ordered = order; Calendar = calen;
            Stamp = stamp; Quittance = quitt; WayBill = wayb; Insurance = insur; CommSugg = commsug;
            PROVERKA = prov;
        }
        public BSOAktRecord(string ops, DateTime dt, bool fact, int aid, List<decimal?> values, int prov) : this(ops, dt, fact, aid, values[0], values[1], values[2]
            , values[3], values[4], values[5], values[6], values[7], values[8], values[9], prov)
        { }
        [Required]
        public string Ops_name { get; set; }
        [Column(TypeName = "datetime")]
        [Required]
        public DateTime Date { get; set; }
        [Column(TypeName = "bit")]
        [Required]
        public bool InFact { get; set; }
        public int AktTypeId { get; set; }
        public int PROVERKA { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Package { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Mail { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? SpeedMail { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Ordered { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Calendar { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Stamp { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Quittance { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? WayBill { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Insurance { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CommSugg { get; set; }
    }

    [Table("AdvanceAktRecord")]
    [PrimaryKey("Ops_name", "Date", "InFact", "FIO", "AktTypeId", "PROVERKA")]
    public class AdvanceAktRecord
    {
        public AdvanceAktRecord()
        { }
        public AdvanceAktRecord(string ops, DateTime dt, bool fact, string fio, string? job, decimal? mark, decimal? goods, decimal? media, decimal? loter, decimal? cons, int aid, int prov)
        {
            Ops_name = ops; Date = dt; InFact = fact; FIO = fio; JobTitle = job; Marks = mark; Goods = goods;
            MassMedia = media; Loter = loter; ConsGoods = cons; AktTypeId = aid; PROVERKA = prov;
        }
        public AdvanceAktRecord(string ops, DateTime dt, bool fact, string fio, string? job, List<decimal?> values, int aid, int prov) : this(ops, dt, fact, fio, job, values[0], values[1],
            values[2], values[3], values[4], aid, prov)
        { }
        [Required]
        public string Ops_name { get; set; }

        [Column(TypeName = "datetime")]
        [Required]
        public DateTime Date { get; set; }

        [Column(TypeName = "bit")]
        [Required]
        public bool InFact { get; set; }
        [Required]
        public string FIO { get; set; }
        public string? JobTitle { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Marks { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Goods { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MassMedia { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Loter { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ConsGoods { get; set; }
        [Required]
        public int AktTypeId { get; set; }
        [Required]
        public int PROVERKA { get; set; }
    }

    [Table("CertificateShort")]
    [PrimaryKey("Ops_name", "Date", "PROVERKA")]
    public class CertificateShort
    {
        [Required]
        public string Ops_name { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Date { get; set; }
        [Required]
        public int PROVERKA { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Period1 { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Period2 { get; set; }
        public string? ToJob { get; set; }
        public string? ToFio { get; set; }
        public string? VyvodPosl { get; set; }
    }

    [Table("CertificRecord")]
    [PrimaryKey("Id")]
    public class CertificRecord
    {
        [Required]
        [Column("Id")]
        public int Id { get; set; }
        public string Inspected { get; set; }
        public string? Comment { get; set; }
        public string Cons { get; set; }
        [Required]
        public string Ops_name { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Date { get; set; }
        [Required]
        public int PROVERKA { get; set; }

        //Метод генерирует новый свободный id из базы данных + таблицы введенных
        public static int GetFreeId(DataGridView? dgvTmp = null)
        {
            TASQLcontext db = new();
            List<int> crs = db.CertificRecord.Select(cr => cr.Id).ToList();
            if (dgvTmp != null)
            {
                crs = crs
                    .Concat(dgvTmp.Rows
                        .OfType<DataGridViewRow>()
                        .Select(r => r.Cells[6].Value)
                        .Where(r => r != null && int.TryParse(r.ToString(), out _))
                        .Cast<int>())
                    .ToList();
            }
            if (!crs.Any())
            {
                return 1;
            }
            int maxId = crs.Max();
            for (int i = 1; i < maxId; i++)
            {
                if (crs.Any(c => c == i))
                {
                    continue;
                }
                else
                {
                    return i;
                }
            }
            return maxId + 1;
        }
    }

    [PrimaryKey("Id")]
    [Table("CertificRecordRev")]
    public class CertificRecordRev
    {
        [Required]
        public int Id { get; set; }
        public string? JobTitle { get; set; }
        public string? FIO { get; set; }
        [Required]
        [ForeignKey("CertificRecord")]
        public int RecordID { get; set; }
        public static int GetFreeId()
        {
            using TASQLcontext db = new();
            List<int> crrs = db.CertificRecordRev.Select(crr => crr.Id).ToList();
            if (!crrs.Any())
            {
                return 1;
            }
            int maxId = crrs.Max();
            for (int i = 1; i < maxId; i++)
            {
                if (crrs.All(c => c != i))
                {
                    return i;
                }
            }
            return maxId + 1;
        }
    }

    [PrimaryKey("Id")]
    [Table("CertRecordPunkt")]
    public class CertRecordPunkt
    {
        [Required]
        public int Id { get; set; }
        public string? Content { get; set; }
        [ForeignKey("CertificRecord")]
        public int RecordId { get; set; }
        public bool IsThirdLvl { get; set; }
        public static int GetFreeId()
        {
            using TASQLcontext db = new();
            List<int> crp = db.CertRecordPunkt.Select(crr => crr.Id).ToList();
            if (!crp.Any())
            {
                return 1;
            }
            int maxId = crp.Max();
            for (int i = 1; i < maxId; i++)
            {
                if (crp.All(c => c != i))
                {
                    return i;
                }
            }
            return maxId + 1;
        }
    }

    /// <summary>
    /// Базовый класс для связей узлов дерева нарушений
    /// </summary>
    [PrimaryKey("ParentNode", "ChildNode")]
    public abstract class NodeBind
    {
        [Required]
        public string ParentNode { get; set; }
        [Required]
        public string ChildNode { get; set; }
    }

    public class TreeOper : NodeBind { }

    public class TreePosl : NodeBind { }

    /// <summary>
    /// Базовый класс для узла нарушений (с контентом)
    /// </summary>
    [PrimaryKey("Id")]
    public abstract class IdsForCons
    {
        [Required]
        public string Id { get; set; }
        public string? Content { get; set; }
        public bool? Include { get; set; }
    }

    [PrimaryKey("Id")]
    public class IdsOper : IdsForCons { }

    [PrimaryKey("Id")]
    public class IdsPosl : IdsForCons { }

    public enum Month
    {
        Январь,
        Февраль,
        Март,
        Апрель,
        Май,
        Июнь,
        Июль,
        Август,
        Сентябрь,
        Октябрь,
        Ноябрь,
        Декабрь
    }

    public static class Validator
    {
        public static bool IsEmail(this string text)
        {
            //bool b1 = text != null;
            //bool b2 = new System.Text.RegularExpressions.Regex("^\\S+@\\S+\\.\\S+$").IsMatch(text);
            return text != null && new System.Text.RegularExpressions.Regex("^\\S+@\\S+\\.\\S+$").IsMatch(text);
        }
        public static List<Revisor> CopyList(this List<Revisor> revsToCopy)
        {
            List<Revisor> revsNew = new();
            foreach (Revisor re in revsToCopy)
            {
                revsNew.Add(new()
                {
                    ID = Revisor.GenerateId(),
                    IsSecondMonth = re.IsSecondMonth,
                    Job_title = re.Job_title,
                    Ops = re.Ops,
                    Rev_Type_Num = re.Rev_Type_Num,
                    Year = re.Year
                });
            }
            return revsNew;
        }
        public static string ConvertSQLval<T>(this T value)
        {
            bool isDecimal = decimal.TryParse(value?.ToString(), out decimal dec);
            bool isBool = bool.TryParse(value?.ToString(), out bool bo);
            return isDecimal ? dec.ToString().Replace(',', '.') : (isBool? BoolParse(bo) : value.ToString());
            string BoolParse(bool b)
            {
                return b ? "1" : "0";
            }
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

    //public class NotIncludedAsDbField : ValidationAttribute
    //{
    //    private string Value { get; set; }
    //    public NotIncludedAsDbField(string value)
    //    {

    //    }
    //}
}
