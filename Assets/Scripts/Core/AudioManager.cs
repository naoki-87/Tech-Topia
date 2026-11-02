using System.Collections; using UnityEngine; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // 効果音とBGM。Title の GameManager と同じオブジェクトに追加する
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;
        public AudioClip jump, attack, dash, hurt, kill, ability, repair, clear, gameOver;
        public AudioClip bgmTitle, bgmStage, bgmBoss;
        [Range(0, 1)] public float sfxVolume = 0.7f, bgmVolume = 0.4f;
        [Header("BGMの切り替え")]
        public float bgmStartDelay = 0.8f;     // シーン読み込み後、次のBGMが始まるまでの待ち時間
        public float fadeOutTime = 0.4f, fadeInTime = 1.0f;
        [Range(0, 1)] public float duckMul = 0.2f;   // ジングル(クリア音など)の間のBGM音量の倍率

        AudioSource sfx, bgm;
        float fade = 1f, duck = 1f, jingleEnd;
        Coroutine switching;

        void Awake()
        {
            if (I != null) { Destroy(this); return; }
            I = this;
            sfx = gameObject.AddComponent<AudioSource>();
            bgm = gameObject.AddComponent<AudioSource>(); bgm.loop = true;
            SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);     // 最初のシーンのBGMも鳴らす
        }
        void OnDestroy() { if (I == this) SceneManager.sceneLoaded -= OnLoaded; }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            AudioClip c = bgmTitle;
            if (s.name == "Stage")
            {
                var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
                c = (run != null && run.IsBossStage && bgmBoss != null) ? bgmBoss : bgmStage;
            }
            RequestBgm(c);
        }

        void RequestBgm(AudioClip c)
        {
            if (bgm.clip == c && bgm.isPlaying) return;              // 同じ曲は鳴らし直さない
            if (switching != null) StopCoroutine(switching);
            switching = StartCoroutine(SwitchBgm(c));
        }

        IEnumerator SwitchBgm(AudioClip c)
        {
            float start = fade;                                       // フェードアウト
            for (float t = 0f; t < fadeOutTime && bgm.isPlaying; t += Time.unscaledDeltaTime)
            { fade = Mathf.Lerp(start, 0f, t / fadeOutTime); yield return null; }
            bgm.Stop(); fade = 0f;

            float until = Mathf.Max(Time.unscaledTime + bgmStartDelay, jingleEnd);   // ジングルが終わるまで待つ
            while (Time.unscaledTime < until) yield return null;

            if (c == null) { switching = null; yield break; }
            bgm.clip = c; bgm.Play();                                 // フェードイン
            for (float t = 0f; t < fadeInTime; t += Time.unscaledDeltaTime)
            { fade = Mathf.Clamp01(t / fadeInTime); yield return null; }
            fade = 1f; switching = null;
        }

        void Update()
        {
            float target = Time.unscaledTime < jingleEnd ? duckMul : 1f;      // ジングル中はBGMを下げる
            duck = Mathf.MoveTowards(duck, target, 4f * Time.unscaledDeltaTime);
            bgm.volume = bgmVolume * fade * duck;
        }

        // ---- 効果音 ----
        public static void Play(AudioClip c) { if (I != null && c != null) I.sfx.PlayOneShot(c, I.sfxVolume); }
        // ジングル: BGMを一時的に下げ、次のBGMはこの音が終わるまで始まらない
        public static void Jingle(AudioClip c)
        {
            if (I == null || c == null) return;
            I.sfx.PlayOneShot(c, I.sfxVolume);
            I.jingleEnd = Mathf.Max(I.jingleEnd, Time.unscaledTime + c.length);
        }

        public static void Jump()     { Play(I != null ? I.jump : null); }
        public static void Attack()   { Play(I != null ? I.attack : null); }
        public static void Dash()     { Play(I != null ? I.dash : null); }
        public static void Hurt()     { Play(I != null ? I.hurt : null); }
        public static void Kill()     { Play(I != null ? I.kill : null); }
        public static void Ability()  { Play(I != null ? I.ability : null); }
        public static void Repair()   { Play(I != null ? I.repair : null); }
        public static void Clear()    { Jingle(I != null ? I.clear : null); }
        public static void GameOver() { Jingle(I != null ? I.gameOver : null); }
    }
}