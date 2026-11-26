using System.Collections.Generic; using UnityEngine;
namespace SteamEngineer
{
    // エリアの条件(リスクと報酬): エリア選択のカードに表示され、選んだエリアの間だけ有効
    public class AreaMod
    {
        public string name, desc, reward = "";       // reward: "" / "heal" / "common" / "relic"
        public float enemyHp = 1f, matMul = 1f;      // 敵のHPの倍率 / 素材の獲得量の倍率
        public int enemyDmg;                         // 敵の接触ダメージの加算
    }

    // 出発ボーナス: 周回の最初に、3つから1つ選ぶ
    public class StartBonus { public string name, desc; public System.Action<RunState> apply; }

    // 周回の間だけ使う、追加の状態(スタートのたびに Reset される)
    public static class RunExtras
    {
        public static bool startBonusPending;
        public static List<StartBonus> bonusOffers = new List<StartBonus>();
        public static AreaMod currentMod;
        public static readonly Dictionary<AreaData, AreaMod> mods = new Dictionary<AreaData, AreaMod>();
        static int extraRelic;

        // ★ エリアの条件を増やすときは、ここに足す
        static readonly AreaMod[] modPool =
        {
            new AreaMod { name = "標準", desc = "特になし" },
            new AreaMod { name = "平穏な道", desc = "敵のHP -20% / 素材 -25%", enemyHp = 0.8f, matMul = 0.75f },
            new AreaMod { name = "精鋭の群れ", desc = "敵のHP +30% / 素材 +50%", enemyHp = 1.3f, matMul = 1.5f },
            new AreaMod { name = "危険地帯", desc = "敵のダメージ +1 / クリアで遺物の候補が1つ増える", enemyDmg = 1, reward = "relic" },
            new AreaMod { name = "古い野営地", desc = "敵のHP +10% / クリアでHP全回復", enemyHp = 1.1f, reward = "heal" },
            new AreaMod { name = "豊かな鉱脈", desc = "敵のHP +15% / 素材 +25% / クリアで汎用素材 +10", enemyHp = 1.15f, matMul = 1.25f, reward = "common" },
        };

        public static void Reset() { startBonusPending = true; bonusOffers.Clear(); currentMod = null; mods.Clear(); extraRelic = 0; }

        // ---------- エリアの条件 ----------
        public static void Assign(IList<AreaData> list)     // 候補のカードに、重ならないように条件を決める
        {
            var used = new HashSet<AreaMod>();
            foreach (var a in list) { AreaMod m0; if (mods.TryGetValue(a, out m0)) used.Add(m0); }
            foreach (var a in list)
            {
                if (mods.ContainsKey(a)) continue;
                var cand = new List<AreaMod>(); foreach (var m in modPool) if (!used.Contains(m)) cand.Add(m);
                if (cand.Count == 0) cand.AddRange(modPool);
                var pick = cand[Random.Range(0, cand.Count)]; mods[a] = pick; used.Add(pick);
            }
        }
        public static AreaMod ModFor(AreaData a)
        {
            AreaMod m; if (!mods.TryGetValue(a, out m)) { m = modPool[Random.Range(0, modPool.Length)]; mods[a] = m; }
            return m;
        }
        public static void SelectMod(AreaData a) { currentMod = ModFor(a); }
        public static float EnemyHpMod { get { return currentMod != null ? currentMod.enemyHp : 1f; } }
        public static int EnemyDmgMod { get { return currentMod != null ? currentMod.enemyDmg : 0; } }
        public static float MatMod { get { return currentMod != null ? currentMod.matMul : 1f; } }
        public static int RelicChoices() { int n = 3 + extraRelic; extraRelic = 0; return n; }
        public static void OnAreaClear(RunState run, SaveData save)
        {
            if (currentMod != null)
            {
                if (currentMod.reward == "heal") run.hp = PlayerStats.Build(run).maxHp;
                else if (currentMod.reward == "common") save.commonMaterials += 10;
                else if (currentMod.reward == "relic") extraRelic = 1;
            }
            currentMod = null;
        }

        // ---------- 出発ボーナス ----------
        // ★ 出発ボーナスを増やすときは、ここに足す
        static List<StartBonus> Pool()
        {
            return new List<StartBonus>
            {
                new StartBonus { name = "備えの一品", desc = "遺物をランダムに1つ得る", apply = r =>
                    { var gm = GameManager.Instance; r.OfferRelics(gm.relicPool, 1); if (r.relicOffers.Count > 0) r.TakeRelic(r.relicOffers[0]); r.relicOffers.Clear(); } },
                new StartBonus { name = "頑丈な体", desc = "最大HP +2(この周回)", apply = r => { r.itemMaxHp += 2; r.hp += 2; } },
                new StartBonus { name = "鋭い刃", desc = "攻撃力 +1(この周回)", apply = r => { r.itemAttack += 1; } },
                new StartBonus { name = "工具箱", desc = "応急修理キットを2個もらう", apply = r =>
                    { var it = ItemManager.Find("Item_RepairKit"); if (it != null) r.itemBag[it] = r.ItemCount(it) + 2; else { r.itemMaxHp += 1; r.hp += 1; } } },
                new StartBonus { name = "羅針盤の写し", desc = "エリアのリロールが2回増える", apply = r => { r.rerolls += 2; } },
                new StartBonus { name = "危うい賭け", desc = "最大HP -1 / 攻撃力 +2(この周回)", apply = r =>
                    { r.itemMaxHp -= 1; r.itemAttack += 2; r.hp = Mathf.Max(1, Mathf.Min(r.hp, PlayerStats.Build(r).maxHp)); } },
                new StartBonus { name = "余裕のある財布", desc = "汎用素材 +30(装備一覧で使える)", apply = r => { GameManager.Instance.Save.commonMaterials += 30; } },
            };
        }
        public static void RollBonuses()
        {
            bonusOffers.Clear(); var all = Pool();
            for (int i = 0; i < 3 && all.Count > 0; i++) { int k = Random.Range(0, all.Count); bonusOffers.Add(all[k]); all.RemoveAt(k); }
        }
    }
}
