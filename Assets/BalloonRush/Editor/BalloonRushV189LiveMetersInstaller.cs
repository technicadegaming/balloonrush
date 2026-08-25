#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// v1.8.9 meter patch helper.
    ///
    /// No gameplay source files need modification. The live-meter runtime
    /// auto-installs on MainGame, so Apply is intentionally just a safe
    /// installation/verification checkpoint.
    /// </summary>
    public static class BalloonRushV189LiveMetersInstaller
    {
        private const string MeterPath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushLiveMetersV189.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.8.9 LIVE Combo + Payout Meters",
            priority = -42)]
        public static void Apply()
        {
            if (!File.Exists(MeterPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.8.9: live meter script is missing. " +
                    "Merge the patch Assets folder into the Unity project first.");
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.8.9 LIVE METERS INSTALLED\n\n" +
                "No gameplay source files were changed.\n" +
                "MainGame will automatically receive:\n" +
                "- combo progress toward the next milestone\n" +
                "- live combo timeout indicator\n" +
                "- live payout/ticket ladder progress\n" +
                "- reached-tier highlighting\n" +
                "- pulsing NEXT payout tier\n\n" +
                "Open MainGame and press Play.");
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.8.9 LIVE Combo + Payout Meters",
            priority = -41)]
        public static void Verify()
        {
            bool exists =
                File.Exists(MeterPath);

            if (exists)
            {
                Debug.Log(
                    "Balloon Rush v1.8.9 VERIFY PASS: " +
                    "live Combo + Payout meter runtime is installed.");
            }
            else
            {
                Debug.LogError(
                    "Balloon Rush v1.8.9 VERIFY FAILED: " +
                    MeterPath +
                    " is missing.");
            }
        }
    }
}
#endif
