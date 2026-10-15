namespace SteamEngineer
{
    public enum Tier { Crude, Common, Refined, Masterwork, Supreme }          // 粗雑 凡作 洗練 傑作 至高
    public enum EquipSlot { Head, Body, Legs, Weapon }                        // 帽子 作業着 安全靴 レンチ
    public static class TierExt
    {
        static readonly string[] names = { "粗雑", "凡作", "洗練", "傑作", "至高" };
        public static string Label(this Tier t) => names[(int)t];
    }
}
