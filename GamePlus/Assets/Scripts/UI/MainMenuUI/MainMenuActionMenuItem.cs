using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.MainMenuUI
{
    /// Helper component on each Action Menu button row for instant PointerEnter (hover)
    /// and PointerClick response matching the UI behavior.
    public class MainMenuActionMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public int index;
        public MainMenuActionMenu menu;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (menu != null)
            {
                menu.OnButtonHover(index);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (menu != null)
            {
                menu.OnButtonClick(index);
            }
        }
    }
}
