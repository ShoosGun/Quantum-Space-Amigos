using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using ServerSide.PacketCouriers.GameRelated.Entities;
using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.GameRelated.TransfromSync
{
    public enum SyncTransform : byte
    {
        PositionOnly,
        RotationOnly,
        ScaleOnly,
        PositionAndRotationOnly,
        All
    }
    public class TransformEntitySync : MonoBehaviour //Usar o primeiro byte do primeiro byte[] para suas informações de inicialização
    {
        //TODO Sincronizar qual Sector o corpo usa como referencia junto com a informação do Transform. Provavelmente gerar um hash com o nome do setor ou algo assim
        private NetworkedEntity networkedEntity;
        private SyncTransform syncTransformType;
        private void Start()
        {
            networkedEntity = GetComponent<NetworkedEntity>();

            object[] instantiateData = networkedEntity.intantiateData;
            if (instantiateData.Length > 0)
                syncTransformType = (SyncTransform)instantiateData[0];
        }
    }
}
