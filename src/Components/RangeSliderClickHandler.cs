using UnityEngine;
using UnityEngine.EventSystems;

namespace Lumination
{
    internal class RangeSliderClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
            //log.Message("OnPointerDown");
            isDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            //log.Message("OnPointerUp");
            //refresh rangeDiff based on current range when mouse is lifted from slider
            lc.UpdateRangeDiff();
            lc.UpdateBaseIntensityFactor();
            isDown = false;
        }
    }
}
