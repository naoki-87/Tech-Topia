using System.Collections.Generic; using UnityEngine;
namespace SteamEngineer
{
    // 一般設定(PlayerPrefs に保存。F9のセーブ初期化では消えない)
    public static class Settings
    {
        const string KBgm = "set_bgm", KSe = "set_se", KFull = "set_full", KW = "set_w", KH = "set_h";
        static bool loaded; static float bgm = 1f, se = 1f; static bool full = true; static int w = 1280, h = 720;

        static void Load()
        {
            if (loaded) return; loaded = true;
            bgm = PlayerPrefs.GetFloat(KBgm, 1f); se = PlayerPrefs.GetFloat(KSe, 1f);
            full = PlayerPrefs.GetInt(KFull, 1) == 1; w = PlayerPrefs.GetInt(KW, 1280); h = PlayerPrefs.GetInt(KH, 720);
        }
        public static float Bgm { get { Load(); return bgm; } set { Load(); bgm = Mathf.Clamp01(Mathf.Round(value * 10f) / 10f); PlayerPrefs.SetFloat(KBgm, bgm); PlayerPrefs.Save(); } }
        public static float Se { get { Load(); return se; } set { Load(); se = Mathf.Clamp01(Mathf.Round(value * 10f) / 10f); PlayerPrefs.SetFloat(KSe, se); PlayerPrefs.Save(); } }
        public static bool Fullscreen { get { Load(); return full; } set { Load(); full = value; PlayerPrefs.SetInt(KFull, value ? 1 : 0); PlayerPrefs.Save(); } }
        public static Vector2Int WindowSize { get { Load(); return new Vector2Int(w, h); } set { Load(); w = value.x; h = value.y; PlayerPrefs.SetInt(KW, w); PlayerPrefs.SetInt(KH, h); PlayerPrefs.Save(); } }

        public static void Apply()
        {
            Load();
            if (full) Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(w, h, FullScreenMode.Windowed);
        }
        public static void ResetAll() { Bgm = 1f; Se = 1f; Fullscreen = true; WindowSize = new Vector2Int(1280, 720); Apply(); }

        public static List<Vector2Int> Resolutions()
        {
            var l = new List<Vector2Int>();
            foreach (var r in Screen.resolutions) { var v = new Vector2Int(r.width, r.height); if (v.x >= 960 && !l.Contains(v)) l.Add(v); }
            if (l.Count == 0) { l.Add(new Vector2Int(1280, 720)); l.Add(new Vector2Int(1920, 1080)); }
            l.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y)); return l;
        }
        public static int ResIndex(List<Vector2Int> l)
        {
            var cur = WindowSize; int best = 0, bd = int.MaxValue;
            for (int i = 0; i < l.Count; i++) { int d = Mathf.Abs(l[i].x - cur.x) + Mathf.Abs(l[i].y - cur.y); if (d < bd) { bd = d; best = i; } }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if !UNITY_EDITOR
            if (PlayerPrefs.HasKey(KFull)) Apply();     // 変更したことがあるときだけ、起動時に反映する
#endif
        }
    }
}