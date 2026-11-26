#if UNITY_EDITOR
using System; using System.Collections.Generic; using System.IO; using System.Text;
using UnityEditor; using UnityEngine;
namespace SteamEngineer.EditorTools
{
    // パッチファイル(.txt)を読んで、スクリプトを自動で書き換えるツール
    // メニュー: Tools > TechTopia > パッチを適用... / 直前のパッチを元に戻す
    public static class PatchTool
    {
        class Op { public string kind, file, path, find, with; public int line; }
        class State
        {
            public Dictionary<string, string> text = new Dictionary<string, string>();   // 絶対パス → 書き換え後の本文
            public Dictionary<string, bool> crlf = new Dictionary<string, bool>();       // 元の改行が CRLF だったか
            public HashSet<string> touched = new HashSet<string>(), deleted = new HashSet<string>(), created = new HashSet<string>();
            public List<string> errors = new List<string>(), warnings = new List<string>();
            public int replaced, skipped;
        }

        static string Root => Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
        static string BackupRoot => Path.Combine(Root, "PatchBackup");
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        static string Rel(string abs) { return abs.Substring(Root.Length).TrimStart('\\', '/').Replace('\\', '/'); }
        static string Short(string s) { string f = s.Split('\n')[0].Trim(); return f.Length > 70 ? f.Substring(0, 70) + "..." : f; }
        // 文字コードを自動で判定して読む(UTF-8 か Shift-JIS)
        static string ReadSmart(string path, out string encName)
        {
            byte[] b = File.ReadAllBytes(path);
            try
            {
                string t = new UTF8Encoding(false, true).GetString(b);        // 厳密に読む(UTF-8 でなければ例外)
                encName = "UTF-8"; return (t.Length > 0 && t[0] == '\uFEFF') ? t.Substring(1) : t;
            }
            catch (Exception) { }
            try { string t = Encoding.GetEncoding(932).GetString(b); encName = "Shift-JIS"; return t; }
            catch (Exception) { throw new Exception("文字コードを判定できませんでした: " + Path.GetFileName(path)); }
        }
        // 見つからなかったとき、ファイルの中の「近い行」を探して見せる(原因を調べやすくする)
        static string Nearest(string text, string find)
        {
            string first = find.Split('\n')[0].Trim(); if (first.Length == 0) return "(なし)";
            var lines = text.Split('\n'); int min = Math.Min(12, first.Length);
            for (int len = Math.Min(first.Length, 60); len >= min; len -= 6)
            {
                string key = first.Substring(0, len);
                foreach (var l in lines)
                    if (l.Contains(key)) { string t = l.Trim(); return t.Length > 260 ? t.Substring(0, 260) + "..." : t; }
                if (len == min) break;
            }
            return "(近い行が見つかりません)";
        }
        static int Count(string text, string find)
        {
            int n = 0, i = 0;
            while ((i = text.IndexOf(find, i, StringComparison.Ordinal)) >= 0) { n++; i += find.Length; }
            return n;
        }
        static string Resolve(string name, out string problem)
        {
            problem = null;
            var found = Directory.GetFiles(Application.dataPath, name, SearchOption.AllDirectories);
            if (found.Length == 1) return Path.GetFullPath(found[0]);
            problem = found.Length == 0 ? "ファイルが見つかりません" : "同じ名前のファイルが複数あります";
            return null;
        }

