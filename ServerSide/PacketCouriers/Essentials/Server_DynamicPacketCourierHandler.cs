using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using ServerSide.Sockets.Servers;
using ServerSide.Utils;
using ServerSide.Sockets;
using UnityEngine;

namespace ServerSide.PacketCouriers.Essentials
{
    //Hibrido de PacketCourier com DynamicPacketIO, ele TEM que ter o HeaderValue IGUAL a 0(ZERO). Assim teremos um canal confiavel para comunicarmos entre os computadores
    public class Server_DynamicPacketCourierHandler : MonoBehaviour
    {
        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        public Server Server { get; private set; }
        public int HeaderValue { get; private set; }

        private List<KeyValuePair<long,int>> HashToHeaderTranslation;
        private List<KeyValuePair<long, int>> HashesToUpdate;

        public void SetVariables(ref Server_DynamicPacketIO dynamicPacketIO, Server server)
        {
            DynamicPacketIO = dynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketCourier(ReadPacket);
            HashToHeaderTranslation = new List<KeyValuePair<long, int>>();
            HashesToUpdate = new List<KeyValuePair<long, int>>();

            Server = server;
            Server.NewConnectionID += Server_NewConnectionID;
        }
        public void OnDestroy()
        {
            Server.NewConnectionID -= Server_NewConnectionID;
        }
        private void Server_NewConnectionID(string clientID)
        {
            StartCoroutine("SendToNewConnection", clientID);
        }
        IEnumerator SendToNewConnection(string clientID)
        {
            for (int i = 0; i < 1; i++)
            {
                yield return new WaitForSeconds(Time.deltaTime);
                Debug.Log(string.Format("Enviando informacoes para {0}, tentativa {1}", clientID, i));
                SendReturnAllHeaders(clientID);
            }
        }

        public void SendReturnAllHeaders(params string[] ClientIDs)
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((byte)DPCHHeaders.SEND_ALL_HEADERS);

            writer.Write(HashToHeaderTranslation.Count);
            if (HashToHeaderTranslation.Count > 0)
            {
                for (int i = 0; i < HashToHeaderTranslation.Count; i++)
                {
                    writer.Write(HashToHeaderTranslation[i].Key);//O hash
                    writer.Write(HashToHeaderTranslation[i].Value);//O valor do HeaderValue
                }
            }
            DynamicPacketIO.SendPackedData((byte)HeaderValue, writer.GetBytes(), ClientIDs);
        }
        public void SendUpdateHeaders()
        {
            if (HashesToUpdate.Count > 0)
            {
                PacketWriter writer = new PacketWriter();
                writer.Write((byte)DPCHHeaders.UPDATE_HEADERS);

                writer.Write(HashesToUpdate.Count);
                for(int i =0; i< HashesToUpdate.Count; i++)
                {
                    writer.Write(HashesToUpdate[i].Key);//O hash
                    writer.Write(HashesToUpdate[i].Value);//O valor do HeaderValue
                }
                HashesToUpdate.Clear();

                DynamicPacketIO.SendPackedData((byte)HeaderValue, writer.GetBytes());
            }
        }
        public int AddPacketCourier(string localizationString, ReadPacketHolder.ReadPacket readPacket)
        {
            long hash = Util.GerarHash(localizationString);
            if (HashToHeaderTranslation.Exists(i => i.Key == hash))
                throw new OperationCanceledException(string.Format("O hash de {0} ja esta gravado, use outra string que apresente um hash diferente de {1}", localizationString, hash));

            int courierHeaderValue = DynamicPacketIO.AddPacketCourier(readPacket);
            if(courierHeaderValue == ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES)
                throw new OperationCanceledException(string.Format("Alcancou-se o maximo permitido de PacketCouriers de {0}", ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES ));

            var HashHeaderPair = new KeyValuePair<long, int>(hash, courierHeaderValue);
            HashToHeaderTranslation.Add(HashHeaderPair);
            HashesToUpdate.Add(HashHeaderPair);
            return courierHeaderValue;
        }
        public void ReadPacket(byte[] data, string ClientID)
        {
            PacketReader reader = new PacketReader(data);
            DPCHHeaders DPCHHeader = (DPCHHeaders)reader.ReadByte();
            switch (DPCHHeader)
            {
                case DPCHHeaders.UPDATE_HEADERS:
                    SendReturnAllHeaders(ClientID);
                    return;
                default:
                case DPCHHeaders.SEND_ALL_HEADERS:
                    return;
            }
        }
    }
    public enum DPCHHeaders: byte
    {
        UPDATE_HEADERS,
        SEND_ALL_HEADERS,
    }
}
