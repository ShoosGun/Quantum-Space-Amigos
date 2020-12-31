using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DIMOWAModLoader;

namespace ServerSide.Shades
{
    public class ShadeMovementModel : CharacterMovementModel
    {
        private ClientDebuggerSide debugger;

        private List<MovementPacket> MovementPacketsCache;

        private bool[] hasBeenRead = new bool[3];

        //Ordem /\:
        // 1  = GetMoveInput
        // 2 = GetTurnInput
        // 3  = GetJumpInput

        private void Start()
        {
            debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();

            MovementPacketsCache = new List<MovementPacket>();
            _jumpSpeed = 6f;
            _turnRate = 160f;
        }

        private void LateUpdate()
        {
            if (hasBeenRead[0] && hasBeenRead[1] && hasBeenRead[2])
            {
                MovementPacketsCache.RemoveAt(0);
                hasBeenRead = new bool[] { false, false, false }; //Para ter certeza que foi tudo lido antes de mais nada
                debugger.SendLogMultiThread("Toda as ações foram lidas");
            }
        }

        protected override Vector3 GetMoveInput()
        {
            if (!hasBeenRead[0] && MovementPacketsCache.Count > 0)
            {
                hasBeenRead[0] = true;
                //debugger.SendLog($"Andei: {MovementPacketsCache[0].MoveInput}",DebugType.UNKNOWN);
                return MovementPacketsCache[0].MoveInput; // x = andar de lado z = andar para frente e para tras
            }

            return Vector3.zero;
        }

        protected override float GetTurnInput()
        {
            if (!_isGrounded)
                hasBeenRead[2] = true;

            if (!hasBeenRead[1] && MovementPacketsCache.Count > 0)
            {
                hasBeenRead[1] = true;
                return MovementPacketsCache[0].TurnInput;
            }

            return 0f;
        }

        protected override bool GetJumpInput()
        {
            if (!hasBeenRead[2] && MovementPacketsCache.Count > 0)
            {
                hasBeenRead[2] = true;
                return MovementPacketsCache[0].JumpInput;
            }

            return false;
        }

        public void AddMovementPacket(MovementPacket packet)
        {
            if(MovementPacketsCache.Count == 10)
                MovementPacketsCache.RemoveAt(0); // Caso, CASO, acumulem

            MovementPacketsCache.Add(packet);
        }

    }
    public struct MovementPacket
    {
        public Vector3 MoveInput;
        public float TurnInput;
        public bool JumpInput;

        public MovementPacket(Vector3 moveInput, float turnInput, bool jumpInput)
        {
            MoveInput = moveInput;
            TurnInput = turnInput;
            JumpInput = jumpInput;
        }
    }
}
