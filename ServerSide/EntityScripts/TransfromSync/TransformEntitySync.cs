using UnityEngine;

using ServerSide.PacketCouriers.GameRelated.Entities;

namespace ServerSide.EntityScripts.TransfromSync
{
    public enum SyncTransform : byte
    {
        PositionOnly,
        RotationOnly,
        ScaleOnly,
        PositionAndRotationOnly,
        All
    }
    public class TransformEntitySync : EntityScriptBehaviour //Usar o primeiro byte do primeiro byte[] para suas informações de inicialização
    {
        //TODO Sincronizar qual Sector o corpo usa como referencia junto com a informação do Transform. Provavelmente gerar um hash com o nome do setor ou algo assim
       private SyncTransform syncTransformType;
        protected override void Start()
        {
            base.Start();

            object[] instantiateData = networkedEntity.intantiateData;
            if (instantiateData.Length > 0)
                syncTransformType = (SyncTransform)instantiateData[0];
        }
    }
}
