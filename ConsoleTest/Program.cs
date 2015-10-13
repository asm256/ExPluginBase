using System;
using System.Collections.Generic;
using System.IO;
using PluginExt;

namespace PluginTest {
  class Test {
    static void Main(string[] args) {
      File.Delete("TestPlugin.ini");
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
      public ConsoleColor enumColor = ConsoleColor.Gray;
      public PluginConfig() {
        priority_hoge["test"] = 0.3f;
        priority_hoge["pi"] = (float)Math.PI;
      }
    }
    public Dictionary<string,bool> Result { get; } = new Dictionary<string , bool>();

    public bool Assert(bool test,string message) {
      if(test)
        WriteLine("{0} is success" , message);
      else
        WriteLine("{0} is failed" , message);
      Result.Add(message , test);
      return test;
    }
    public void Awake() {
      DebugWriteLine(DataPath);
      WriteLine("{0} Plugin Test" , Name);

      var cfg = ReadConfig<PluginConfig>();
      // Initialize
      Assert(cfg.x == 10 , "Int Initialize");
      Assert(cfg.enumColor == ConsoleColor.Gray , "Enum Initialize");
      Assert(cfg.priority_hoge["test"] == 0.3f , "Dictionary<string,float> Initialize");
      Assert(cfg.ss[0][1] == "bb" && cfg.ss[1][1] == "pipi" , "Jagged String Array Initialize");

      cfg.x = 500;
      cfg.ss[1] = new string[] { "test" , "test" };
      cfg.priority_hoge["test"] = 300f;
      cfg.enumColor = ConsoleColor.White;

      SaveConfig(cfg);
      cfg = ReadConfig<PluginConfig>();

      Assert(cfg.x == 500 , "Int Read/Write");
      Assert(cfg.enumColor == ConsoleColor.White , "Enum Read/Write");
      Assert(cfg.priority_hoge["test"] == 300f , "Dictionary<string,float> Read/Write");
      Assert(cfg.ss[1][1] == "test" , "Jagged String Arrar Read/Write");

      bool x = true;
      foreach(var item in Result) {
        if(!item.Value) {
          x = false;
          WriteLine("Failed");
          break;
        }
      }
      if(x)
        WriteLine("Success");
    }
  }
}
