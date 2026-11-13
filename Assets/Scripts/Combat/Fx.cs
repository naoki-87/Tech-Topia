using UnityEngine;
namespace SteamEngineer
{
    // ÉGÉtÉFÉNÉg1å¬ï™: ägëÂÇµÇ»Ç™ÇÁìÆÇ´ÅAç≈å„Ç…îñÇÍÇƒè¡Ç¶ÇÈ
    public class FxSprite : MonoBehaviour
    {
        public float life = 0.3f, growFrac = 0.35f, fadeFrac = 0.5f, gravity;
        public Vector3 s0, s1; public Vector2 vel; public Color color;
        SpriteRenderer sr; float t;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime; float u = t / life;
            if (u >= 1f) { Destroy(gameObject); return; }
            float g = Mathf.Clamp01(u / Mathf.Max(0.01f, growFrac)); g = 1f - (1f - g) * (1f - g);
            transform.localScale = Vector3.Lerp(s0, s1, g);
            vel.y += gravity * Time.deltaTime; transform.position += (Vector3)(vel * Time.deltaTime);
            var c = color; c.a *= u < fadeFrac ? 1f : 1f - (u - fadeFrac) / (1f - fadeFrac); sr.color = c;
        }
    }

    public static class Fx
    {
        static Sprite white, circle, rect, arc;
        static readonly Color Clear = new Color(1, 1, 1, 0);

        static Sprite Make(int w, int h, System.Func<int, int, Color> f, float ppu, Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu);
        }
        static void Ensure()
        {
            if (white != null) return;
            white = Make(1, 1, (x, y) => Color.white, 1f, new Vector2(0.5f, 0.5f));
            circle = Make(128, 128, (x, y) =>                       // âèéÊÇËÇ∆îñÇ¢ìhÇËÇÃâ~(íºåa1)
            {
                float dx = (x + 0.5f - 64f) / 64f, dy = (y + 0.5f - 64f) / 64f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 1f) return Clear; if (d > 1f - 4f / 64f) return Color.white; return new Color(1, 1, 1, 0.16f);
            }, 128f, new Vector2(0.5f, 0.5f));
            rect = Make(32, 32, (x, y) =>                           // âèéÊÇËÇ∆îñÇ¢ìhÇËÇÃéläp(1x1)
                (x == 0 || y == 0 || x == 31 || y == 31) ? Color.white : new Color(1, 1, 1, 0.16f), 32f, new Vector2(0.5f, 0.5f));
            arc = Make(128, 128, (x, y) =>                          // éOì˙åéÇÃéaåÇ(ç∂í[íÜâõÇ™å¥ì_ÅAâEÇ÷1)
            {
                float nx = (x + 0.5f) / 128f, ny = (y + 0.5f - 64f) / 64f, r = Mathf.Sqrt(nx * nx + ny * ny), th = Mathf.Atan2(ny, nx);
                const float lim = 1.15f;
                if (Mathf.Abs(th) > lim || r > 1f) return Clear;
                float thick = 0.10f + 0.22f * Mathf.Cos(th / lim * Mathf.PI * 0.5f);
                if (r < 1f - thick) return Clear;
                return new Color(1, 1, 1, Mathf.Lerp(1f, 0.45f, (1f - r) / thick));
            }, 128f, new Vector2(0f, 0.5f));
        }

        static FxSprite Spawn(Sprite s, Vector2 pos, Vector3 s0, Vector3 s1, Color c, float life, int order, float grow = 0.35f, float fade = 0.5f)
        {
            var go = new GameObject("Fx"); go.transform.position = pos; go.transform.localScale = s0;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order; sr.color = c;
            var f = go.AddComponent<FxSprite>(); f.life = life; f.s0 = s0; f.s1 = s1; f.color = c; f.growFrac = grow; f.fadeFrac = fade;
            return f;
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Steam: return new Color(0.85f, 0.92f, 1f);
                case Element.Wind: return new Color(0.55f, 1f, 0.8f);
                case Element.Plant: return new Color(0.5f, 0.9f, 0.4f);
                case Element.Water: return new Color(0.3f, 0.7f, 1f);
                case Element.Fire: return new Color(1f, 0.55f, 0.2f);
                case Element.Spirit: return new Color(0.7f, 0.8f, 1f);
                case Element.Sand: return new Color(0.9f, 0.75f, 0.4f);
                case Element.Ice: return new Color(0.6f, 0.9f, 1f);
                default: return new Color(1f, 0.95f, 0.8f);
            }
        }

        // í èÌçUåÇ: ìñÇΩÇËîªíËÇÃéläp + éOì˙åéÇÃéaåÇ
        public static void Slash(Vector2 boxCenter, Vector2 boxSize, int facing)
        {
            Ensure();
            Box(boxCenter, boxSize, new Color(1f, 0.95f, 0.8f, 0.9f), 0.18f);
            var origin = new Vector2(boxCenter.x - facing * boxSize.x * 0.5f, boxCenter.y);
            Spawn(arc, origin, new Vector3(facing * boxSize.x * 0.55f, boxSize.y * 0.9f, 1f), new Vector3(facing * boxSize.x, boxSize.y * 1.1f, 1f),
                  new Color(1f, 0.98f, 0.9f, 1f), 0.2f, 6, 0.4f, 0.4f);
        }
        public static void Box(Vector2 center, Vector2 size, Color c, float life) { Ensure(); Spawn(rect, center, size, size, c, life, 4, 0.01f, 0.4f); }
        // â~å`ÇÃîÕàÕ(îºåa=é¿ç€ÇÃîªíËÇÃîºåa)
        public static void Ring(Vector2 center, float radius, Color c, float life = 0.45f)
        {
            Ensure(); float d = radius * 2f; Spawn(circle, center, Vector3.one * d * 0.55f, Vector3.one * d, c, life, 4, 0.3f, 0.45f);
        }
        public static void Puffs(Vector2 pos, Color c, int n, float speed, Vector2 dir, float size = 0.25f, float life = 0.5f)
        {
            Ensure(); if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            for (int i = 0; i < n; i++)
            {
                var v = (dir.normalized * 0.6f + Random.insideUnitCircle * 0.8f).normalized * speed * Random.Range(0.5f, 1f);
                float s = size * Random.Range(0.6f, 1.2f);
                var f = Spawn(circle, pos + Random.insideUnitCircle * 0.15f, Vector3.one * s * 0.6f, Vector3.one * s * 1.6f, c, life * Random.Range(0.7f, 1.2f), 5, 1f, 0.2f);
                f.vel = v;
            }
        }
        public static void Hit(Vector2 pos)
        {
            Ensure();
            for (int i = 0; i < 6; i++)
            {
                var f = Spawn(white, pos, Vector3.one * 0.14f, Vector3.one * 0.04f, new Color(1f, 0.9f, 0.4f), 0.22f, 7, 1f, 0.3f);
                f.vel = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            }
        }
        public static void WindStreaks(Vector2 center, int facing, float width, float height, Color c)
        {
            Ensure();
            for (int i = 0; i < 7; i++)
            {
                var p = center + new Vector2(Random.Range(-width * 0.5f, width * 0.5f), Random.Range(-height * 0.5f, height * 0.5f));
                float len = Random.Range(0.6f, 1.2f);
                var f = Spawn(white, p, new Vector3(len * 0.3f, 0.05f, 1f), new Vector3(len, 0.03f, 1f), c, 0.4f, 5, 0.5f, 0.3f);
                f.vel = new Vector2(facing * Random.Range(7f, 11f), 0f);
            }
        }
    }
}