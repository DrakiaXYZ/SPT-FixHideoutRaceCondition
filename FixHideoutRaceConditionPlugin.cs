using BepInEx;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;

namespace DrakiaXYZ.FixHideoutRaceCondition
{
    [BepInPlugin("xyz.drakia.fixhideoutracecondition", "DrakiaXYZ-FixHideoutRaceCondition", "1.0.0")]
    [BepInDependency("com.SPT.core", "4.0.0")]
    public class FixHideoutRaceConditionPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            new RemoveHideoutInitPatch().Enable();
            new AddHideoutInit().Enable();
        }
    }

    /**
     * Find and remove the original call to hideoutClass.Init(), so we can add it elsewhere
     */
    public class RemoveHideoutInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication.Struct400).GetMethod(nameof(TarkovApplication.Struct400.MoveNext));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instr)
        {
            var codes = new List<CodeInstruction>(instr);
            var codeOffset = codes.FindIndex(code => code.opcode == OpCodes.Callvirt && code.operand.ToString().Contains("Init("));
            if (codeOffset >= 0)
            {
                var startOffset = codeOffset - 4;
                for (int i = 0; i < 6; i++)
                {
                    codes[startOffset + i].opcode = OpCodes.Nop;
                }
            }
            else
            {
                Logger.LogError("Unable to find call to hideoutClass.Init(Session)");
            }

            for (int i = 0; i < codes.Count; i++)
            {
                Logger.LogWarning($"{codes[i]?.opcode}  {codes[i]?.operand?.ToString()}");
            }

            return codes;
        }
    }

    /**
     * We've removed the call to hideoutClass.Init() above, so we do it here instead, so we can await it
     */
    public class AddHideoutInit : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_28));
        }

        [PatchPostfix]
        public static async void PatchPostfix(Task __result, TarkovApplication __instance, HideoutClass ___hideoutClass)
        {
            await __result;

            await ___hideoutClass.Init(__instance.Session).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception);
                }
            });
        }
    }
}
