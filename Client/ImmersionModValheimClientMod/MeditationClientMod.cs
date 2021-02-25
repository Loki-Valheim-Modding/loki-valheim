using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImmersionModValheimClientMod
{

    [BepInPlugin("com.loki.clientmods.valheim.immersion.meditation", "Meditation mod", "1.0.0.0")]
    public class MeditationClientMod : BaseUnityPlugin
    {
        private static FieldInfo m_pinsField;
        private ConfigEntry<bool> _configEnabled;
        private static ConfigEntry<float> _distanceMultiplier;
        private static List<GameObject> _customHudThings = new List<GameObject>();
        private static bool firstTickRestingEtc;

        void Awake()
        {
            m_pinsField = AccessTools.Field(typeof(Minimap), "m_pins");

            _configEnabled = Config.Bind("Settings", "EnableMod", false, "Enables the mod if true");
            _distanceMultiplier = Config.Bind("Settings", "DistanceMultiplier", 1f, "The max distance multiplier of known boss points that your meditation radar can see. Your resting bonus timer (in seconds) is multiplied with this value to determine the max distance");

            if (_configEnabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(MeditationClientMod));
            }
        }

        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        [HarmonyPostfix]
        public static void PostFixedUpdate(Player __instance)
        {
            if (Player.m_localPlayer != __instance)
                return;

            if (!__instance.IsSitting())
            {
                firstTickRestingEtc = true;
                if (_customHudThings.Any())
                {
                    foreach (var x in _customHudThings)
                    {
                        GameObject.Destroy(x);
                    }
                    _customHudThings.Clear();
                }

                return;
            }

            var seman = __instance.GetSEMan();
            if (seman.HaveStatusEffect("Rested"))
            {
                if (firstTickRestingEtc)
                {
                    firstTickRestingEtc = false;
                    var playerPos = Player.m_localPlayer.transform.position;
                    var rested = seman.GetStatusEffect("Rested");
                    var duration = rested.GetRemaningTime();
                    var maxDistance = duration * _distanceMultiplier.Value;

                    var pins = (List<Minimap.PinData>)m_pinsField.GetValue(Minimap.instance);
                    foreach (var pin in pins.Where(x => x.m_type == Minimap.PinType.Boss))
                    {
                        var pos = pin.m_pos;

                        var distance = Vector3.Distance(pos, playerPos);
                        if (distance > maxDistance)
                            continue;

                        var screenPos = GameCamera.instance.GetComponent<Camera>().WorldToScreenPoint(pos);

                        var blok = new GameObject();
                        var normalizedDir = (pos - playerPos).normalized;
                        blok.transform.position = (normalizedDir) + playerPos + new Vector3(0, 1.5f, 0);
                        blok.transform.LookAt(blok.transform.position + normalizedDir);
                        blok.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                        var sr = blok.AddComponent<SpriteRenderer>();
                        sr.sprite = pin.m_icon;
                        _customHudThings.Add(blok);
                    }
                }
            }
        }
    }
}
