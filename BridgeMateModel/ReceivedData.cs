using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class ReceivedData
    {
        private DateTime dateLog;
        private DateTime timeLog;

        [NotMapped]
        public DateTime Date               => new DateTime(dateLog.Year, dateLog.Month, dateLog.Day, timeLog.Hour, timeLog.Minute, timeLog.Second, timeLog.Millisecond);
        //--------------------
        public int?     Id                 { get; set; }
        public short?   Section            { get; set; }
        public short?   Table              { get; set; }
        public short?   Round              { get; set; }
        public short?   Board              { get; set; }
        public short?   PairNs             { get; set; }
        public short?   PairEw             { get; set; }
        public short?   Declarer           { get; set; }
        public string   NsEw               { get; set; }
        public string   Contract           { get; set; }
        public string   Result             { get; set; }
        public string   LeadCard           { get; set; }
        public string   Remarks            { get; set; }
        public DateTime DateLog
        {
            get => dateLog;
            set
            {
                dateLog = value;
                //Date = new DateTime(dateLog.Year, dateLog.Month, dateLog.Day, timeLog.Hour, timeLog.Minute, timeLog.Second, timeLog.Millisecond);
            }
        }

        public DateTime TimeLog
        {
            get => timeLog;
            set
            {
                timeLog = value;
                //Date = new DateTime(dateLog.Year, dateLog.Month, dateLog.Day, timeLog.Hour, timeLog.Minute, timeLog.Second, timeLog.Millisecond);
            }
        }

        public bool?    Processed          { get; set; }
        public bool?    Processed1         { get; set; }
        public bool?    Processed2         { get; set; }
        public bool?    Processed3         { get; set; }
        public bool?    Processed4         { get; set; }
        public bool?    Erased             { get; set; }
        public bool?    ExternalUpdate     { get; set; }
        public short?   SuspiciousContract { get; set; }
    }
}
