# ExPluginBase

ざっくり説明するとプラグイン作成の際のよく使う・作る機能をあらかじめ実装しておこう的な

## INI シリアライザ
- 設定値をiniから読み込む
- 設定値をiniへ書き込む

```csharp
class PluginConfig {
  public int x = 10;
  public float s = (float)Math.PI;
  public double doubleValue = Math.PI;
  public string path = "pathtest";
  public int[] d = new int[] { 1 , 4 , 6 , 7 , 8 };
}
// iniからの読み取り
var cfg = ReadConfig<PluginConfig>();
WriteLine("cfg.path = {0}" , cfg.path);
// iniへの書き込み
SaveConfig(cfg);
```
#### 対応状況
|Type  |状況       |
|:-----|----------|
|string|Write/Read|
|(u)int|Write/Read|
|(u)long|Write/Read|
|float/double|Write/Read|
|bool|Write/Read|
|Array|Write/Read *1|
|enum|未確認だけど対応してる気がする|
|Dictionary|ReadOnly|
|struct|NotSupported|
|class |NotSupported|

-\*1 ユーザーが編集するなどして **添字が0からはじまらない** 場合や **添字に抜けがある** 場合にデータを壊してしまうバグがある

## コンソール出力
- WriteLine
- DebugWriteLine
- LogWithCallTree
- DebugLogWithCallTree
