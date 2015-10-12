using System;
using System.Collections.Generic;
using PluginExt;

namespace PluginTest {
  class Program {
    static void Main(string[] args) {
      var t = new TestPlugin();
      t.Awake();
    }
  }
  public class TestPlugin : ExPluginBase {
    class PluginConfig {
      public int x = 10;
      public float s = (float)Math.PI;
      public double doubleValue = Math.PI;
      public string path = "pathtest";
      //テスト
      public string[] sFaceAnime41 = new string[] { "優しさ" , "微笑み" };
      public int[] d = new int[] { 1 , 4 , 6 , 7 , 8 };
      public Dictionary<string , float> priority_hoge = new Dictionary<string,float>();
      public PluginConfig() {
        priority_hoge["test"] = 0.3f;
        priority_hoge["pi"] = (float)Math.PI;
      }
    }
    public void Awake() {
      DebugWriteLine(DataPath);
      WriteLine("{0} Plugin Test",Name);
      var cfg = ReadConfig<PluginConfig>();
      WriteLine("cfg.path = {0}" , cfg.path);
      foreach(var item in cfg.sFaceAnime41) {
        WriteLine(item);
      }
      SaveConfig(cfg);
    }
  }
}
