using UnityEngine;
using UnityEngine.EventSystems;

namespace Lumination
{
    internal class SpotAngleSliderClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        //private Log log = new Log(nameof(SpotAngleSliderClickHandler));
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
            lc.UpdateSpotBaseWidth();
            isDown = false;
        }
    }
}
