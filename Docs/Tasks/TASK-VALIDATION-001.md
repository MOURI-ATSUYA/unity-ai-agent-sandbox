# TASK-VALIDATION-001 Codex Unity Validation Test

## Objective

Codexから起動済みUnity Editorへの自動検証経路が正常に動作することを確認する。

## Requirements

以下の新規C#ファイルを作成すること。

Assets/Scripts/Diagnostics/AgentValidationProbe.cs

AgentValidationProbeはMonoBehaviourを継承すること。

以下のprivateフィールドを持つこと。

[SerializeField]
private string message = "Agent validation succeeded";

以下のpublicメソッドを持つこと。

public string GetMessage()

GetMessage()はmessageを返すこと。

## Allowed Files

新規作成可能:

Assets/Scripts/Diagnostics/AgentValidationProbe.cs

## Restrictions

- Sceneを変更しない
- Prefabを変更しない
- ProjectSettingsを変更しない
- Packagesを変更しない
- 他のC#ファイルを変更しない
- Unity.exeを直接起動しない
- Unity Hubを操作しない
- commitしない
- pushしない

## Validation

AGENTS.mdに定義されたValidation手順をすべて実行すること。

特に以下を実行すること。

1. git diff --check
2. .\Tools\Scripts\check-unity.ps1
3. .\Tools\Scripts\validate-unity.ps1 -Mode Compile

Unity検証が失敗した場合は原因を確認し、
今回の変更に起因する問題であれば修正して再検証すること。

## Completion Criteria

- AgentValidationProbe.csが作成されている
- git diff --checkが成功する
- check-unity.ps1がUnity Editorを検出する
- validate-unity.ps1 -Mode Compileが成功する
- Unity上でコンパイルエラーがない