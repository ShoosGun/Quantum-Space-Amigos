using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.EntityScripts.TransfromSync
{
    public enum SyncRigidbody : byte
    {
        VelocityOnly,
        AngularMomentumOnly,
        Both
    }
    public class RigidbodyEntitySync : EntityScriptBehaviour //Usar o segundo byte do primeiro das informacoes para suas informações de inicialização
    {
        private SyncRigidbody syncRigidbodyType;

        private void Awake()
        {
            UniqueScriptIdentifingString = "RigidbodyEntitySync";
        }
        protected override void Start()
        {
            base.Start();

            object[] instantiateData = networkedEntity.intantiateData;
            if (instantiateData.Length > 1)
                syncRigidbodyType = (SyncRigidbody)(byte)instantiateData[1];
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.velocity = reader.ReadVector3();
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.angularVelocity = reader.ReadVector3();
        }
    }
}
