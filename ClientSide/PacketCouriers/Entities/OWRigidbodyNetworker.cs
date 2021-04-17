using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.PacketCouriers.Entities
{
    public class OWRigidbodyNetworker : NetworkedEntity
    {
        public bool isItInSimulationMode { get; protected set; } = true;
        
        public Transform[] Colliders = new Transform[] { };
        
        /// <summary>
        /// Dissables the ability of the game object of feeling gravity fields, makes it kinematic and disables collision/detectors
        /// </summary>
        public void GoToNetworkedMode()
        {
            gameObject.GetAttachedOWRigidbody().MakeKinematic();

            foreach (var col in Colliders)
                col.collider.enabled = false;

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
            GetCollidersFromPaths(CollidersNames, transform);
        }

        public void GetCollidersFromPaths(string[] CollidersNames, Transform parentTransform)
        {
            List<Transform> colliders = new List<Transform>();

            foreach (string name in CollidersNames)
                colliders.Add(parentTransform.FindChild(name));

            Colliders = colliders.ToArray();
        }
    }
}
