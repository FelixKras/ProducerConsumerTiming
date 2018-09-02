using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ProdCons
{
    public class ProducerConsumerInterlocked<T> : IProduceConsume<T>
    {
        private T[] m_buffer;
        private volatile int m_consumerIndex;
        private volatile int m_consumerWaiting;
        private AutoResetEvent m_consumerEvent;
        private volatile int m_producerIndex;
        private volatile int m_producerWaiting;
        private AutoResetEvent m_producerEvent;


        public void Init(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException("capacity");

            m_buffer = new T[capacity];
            m_consumerEvent = new AutoResetEvent(false);
            m_producerEvent = new AutoResetEvent(false);
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

    public class ConcurrQueProdCons<T> : IProduceConsume<T>
    {
        private ConcurrentQueue<T> que;
        public void Init(int capacity)
        {
            que=new ConcurrentQueue<T>();
            
        }

        public void Enqueue(T value)
        {
            que.Enqueue(value);
        }

        public T Dequeue()
        {
            T val;
            if (que.TryDequeue(out val))
            {
                return val;
            }

            return default(T);
        }
    }

    enum ProdCons
    {
        Interlocked,
        ConcurQue
    }

    static class ProduceConsumeFactory<T>
    {
        public static IProduceConsume<T> GetImplementation(ProdCons enmType, int capacity)
        {
            IProduceConsume<T> interfaceToReturn;
            switch (enmType)
            {
                case ProdCons.Interlocked:
                    ProducerConsumerInterlocked<T> prod1 = new ProducerConsumerInterlocked<T>();
                    prod1.Init(capacity);
                    interfaceToReturn = prod1;
                    break;
                case ProdCons.ConcurQue:
                    ConcurrQueProdCons<T> prod2 = new ConcurrQueProdCons<T>();
                    prod2.Init(capacity);
                    interfaceToReturn = prod2;
                    break;
                default:
                    interfaceToReturn = null;
                    break;
            }

            return interfaceToReturn;
        }
    }

    public interface IProduceConsume<T>
    {
        void Init(int capacity);
        void Enqueue(T value);
        T Dequeue();
    }
}