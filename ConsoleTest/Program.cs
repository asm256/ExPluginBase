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
      public Dictionary<string , string[][]> deep_array = new Dictionary<string , string[][]>();
      public ConsoleColor enumColor = ConsoleColor.Gray;
      public PluginConfig() {
        priority_hoge["test"] = 0.3f;
        priority_hoge["pi"] = (float)Math.PI;
        deep_array["Cool"] = new string[][]{
          new string[] { "c11" , "c12" , "c13" , "c14" },
          new string[] { "c21" , "c22" , "c23" , "c24" }};
        deep_array["Pure"] = new string[][] {
          new string[] { "p11" , "p12" , "p13" , "p14" },
          new string[] { "p21" , "p22" , "p23" , "p24" }};
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
      Assert(cfg.deep_array["Cool"][1][1] == "c22" , "Dictionary<string,string[][]> Initialize");

      cfg.x = 500;
      cfg.ss[1] = new string[] { "test" , "test" };
      cfg.priority_hoge["test"] = 300f;
      cfg.enumColor = ConsoleColor.White;
      cfg.deep_array["Cool"][1] = new string[] { "cc21" , "cc22" , "cc23" , "cc24" };


      SaveConfig(cfg);
      cfg = ReadConfig<PluginConfig>();

      Assert(cfg.x == 500 , "Int Read/Write");
      Assert(cfg.enumColor == ConsoleColor.White , "Enum Read/Write");
      Assert(cfg.priority_hoge["test"] == 300f , "Dictionary<string,float> Read/Write");
      Assert(cfg.ss[1][1] == "test" , "Jagged String Array Read/Write");
      Assert(cfg.deep_array["Cool"][1][1] == "cc22" , "Dictionary<string,string[][]> Read/Write");

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
      else
        Environment.Exit(-1);
    }
  }
}
