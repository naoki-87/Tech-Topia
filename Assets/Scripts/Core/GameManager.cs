using UnityEngine; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // Title シーンに1つ置く。DontDestroyOnLoad。
    // Scenes: Title / AreaSelect / Stage / Garage / Result
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public AreaData[] allAreas;
        public EquipmentData[] starters;     // Head, Body, Legs, Weapon の順
        public RelicData[] relicPool;        // 遺物の候補(Tools > TechTopia > 遺物アセットを作成 で自動設定)
        [Header("1周の長さ")]
        public int visitsPerArea = 2;        // 各エリアを何回使うか(2エリア × 2 = 4エリア)
        [Header("敵の強さ")]
        public bool scaleEnemies = true;
        public float enemyHpPerPower = 0.05f, enemyHpCap = 2.5f, enemyHpPerArea = 0.12f;
        public int enemyDmgStep = 8, enemyDmgMax = 2;
        [Header("素材の分解")]
        public int disassembleBase = 2;      // 分解で得る汎用素材の基準値(×(ティア+1))
        public SaveData Save; public RunState Run;

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
                return m * (1f + Run.RelicValue2(RelicEffect.Greed));                      // 強欲な歯車
            }
        }
        public int EnemyDmgBonus => (!scaleEnemies || Run == null) ? 0 : Mathf.Min(enemyDmgMax, Run.EquipPower() / Mathf.Max(1, enemyDmgStep));

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Save = SaveData.Load();
        }

        public void StartRun()
        {
            if (ExhibitionManager.Instance != null)
                Save.commonMaterials = Mathf.Max(Save.commonMaterials, ExhibitionManager.Instance.minCommonMaterials);
            var lv = new int[RunState.Slots];
            for (int i = 0; i < lv.Length; i++) lv[i] = Save.StartLevel(i);
            Run = new RunState(allAreas, starters, Save, lv, visitsPerArea);
            Run.RollChoices(); Load("AreaSelect");
        }
        public void ChooseArea(AreaData a) { Run.BeginArea(a); Load("Stage"); }

        public void OnStageClear()
        {
            AudioManager.Clear();
            bool wasBoss = Run.IsBossStage;
            Run.stagesCleared++; Run.stageIndex++;
            if (Run.stageIndex >= RunState.StagesPerArea) Run.CompleteArea();
            // 遺物の提示: 最初のステージのあと、最終以外のボスのあと
            if (Run.stagesCleared == 1 || (wasBoss && Run.remaining.Count > 0)) Run.OfferRelics(relicPool);
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