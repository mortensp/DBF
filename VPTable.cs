//using HtmlAgilityPack;
//using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace DBF.ViewModels
{  
    public class VPTable
    {
        private List <decimal> _tbl;
        private int _boards;

        public VPTable(int boards)
        {
            _tbl   = new();
            _boards = boards;

            double B = 15 * Math.Sqrt(boards);
            double T = (Math.Sqrt(5) - 1) / 2;

            for (var M = 0; M <  B; M++)
            {
                double t2 = 1d - Math.Pow(T, 3 * M / B);

                double V = 10d + 10d * t2 / (1d - Math.Pow(T, 3));
                _tbl.Add((decimal)Double.Round(V,2));
            }
        }

        public int NumberOfBoards => _boards;

        public decimal this[int index]
        {
            get
            {
                if (index < 0m)
                {
                    index = Math.Abs(index);
                    if (index >=(decimal)_tbl.Count)
                        return 0m;

                    return 20m - _tbl[index];
                }
                else
                {                 

                    if (index >= (decimal)_tbl.Count)
                        return 20m;

                    return _tbl[index];
                }
            }
        }
    }
}

