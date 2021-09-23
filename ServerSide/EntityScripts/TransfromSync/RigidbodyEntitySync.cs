using ServerSide.Sockets;
using UnityEngine;

namespace ServerSide.EntityScripts.TransfromSync
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
            Serialize = true;
        }
        protected override void Start()
        {
            base.Start();
            object[] instantiateData = networkedEntity.intantiateData;

            if (instantiateData.Length > 1)
                syncRigidbodyType = (SyncRigidbody)(byte)instantiateData[1];
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(rigidbody.velocity);
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(rigidbody.angularVelocity);
        }
    }
}
