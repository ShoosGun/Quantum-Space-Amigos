using ServerSide.Sockets;
using UnityEngine;

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
        private MajorSector sectorReferenceFrame;

        private bool hasSectorDetector = false;
        private SectorDetector sectorDetector;
        private bool isOnSpace = true;
        private SyncTransform syncTransformType;

        private void Awake()
        {
            UniqueScriptIdentifingString = "TransformEntitySync";
            Serialize = true;
        }
        protected override void Start()
        {
            base.Start();
            object[] instantiateData = networkedEntity.intantiateData;

            if (instantiateData.Length > 0)
                syncTransformType = (SyncTransform)(byte)instantiateData[0];


            sectorDetector = GetComponentInChildren<SectorDetector>();
            hasSectorDetector = sectorDetector != null;

            if (hasSectorDetector)
            {
                sectorDetector.OnSwitchMajorSector += SectorDetector_OnSwitchMajorSector;
                sectorDetector.OnEnterTheVoid += SectorDetector_OnEnterTheVoid;
                sectorDetector.OnExitTheVoid += SectorDetector_OnExitTheVoid;
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (hasSectorDetector)
            {
                sectorDetector.OnSwitchMajorSector -= SectorDetector_OnSwitchMajorSector;
                sectorDetector.OnEnterTheVoid -= SectorDetector_OnEnterTheVoid;
                sectorDetector.OnExitTheVoid -= SectorDetector_OnExitTheVoid;
            }
        }

        private void SectorDetector_OnExitTheVoid()
        {
            isOnSpace = false;
        }
        private void SectorDetector_OnEnterTheVoid()
        {
            isOnSpace = true;
        }
        private void SectorDetector_OnSwitchMajorSector(MajorSector majorSector)
        {
            sectorReferenceFrame = majorSector;
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (!hasSectorDetector)
                return;

            Transform referenceFrame;
            if (!isOnSpace)
            {
                writer.Write((byte)sectorReferenceFrame.GetName());
                referenceFrame = sectorReferenceFrame.GetAttachedOWRigidbody().transform;
            }
            else //When on space, default to sun transform
            {
                writer.Write((byte)Sector.SectorName.Sun);
                referenceFrame = Locator.GetSunTransform();
            }

            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.position - referenceFrame.position);
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.rotation);
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.localScale);
        }
    }
}
