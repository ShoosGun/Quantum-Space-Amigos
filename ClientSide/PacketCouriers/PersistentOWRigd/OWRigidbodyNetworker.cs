using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.PacketCouriers.Entities;
using UnityEngine;

namespace ClientSide.PacketCouriers.PersistentOWRigd
{
    public class OWRigidbodyNetworker : NetworkedEntity
    {
        public bool isItInSimulationMode = true;
        
        public Transform[] Colliders;
        
        /// <summary>
        /// Dissables the ability of the game object of feeling gravity fields, makes it kinematic and disables collision/detectors
        /// </summary>
        public void GoToNetworkedMode()
        {
            gameObject.GetAttachedOWRigidbody().MakeKinematic();

            foreach (var col in Colliders)
                col.collider.enabled = false;

            gameObject.rigidbody.velocity = Vector3.zero;

            isItInSimulationMode = false;
        }
        /// <summary>
        /// Reenables the ability of the game object of feeling gravity fields
        /// </summary>
        public void GoToSimulationMode(bool stayKinematic = false, bool stayWithoutCollisionsAndDetectors = false)
        {
            if (!stayKinematic)
                gameObject.GetAttachedOWRigidbody().MakeNonKinematic();

            foreach (var col in Colliders)
                col.collider.enabled = !stayWithoutCollisionsAndDetectors;
           
            isItInSimulationMode = true;
        }

        public void GetCollidersFromPaths(string[] CollidersNames)
        {
            List<Transform> colliders = new List<Transform>();

            foreach (string name in CollidersNames)
                colliders.Add(transform.FindChild(name));

            Colliders = colliders.ToArray();
        }
    }
}
