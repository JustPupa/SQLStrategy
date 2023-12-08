namespace SQLStrategyProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            /**
             * ЗДЕСЬ ПРОИСХОДИТ МАГИЯ ВНЕ ХОГВАРТСА
             * */

            //new TASQLRepository();    ИЛИ     new ODBCRepository();
            Repo.SetStrategy(new ODBCRepository());
            
            //ПРОВЕРКА SELECT * FROM [Table]
            {
                //var objectList = Repo.GetAll<User>("Users").ToList(); //4
                //var objectList2 = Repo.GetAll<Req>("Request").ToList(); //0
                //var objectList3 = Repo.GetAll<Year_Plan>("Year_Plan").ToList(); //34
                //var objectList4 = Repo.GetAll<Revisor>("Revisors").ToList(); //181
                //var objectList5 = Repo.GetAll<RegionYp>("RegionsYP").ToList(); //6
                //var objectList6 = Repo.GetAll<VneplanRecord>("Vneplans").ToList(); //1

                //var object2List = Repo.GetAll<CashAktRecord>("CashAktRecord").ToList(); //16
                //var object2List2 = Repo.GetAll<BSOAktRecord>("BSOAktRecord").ToList(); //16
                //var object2List3 = Repo.GetAll<AdvanceAktRecord>("AdvanceAktRecord").ToList(); //0
                //var object2List4 = Repo.GetAll<TreeOper>("TreeOper").ToList(); //149
                //var object2List5 = Repo.GetAll<TreePosl>("TreePosl").ToList(); //122
                //var object2List6 = Repo.GetAll<IdsOper>("IdsOper").ToList(); //119

                //var object3List = Repo.GetAll<IdsPosl>("IdsPosl").ToList(); //117
                //var object3List2 = Repo.GetAll<CertificateShort>("CertificateShort").ToList(); //3
                //var object3List3 = Repo.GetAll<CertificRecord>("CertificRecord").ToList(); //5
                //var object3List4 = Repo.GetAll<CertificRecordRev>("CertificRecordRev").ToList(); //4
                //var object3List5 = Repo.GetAll<CertRecordPunkt>("CertRecordPunkt").ToList(); //14
            }

            //Просто объекты
            //User user1 = new()
            //{
            //    Login = "3",
            //    Password = "3",
            //    Role = 1,
            //    ObjName = "Глусский УПС",
            //    Email = "3@mail.ru"
            //};
            //Req req1 = new()
            //{
            //    Login = "4",
            //    Password = "4",
            //    Role = 1,
            //    ObjName = "Глусский УПС",
            //    Email = "3@mail.ru"
            //};
            //Year_Plan yp = new()
            //{
            //    Ops_name = "Доколлллль",
            //    Srok_doc_1 = "Январь",
            //    Srok_doc_2 = "Март",
            //    Date_doc = new DateTime(2023, 1, 4),
            //    Date_doc_2 = new DateTime(2023, 3, 5),
            //    Srok_oper = "Май",
            //    Date_oper = new DateTime(2023, 5, 23),
            //    Srok_posl_1 = "Июнь",
            //    Srok_posl_2 = "Август",
            //    Date_posl = new DateTime(2023, 6, 15),
            //    Date_posl_2 = new DateTime(2023, 8, 9),
            //    Year = 2023,
            //    Serial = 99999,
            //    Region = "Глусский УПС"
            //};
            //Revisor rev1 = new()
            //{
            //    ID = 9999,
            //    Ops = "Доколь",
            //    Job_title = "jj",
            //    Rev_Type_Num = 2,
            //    Year = 2023,
            //    IsSecondMonth = false
            //};
            //RegionYp reg1 = new()
            //{
            //    Region = "testreg",
            //    Year = 2024
            //};
            //VneplanRecord vnep1 = new()
            //{
            //    Ops_name = "Доколь",
            //    Year = 2023,
            //    Akt = "AKKKKT",
            //    Date = new DateTime(2023, 12, 5),
            //    Reason = "just cause"
            //};
            //CashAktRecord cash1 = new()
            //{
            //    Ops_name = "Доколь",
            //    Date = new DateTime(2023, 12, 7),
            //    InFact = false,
            //    Remaining = (decimal?)15.6,
            //    Chancellery = (decimal?)17.24,
            //    Periodic = (decimal?)84.12,
            //    Retail = (decimal?)43.8,
            //    AktTypeId = 2,
            //    PROVERKA = 3
            //};
            //BSOAktRecord bso1 = new()
            //{
            //    AktTypeId = 2,
            //    PROVERKA = 3,
            //    Date = new DateTime(2023, 9, 11),
            //    InFact = false,
            //    Ops_name = "Доколь",
            //    Calendar = 54.2m,
            //    Stamp = 12.33m,
            //    Package = 0
            //};
            //AdvanceAktRecord adv1 = new()
            //{
            //    AktTypeId = 2,
            //    PROVERKA = 3,
            //    Date = new DateTime(2023, 5, 3),
            //    FIO = "F IIIII OO",
            //    InFact = false,
            //    JobTitle = "jjO-b",
            //    Ops_name = "Березовка",
            //    Marks = 17.37m,
            //    Goods = 11.95m
            //};
            //IdsOper idso1 = new()
            //{
            //    Id = "9999",
            //    Content = "testNode",
            //    Include = false
            //};
            //TreeOper trop1 = new()
            //{
            //    ParentNode = "ROOT",
            //    ChildNode = "9999"
            //};
            //IdsPosl idsp1 = new()
            //{
            //    Id = "9999",
            //    Content = "testNode",
            //    Include = false
            //};
            //TreePosl trpo1 = new()
            //{
            //    ParentNode = "ROOT",
            //    ChildNode = "9999"
            //};
            //CertificateShort cshort1 = new()
            //{
            //    Ops_name = "Доколь",
            //    Date = new DateTime(2023, 12, 1),
            //    PROVERKA = 3,
            //    Period1 = new DateTime(2023, 12, 2),
            //    Period2 = new DateTime(2023, 12, 4),
            //    ToFio = "FF",
            //    ToJob = "JJ",
            //    VyvodPosl = "vyvod takoi vyvod"
            //};
            //CertificRecord crec1 = new()
            //{
            //    Id = 777,
            //    Inspected = "ins",
            //    Cons = "cocons",
            //    Ops_name = "Доколь",
            //    Date = new DateTime(2023, 12, 1),
            //    PROVERKA = 3
            //};
            //CertificRecordRev crecrev1 = new()
            //{
            //    Id = 888,
            //    JobTitle = "inspuk",
            //    FIO = "josh hutchinson",
            //    RecordID = 777
            //};
            //CertRecordPunkt crecpunk1 = new()
            //{
            //    Id = 4444,
            //    Content = "phub",
            //    RecordId = 777,
            //    IsThirdLvl = true
            //};

            //ПРОВЕРКА INSERT INTO [Table]
            {
                //Repo.Insert("Users", user1);
                //Repo.Insert("Request", req1);
                //Repo.Insert("Year_Plan", yp);
                //Repo.Insert("Revisors", rev1);
                //Repo.Insert("RegionsYP", reg1);
                //Repo.Insert("Vneplans", vnep1);
                //Repo.Insert("CashAktRecord", cash1);
                //Repo.Insert("BSOAktRecord", bso1);
                //Repo.Insert("AdvanceAktRecord", adv1);
                //Repo.Insert("IdsOper", idso1);
                //Repo.Insert("TreeOper", trop1);
                //Repo.Insert("IdsPosl", idsp1);
                //Repo.Insert("TreePosl", trpo1);
                //Repo.Insert("CertificateShort", cshort1);
                //Repo.Insert("CertificRecord", crec1);
                //Repo.Insert("CertificRecordRev", crecrev1);
                //Repo.Insert("CertRecordPunkt", crecpunk1);
            }

            //ПРОВЕРКА DELETE FROM [Table]
            {
                //Repo.Remove("Users", user1);
                //Repo.Remove("Request", req1);
                //Repo.Remove("Year_Plan", yp);
                //Repo.Remove("Revisors", rev1);
                //Repo.Remove("RegionsYP", reg1);
                //Repo.Remove("Vneplans", vnep1);
                //Repo.Remove("CashAktRecord", cash1);
                //Repo.Remove("BSOAktRecord", bso1);
                //Repo.Remove("AdvanceAktRecord", adv1);
                //Repo.Remove("TreeOper", trop1);
                //Repo.Remove("IdsOper", idso1);
                //Repo.Remove("TreePosl", trpo1);
                //Repo.Remove("IdsPosl", idsp1);
                //Repo.Remove("CertificRecordRev", crecrev1);
                //Repo.Remove("CertRecordPunkt", crecpunk1);
                //Repo.Remove("CertificRecord", crec1);
                //Repo.Remove("CertificateShort", cshort1);
            }

            //Проверка Update
            {
                List<(string, string, SQLwhereOperations)> whereState = new()
                {
                    ("Ops_name", "ОПС №5 г. Горки", SQLwhereOperations.Equals),
                    ("InFact", "0", SQLwhereOperations.Equals),
                    ("AktTypeId", "1", SQLwhereOperations.Equals)
                };
                List<(string, object)> valsUpd = new() { ("Stamp", 13.82) };
                Repo.Update<object, BSOAktRecord>("BSOAktRecord", whereState, valsUpd);
            }
        }
    }
}