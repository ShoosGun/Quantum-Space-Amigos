using System;
using System.Collections.Generic;
using UnityEngine;
using ClientSide.Sync;
using ClientSide.Sockets;

namespace ClientSide.PacketCouriers.Shades
{
    public class Client_ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private Client client;

        private List<KeyValuePair<ShadeTransform, DateTime>> serverSnapshots = new List<KeyValuePair<ShadeTransform, DateTime>>();

        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo

        Shade playerShade;

        void Start()
        {
            client = GameObject.Find("ShadeTest").GetComponent<ClientMod>()._clientSide;

            client.Connection += Client_Connection;
            client.Disconnection += Client_Disconnection;
        }

        void Destroy()
        {
            client.Connection -= Client_Connection;
            client.Disconnection -= Client_Disconnection;
        }

        private void Client_Connection()
        {
            playerShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();

            Debug.Log("Conectados no Servidor!");
        }


        private void Client_Disconnection()
        {
            playerShade.DestroyShade();
            Debug.Log("Desconectados do servidor");
        }
        
        void FixedUpdate()
        {
            foreach(var shadeTransf in serverSnapshots)
            {
                playerShade.rigidbody.MovePosition(shadeTransf.Key.Position);
                playerShade.rigidbody.MoveRotation(shadeTransf.Key.Rotation);
            }

        }

        public void Receive(ref PacketReader packet)
        {
            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.TRANSFORM:
                    DateTime arriveTime = packet.ReadDateTime();
                    ShadeTransform shadeTransform = new ShadeTransform(packet.ReadVector3(), packet.ReadQuaternion());
                    serverSnapshots.Add(new KeyValuePair<ShadeTransform, DateTime>(shadeTransform, arriveTime));
                    break;
                    
                default:
                    break;
            }
        }
    }
    public struct ShadeTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public ShadeTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        public ShadeTransform(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }
    }

    public enum ShadeHeader : byte
    {
        MOVEMENT,
        SET_NAME,
        TRANSFORM
    }

    public enum ShadeMovementHeader : byte
    {
        HORIZONTAL_MOVEMENT,
        JUMP,
        SPIN
    }
}
