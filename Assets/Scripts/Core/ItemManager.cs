using System.Collections.Generic; using System.Text; using UnityEngine;
namespace SteamEngineer
{
    public enum UseScene { Stage, Garage, AreaSelect }
    public static class ItemManager
    {
        public const int CarryPerKind = 3;

        public static ItemData Find(string id)
        {
            var gm = GameManager.Instance; if (gm == null || gm.itemPool == null) return null;
            foreach (var i in gm.itemPool) if (i != null && i.name == id) return i;
            return null;
        }
        static ItemUse Flag(UseScene s) => s == UseScene.Stage ? ItemUse.Stage : s == UseScene.Garage ? ItemUse.Garage : ItemUse.AreaSelect;

        public static string WhenText(ItemData it)
        {
            var l = new List<string>();
            if ((it.usableIn & ItemUse.Stage) != 0) l.Add("ステージ中" + (it.stageKey != KeyCode.None ? $"({KeyName(it.stageKey)})" : ""));
            if ((it.usableIn & ItemUse.Garage) != 0) l.Add("ガレージ");
            if ((it.usableIn & ItemUse.AreaSelect) != 0) l.Add("エリア選択");
            return l.Count == 0 ? "使えない" : string.Join(" / ", l);
        }
        static string KeyName(KeyCode k) => k.ToString();

        // 使えない理由(使えるなら空)
        public static string Why(ItemData it, RunState run, UseScene scene, Health hp = null, Energy en = null)
        {
            if (run == null || run.ItemCount(it) <= 0) return "持っていない";
            if ((it.usableIn & Flag(scene)) == 0) return $"使える場面: {WhenText(it)}";
            switch (it.effect)
            {
                case ItemEffect.Heal:
                case ItemEffect.HealFull:
                    int cur = hp != null ? hp.Current : run.hp, max = hp != null ? hp.max : PlayerStats.Build(run).maxHp;
                    if (cur >= max) return "HPが満タン"; break;
                case ItemEffect.Energy:
                    if (en != null && en.Current >= en.max - 0.1f) return "エネルギーが満タン"; break;
                case ItemEffect.AreaGuide:
                    if (it.guideArea == null) return "設定が不足(Guide Area)";
                    if (!run.remaining.Contains(it.guideArea)) return "そのエリアは、もう行けない";
                    if (run.choices.Contains(it.guideArea)) return "すでに候補にある"; break;
            }
            return "";
        }

        public static string Use(ItemData it, RunState run, UseScene scene, PlayerController p = null)
        {
            var hp = p != null ? p.Health : null; var en = p != null ? p.GetComponent<Energy>() : null;
            string why = Why(it, run, scene, hp, en); if (why != "") return why;
            string msg = "";
            switch (it.effect)
            {
                case ItemEffect.Heal:
                case ItemEffect.HealFull:
                    int max = hp != null ? hp.max : PlayerStats.Build(run).maxHp;
                    int amount = it.effect == ItemEffect.HealFull ? max : Mathf.RoundToInt(it.value);
                    if (hp != null) { hp.Heal(amount); p.SyncHp(); } else run.hp = Mathf.Min(max, run.hp + amount);
                    msg = $"{it.displayName}: HPが回復した"; break;
                case ItemEffect.Energy:
                    if (en != null) en.Restore(en.max); msg = $"{it.displayName}: エネルギーが全回復"; break;
                case ItemEffect.AttackUp: run.itemAttack += Mathf.RoundToInt(it.value); msg = $"{it.displayName}: 攻撃力 +{Mathf.RoundToInt(it.value)}(この周回)"; break;
                case ItemEffect.MaxHpUp:
                    int up = Mathf.RoundToInt(it.value); run.itemMaxHp += up; run.hp += up; msg = $"{it.displayName}: 最大HP +{up}(この周回)"; break;
                case ItemEffect.AreaGuide: run.ForceChoice(it.guideArea); msg = $"{it.displayName}: {it.guideArea.displayName}が候補に加わった"; break;
            }
            run.ConsumeItem(it);
            var sv = GameManager.Instance.Save; sv.AddStat("items_used", 1); Achievements.CheckAll(sv);
            return msg;
        }
    }
}