# Claude向け引き継ぎ：ステージ自動生成

## 依頼の目的

Tech×Topiaのステージを、壁・床・足場・障害物・ギミック・敵などの部品を組み合わせて自動生成できるようにする。ユーザーはエリアやステージを増やす前に、この生成方式を先に進めたい。まず工房エリアを試作対象にしている。

この資料を受け取ったら、最初に現在のUnityプロジェクトとこの資料内の現状を確認する。Play Mode確認がまだなら、生成コードを大きく拡張する前に、既存の工房生成を動かして不具合・重なり・通行性を確認する。その後、確認結果に基づいてユーザーの希望に沿って改善する。

## ゲーム概要

- スチームパンク×ローグライト×横スクロールアクション。主人公はエンジニア。
- 1ステージは短く、複数ステージとボスでエリアを進行する。クリア後に次のエリアを選ぶ。
- 通常ステージは部屋の組み合わせで変化させ、ボスは当面手作りPrefabにする。
- エネルギーを使うダッシュ、追加ジャンプ、装備アクションがある。生成部屋の接続はジャンプ可能距離や着地点の安全性を考慮する必要がある。
- Unity Editor 6000.4.1f1、URP 2D。

## 現在の方式

地形をセル単位で無制約にランダム生成するのではなく、通行可能性を作り手が確認する部屋Prefab（chunk）を接続する。そのあと各部屋のソケット位置へ、対応する部品Prefabを配置する。

```text
開始部屋 → 中間部屋を2〜3個選択 → 終了部屋
   各部屋内のStagePartSocketへ種別の合うStagePartを配置
```

- 部屋同士は出口・入口の座標と接続名（現状は`Ground`）を使って横につなぐ。
- 出発run seedとステージ番号からステージseedを作る。同じseedなら同じ選択を再現する意図。
- 任意ソケットは出現率でスキップされ、部品候補は`weight`による重み付き抽選。
- 必須部品（工房では敵）が配置できなければ生成失敗にして、従来の`stagePrefabs`へフォールバック。
- `AreaData.generatedStages`がある通常ステージだけ自動生成する。ボスは既存Prefab。

