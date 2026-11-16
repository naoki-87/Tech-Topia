using System; using System.Collections; using System.Collections.Generic; using System.Text;
using TMPro; using UnityEngine; using UnityEngine.EventSystems; using UnityEngine.InputSystem; using UnityEngine.InputSystem.UI; using UnityEngine.SceneManagement; using UnityEngine.UI;
namespace SteamEngineer
{
    // ゲーム内の出来事を、チュートリアルの課題に伝えるための通知
    public static class TutorialEvents
    {
        public static event Action<string> Fired;
        public static void Fire(string key) { Fired?.Invoke(key); }
    }

    // Tutorial シーンに1つ置く。レッスンごとの練習場をコードで作り、課題の達成を数える
    public class TutorialManager : MonoBehaviour
    {
        public static readonly (string group, string name, string intro)[] Lessons =
        {
            ("基本操作", "移動とジャンプ", "移動、ジャンプ、ダッシュを練習します。右の穴を越えてみましょう(ダッシュや二段ジャンプが役立ちます)。"),
            ("基本操作", "攻撃と敵", "J / Z でレンチを振って攻撃します。この練習場では、ダメージを受けません。"),
            ("ギミック", "壊れた機械", "壊れた機械をレンチ(J)で叩くと、修理できます。素材とHPが手に入ります。"),
            ("ギミック", "蒸気ボイラー", "ボイラーの近くで、L キーを押してスチーム・ジャンプ(レンチのアクション)を使うと、起動します。"),
            ("ギミック", "風と雲", "上昇気流に乗ると、高い所へ行けます。雲の足場は、乗ると少しして消えます。"),
            ("装備", "装備のアクション", "この練習では、風の武器を装備しています。L でウィンド・ブラスト。帆に風を当てると、素材が手に入ります。"),
        };

        public GameObject playerPrefab, walkerPrefab, repairPrefab, boilerPrefab, sailPrefab, airUpPrefab, cloudPrefab;
        public EquipmentData windWeapon;                       // 装備のレッスンで装備する武器(Air_Weapon)

        class Task { public string text, key; public int need = 1, count; public bool Done => count >= need; }
        readonly List<Task> tasks = new List<Task>(); readonly List<string> missing = new List<string>();
        int lesson; bool done; Vector3 spawn = new Vector3(-8f, 1f, 0f);
        PlayerController player; Energy energy; Sprite white;
        TMP_Text main, hint; Image energyFill; GameObject banner; Button nextBtn;

