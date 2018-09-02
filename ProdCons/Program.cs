using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;


namespace ProdCons
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DTO
    {
        public ushort usData1;
        public ushort usData2;
        public uint uiData1;
        public uint uiData2;

        public static byte[] GetBytes(DTO dto)
        {
            int size = Marshal.SizeOf(typeof(DTO));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            byte[] arr = new byte[size];

            try
            {
                Marshal.StructureToPtr(dto, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        public static DTO GetStruct(byte[] bytes)
        {
            int size = Marshal.SizeOf(typeof(DTO));
            IntPtr ptStrct = Marshal.AllocHGlobal(size);
            DTO dto = new DTO();
            try
            {
                Marshal.Copy(bytes, 0, ptStrct, size);
                dto = (DTO) Marshal.PtrToStructure(ptStrct, typeof(DTO));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(ptStrct);
            }


            return dto;
        }
    }

    internal class Program
    {
        static bool bKillSwitch = false;

        public static void Main(string[] args)
        {
            IProduceConsume<DTO> prodConsume = ProduceConsumeFactory<DTO>.GetImplementation(ProdCons.ConcurQue, 2);
            

            Thread thrProduce = new Thread(new ParameterizedThreadStart(ProduceMethod));
            thrProduce.Start(prodConsume);

            Thread thrConsume = new Thread(new ParameterizedThreadStart(ConsumeMethod));
            thrConsume.Start(prodConsume);
        }

        private static void ConsumeMethod(object prodConsObj)
        {
            ProducerConsumerInterlocked<DTO> prodCons = prodConsObj as ProducerConsumerInterlocked<DTO>;
            DTO dto;

            if (prodCons == null)
            {
                bKillSwitch = true;
            }

            while (!bKillSwitch)
            {
                dto = prodCons.Dequeue();
                cTimer.RecordTime(cTimer.ActionsEnm.Deq,dto.uiData2);

                
            }
        }

        private static void ProduceMethod(object prodConsObj)
        {
            IProduceConsume<DTO> prodCons = prodConsObj as IProduceConsume<DTO>;
            Random rnd = new Random();
            byte[] rndData = new byte[8];
            byte[] allData = new byte[12];

            if (prodCons == null)
            {
                bKillSwitch = true;
            }

            int iCount = 0;
            while (!bKillSwitch)
            {
                rnd.NextBytes(rndData);
                Buffer.BlockCopy(rndData, 0, allData, 0, rndData.Length);
                Buffer.BlockCopy(BitConverter.GetBytes((uint)iCount), 0, allData, 8, sizeof(uint));
                prodCons.Enqueue(DTO.GetStruct(allData));
                cTimer.RecordTime(cTimer.ActionsEnm.Enq,(uint)iCount);
                iCount++;
                if (iCount == 200)
                {
                    bKillSwitch = true;
                    cTimer.WriteAllRecords();
                }

            }
        }
    }


   
}