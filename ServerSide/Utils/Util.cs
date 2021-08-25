﻿using ServerSide.PacketCouriers.GameRelated.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ServerSide.Utils
{
    public static class Util
    {
        // Código de Cpp em C# 0_0
        public static long GerarHash(string s) //Gerar o Hash code de strings
        {
            const int p = 53;
            const int m = 1000000000 + 9; //10e9 + 9
            long hash_value = 0;
            long p_pow = 1;
            foreach (char c in s)
            {
                hash_value = (hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = p_pow * p % m;
            }
            return hash_value;
        }

        public static NetworkedEntity GetAttachedNetworkedEntity(this GameObject gameObject)
        {
            return gameObject.GetComponent<NetworkedEntity>();
        }
        public static NetworkedEntity GetAttachedNetworkedEntity(this Component component)
        {
            return component.GetComponent<NetworkedEntity>();
        }
    }
}
