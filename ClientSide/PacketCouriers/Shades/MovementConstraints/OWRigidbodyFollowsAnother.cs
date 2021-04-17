using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.PacketCouriers.Shades.MovementConstraints
{
    //Achar um nome melhor
    public class OWRigidbodyFollowsAnother : MonoBehaviour
    {
        private const float _acceptableDistanceSqr = 1E-9f;
        private bool Constrain = false;

        private OWRigidbody rigidbodyToFollow;
        private OWRigidbody oWRigidbody;
        //Pode ser usado para fazer um shade ou um player seguir entre si
        //melhor não usar nos dois

        //não podemos usar construtores, o que mais você quer que eu faça
        public void SetConstrain(OWRigidbody rigidbodyToFollow)
        {
            this.rigidbodyToFollow = rigidbodyToFollow;
            oWRigidbody = OWUtilities.GetAttachedOWRigidbody(gameObject, false);
            Constrain = true;
        }

        public void Reset()
        {
            Constrain = false;
        }
        
        private void FixedUpdate()
        {
            if (Constrain)
            {
                Vector3 distance = rigidbodyToFollow.GetPosition() - oWRigidbody.GetPosition();

                if (distance.sqrMagnitude >= _acceptableDistanceSqr)
                {
                    oWRigidbody.MoveToPosition(distance + oWRigidbody.GetPosition());
                    oWRigidbody.AddVelocityChange(oWRigidbody.GetRelativeVelocity(rigidbodyToFollow));
                }
                else if(oWRigidbody.GetRelativeVelocity(rigidbodyToFollow).sqrMagnitude >= _acceptableDistanceSqr)
                    oWRigidbody.AddVelocityChange(oWRigidbody.GetRelativeVelocity(rigidbodyToFollow));
            }
        }

    }
}
