using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProdCons
{
    public static class cTimer
    {
        public enum ActionsEnm
        {
            Enq,
            Deq,
        }

        private static Stopwatch sw;
        private static List<Tuple<ActionsEnm, uint, double>> lstTimes;

        static cTimer()
        {
            lstTimes = new List<Tuple<ActionsEnm, uint, double>>(1000);
            sw = Stopwatch.StartNew();
        }

        public static void RecordTime(ActionsEnm enm, uint iCount)
        {
            Tuple<ActionsEnm, uint, double> tup =
                new Tuple<ActionsEnm, uint, double>(enm, iCount, sw.Elapsed.TotalMilliseconds);
            lstTimes.Add(tup);
        }

        public static void WriteAllRecords()
        {
            using (StreamWriter strw = new StreamWriter(new FileStream(DateTime.Now.ToString("ddMMyyyyhhmmss") + ".txt",
                FileMode.Append, FileAccess.Write, FileShare.Write)))
            {
                for (int ii = 0; ii < lstTimes.Count; ii++)
                {
                    for (int jj = 1; jj < 5; jj++)
                    {
                        if (lstTimes[ii].Item1 == ActionsEnm.Enq &&
                            (lstTimes[ii + jj].Item1 == ActionsEnm.Deq) &&
                            (lstTimes[ii].Item2 == lstTimes[ii + jj].Item2))
                        {
                            string ss = lstTimes[ii].Item1 + "," + lstTimes[ii].Item2 + "," + lstTimes[ii].Item3+
                                        ","+lstTimes[ii + jj].Item1 + "," + lstTimes[ii + jj].Item2 + "," + lstTimes[ii + jj].Item3;
                            strw.WriteLine(ss);
                            break;
                        }
                    }

                    
                    
                }
            }
        }
    }
}