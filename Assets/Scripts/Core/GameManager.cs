using UnityEngine; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // Title シーンに1つ置く。DontDestroyOnLoad。
    // Scenes: Title / AreaSelect / Stage / Garage / Result  (Build Settingsに登録)
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public AreaData[] allAreas;
        public EquipmentData[] starters;     // Head, Body, Legs, Weapon の順
        public SaveData Save; public RunState Run;
        [Header("敵の強さ(装備の強化度合いで上がる)")]
        public bool scaleEnemies = true;
        public float enemyHpPerPower = 0.05f, enemyHpCap = 2.5f;     // パワー1につきHP+5%(最大×2.5)
        public int enemyDmgStep = 8, enemyDmgMax = 2;                // パワー8ごとにダメージ+1(最大+2)

        public float EnemyHpMul => (!scaleEnemies || Run == null) ? 1f : Mathf.Min(enemyHpCap, 1f + enemyHpPerPower * Run.EquipPower());
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
            Run = new RunState(allAreas, starters, Save, lv);
            Run.RollChoices(); Load("AreaSelect");
        }
        public void ChooseArea(AreaData a) { Run.BeginArea(a); Load("Stage"); }
        public void OnStageClear()
        {
            Run.stageIndex++;
            if (Run.stageIndex >= RunState.StagesPerArea) Run.CompleteArea();
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
