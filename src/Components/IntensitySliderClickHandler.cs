using UnityEngine;
using UnityEngine.EventSystems;

namespace Lumination
{
    internal class IntensitySliderClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        //private Log log = new Log(nameof(RangeSliderClickHandler));
        private LightControl lc;

        public bool isDown = false;

        public void Init(LightControl lc)
        {
            this.lc = lc;
        }

        public void OnPointerDown(PointerEventData data)
        {
            isDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            //log.Message("OnPointerUp");
            lc.UpdateBaseIntensityFactor();
            isDown = false;
        }
    }
}
