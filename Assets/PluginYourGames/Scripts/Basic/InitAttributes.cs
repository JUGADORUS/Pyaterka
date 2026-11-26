using System;
using UnityEngine.Scripting;

namespace YG
{
    public static partial class YG2
    {
        [AttributeUsage(AttributeTargets.Method)]
        // These callbacks are invoked through CallAction reflection, including in IL2CPP players.
        // A PreserveAttribute subclass keeps annotated methods in stripped release builds.
        private class InitYG_0Attribute : PreserveAttribute { }

        [AttributeUsage(AttributeTargets.Method)]
        private class InitYG_1Attribute : PreserveAttribute { }

        [AttributeUsage(AttributeTargets.Method)]
        private class InitYG_2Attribute : PreserveAttribute { }

        [AttributeUsage(AttributeTargets.Method)]
        private class InitYGAttribute : PreserveAttribute { }

        [AttributeUsage(AttributeTargets.Method)]
        private class StartYGAttribute : PreserveAttribute { }
    }
}
