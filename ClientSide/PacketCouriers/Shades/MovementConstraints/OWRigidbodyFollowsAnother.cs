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
        private OWRigidbody rigidbodyThatFollows;
        //Pode ser usado para fazer um shade ou um player seguir entre si
        //melhor não usar nos dois

        //não podemos usar construtores, o que mais você quer que eu faça

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rigidbody"></param>
        /// <param name="isAConstrainer">If set to false, the object with the script will be the one who follows, and if set to true, the opposite happens.</param>
        public void SetConstrain(OWRigidbody rigidbody , bool isAConstrainer = false)
        {
            if (!isAConstrainer)
            {
                rigidbodyToFollow = rigidbody;
                rigidbodyThatFollows = OWUtilities.GetAttachedOWRigidbody(gameObject, false);
            }
            else
            {
                rigidbodyToFollow = OWUtilities.GetAttachedOWRigidbody(gameObject, false);
                rigidbodyThatFollows = rigidbody;
            }

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
                //Vector3 distance = rigidbodyToFollow.GetPosition() - rigidbodyThatFollows.GetPosition();

                //if (distance.sqrMagnitude >= _acceptableDistanceSqr)
                //{
                    rigidbodyThatFollows.MoveToPosition(rigidbodyToFollow.GetPosition());
                    rigidbodyThatFollows.AddVelocityChange(rigidbodyThatFollows.GetRelativeVelocity(rigidbodyToFollow));
                //}
                //else if(rigidbodyThatFollows.GetRelativeVelocity(rigidbodyToFollow).sqrMagnitude >= _acceptableDistanceSqr)
                //    rigidbodyThatFollows.AddVelocityChange(rigidbodyThatFollows.GetRelativeVelocity(rigidbodyToFollow));

                //Quaternion rotation = Quaternion.Inverse(rigidbodyToFollow.GetRotation()) * rigidbodyThatFollows.GetRotation();
                rigidbodyThatFollows.MoveToRotation(rigidbodyToFollow.GetRotation());
                rigidbodyThatFollows.SetAngularVelocity(rigidbodyToFollow.GetAngularVelocity());
                
            }
        }

    }
}