## 主なファイル

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Stage/StageChunk.cs` | 部屋Prefabの入口・出口・接続名・難易度 |
| `Assets/Scripts/Stage/StageGenerationProfile.cs` | 開始／中間／終了部屋、部品候補、中間部屋数の設定 |
| `Assets/Scripts/Stage/StagePart.cs` | 配置するPrefabの分類と抽選weight |
| `Assets/Scripts/Stage/StagePartSocket.cs` | 部品の種類、必須指定、出現確率、Scene Gizmo |
| `Assets/Scripts/Stage/StageGenerator.cs` | seedに基づく部屋選択・接続・部品配置 |
| `Assets/Scripts/Stage/StageLoader.cs` | 通常ステージ生成呼び出し、従来Prefabへのfallback、PlayerSpawn |
| `Assets/Scripts/Core/AreaData.cs` | エリアの生成Profile参照 |
| `Assets/Scripts/Core/RunState.cs` / `GameManager.cs` | 周回seedとラン状態 |
| `Assets/Editor/WorkshopGenerationPrototypeBuilder.cs` | 工房の部屋・部品PrefabとProfileをUnity Editorで生成 |
| `Assets/Data/Areas/WorkshopGenerationProfile.asset` | 工房の生成候補設定 |
| `Assets/Data/Areas/Area_Workshop.asset` | 工房エリア。`generatedStages`参照を持つ |
| `Assets/Prefabs/Stages/Generated/` | 生成済み部屋Prefabと部品Prefab |
| `STAGE_GENERATION_PLAN.md` | 方式と試作、確認項目の設計メモ |
| `PROJECT_STATUS.md` | プロジェクト全体の引き継ぎ状況 |

## 現在ある工房アセット

部屋Prefabは開始1、中間3、終了1。各部屋は幅12ユニット、Ground Tilemap、落下時のKillZoneを持つ。開始部屋に`PlayerSpawn`、終了部屋に`StageGoal`を置いている。Profileは中間2〜3部屋、同一ステージ内での中間部屋重複を避ける。

部品Prefabは以下の5種。

- `WorkshopPart_Walker`：既存の歩行敵Prefabを包んだEnemy部品。
- `WorkshopPart_RepairMachine`：既存の修理機械Prefabを包んだGimmick部品。
- `WorkshopPart_Block`：1セルの障害物。
- `WorkshopPart_RaisedPlatform`：横3セルの足場。
- `WorkshopPart_Wall`：縦3セルの壁。

中間部屋3種には必須のEnemy socketを各1つ配置済み。ほかにPlatform、Obstacle、Wall、Gimmick socketがあり、合計10 socketを確認している。修理機械は中間部屋の1つにある任意Gimmick socketから選ばれる。

## 確認済みと未確認

### 確認済み

- Unity 6000.4.1f1のbatchmodeで`SteamEngineer.EditorTools.WorkshopGenerationPrototypeBuilder.Build`を実行し、部屋・部品PrefabとProfileを再生成した。
- ビルダーの完了ログ、batchmodeの正常終了、C#コンパイルエラーがないことを確認した。
- `Area_Workshop.generatedStages`がProfileを参照し、部品Prefab5種と中間部屋のsocketが作られていることを確認した。
- `git diff --check`を実行した。

### 未確認（最優先）

- Unity Play Modeで実際に通常ステージが生成されるか。
- PlayerSpawn、床の接地、敵の挙動、KillZone、StageGoalからガレージへの進行。
- 部品のCollider、レイヤー、Spriteの見え方、socket位置、敵との重なり、プレイヤーが通れない配置の有無。
- seedが同じとき再現し、異なるseedで部屋／部品の組み合わせが変わるか。
- 既存Prefabへfallbackした場合も正常にプレイできるか。

生成コードやEditorビルダーの実行成功は、ゲーム内での動作確認を意味しない。資料ではこの区別を保つこと。

## 実装上の注意点・レビュー観点

1. `StageGenerator.PlaceParts`は現状、部屋の全socketからPrefabを選ぶだけで、衝突・占有領域・プレイヤーの通路・ジャンプ到達性を検査しない。socket位置によっては壁や足場が重なる可能性がある。
2. 部品PrefabはsocketのTransform位置・回転へルートを合わせる。Tilemap部品はroot原点基準で作られているため、socket位置をセル基準で設定する。
3. 中間部屋CにはPlatform socketとGimmick socketが近接している。両方出た場合の干渉をPlay Modeで重点確認する。
4. 歩行敵Prefabはwrapperの子オブジェクト。Root/childのレイヤー、初期化処理、敵AIの有効化が既存Prefabの期待どおりかを実機確認する。
5. `StageGenerator`はUnityのglobal `Random.state`を保存・復元している。再現性を壊さないよう、追加の乱数利用もこのseed付き生成処理内に閉じ込める。将来的には独立した乱数Generatorへ置き換える案もある。
6. `avoidRepeatingChunks`は同じステージ内の部屋再利用を制限する。Profile候補数や中間部屋数を変える際は、選択不能でfallbackしないよう候補数を確認する。
7. `StageLoader`が生成後に`GameObject.Find("PlayerSpawn")`を使うので、複数のSpawn名を増やさず、開始chunkのものだけにする。
8. 現状の自動生成は横方向の`Ground`接続が中心。縦分岐、報酬ルート、エリア別の敵・素材・ギミック表現は未実装。
9. 今後、コードを変更する前にUnityプロジェクトと作業ツリーの差分を確認し、ユーザーの既存変更を上書きしない。

## Claude・Codex・ユーザーの役割

- Claude：企画・生成ルールの整理、選択肢やバランス案、Unityでの操作手順と確認観点、既存実装の設計レビューを担当する。Claude環境からプロジェクトへ直接アクセスできる場合は、作業前にパスと現状を確認する。アクセスできなければ、確認できたふりをせず、Codexへ渡す具体的な変更仕様・パッチ手順を作る。
- Codex：共有プロジェクトのコード調査・編集、Editorビルダーとアセット作成、Unityコンパイル／生成ログ確認、進捗記録を担当する。
- ユーザー：ゲーム体験・企画方針の最終決定をする。合意前のClaude/Codex提案は未確定案として扱う。
- 作業がどちらの環境で行われても、実装後に`PROJECT_STATUS.md`、この資料、`STAGE_GENERATION_PLAN.md`を同期して引き継ぎ可能にする。

## 次の作業手順

1. Unityでプロジェクトを開き、Consoleのエラーを確認する。
2. Titleから工房の通常ステージを開始し、生成ログのseed・sections・parts数を確認する。
3. 数ステージ分を遊び、spawn、移動・ジャンプ、敵、部品の当たり判定、落下復帰、Goal、ガレージ遷移を確認する。
4. 問題はseedと部屋名／socket名を記録して再現し、socket位置や部品形状を直す。通れない部屋列がある場合は到達性の検査または接続候補制約を先に加える。
5. 動作確認後、seed固定のEditorプレビューまたは開発用再現手段を整え、複数seedで配置の偏り・重複を評価する。
6. 安定したらProfileの難易度予算、敵／ギミック頻度、報酬部屋、エリア別部品カタログへ拡張する。新エリアは一度に1つずつ行う。
7. 各段階の実施内容、未確認事項、Unityログ上のエラー／警告、手動確認結果を記録する。

## Claudeへ最初に渡す依頼文

「この引き継ぎ資料を読み、まず記載されたUnityプロジェクトと現在の状態を確認してください。最初の作業は既存の工房ステージ自動生成をPlay Modeで検証することです。コード変更やテスト追加の前に、プロジェクト内の指示と未コミット差分を確認してください。Unityを操作できない場合は、実装済みと未確認を混同せず、必要なPlay Mode確認手順と、Codex側で続けるための具体的な修正案を提示してください。ユーザーが合意していないゲーム仕様は決定事項にしないでください。」
