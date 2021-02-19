using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Loki.Mods
{
    
    [BepInPlugin("com.loki.servermods.valheim.deathannouncer", "Death Announcer Server Mod", "1.0.0.0")]
    public class DeathAnnouncerValheimServerMod : BaseUnityPlugin {

        private static int _onDeathHash;
        private static Func<long, ZNetPeer> _getPeer;
        private static MethodInfo _getPeerInfo;
        
        void Awake(){
            _onDeathHash = "OnDeath".GetStableHashCode();
            _getPeerInfo = typeof(ZRoutedRpc).GetMethod("GetPeer",BindingFlags.NonPublic | BindingFlags.Instance);
            
            Harmony.CreateAndPatchAll(typeof(DeathAnnouncerValheimServerMod));
        }
        
        [HarmonyPatch(typeof(ZRoutedRpc), "RouteRPC")]
        [HarmonyPrefix]
        static void NotifyOtherPlayersOfDeath(ZRoutedRpc __instance, ZRoutedRpc.RoutedRPCData rpcData) {
            
            // Late bind to instance
            if (_getPeer == null) {
                _getPeer = (Func<long, ZNetPeer>) Delegate.CreateDelegate(typeof(Func<long, ZNetPeer>), __instance, _getPeerInfo);
            }

            if (!ZNet.instance.IsServer()) return;
            if (rpcData.m_methodHash != _onDeathHash) return;
            
            ZNetPeer peer = _getPeer(rpcData.m_senderPeerID);
            if (peer == null) return;
            
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, 
                "ShowMessage", 
                2, "Player " + peer.m_playerName + " has died.");

        }

    }
}