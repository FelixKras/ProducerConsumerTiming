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
            ProducerConsumerRendezvousPoint<DTO> prodCons = new ProducerConsumerRendezvousPoint<DTO>(2);


            Thread thrProduce = new Thread(new ParameterizedThreadStart(ProduceMethod));
            thrProduce.Start(prodCons);

            Thread thrConsume = new Thread(new ParameterizedThreadStart(ConsumeMethod));
            thrConsume.Start(prodCons);
        }

        private static void ConsumeMethod(object prodConsObj)
        {
            ProducerConsumerRendezvousPoint<DTO> prodCons = prodConsObj as ProducerConsumerRendezvousPoint<DTO>;
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
            ProducerConsumerRendezvousPoint<DTO> prodCons = prodConsObj as ProducerConsumerRendezvousPoint<DTO>;
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


    public class ProducerConsumerRendezvousPoint<T>
    {
        private T[] m_buffer;
        private volatile int m_consumerIndex;
        private volatile int m_consumerWaiting;
        private AutoResetEvent m_consumerEvent;
        private volatile int m_producerIndex;
        private volatile int m_producerWaiting;
        private AutoResetEvent m_producerEvent;

        public ProducerConsumerRendezvousPoint(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException("capacity");

            m_buffer = new T[capacity];
            m_consumerEvent = new AutoResetEvent(false);
            m_producerEvent = new AutoResetEvent(false);
        }

        private int Capacity
        {
            get { return m_buffer.Length; }
        }

        private bool IsEmpty
        {
            get { return (m_consumerIndex == m_producerIndex); }
        }

        private bool IsFull
        {
            get { return (((m_producerIndex + 1) % Capacity) == m_consumerIndex); }
        }

        public void Enqueue(T value)
        {
            if (IsFull)
            {
                WaitUntilNonFull();
            }

            m_buffer[m_producerIndex] = value;

            Interlocked.Exchange(
                ref m_producerIndex, (m_producerIndex + 1) % Capacity);

            if (m_consumerWaiting == 1)
            {
                m_consumerEvent.Set();
            }
        }

        private void WaitUntilNonFull()
        {
            Interlocked.Exchange(ref m_producerWaiting, 1);

            try
            {
                while (IsFull)
                {
                    m_producerEvent.WaitOne();
                }
            }
            finally
            {
                m_producerWaiting = 0;
            }
        }

        public T Dequeue()
        {
            if (IsEmpty)
            {
                WaitUntilNonEmpty();
            }

            T value = m_buffer[m_consumerIndex];
            m_buffer[m_consumerIndex] = default(T);

            Interlocked.Exchange(ref m_consumerIndex, (m_consumerIndex + 1) % Capacity);

            if (m_producerWaiting == 1)
            {
                m_producerEvent.Set();
            }

            return value;
        }

        private void WaitUntilNonEmpty()
        {
            Interlocked.Exchange(ref m_consumerWaiting, 1);

            try
            {
                while (IsEmpty)
                {
                    m_consumerEvent.WaitOne();
                }
            }
            finally
            {
                m_consumerWaiting = 0;
            }
        }
    }
}