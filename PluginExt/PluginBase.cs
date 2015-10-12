/* ExPluginBase
 * Pluginを楽に作るための抽象クラス
 * MIT License
 * Copyright © asm__ 2015
 */
// テスト用にMonoBehaviourを継承しない
//#define NOUNITY
// 構想中
//#define NOUNITYINJECTOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace PluginExt {

  /// <summary>
  /// 共有コンフィグ
  /// </summary>
  public static class SharedConfig {
    public static string DataPath{ get; }
    public static bool ChangeConsoleColor;
    static SharedConfig() {
#if NOUNITY
      string DirName = ".";
#elif NOUNITYINJECTOR
      //TODO
      string DirName = "Config";
#else
      string DirName = @"UnityInjector\Config";
#endif
      DataPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) , DirName);
      if(!Directory.Exists(DataPath))
        Directory.CreateDirectory(DataPath);
      try {
        Console.ForegroundColor = ConsoleColor.Gray;
        ChangeConsoleColor = true;
      } catch{
        ChangeConsoleColor = false;
      }
    }

    #region Save/ReadConfig
    public static class Ini {
      protected static class SafeNativeMethods {
        [DllImport("shlwapi.dll" , CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        public static extern int StrCmpLogicalW(string psz1 , string psz2);
        [DllImport("KERNEL32.DLL" , CharSet = CharSet.Auto), SuppressUnmanagedCodeSecurity]
        public static extern uint GetPrivateProfileString(string lpAppName , string lpKeyName , string lpDefault , StringBuilder lpReturnedString , uint nSize , string lpFileName);

        [DllImport("KERNEL32.DLL" , CharSet = CharSet.Auto), SuppressUnmanagedCodeSecurity]
        public static extern uint GetPrivateProfileInt(string lpAppName , string lpKeyName , int nDefault , string lpFileName);

        [DllImport("kernel32.dll" , CharSet = CharSet.Auto), SuppressUnmanagedCodeSecurity]
        public static extern int GetPrivateProfileSection(string lpAppName , IntPtr lpszReturnBuffer , int nSize , string lpFileName);
        [DllImport("kernel32.dll" , CharSet = CharSet.Auto), SuppressUnmanagedCodeSecurity]
        public static extern int GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer , int nSize , string lpFileName);

        [DllImport("KERNEL32.DLL" , CharSet = CharSet.Auto), SuppressUnmanagedCodeSecurity]
        public static extern uint WritePrivateProfileString(string lpAppName , string lpKeyName , string lpString , string lpFileName);
      }

      public sealed class NaturalStringComparer : IComparer<string> {
        public int Compare(string a , string b) {
          return SafeNativeMethods.StrCmpLogicalW(a , b);
        }
      }
      static string separator = "::";

      public static List<string> GetKeys(string section , string filepath) {
        IntPtr sb = IntPtr.Zero; int nsize = 1024; int ns;
        try {
          do {
            nsize *= 2;
            sb = Marshal.AllocCoTaskMem(nsize * 2);
            ns = SafeNativeMethods.GetPrivateProfileSection(section , sb , nsize , filepath);
          } while(nsize - 2 == ns);
          string sec = Marshal.PtrToStringAuto(sb , ns);

          string[] tmp = sec.TrimEnd('\0').Split('\0');
          if(tmp[0] == String.Empty)
            return new List<string>(0);
          List<string> result = new List<string>(tmp.Length);
          foreach(string entry in tmp) {
            result.Add(entry.Substring(0 , entry.IndexOf('=')));
          }
          return result;
        } finally {
          if(sb != IntPtr.Zero)
            Marshal.FreeCoTaskMem(sb);
        }
      }
      #region 値型の読み込み
      delegate bool TryParseDelgate<T>(string input , out T dest);
      public static object ReadNumber(Type t , string section , string key , string filepath , object def) {
        if(!t.IsValueType)
          return def;
        string s = ReadString(section , key , filepath , def.ToString());
        if(t.IsEnum) {
          try {
            return Enum.Parse(t , s);
          } catch(Exception) {
            return def;
          }
        }
        Type[] p = new Type[2] { typeof(string) , t.MakeByRefType() };
        var methodTryParse = t.GetMethod("TryParse" , p);
        if(methodTryParse == null) {
          //たぶんユーザー定義構造体
          // TODO
          return def;
        }

        var _a = new object[] { s , def };
        if((bool)methodTryParse.Invoke(null , _a))
          return _a[1];
        return def;
      }
      public static T ReadNumber<T>(string section , string key , string filepath , T def) {
        return (T)ReadNumber(typeof(T) , section , key , filepath , def);
      }
      public static string ReadString(string section , string key , string filepath , string def) {
        StringBuilder sb = null;
        int size = 1024; uint ns = 0;
        do {
          size *= 2;
          sb = new StringBuilder(size);
          ns = SafeNativeMethods.GetPrivateProfileString(section , key , def , sb , (uint)size , filepath);
        } while(size - 2 == ns);
        return sb.ToString();
      }
      #endregion
      #region コレクション型
      #region  IList
      public static IList<T> ReadList<T>(string section , string key , string filepath , IList<T> def) {
        int t;
        string sec = section + separator + key;
        var list = GetKeys(sec , filepath).FindAll((s) => Int32.TryParse(s , out t));
        list.Sort(new NaturalStringComparer());
        if(list.Count == 0)
          return def;

        IList<T> result = new List<T>(list.Capacity);
        if(typeof(T).IsValueType) {
          foreach(var item in list) {
            result.Add(ReadNumber<T>(sec , item , filepath , default(T)));
          }
        } else {
          //参照型
          if(typeof(T) == typeof(string)) {
            foreach(var item in list) {
              result.Add((T)(object)ReadString(sec , item , filepath , default(string)));
            }
          }else
            return def;
        }
        return result;
      }
      #endregion
      #region IDictionary
      public static Dictionary<string , V> ReadDictionary<V>(string section , string key , string filepath , Dictionary<string , V> def) {
        string sec = section + separator + key;
        var list = GetKeys(sec , filepath);
        if(list.Count == 0)
          return def;
        Dictionary<string , V> result = new Dictionary<string , V>(list.Capacity);
        if(typeof(V).IsValueType) {
          foreach(var item in list)
            result.Add(item , ReadNumber<V>(sec , item , filepath , default(V)));
        } else {
          //TODO 参照型
          return def;
        }
        return result;
      }
      #endregion
      #endregion
      #region 配列型
      public static T[] ReadArray<T>(string section , string key , string filepath , T[] def) {
        IList<T> result = ReadList<T>(section , key , filepath , def);
        var res = new T[result.Count];
        int i = 0;
        foreach(var item in result) {
          res[i++] = item;
        }
        return res;
      }
      #endregion

      public static T Read<T>(string section , string filepath) {
        T ret = (T)Activator.CreateInstance(typeof(T));

        foreach(var n in typeof(T).GetFields()) {
          Type t = n.FieldType;
          object[] invoke_param = new object[] { section , n.Name , filepath , n.GetValue(ret) };
          if(t.IsValueType) {
            // 値型
            n.SetValue(ret , ReadNumber(t , section , n.Name , filepath , n.GetValue(ret)));
          } else if(t == typeof(string)) {
            n.SetValue(ret , ReadString(section , n.Name , filepath , (string)n.GetValue(ret)));
          } else if(t.IsArray) {
            // 配列型
            var generic_ra = typeof(Ini).GetMethod(nameof(Ini.ReadArray));
            var ra = generic_ra.MakeGenericMethod(t.GetElementType());
            n.SetValue(ret , ra.Invoke(null , invoke_param));
          } else if(t.IsGenericType) {
            //Generic型
            Type genericType = t.GetGenericTypeDefinition();
            if(genericType == typeof(IList<>)) {
              var generic = typeof(Ini).GetMethod(nameof(ReadList));
              var m = generic.MakeGenericMethod(t.GetGenericArguments()[0]);
              n.SetValue(ret , m.Invoke(null , invoke_param));
            } else if(genericType == typeof(Dictionary<,>)) {
              var generic = typeof(Ini).GetMethod(nameof(ReadDictionary));
              var m = generic.MakeGenericMethod(t.GetGenericArguments()[1]);
              n.SetValue(ret , m.Invoke(null , invoke_param));
            }
          } else {
            var sb = new StringBuilder(2048);
            SafeNativeMethods.GetPrivateProfileString(section , n.Name , n.GetValue(ret).ToString() , sb , (uint)sb.Capacity , Path.GetFullPath(filepath));
            n.SetValue(ret , sb.ToString());
          }
        }

        var postd = typeof(T).GetMethod("OnPostDeserialize");
        if(postd != null)
          postd.Invoke(ret , null);

        return ret;
      }
      public static void Write<T>(string section , T data , string filepath) {
        var pres = typeof(T).GetMethod("OnPreSerialize");
        if(pres != null)
          pres.Invoke(data , null);
        foreach(var n in typeof(T).GetFields()) {
          Type t = n.FieldType;
          if(t.IsValueType) {
            Type[] p = new Type[2] { typeof(string) , t.MakeByRefType() };
            if(t.GetMethod("TryParse" , p) != null || t.IsEnum) {
              SafeNativeMethods.WritePrivateProfileString(section , n.Name , n.GetValue(data).ToString() , filepath);
            } else {
              //ユーザー定義構造体
            }
          } else if(t == typeof(string)) {
            SafeNativeMethods.WritePrivateProfileString(section , n.Name , (string)n.GetValue(data) , filepath);
          } else if(t.IsArray) {
            Array x = (Array)n.GetValue(data);
            string sec = section + separator + n.Name;
            int i = 0;
            foreach(var item in x) {
              SafeNativeMethods.WritePrivateProfileString(sec , i.ToString() , x.GetValue(i).ToString() , filepath);
              i++;
            }
          } else if(t.IsGenericType) {
            Type genericType = t.GetGenericTypeDefinition();
            if(genericType == typeof(Dictionary<,>)) {

            }
          }
        }
      }
    }
    public static T ReadConfig<T>(string section,string filename) where T : new() {
      string path = Path.Combine(DataPath , filename);
      return Ini.Read<T>(section , path);
    }
    public static void SaveConfig<T>(string section ,string filename , T data) {
      string path = Path.Combine(DataPath , filename);
      Ini.Write(section , data , path);
    }
    #endregion
  }
