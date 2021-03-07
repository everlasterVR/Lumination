using UnityEngine;
using UnityEngine.EventSystems;

namespace Lumination
{
    internal class DistanceSliderClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        //private Log log = new Log(nameof(DistanceSliderClickHandler));
        private LightControl lc;

        private bool isDown = false;

        public void Init(LightControl lc)
        {
            this.lc = lc;
        }

        public void OnPointerDown(PointerEventData data)
        {
            //log.Message("OnPointerDown");
            JSONStorableFloat jsf = lc.distance;
            if(jsf == null || jsf.slider == null || !jsf.slider.interactable)
            {
                return;
            }

            if(!isDown)
            {
                //log.Message("Added listener");
                jsf.slider.onValueChanged.AddListener(lc.DistanceSliderListener);
            }
            isDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            //log.Message("OnPointerUp");
            JSONStorableFloat jsf = lc.distance;
            if(jsf == null || jsf.slider == null || !jsf.slider.interactable)
            {
                return;
            }

            if(isDown)
            {
                //log.Message("Removed listener");
                jsf.slider.onValueChanged.RemoveListener(lc.DistanceSliderListener);
            }
            isDown = false;
        }

        private void FixedUpdate()
        {
            if(!isDown)
            {
                lc.UpdateDistanceVal();
            }
        }
    }
}
