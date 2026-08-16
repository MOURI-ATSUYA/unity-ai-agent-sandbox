# Coding Rules

## C#

- クラス名: PascalCase
- publicメソッド: PascalCase
- privateフィールド: camelCase
- 定数: PascalCase
- 1クラス1責務を基本とする
- 不要なSingletonを作らない
- Update内で重い処理を繰り返さない
- 不要なFind系APIの多用を避ける
- Inspectorから設定する値は[SerializeField]を使用する

## Unity

- MonoBehaviourの責務を小さく保つ
- GameObject参照は可能な限りInspectorまたは初期化時に取得する
- .metaファイルを削除しない
- SceneやPrefabの変更は必要最小限にする