using System;
using System.Collections.Generic;
using System.IO;
using PluginExt;

namespace PluginTest {
  class Test {
    static void Main(string[] args) {
      var t = new TestPlugin();
      t.Awake();
    }
  }
  public class TestPlugin : ExPluginBase {
    class PluginConfig {
      public string[][] ss = new string[][] {
        new string[] {"aaaa","bb" },
        new string[] {"test","pipi"}
      };
      public int x = 10;
      public float s = (float)Math.PI;
      public double doubleValue = Math.PI;
      public string path = "pathtest";
      //テスト
      public string[] sFaceAnime41 = new string[] { "優しさ" , "微笑み" };
      public int[] d = new int[] { 1 , 4 , 6 , 7 , 8 };
      public Dictionary<string , float> priority_hoge = new Dictionary<string , float>();
      public PluginConfig() {
        priority_hoge["test"] = 0.3f;
        priority_hoge["pi"] = (float)Math.PI;
      }
    }
    public void Awake() {
      DebugWriteLine(DataPath);
      WriteLine("{0} Plugin Test" , Name);
      var cfg = ReadConfig<PluginConfig>();
      WriteLine("cfg.path = {0}" , cfg.path);
      foreach(var item in cfg.ss) {
        foreach(var s in item) {
          WriteLine(s);
        }
      }
      cfg.ss[1] = new string[] { "uwagaki" , "test" };
      SaveConfig(cfg);
      cfg = ReadConfig<PluginConfig>();
      foreach(var item in cfg.ss) {
        foreach(var s in item) {
          WriteLine(s);
        }
      }
    }
  }
}
