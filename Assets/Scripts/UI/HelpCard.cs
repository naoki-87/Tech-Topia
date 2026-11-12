using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    public class HelpCard : MonoBehaviour
    {
        public Image icon; public TMP_Text title, desc;
        public void Set(Sprite s, string t, string d)
        {
            if (icon) { icon.sprite = s; icon.enabled = s != null; icon.preserveAspect = true; }
            title.text = t; desc.text = d;
        }
    }
}