        Sprite White { get { if (white == null) { var t = new Texture2D(1, 1) { filterMode = FilterMode.Point }; t.SetPixel(0, 0, Color.white); t.Apply(); white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); } return white; } }
        void OnEnable() { TutorialEvents.Fired += OnEvent; }
        void OnDisable() { TutorialEvents.Fired -= OnEvent; }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) { SceneManager.LoadScene("Title"); return; }       // Tutorial シーンから直接再生したとき
            lesson = Mathf.Clamp(gm.tutorialLesson, 0, Lessons.Length - 1);
            gm.Run = new RunState(gm.allAreas, gm.starters, gm.Save, new int[RunState.Slots], 1, null, false);
            if (lesson == 5 && windWeapon != null) { gm.Run.equipped[(int)EquipSlot.Weapon] = windWeapon; gm.Run.tier[(int)EquipSlot.Weapon] = Tier.Crude; }
            gm.Run.hp = PlayerStats.Build(gm.Run).maxHp;
            SetupCamera(); EnsureEventSystem(); BuildLesson(); BuildUi();
            player = Instantiate(playerPrefab, spawn, Quaternion.identity).GetComponent<PlayerController>();
            energy = player.GetComponent<Energy>(); StartCoroutine(MakeSafe());
            Refresh();
        }
        IEnumerator MakeSafe() { yield return null; if (player != null) player.Health.damageModifier = d => 0; }   // 練習場ではダメージを受けない

        void SetupCamera()
        {
            var cam = Camera.main; if (cam == null) { var g = new GameObject("Main Camera", typeof(Camera)); g.tag = "MainCamera"; cam = g.GetComponent<Camera>(); }
            cam.orthographic = true; cam.orthographicSize = 5.625f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(0x1E, 0x2A, 0x38, 255);
            cam.transform.position = new Vector3(spawn.x, 2f, -10f);
            if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();
        }
        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ---------- 練習場の部品 ----------
        void Block(float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("Block"); go.layer = LayerMask.NameToLayer("Ground");
            go.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, 0f); go.transform.localScale = new Vector3(x1 - x0, y1 - y0, 1f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White; sr.color = new Color32(0x5E, 0x40, 0x2A, 255); sr.sortingOrder = 0; go.AddComponent<BoxCollider2D>();
            var top = new GameObject("Top"); top.transform.position = new Vector3((x0 + x1) / 2f, y1 - 0.08f, 0f); top.transform.localScale = new Vector3(x1 - x0, 0.16f, 1f);
            var ts = top.AddComponent<SpriteRenderer>(); ts.sprite = White; ts.color = new Color32(0xD6, 0xAA, 0x50, 255); ts.sortingOrder = 1;
        }
        GameObject Put(GameObject prefab, string name, float x, float y)
        {
            if (prefab == null) { if (!missing.Contains(name)) missing.Add(name); return null; }
            return Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
        }
        void Walls(float x0, float x1) { Block(x0 - 1f, -4f, x0, 14f); Block(x1, -4f, x1 + 1f, 14f); }
        void AddTask(string text, string key, int need = 1) { tasks.Add(new Task { text = text, key = key, need = need }); }

        void BuildLesson()
        {
            switch (lesson)
            {
                case 0:
                    Block(-12f, -4f, 14f, 0f); Block(21f, -4f, 40f, 0f); Walls(-12f, 40f);
                    AddTask("← → / A D で移動する", "move"); AddTask("Space / W / ↑ でジャンプする", "jump");
                    AddTask("空中でもう一度ジャンプする(エネルギーを消費)", "airjump"); AddTask("Shift / K でダッシュする(エネルギーを消費・一瞬無敵)", "dash");
                    AddTask("右の穴を越える", "cross"); break;
                case 1:
                    Block(-12f, -4f, 30f, 0f); Walls(-12f, 30f);
                    Put(walkerPrefab, "Enemy_Walker", 10f, 0.5f); Put(walkerPrefab, "Enemy_Walker", 18f, 0.5f);
                    AddTask("J / Z で攻撃する", "attack"); AddTask("敵を倒す", "kill", 2); break;
                case 2:
                    Block(-12f, -4f, 20f, 0f); Walls(-12f, 20f);
                    Put(repairPrefab, "Machine_Repair", 5f, 0.5f); Put(repairPrefab, "Machine_Repair", 11f, 0.5f);
                    AddTask("壊れた機械を、レンチ(J)で修理する", "repair", 2); break;
                case 3:
                    Block(-12f, -4f, 20f, 0f); Walls(-12f, 20f);
                    Put(boilerPrefab, "Gimmick_SteamBoiler", 4f, 0.5f);
                    AddTask("L でスチーム・ジャンプを使う", "ability"); AddTask("蒸気ボイラーを起動する(近くで L)", "gimmick:Steam"); break;
                case 4:
                    Block(-12f, -4f, 6f, 0f); Block(9.5f, 8f, 18f, 9f); Walls(-12f, 18f);
                    Put(airUpPrefab, "AirCurrent_Up", 8f, 4.5f); Put(cloudPrefab, "Cloud_Platform", -3f, 1.3f); Put(cloudPrefab, "Cloud_Platform", 0.5f, 2.8f);
                    AddTask("上昇気流に入る", "air"); AddTask("雲の足場に乗る", "cloud"); break;
                default:
                    Block(-12f, -4f, 20f, 0f); Walls(-12f, 20f);
                    Put(sailPrefab, "Gimmick_Sail", 6f, 1.25f); Put(walkerPrefab, "Enemy_Walker", 14f, 0.5f);
                    if (windWeapon == null) missing.Add("Wind Weapon(装備)");
                    AddTask("L でウィンド・ブラストを使う", "ability"); AddTask("帆に風を当てて、張る", "gimmick:Wind"); break;
            }
        }

        // ---------- UI ----------
        void BuildUi()
        {
            var go = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
            var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920f, 1080f); sc.matchWidthOrHeight = 0.5f;
            main = UiKit.Txt(go.transform, "", 32, UiKit.Cream, TextAlignmentOptions.TopLeft, "Main");
            UiKit.Place(main.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -40f), new Vector2(1300f, 620f));
            hint = UiKit.Txt(go.transform, "Esc: 終了してタイトルへ     R: やり直し", 32, UiKit.Cream, TextAlignmentOptions.BottomLeft, "Hint");
            UiKit.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 30f), new Vector2(1300f, 50f));

            var bg = new GameObject("EnergyBack", typeof(RectTransform), typeof(Image)); bg.transform.SetParent(go.transform, false);
            bg.GetComponent<Image>().color = new Color32(0x3A, 0x2A, 0x1C, 255);
            UiKit.Place((RectTransform)bg.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -50f), new Vector2(420f, 30f));
            var fill = new GameObject("EnergyFill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(bg.transform, false);
            energyFill = fill.GetComponent<Image>(); energyFill.sprite = White; energyFill.type = Image.Type.Filled; energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.color = new Color32(0x4F, 0xC3, 0xF7, 255); UiKit.Stretch((RectTransform)fill.transform, 3f, 3f);
            var el = UiKit.Txt(go.transform, "エネルギー", 32, UiKit.Cream, TextAlignmentOptions.Right, "EnergyLabel");
            UiKit.Place(el.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -90f), new Vector2(420f, 44f));

            banner = new GameObject("Banner", typeof(RectTransform), typeof(Image)); banner.transform.SetParent(go.transform, false);
            banner.GetComponent<Image>().color = new Color(0.1f, 0.07f, 0.04f, 0.94f);
            UiKit.Place((RectTransform)banner.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 420f));
            var bt = UiKit.Txt(banner.transform, "レッスンクリア！", 64, UiKit.BrassHi, TextAlignmentOptions.Center, "T");
            UiKit.Place(bt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 90f));
            nextBtn = Btn(banner.transform, "次のレッスン", -330f, () => Next());
            Btn(banner.transform, "もう一度", 0f, () => Retry()); Btn(banner.transform, "タイトルへ", 330f, () => GameManager.Instance.BackToTitle());
            nextBtn.gameObject.SetActive(lesson + 1 < Lessons.Length); banner.SetActive(false);
        }
        Button Btn(Transform parent, string label, float x, UnityEngine.Events.UnityAction act)
        {
            var b = UiKit.Btn(parent, label, 32, UiKit.Brass, UiKit.Dark);
            UiKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 60f), new Vector2(300f, 110f)); b.onClick.AddListener(act); return b;
        }

        void Refresh()
        {
            var (group, name, intro) = Lessons[lesson]; var sb = new StringBuilder();
            sb.AppendLine($"<size=48><color=#D9A520>{group} / {name}</color></size>");
            sb.AppendLine(intro); sb.AppendLine();
            foreach (var t in tasks)
            {
                string c = t.need > 1 ? $"  ({Mathf.Min(t.count, t.need)}/{t.need})" : "";
                sb.AppendLine(t.Done ? $"<color=#8AE08A>[済] {t.text}{c}</color>" : $"[  ] {t.text}{c}");
            }
            if (missing.Count > 0) sb.AppendLine($"\n<color=#FF8080>※ 設定が不足しています: {string.Join(", ", missing)}(TutorialManager の欄に割り当ててください)</color>");
            main.text = sb.ToString();
        }

        // ---------- 進行 ----------
        void OnEvent(string key)
        {
            if (done || player == null) return;
            if (key == "jump" && !player.Grounded) Count("airjump");
            Count(key);
        }
        void Count(string key)
        {
            bool changed = false;
            foreach (var t in tasks) if (t.key == key && !t.Done) { t.count++; changed = true; }
            if (!changed) return;
            Refresh();
            foreach (var t in tasks) if (!t.Done) return;
            Finish();
        }
        void Finish()
        {
            done = true; var sv = GameManager.Instance.Save;
            sv.AddStat("tutorial_" + lesson, 1); sv.AddStat("tutorials_done", 1); sv.Write();
            AudioManager.Clear(); banner.SetActive(true);
        }
        void Next() { GameManager.Instance.StartTutorial(lesson + 1); }
        void Retry() { GameManager.Instance.StartTutorial(lesson); }

        void Update()
        {
            if (player == null) return;
            if (energy != null && energyFill != null) energyFill.fillAmount = energy.Current / Mathf.Max(1f, energy.max);
            var p = player.transform.position;
            if (p.y < -12f) { player.transform.position = spawn; player.Body.linearVelocity = Vector2.zero; }       // 穴に落ちたら、開始位置へ
            if (Mathf.Abs(player.Body.linearVelocity.x) > 1.5f) OnEvent("move");
            if (lesson == 0 && p.x > 22f && player.Grounded) OnEvent("cross");
            var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) GameManager.Instance.BackToTitle();
            if (k.rKey.wasPressedThisFrame) Retry();
            if (done && k.enterKey.wasPressedThisFrame && lesson + 1 < Lessons.Length) Next();
        }
    }
}