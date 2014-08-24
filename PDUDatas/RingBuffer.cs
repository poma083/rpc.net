using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDUDatas
{
    public sealed class RingBuffer
    {
        public RingBuffer()
        {
            dataBuffer = new byte[1024];
        }
        public RingBuffer(int bufferSize)
        {
            dataBuffer = new byte[1024 * bufferSize];
        }

        private object enterLockObject = new object();

        private byte[] dataBuffer = null;
        private int positionStart = -1;
        private uint positionFinish = 0;

        public int BufferLength
        {
            get
            {
                return dataBuffer.Length;
            }
        }

        private uint GetLenPacket(uint start)
        {
            uint result = 0;
            lock (enterLockObject)
            {
                if (start + 4 <= dataBuffer.Length)
                {
                    Tools.ConvertArrayToUInt(dataBuffer, (int)start, ref result);
                }
                else
                {
                    byte[] tmp = new byte[4];
                    Array.Copy(dataBuffer, start, tmp, 0, dataBuffer.Length - start);
                    Array.Copy(dataBuffer, 0, tmp, dataBuffer.Length - start, 4 - (dataBuffer.Length - start));
                    Tools.ConvertArrayToUInt(tmp, 0, ref result);
                }
            }
            return result;
        }
        private uint DataLength
        {
            get
            {
                lock (enterLockObject)
                {
                    if (positionStart < 0)
                    {
                        return 0;
                    }
                    if (positionFinish > positionStart)
                    {
                        return positionFinish - (uint)positionStart;
                    }
                    else if (positionFinish < positionStart)
                    {
                        return (uint)dataBuffer.Length - (uint)positionStart + positionFinish;
                    }
                    else
                    {
                        return (uint)dataBuffer.Length;
                    }
                }
            }
        }

        public void Add(byte[] data, Action<object> DoWorkPacket)
        {
            uint dataLength = (uint)data.Length;
            lock (enterLockObject)
            {
                uint dataBufferLength = (uint)dataBuffer.Length;
                #region если пришедшие данные не влазят в свободное пространство кольцевого буфера
                // то выделим под кольцевой буффер дополнительное пространство
                if (dataBufferLength < DataLength + dataLength) // не влазят
                {
                    Array.Resize(ref dataBuffer, (int)dataBufferLength + (int)dataLength);
                    if (positionStart >= positionFinish)
                    {
                        Array.Copy(dataBuffer, positionStart, dataBuffer, positionStart + dataLength, dataBufferLength - positionStart);
                        positionStart += (int)dataLength;
                    }
                    dataBufferLength = (uint)dataBuffer.Length;
//                    Logger.Log.WarnFormat("Перераспределение размера буфера. Новый размер буфера \"{0}\"", requestBufferLength);

                }
                #endregion
                #region переносим пришедшие данные в наш кольцевой буфер
                if (positionFinish > positionStart)
                {
                    if (positionFinish + dataLength <= dataBufferLength)
                    {
                        System.Buffer.BlockCopy(data, 0, dataBuffer, (int)positionFinish, (int)dataLength);
                    }
                    else
                    {
                        Array.Copy(data, 0, dataBuffer, positionFinish, dataBufferLength - positionFinish);
                        Array.Copy(data, dataBufferLength - positionFinish, dataBuffer, 0, dataLength - (dataBufferLength - positionFinish));
                    }
                    if (positionStart < 0) //то-есть буфер был пуст
                    {
                        positionStart = (int)positionFinish;
                    }
                    positionFinish += dataLength;
                    if (positionFinish > dataBufferLength)
                    {
                        positionFinish -= dataBufferLength;
                    }
                }
                else if (positionFinish < positionStart)
                {
                    System.Buffer.BlockCopy(data, 0, dataBuffer, (int)positionFinish, (int)dataLength);
                    positionFinish += dataLength;
                }
                else
                {
                    throw new StackOverflowException("Произошло переполнение буффера requestBuffer");
                }
                #endregion 
                #region запускаем цикл обработки пакетов находящихся в кольцевом буффере
                for (; ; )
                {
                    dataLength = DataLength;
                    if (dataLength >= 16)
                    {
                        uint FirstPacketLength = GetLenPacket((uint)positionStart);
                        if (FirstPacketLength <= dataLength)
                        {
                            byte[] packet = new byte[FirstPacketLength];
                            if (positionStart + FirstPacketLength <= dataBufferLength)
                            {
                                Array.Copy(dataBuffer, positionStart, packet, 0, FirstPacketLength);
                                positionStart += (int)FirstPacketLength;
                            }
                            else
                            {
                                Array.Copy(dataBuffer, positionStart, packet, 0, dataBufferLength - positionStart);
                                Array.Copy(dataBuffer, 0, packet, dataBufferLength - positionStart, FirstPacketLength - (dataBufferLength - positionStart));
                                positionStart = (int)FirstPacketLength - ((int)dataBufferLength - positionStart);
                            }
                            if (positionStart == positionFinish)//то-есть буфер пуст
                            {
                                positionStart = -1;
                            }
                            Task doRequestWork = new Task(DoWorkPacket, packet);
                            doRequestWork.Start();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                #endregion
            }
        }
    }
}
