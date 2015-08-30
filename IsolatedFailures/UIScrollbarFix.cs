using ColossalFramework;
using ColossalFramework.UI;

using UnityEngine;
using System.Reflection;

namespace IsolatedFailures
{
    /// <summary>
    /// The scrollbar increment button is wrongly disabled in some conditions
    /// This bug prevent the access to the end of the list if it contains a lot of items
    /// Here is a fix
    /// </summary>
    public class UIScrollbarFix: UIScrollbar
    {
        public static void Init()
        {
            MethodInfo from = typeof(UIScrollbar).GetMethod("AutoDisableButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo to = typeof(UIScrollbarFix).GetMethod("AutoDisableButtons", BindingFlags.NonPublic | BindingFlags.Instance);

            RedirectionHelper.RedirectCalls(from, to);
        }

        private void AutoDisableButtons()
        {
            if (!this.m_AutoDisableButtons || !Application.isPlaying)
            {
                return;
            }
            if (this.m_DecrementButton != null)
            {
                this.m_DecrementButton.isEnabled = (this.value.Quantize(this.stepSize) >= this.minValue && !this.value.NearlyEqual(this.minValue, 0.1f));
            }
            if (this.m_IncrementButton != null)
            {
                float num = Mathf.Max(this.maxValue - this.minValue, 0f);
                float num2 = Mathf.Max(num - this.scrollSize, 0f) + this.minValue;
                this.m_IncrementButton.isEnabled = (this.value.Quantize(this.stepSize) <= num2 && !this.value.NearlyEqual(num2, 0.01f)); // 0.01f instead of 0.1f
            }
        }
    }
}
