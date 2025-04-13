using BepInEx;
using BepInEx.Logging;
using MoreHeadUtilities;

namespace HeadPlugin
{
    [BepInPlugin("com.maygik.moreheadutilities", "MoreHeadUtilities", "1.0.0")]
    public class MoreHeadUtilities : BaseUnityPlugin
    {
        void Awake()
        {


            PartShrinker.Init(Logger);
            Logger.LogInfo("MoreHeadUtilities initialized and logger passed to PartShrinker.");
            HiddenParts.Init(Logger);
            Logger.LogInfo("MoreHeadUtilities initialized and logger passed to HiddenParts.");


        }
    }
}
