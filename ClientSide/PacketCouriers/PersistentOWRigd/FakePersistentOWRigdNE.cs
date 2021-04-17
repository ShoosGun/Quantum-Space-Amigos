using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.PacketCouriers.Entities;
using ClientSide.PacketCouriers.Shades.MovementConstraints;

namespace ClientSide.PacketCouriers.PersistentOWRigd
{
    public class FakePersistentOWRigdNE : OWRigidbodyNetworker
    {
        private OWRigidbodyFollowsAnother OWRigidbodyToFake;

        public void SetToConstrain(OWRigidbody oWRigidbody)
        {
            OWRigidbodyToFake = oWRigidbody.gameObject.AddComponent<OWRigidbodyFollowsAnother>();
        }

        public void Constrain()
        {
            OWRigidbodyToFake.SetConstrain(this.GetAttachedOWRigidbody());
        }

        public void ResetConstraint()
        {
            OWRigidbodyToFake.Reset();
        }
    }
}
