using UnityEngine;
using UnityEngine.EventSystems;

namespace Lumination
{
    public class PointerStatus : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public bool changed = false;
        public bool isDown = false;

        public void OnPointerDown(PointerEventData data)
        {
            changed = !isDown;
            isDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            changed = isDown;
            isDown = false;
        }
    }
}
