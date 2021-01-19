using System.Collections.Generic;
using UnityEngine;

namespace ServerSide.Shades
{
    [RequireComponent(typeof(ShadeMovementModel))]
    public class ShadePacketCourier : MonoBehaviour
    {
        private ShadeMovementModel shadeMovementModel;
       
        private List<MovementPacket> MovementPacketsCache = new List<MovementPacket>();

        void Start()
        {
            shadeMovementModel = gameObject.GetComponent<ShadeMovementModel>();
        }

        void FixedUpdate()
        {
           
            if (MovementPacketsCache.Count > 0)
            {
                shadeMovementModel.SetNewPacket(MovementPacketsCache[0]);
                MovementPacketsCache.RemoveAt(0);
            }
            else
                shadeMovementModel.SetNewPacket(new MovementPacket(Vector3.zero, 0f, false, System.DateTime.UtcNow));
        }

        public void AddMovementPacket(MovementPacket packet)
        {
            
                if (MovementPacketsCache.Count == 10)
                    MovementPacketsCache.RemoveAt(0); // Caso, CASO, acumulem

                MovementPacketsCache.Add(packet);
        }

    }

    public enum MovementTypes : int
    {
        MOVE_INPUT,
        TURN_INPUT,
        JUMP_INPUT,
        UNKOWN,
    }
}
