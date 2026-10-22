using System.Collections; using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        public Color flashColor = new Color(1f, 0.3f, 0.3f); public float duration = 0.1f;
        SpriteRenderer[] rends; Color[] orig;
        void Awake()
        {
            rends = GetComponentsInChildren<SpriteRenderer>(); orig = new Color[rends.Length];
            for (int i = 0; i < rends.Length; i++) orig[i] = rends[i].color;
            GetComponent<Health>().Damaged += () => StartCoroutine(Flash());
        }
        IEnumerator Flash()
        {
            foreach (var r in rends) if (r) r.color = flashColor;
            yield return new WaitForSeconds(duration);
            for (int i = 0; i < rends.Length; i++) if (rends[i]) rends[i].color = orig[i];
        }
    }
}