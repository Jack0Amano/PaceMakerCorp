# [GameManager](../../../Assets/MyAssets/SceneManager/GameManager.cs)

## Properties

- `DataRootPath`: データのルートパスを定義する静的な文字列フィールドです。
- `StaticDataRootPath`: 静的データのルートパスを定義する静的な文字列フィールドです。
- `TacticsSceneID`: 現在tacticsシーン中であればそのシーンの名前を保持するプロパティです。
- `AddTimeSeconds`: `addTimeEventHandler`を呼び出す秒数を計算するプロパティです。このプロパティは、`IsHighSpeedMode`がtrueの場合とfalseの場合で異なる値を返します。

## Fields

- `backToMainMapHandler`: ストラテジーシーンを非表示にしてMainMapに戻る際に呼び出されるイベントハンドラです。
- `encount`: MapSceneでエンカウントしたときの敵味方の情報を保持するフィールドです。
- `CurrentDateTime`: ゲーム内の現在時刻を保持するフィールドです。

## Constants

- `mapSceneID`: "Map"という文字列を保持する定数です。
- `prepareSceneID`: "Prepare"という文字列を保持する定数です。
- `startSceneID`: "StartScene"という文字列を保持する定数です。