        // ---------- パッチファイルの読み込み ----------
        static List<Op> Parse(string raw, out string err)
        {
            err = null; var ops = new List<Op>();
            var lines = raw.Replace("\r\n", "\n").Split('\n');
            Op cur = null; int mode = 0; var buf = new List<string>();     // mode 0=外 1=FIND 2=WITH 3=NEWの中身
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i]; int no = i + 1;
                if (l.StartsWith("@@"))
                {
                    string cmd = l.Trim(), arg = ""; int sp = cmd.IndexOf(' ');
                    if (sp > 0) { arg = cmd.Substring(sp + 1).Trim(); cmd = cmd.Substring(0, sp); }
                    if (cmd == "@@NOTE" && mode == 0) continue;
                    if (cmd == "@@REPLACE" && mode == 0) { cur = new Op { kind = "REPLACE", file = arg, line = no }; continue; }
                    if (cmd == "@@FIND" && mode == 0 && cur != null && cur.kind == "REPLACE") { mode = 1; buf.Clear(); continue; }
                    if (cmd == "@@WITH" && mode == 1) { cur.find = string.Join("\n", buf.ToArray()); mode = 2; buf.Clear(); continue; }
                    if (cmd == "@@END" && mode == 2) { cur.with = string.Join("\n", buf.ToArray()); ops.Add(cur); cur = null; mode = 0; continue; }
                    if (cmd == "@@NEW" && mode == 0) { cur = new Op { kind = "NEW", path = arg, line = no }; mode = 3; buf.Clear(); continue; }
                    if (cmd == "@@OVERWRITE" && mode == 0) { cur = new Op { kind = "OVERWRITE", file = arg, line = no }; mode = 3; buf.Clear(); continue; }
                    if (cmd == "@@END" && mode == 3) { cur.with = string.Join("\n", buf.ToArray()).TrimEnd('\n') + "\n"; ops.Add(cur); cur = null; mode = 0; continue; }
                    if (cmd == "@@DELETE" && mode == 0) { ops.Add(new Op { kind = "DELETE", file = arg, line = no }); continue; }
                    err = no + "行目: 位置が正しくないか、知らない命令です: " + l.Trim(); return null;
                }
                if (mode == 1 || mode == 2 || mode == 3) buf.Add(l);
            }
            if (mode != 0) { err = "最後の @@END が足りません"; return null; }
            return ops;
        }

        // ---------- 事前チェック(ここではファイルを変更しない) ----------
        static State Plan(List<Op> ops)
        {
            var s = new State();
            foreach (var op in ops)
            {
                if (op.kind == "NEW")
                {
                    string abs = Path.GetFullPath(Path.Combine(Root, op.path));
                    if (!abs.StartsWith(Path.GetFullPath(Application.dataPath))) { s.errors.Add(op.line + "行目 NEW: Assets の外には作れません: " + op.path); continue; }
                    if (File.Exists(abs) && !s.text.ContainsKey(abs)) { s.touched.Add(abs); s.crlf[abs] = false; s.warnings.Add("既にあるファイルを上書きします: " + op.path); }
                    else if (!File.Exists(abs)) s.created.Add(abs);
                    s.text[abs] = op.with; continue;
                }
                if (op.kind == "OVERWRITE")
                {
                    string pr; string ap = Resolve(op.file, out pr);
                    if (ap == null) { s.errors.Add(op.line + "行目 " + op.file + ": " + pr + "(丸ごと入れ替えるファイルが見つかりません)"); continue; }
                    s.touched.Add(ap); s.crlf[ap] = false; s.text[ap] = op.with;
                    s.warnings.Add(op.file + ": 中身を丸ごと入れ替えます");
                    continue;
                }
                string problem; string p = Resolve(op.file, out problem);
                if (p == null)
                {
                    if (op.kind == "DELETE") s.warnings.Add(op.file + ": 見つからないので、削除しません");
                    else s.errors.Add(op.line + "行目 " + op.file + ": " + problem);
                    continue;
                }
                if (op.kind == "DELETE") { s.touched.Add(p); s.deleted.Add(p); s.text.Remove(p); continue; }
                if (s.deleted.Contains(p)) { s.errors.Add(op.line + "行目 " + op.file + ": 削除予定のファイルです"); continue; }
                if (!s.text.ContainsKey(p))
                {
                    string t, en;
                    try { t = ReadSmart(p, out en); }
                    catch (Exception ex) { s.errors.Add(op.line + "行目 " + op.file + ": " + ex.Message); continue; }
                    if (en != "UTF-8") s.warnings.Add(op.file + ": 文字コードが " + en + " でした。UTF-8 に変換して保存します");
                    s.crlf[p] = t.Contains("\r\n"); s.text[p] = t.Replace("\r\n", "\n"); s.touched.Add(p);
                }
                if (string.IsNullOrEmpty(op.find)) { s.errors.Add(op.line + "行目 " + op.file + ": 変更前の文が空です"); continue; }
                int n = Count(s.text[p], op.find);
                if (n == 0)
                {
                    if (op.with.Length > 0 && Count(s.text[p], op.with) >= 1)      // すでに変更済み
                    { s.skipped++; s.warnings.Add(op.line + "行目 " + op.file + ": すでに変更済みのようです(飛ばします) → " + Short(op.with)); continue; }
                    s.errors.Add(op.line + "行目 " + op.file + ": 変更前の文が見つかりません → " + Short(op.find) + "\n    ファイルの近い行 → " + Nearest(s.text[p], op.find));
                    continue;
                }
                if (n > 1) { s.errors.Add(op.line + "行目 " + op.file + ": 変更前の文が " + n + " 箇所あります → " + Short(op.find)); continue; }
                int idx = s.text[p].IndexOf(op.find, StringComparison.Ordinal);
                s.text[p] = s.text[p].Substring(0, idx) + op.with + s.text[p].Substring(idx + op.find.Length);
                s.replaced++;
            }
            return s;
        }

        // ---------- 書き込み(先にバックアップを作る) ----------
        static string Commit(State s)
        {
            string dir = Path.Combine(BackupRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dir);
            var manifest = new List<string>();
            foreach (var abs in s.touched)
            {
                string rel = Rel(abs), dst = Path.Combine(dir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(abs, dst, true);
                if (File.Exists(abs + ".meta")) File.Copy(abs + ".meta", dst + ".meta", true);
                manifest.Add((s.deleted.Contains(abs) ? "DEL " : "MOD ") + rel);
            }
            foreach (var abs in s.created) manifest.Add("NEW " + Rel(abs));
            File.WriteAllLines(Path.Combine(dir, "manifest.txt"), manifest.ToArray(), Utf8);
            foreach (var kv in s.text)
            {
                string body = kv.Value;
                bool crlf; if (s.crlf.TryGetValue(kv.Key, out crlf) && crlf) body = body.Replace("\n", "\r\n");
                Directory.CreateDirectory(Path.GetDirectoryName(kv.Key));
                File.WriteAllText(kv.Key, body, Utf8);
            }
            foreach (var abs in s.deleted) AssetDatabase.DeleteAsset(Rel(abs));
            AssetDatabase.Refresh();
            return dir;
        }

        static void Fail(string title, string msg)
        {
            Debug.LogError("[パッチ] " + title + "\n" + msg);
            string shown = msg.Length > 1200 ? msg.Substring(0, 1200) + "\n...(続きは Console を見てください)" : msg;
            EditorUtility.DisplayDialog(title, shown, "OK");
        }

        [MenuItem("Tools/TechTopia/パッチを適用...")]
        static void ApplyMenu()
        {
            string path = EditorUtility.OpenFilePanel("パッチファイル(.txt)を選んでください", Root, "txt");
            if (string.IsNullOrEmpty(path)) return;
            string err, penc; var ops = Parse(ReadSmart(path, out penc), out err);
            if (ops == null) { Fail("パッチファイルの形式エラー", err); return; }
            var st = Plan(ops);
            if (st.errors.Count > 0)
            {
                Fail("適用できませんでした(何も変更していません)",
                     string.Join("\n", st.errors.ToArray()) + "\n\nこの内容(Console の赤い文字)を、そのまま貼って教えてください。");
                return;
            }
            string summary = "変更するファイル " + (st.touched.Count - st.deleted.Count) + " 個 / 新しいファイル " + st.created.Count +
                             " 個 / 削除 " + st.deleted.Count + " 個 / 置き換え " + st.replaced + " 箇所" +
                             (st.skipped > 0 ? " / すでに変更済みで飛ばす " + st.skipped + " 箇所" : "");
            string warn = st.warnings.Count > 0 ? "\n\n注意:\n" + string.Join("\n", st.warnings.ToArray()) : "";
            if (!EditorUtility.DisplayDialog("パッチを適用します", summary + warn + "\n\n元のファイルは、プロジェクト直下の PatchBackup フォルダに保存します。", "適用する", "やめる")) return;
            string dir = Commit(st);
            Debug.Log("[パッチ] 適用しました: " + summary + "\nバックアップ: " + dir);
            EditorUtility.DisplayDialog("適用しました", summary + "\n\nUnity の再読み込みが終わるまで待ち、Console にエラーがないか確認してください。\n戻したいときは Tools > TechTopia > 直前のパッチを元に戻す", "OK");
        }

        [MenuItem("Tools/TechTopia/直前のパッチを元に戻す")]
        static void UndoMenu()
        {
            string latest = null;
            if (Directory.Exists(BackupRoot))
                foreach (var d in Directory.GetDirectories(BackupRoot))
                {
                    if (d.EndsWith("_restored")) continue;
                    if (latest == null || string.CompareOrdinal(Path.GetFileName(d), Path.GetFileName(latest)) > 0) latest = d;
                }
            if (latest == null || !File.Exists(Path.Combine(latest, "manifest.txt"))) { Fail("元に戻せません", "戻せるバックアップがありません。"); return; }
            if (!EditorUtility.DisplayDialog("元に戻します", Path.GetFileName(latest) + " のパッチを取り消します。", "戻す", "やめる")) return;
            foreach (var line in File.ReadAllLines(Path.Combine(latest, "manifest.txt"), Encoding.UTF8))
            {
                if (line.Length < 5) continue;
                string kind = line.Substring(0, 3), rel = line.Substring(4);
                if (kind == "NEW") { AssetDatabase.DeleteAsset(rel); continue; }
                string src = Path.Combine(latest, rel), abs = Path.GetFullPath(Path.Combine(Root, rel));
                if (!File.Exists(src)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(abs));
                File.Copy(src, abs, true);
                if (File.Exists(src + ".meta")) File.Copy(src + ".meta", abs + ".meta", true);
            }
            AssetDatabase.Refresh();
            Directory.Move(latest, latest + "_restored");
            EditorUtility.DisplayDialog("元に戻しました", "再読み込みが終わるまで待ってください。", "OK");
        }
    }
}
#endif
