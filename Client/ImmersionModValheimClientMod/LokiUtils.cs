using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImmersionModValheimClientMod
{
    public class LokiUtils
    {
        public static bool IsDown(KeyboardShortcut value)
        {
            if (value.MainKey == KeyCode.None)
                return false;

            if (Input.GetKeyDown(value.MainKey))
            {
                if (value.Modifiers != null)
                {
                    foreach (var mod in value.Modifiers)
                    {
                        if (!Input.GetKey(mod))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

    }
}
