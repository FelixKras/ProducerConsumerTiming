using System;
using System.Collections.Concurrent;
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
        private static ConcurrentDictionary<uint, double[]> lstTimes;

        static cTimer()
        {
            lstTimes = new ConcurrentDictionary<uint, double[]>(2, 10000);
            sw = Stopwatch.StartNew();
        }
        readonly static object _locker = new object();
        public static void RecordTime(ActionsEnm enm, uint iCount)
        {

            lock(_locker){
                double dTime = sw.ElapsedTicks * 1000D / Stopwatch.Frequency;
                if (!lstTimes.ContainsKey(iCount))
                {
                    if (enm == ActionsEnm.Enq)
                    {
                        lstTimes.TryAdd(iCount, new double[] { -100, dTime });
                    }
                    else
                    {
                        lstTimes.TryAdd(iCount, new double[] { dTime, -100 });
                    }

                }
                else
                {
                    if (enm == ActionsEnm.Enq)
                    {
                        lstTimes[iCount][1] = dTime;
                    }
                    else
                    {
                        lstTimes[iCount][0] = dTime;
                    }

                }
            }

        }

        public static void WriteAllRecords()
        {
            using (StreamWriter strw = new StreamWriter(new FileStream(DateTime.Now.ToString("ddMMyyyyhhmmss") + ".txt",
                FileMode.Append, FileAccess.Write, FileShare.Write)))
            {
                for (uint ii = 0; ii < lstTimes.Count; ii++)
                {

                    string ss = ii + "," + (lstTimes[ii][1] - lstTimes[ii][0]);
                    strw.WriteLine(ss);
                }



            }
        }
    }
}
