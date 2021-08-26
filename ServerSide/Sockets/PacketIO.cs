using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ServerSide.Sockets
{
    public class PacketWriter : BinaryWriter
    {
        private MemoryStream _memStream;

        public PacketWriter() : base()
        {
            _memStream = new MemoryStream();
            OutStream = _memStream;
        }
        public byte[] GetBytes()
        {
            Close();
            byte[] data = _memStream.ToArray();
            return data;
        }
        public void WriteAsArray(byte[] array)
        {
            Write(array.Length);
            Write(array);
        }
        public void Write(byte[][] byteMatrix)
        {
            int lenght = byteMatrix.Length;
            Write(lenght);
            for (int i = 0; i < lenght; i++)
                WriteAsArray(byteMatrix[i]);
        }
        public void Write(int[] array)
        {
            Write(array.Length);
            for(int i =0; i< array.Length;i++)
                Write(array[i]);
        }
        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }
        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }
        public void Write(Vector4 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
            Write(vector.x);
        }
        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.x);
        }
        /// <summary>
        /// Use DateTime.UtcNow to avoid problems
        /// </summary>
        /// <param name="dateTime"></param>
        public void Write(DateTime dateTime)
        {
            Write(dateTime.ToBinary());
        }

    }

    public class PacketReader : BinaryReader
    {
        public PacketReader(byte[] data) : base(new MemoryStream(data))
        {
        }

        public  byte[] ReadByteArray()
        {
            int arrayLenght = ReadInt32();
            return ReadBytes(arrayLenght);
        }
        public byte[][] ReadByteMatrix()
        {
            int lenght = ReadInt32();
            byte[][] byteMatrix = new byte[lenght][];
            for (int i = 0; i < lenght; i++)
                byteMatrix[i] = ReadByteArray();

            return byteMatrix;
        }
        public int[] ReadInt32Array()
        {
            int lenght = ReadInt32();
            int[] intArray = new int[lenght];
            for (int i = 0; i < lenght; i++)
                intArray[i] = ReadInt32();

            return intArray;
        }
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }
        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        /// <summary>
        /// Read as if the DateTime was a DateTime.UtcNow to avoid problems
        /// </summary>
        /// <returns></returns>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }
    }
}