#if NOUNITY
  public abstract class ExPluginBase
#elif NOUNITYINJECTOR
  public abstract class ExPluginBase : UnityEngine.MonoBehaviour
#else
  // abstract class まで自動登録しようとするのは流石にバグだと思うんだよなぁ
  // ファイルに使えない文字列突っ込んで回避
  [UnityInjector.Attributes.PluginFilter("|NoNeedAutoLoad|")]
  public abstract class ExPluginBase : UnityInjector.PluginBase
#endif
  {
#if NOUNITYINJECTOR || NOUNITY
    /// <summary>
    /// クラス名の取得
    /// </summary>
    protected string Name { get { return GetType().Name; } }
    /// <summary>
    /// 設定などの保管場所の取得
    /// </summary>
    protected static string DataPath { get; } = SharedConfig.DataPath;
#else
    /// <summary>
    /// 設定などの保管場所の取得
    /// </summary>
    protected static new string DataPath { get; } = SharedConfig.DataPath;
#endif

    #region Config
    /// <summary>
    /// 設定値をiniから読み込む
    /// </summary>
    /// <typeparam name="T">受け取る型</typeparam>
    /// <param name="section">[OPTION]Rootセクション名</param>
    /// <returns></returns>
    public T ReadConfig<T>(string section = "Config") where T : new() {
      return SharedConfig.ReadConfig<T>("Config" ,Name + ".ini");
    }

    /// <summary>
    /// 設定値をiniへ書き込む
    /// </summary>
    /// <typeparam name="T">書き込む型</typeparam>
    /// <param name="data">書き込むデータ</param>
    /// <param name="section">[OPTION]Rootセクション名</param>
    public void SaveConfig<T>(T data,string section = "Config") {
      SharedConfig.SaveConfig("Config",Name + ".ini" , data);
    }
    #endregion

    #region Console
    /// <summary>
    /// コンソールに文字列出力
    /// Console.WriteLineを使うとUnityがトレースログ吐く度に文字色が黒になるので対策
    /// </summary>
    /// <param name="fmt">フォーマット文字列</param>
    /// <param name="arg">[option]引数,...</param>
    public static void WriteLine(string fmt , params object[] arg) {
#if NOUNITYINJECTOR || NOUNITY
      if(SharedConfig.ChangeConsoleColor)
        Console.ForegroundColor = ConsoleColor.Gray;
#else
      UnityInjector.ConsoleUtil.SafeConsole.ForegroundColor = ConsoleColor.Gray;
#endif
      if(arg.Length == 0) {
        Console.WriteLine(fmt);
        return;
      }
      Console.WriteLine(fmt , arg);
    }

    /// <summary>
    /// DEBUGビルド時のみコンソールに文字列出力
    /// Console.WriteLineを使うとUnityがトレースログ吐く度に文字色が黒になるので対策
    /// </summary>
    /// <param name="fmt">フォーマット文字列</param>
    /// <param name="arg">[option]引数,...</param>
    [Conditional("DEBUG")]
    public static void DebugWriteLine(string fmt , params object[] arg) {
      WriteLine(fmt , arg);
    }

    /// <summary>
    /// CallTree付きログをoutput_log.txtに出力する
    /// </summary>
    /// <param name="fmt">書式指定文字列</param>
    /// <param name="arg">[OPTION]引数,...</param>
    public static void LogWithCallTree(string fmt , params object[] arg) {
      string message = String.Format(fmt , arg);
      UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// DEBUGビルド時のみCallTree付きログをoutput_log.txtに出力する
    /// </summary>
    /// <param name="fmt">書式指定文字列</param>
    /// <param name="arg">[OPTION]引数,...</param>
    [Conditional("DEBUG")]
    public static void DebugLogWithCallTree(string fmt , params object[] arg) {
      string message = String.Format(fmt , arg);
      UnityEngine.Debug.Log(message);
    }
    #endregion
  }
}
