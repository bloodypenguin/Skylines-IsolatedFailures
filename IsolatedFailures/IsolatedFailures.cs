using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework.UI;
using ICities;

namespace IsolatedFailures
{
    public class IsolatedFailures : IUserMod, ILoading
    {

        private static bool bootstrapped;

        public string Name
        {
            get
            {
                if (!bootstrapped)
                {
                    UIScrollbarFix.Init();

                    Bootstrap();
                    bootstrapped = true;
                }

                return "Isolated Failures";
            }
        }

        public string Description
        {
            get { return "Makes mods fail isolated"; }
        }

        private static void Bootstrap()
        {
            RedirectionHelper.RedirectCalls(
                typeof(LoadingWrapper).GetMethod("OnLoadingExtensionsCreated", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(IsolatedFailures).GetMethod("OnLoadingExtensionsCreated", BindingFlags.Instance | BindingFlags.NonPublic));

            if (FindType("ModTools") == null)
            {
                RedirectionHelper.RedirectCalls(
                    typeof (LoadingWrapper).GetMethod("OnLevelLoaded", BindingFlags.Instance | BindingFlags.Public),
                    typeof (IsolatedFailures).GetMethod("OnLevelLoaded", BindingFlags.Instance | BindingFlags.Public));
            }
            else
            {
                UnityEngine.Debug.LogWarning("IsolatedFailures#Bootstrap(): ModTools discovered");
            }

            RedirectionHelper.RedirectCalls(
                typeof(LoadingWrapper).GetMethod("OnLevelUnloading", BindingFlags.Instance | BindingFlags.Public),
                typeof(IsolatedFailures).GetMethod("OnLevelUnloading", BindingFlags.Instance | BindingFlags.Public));
            RedirectionHelper.RedirectCalls(
                typeof(LoadingWrapper).GetMethod("OnLoadingExtensionsReleased", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(IsolatedFailures).GetMethod("OnLoadingExtensionsReleased", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        private static Type FindType(string className)
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.Name == className
                    select type).FirstOrDefault();
        }

        private void OnLoadingExtensionsCreated()
        {
            ProcessLoadingExtensions(SimulationManager.UpdateMode.Undefined, "OnCreated", (e, m) =>
            {
                e.OnCreated(this);
                return true;
            });
        }

        public void OnLevelLoaded(SimulationManager.UpdateMode mode)
        {
            ProcessLoadingExtensions(mode, " OnLevelLoaded", (e, m) =>
            {
                e.OnLevelLoaded((LoadMode)m);
                return true;
            });
        }

        public void OnLevelUnloading()
        {
            ProcessLoadingExtensions(SimulationManager.UpdateMode.Undefined, "OnLevelUnloading", (e, m) =>
            {
                e.OnLevelUnloading();
                return true;
            });
        }

        private void OnLoadingExtensionsReleased()
        {
            ProcessLoadingExtensions(SimulationManager.UpdateMode.Undefined, "OnReleased", (e, m) =>
            {
                e.OnReleased();
                return true;
            });
        }


        private void ProcessLoadingExtensions(SimulationManager.UpdateMode mode, string methodName, Func<ILoadingExtension, SimulationManager.UpdateMode, bool> callback)
        {
            var loadingExtensions = (List<ILoadingExtension>)(typeof(LoadingWrapper).
                GetField("m_LoadingExtensions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this));
            var exceptions = new List<Exception>();
            foreach (var loadingExtension in loadingExtensions)
            {
                try
                {
                    callback(loadingExtension, mode);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count <= 0)
            {
                return;
            }
            var mergedTraces = "";
            var i = 0;
            exceptions.ForEach((e) =>
            {
                if (e == null)
                {
                    mergedTraces += String.Format("\n---------------------------\n[{0}]: <null exception>", i);
                }
                else
                {
                    mergedTraces += String.Format("\n---------------------------\n[{0}]: {1}\n{2}", i, e.Message, e.StackTrace);
                }
                ++i;
            });
            UIView.ForwardException(new ModException(String.Format("{0} - Some mods caused errors:", mode),
                new Exception(mergedTraces)));
        }

        public IManagers managers
        {
            get { throw new NotImplementedException(); }
        }

        public bool loadingComplete
        {
            get { throw new NotImplementedException(); }
        }

        public AppMode currentMode
        {
            get { throw new NotImplementedException(); }
        }

        public string currentTheme
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}
