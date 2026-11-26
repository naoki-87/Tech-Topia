using UnityEngine; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // Title シーンに1つ置く。DontDestroyOnLoad。
    // Scenes: Title / AreaSelect / Stage / Garage / Result
    public enum RunLength { Exhibition, Standard }     // 展示用 / 通常用
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public AreaData[] allAreas;
        public EquipmentData[] starters;     // Head, Body, Legs, Weapon の順
        public RelicData[] relicPool;        // 遺物の候補(Tools > TechTopia > 遺物アセットを作成 で自動設定)
        public ItemData[] itemPool;          // 補助アイテムの一覧(Tools > TechTopia > 補助アイテムを作成 で自動設定)
        [Header("1周の長さ")]
        public int visitsPerArea = 2; public RunLength runLength = RunLength.Exhibition; public int exhibitionStages = 3, standardStages = 5;        // 各エリアを何回使うか(2エリア × 2 = 4エリア)
        public bool keepTierOnCarryOver = false;   // 次の周回で、解放済みの装備を、解放したティアのまま使うか
        [Header("敵の強さ")]
        public bool scaleEnemies = true;
        public float enemyHpPerPower = 0.05f, enemyHpCap = 2.5f, enemyHpPerArea = 0.12f;
        public int enemyDmgStep = 8, enemyDmgMax = 2;
        [Header("素材の分解")]
        public int disassembleBase = 2;      // 分解で得る汎用素材の基準値(×(ティア+1))
        [Header("装備一覧(ティア上昇のハードル)")]
        public int tierUpCoreMul = 2, tierUpCommonMul = 2;
        public SaveData Save; public RunState Run;
        public int tutorialLesson;
        public void StartTutorial(int lesson) { tutorialLesson = lesson; Load("Tutorial"); }

        public float EnemyHpMul
        {
            get
            {
                if (Run == null) return 1f;
                float m = 1f;
                if (scaleEnemies)
                {
                    m = Mathf.Min(enemyHpCap, 1f + enemyHpPerPower * Run.EquipPower());   // 装備の強化度合い
                    m *= 1f + enemyHpPerArea * Run.areasCleared;                           // 攻略したエリアの数
                }
                return m * (1f + Run.RelicValue2(RelicEffect.Greed)) * RunExtras.EnemyHpMod;                      // 強欲な歯車
            }
        }
        public int EnemyDmgBonus => ((!scaleEnemies || Run == null) ? 0 : Mathf.Min(enemyDmgMax, Run.EquipPower() / Mathf.Max(1, enemyDmgStep))) + RunExtras.EnemyDmgMod;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Save = SaveData.Load();
        }

        public EquipmentData FindEquipment(string id)
        {
            foreach (var a in allAreas)
                if (a != null && a.craftable != null)
                    foreach (var e in a.craftable) if (e != null && e.name == id) return e;
            return null;
        }

        public void StartRun()
        {
            if (ExhibitionManager.Instance != null)
                Save.commonMaterials = Mathf.Max(Save.commonMaterials, ExhibitionManager.Instance.minCommonMaterials);
            var lv = new int[RunState.Slots];
            for (int i = 0; i < lv.Length; i++) lv[i] = Save.StartLevel(i);
            RunState.StagesPerArea = runLength == RunLength.Exhibition ? exhibitionStages : standardStages;
            int visits = runLength == RunLength.Exhibition ? visitsPerArea : 1;
            Run = new RunState(allAreas, starters, Save, lv, visits, FindEquipment, keepTierOnCarryOver);
            foreach (var e in new System.Collections.Generic.List<ItemEntry>(Save.carry))
            {
                var it = ItemManager.Find(e.id); int n = Mathf.Min(e.count, Save.ItemOwned(e.id), ItemManager.CarryPerKind);
                if (it == null || n <= 0) continue;
                Save.AddItem(e.id, -n); Run.itemBag[it] = n;
            }
            Save.carry.Clear(); Save.Write(); RunExtras.Reset();
            Run.RollChoices(); Load("AreaSelect");
        }
        public void ChooseArea(AreaData a) { RunExtras.SelectMod(a); Run.BeginArea(a); Load("Stage"); }

        public void OnStageClear()
        {
            AudioManager.Clear();
            bool wasBoss = Run.IsBossStage;
            Run.stagesCleared++; Run.stageIndex++;
            if (Run.stageIndex >= RunState.StagesPerArea) { RunExtras.OnAreaClear(Run, Save); Run.CompleteArea(); }
            // 遺物の提示: 最初のステージのあと、最終以外のボスのあと
            if (Run.stagesCleared == 1 || (wasBoss && Run.remaining.Count > 0)) Run.OfferRelics(relicPool, RunExtras.RelicChoices());
            Load("Garage");
        }
        public void AfterGarage()
        {
            if (Run.current != null) { Load("Stage"); return; }
            if (Run.remaining.Count == 0) { EndRun(true); return; }
            Run.RollChoices(); Load("AreaSelect");
        }
        public void OnPlayerDead() { AudioManager.GameOver(); EndRun(false); }
        void EndRun(bool win) { Run.Finish(win); Save.Apply(Run, win); Save.Write(); Load("Result"); }
        public void BackToTitle() => Load("Title");
        static void Load(string scene) => SceneManager.LoadScene(scene);
    }
}