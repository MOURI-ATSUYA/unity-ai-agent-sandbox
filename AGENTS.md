# Agent Instructions

## Project
UnityによるAIエージェント開発検証用プロジェクト。

## Required reading order
作業開始前に以下を確認すること。

1. Docs/00_ProjectOverview.md
2. Docs/02_FunctionalRequirements.md
3. Docs/03_TechnicalDesign.md
4. Docs/04_CodingRules.md
5. Docs/05_TestPolicy.md
6. 対象のDocs/Tasks/TASK-XXX.md

## Git rules
- mainへ直接commitまたはpushしない
- developへ直接機能実装をcommitしない
- 作業ごとにfeatureブランチを作る
- 作業前にgit statusを確認する
- ユーザーが作成した無関係な変更を削除しない
- force pushを行わない
- .metaファイルを意図なく削除しない

## Unity rules
- Library、Temp、LogsをGit管理しない
- SceneやPrefabを直接編集する場合は慎重に扱う
- ProjectSettingsを必要なく変更しない
- 既存のpublic APIを変更する場合は理由を報告する

## Task rules
- 仕様が不明確な場合は推測して実装しない
- 対象タスクの範囲外を勝手に変更しない
- 可能な限り小さい差分で実装する

## Completion report
作業終了時に以下を報告する。

- 作業ブランチ
- 変更ファイル
- 実装内容
- 実行したテスト
- テスト結果
- 未確認事項
- 残存リスク