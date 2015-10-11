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

## コンソール出力
- WriteLine
- DebugWriteLine
- LogWithCallTree
- DebugLogWithCallTree
