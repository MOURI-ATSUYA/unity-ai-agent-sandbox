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

## Validation

コード変更後は可能な場合、以下の順で検証すること。

1. 今回変更したファイルに対して `git diff --check` を実行する。

2. 以下を実行して、起動済みUnity Editorが利用可能であることを確認する。

   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Scripts\check-unity.ps1`

3. 以下を実行して、Unity上でコンパイル検証を行う。

   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Scripts\validate-unity.ps1 -Mode Compile`

各コマンドの終了コードと出力内容を確認すること。

Unity Editorが利用できない場合、
Unity.exe、Unity Hub、Unity Licensing Clientを自分で起動・終了・操作してはならない。
ユーザーへUnity Editorが利用できないことを報告すること。

Unityコンパイル検証に失敗し、
今回の変更に起因するコンパイルエラーが存在する場合は、
エラーを修正して再検証すること。

仕様変更によって検証を通してはならない。

## Completion report
作業終了時に以下を報告する。

- 作業ブランチ
- 変更ファイル
- 実装内容
- 実行したテスト
- テスト結果
- 未確認事項
- 残存リスク