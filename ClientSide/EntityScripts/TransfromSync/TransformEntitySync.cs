using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.EntityScripts.TransfromSync
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
        private SyncTransform syncTransformType;
        
        private void Awake()
        {
            UniqueScriptIdentifingString = "TransformEntitySync";
            Serialize = false;
        }
        protected override void Start()
        {
            base.Start();
            
            object[] instantiateData = networkedEntity.intantiateData;
            if (instantiateData.Length > 0)
            {
                object syncType = instantiateData[0];
                Debug.Log(string.Format("Tipo: {0} - {1}", syncType.GetType(), syncType.ToString()));
                syncTransformType = (SyncTransform)(byte)syncType;
                Debug.Log("Transform type: " + instantiateData[0]);
            }
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            Sector.SectorName referenceFrameName = (Sector.SectorName)reader.ReadByte();
            Transform referenceFrame = Utils.MajorSectorLocator.GetMajorSector(referenceFrameName).GetAttachedOWRigidbody().transform;

            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.position = reader.ReadVector3() + referenceFrame.position;
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.rotation = reader.ReadQuaternion();
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                transform.localScale = reader.ReadVector3();
        }
    }
}
