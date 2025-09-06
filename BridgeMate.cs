using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DBF.BridgeMateModel;
using DBF.DataModel;
using Syncfusion.UI.Xaml.Diagram.Controls;
using Syncfusion.UI.Xaml.Schedule;

namespace DBF
{
    public class BridgeMate : INotifyPropertyChanged
    {
        private readonly DispatcherTimer   timer;
        private          DateTime          sidstTjekket;
        private          BridgeMateContext db;
        private readonly Configuration     configuration;
        //private          string            bmDir    = null;
        private string bmFile   = null;
        private int    bmClubNo = -1;

        #region Public Properties
            public Session                                     Session  { get; private set; }
            public ObservableCollection<BridgeMateModel.Table> Tables   { get; private set; } = new();
            public ObservableCollection<RoundData>             Rounds   { get; private set; } = new();
            public ObservableCollection<ReceivedData>          Received { get; private set; } = new();

            public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
            public BridgeMate(Configuration configuration)
            {
                this.configuration = configuration;
                this.sidstTjekket  = DateTime.MinValue;

                this.timer = new DispatcherTimer
                             {
                                 Interval = TimeSpan.FromSeconds(7)
                             };
            }
        #endregion

        public void CheckOrOpen(DateTime date, int clubNo)
        {
            if (date   == Session?.Date
            &&  clubNo == bmClubNo
            &&  bmFile is not null)
                return;

            var path  = $"{configuration.BridgeMatePath}{clubNo}\\";
            var after = DateTime.Now.AddDays(-60); //TODO: rettes ned til 14 dage;

            if (!Directory.Exists(path))
                MessageBox.Show($"Mappen: '{path}' findes ikke. Så oplysningerne fra terminalerne kan ikke indlæses", "Fejl");
            else
            {
                foreach (var file in Directory.GetFiles(path, "*.bws")
                                              .Where(f => File.GetLastWriteTime(f) >= after)
                                              .OrderByDescending(f => File.GetLastWriteTime(f))) //TODO: skal det være oprettelses datetime?
                {
                    db = new BridgeMateContext(file);

                    foreach (var session in db.Sessions)
                        if (session.Date == date)
                        {
                            if (file != bmFile)
                            {
                                bmFile   = file;
                                bmClubNo = clubNo;
                                Session  = session;
                                db       = new BridgeMateContext(bmFile);

                                if (Session.Status != 2)
                                {
                                    //timer.Tick += Timer_Tick;
                                    //timer.Start();
                                }
                            }

                            return;
                        }
                }                                
            }

            // BridgeMate er fil ikke fundet
            Close();
        }

        public void Close()
        {
            timer.Stop();
            bmFile   = null;
            bmClubNo = -1;
            Session  = null;
            db       = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var nye = db.ReceivedData
                        .Where(p =>  p.DateLog >  sidstTjekket
                                 ||  p.DateLog == sidstTjekket && p.TimeLog >  sidstTjekket
                                 )
                        .ToList();

            foreach (var entry in nye)
                Received.Add(entry);

            if (nye.Any())
                OnPropertyChanged(nameof(Tables));

            sidstTjekket = DateTime.UtcNow;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
