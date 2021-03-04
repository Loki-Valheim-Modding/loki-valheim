using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static Loki.Mods.FirstPersonValheimClientMod;

namespace Loki.Mods.Fixes {
    
    /// <summary>
    /// Fixes objects getting culled down directly around the player by the shader.
    /// </summary>
    public static class FixRenderCullingAroundPlayer {
        
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        [HarmonyPostfix]
        public static void OnSpawnedPost(Player __instance)
        {
            if (__instance == Player.m_localPlayer) {
                INSTANCE.StartCoroutine(SmallSpawndelay());
            }
        }

        public static IEnumerator SmallSpawndelay() {
            
            yield return new WaitForSeconds(0.1f);
            
            _setVisible = null;
            _animator = Player.m_localPlayer.GetComponentInChildren<Animator>();
            CurrentFPMode = _statesAllowed.First();

            var allMats = Resources.FindObjectsOfTypeAll<Material>();

            // Mark everything that can be culled (by the shader) as not culleable.
            // TODO: How would this have impact on FPS?
            foreach (var mat in allMats)
            {
                if (mat.HasProperty("_CamCull"))
                {
                    mat.SetFloat("_CamCull", 0);
                }
            }
        }
        
    }
    
}