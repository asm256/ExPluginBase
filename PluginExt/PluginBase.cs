/* ExPluginBase
 * Pluginを楽に作るための抽象クラス
 * MIT License
 * Copyright © asm__ 2015
 */
// テスト用にMonoBehaviourを継承しない
//#define NOUNITY
// 構想中
//#define NOUNITYINJECTOR
#if !NOUNITY
#define CM3D2
#endif
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
      public class IniFile {
        public string File { get; }
        public List<string> Sections { get; }
        public IniFile(string file) {
          File = file;
          var sec = new List<string>();
          IntPtr ptr = IntPtr.Zero;
          try {
            int nSize = 1024;
            int ns;
            do {
              nSize *= 2;
              if(ptr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(ptr);
              ptr = Marshal.AllocCoTaskMem(nSize * 2);
              ns = SafeNativeMethods.GetPrivateProfileSectionNames(ptr , nSize , File);
            } while(ns == nSize - 2);
            string[] sections = Marshal.PtrToStringAuto(ptr,ns).TrimEnd('\0').Split('\0');
            sec.AddRange(sections);
            Sections = sec;
          } finally {
            if(ptr != IntPtr.Zero)
              Marshal.FreeCoTaskMem(ptr);
          }
        }
        public List<string> GetKeyAndSections(string section) {
          var parent = section + separator;
          var len = parent.Length;
          var res = Ini.GetKeys(section , File);
          foreach(var s in Sections.FindAll((s) => s.StartsWith(parent) && s.LastIndexOf(separator) <= len))
            res.Add(s.Substring(len));
          return res;
        }
        public Dictionary<string,T> ReadDictionary<T>(string section , string key , Dictionary<string,T> def) {
          var sec = section + separator + key;
          var keys = GetKeyAndSections(sec);
          if(keys.Count == 0)
            return def;
          Dictionary<string , T> res = new Dictionary<string , T>(keys.Count);
          foreach(var item in keys) {
            T val = ReadObject<T>(sec , item , default(T));
            res.Add(item , val);
          }
          return res;
        }
        public T[] ReadArray<T>(string section , string key , T[] def) {
          var defList = def == null ? new List<T>() : new List<T>(def.Length);
          if(def != null)
            defList.AddRange(def);
          var resList = ReadList(section , key , defList);
          return resList.ToArray();
        }
        public List<T> ReadList<T>(string section , string key , List<T> def) {
          var sec = section + separator + key;
          int t;
          int len = sec.Length;
          var keys = GetKeyAndSections(sec).FindAll((s) => Int32.TryParse(s , out t));
          keys.Sort(new NaturalStringComparer());
          if(keys.Count == 0)
            return def;
          var res = new List<T>(keys.Count);
          foreach(var item in keys) {
            T val = ReadObject<T>(sec , item , default(T));
            res.Add(val);
          }
          return res;
        }
        public object ReadObject(Type t,string section,string key,object def) {
          string _default = String.IsNullOrEmpty(def as string) ? "" : def.ToString();
          string s = ReadString(section , key , File , _default);
          if(t == typeof(string)) {
            return s;
          } else if(t.IsEnum) {
            try {
              return Enum.Parse(t , s);
            } catch {
              return def;
            }
          }
          Type[] p = new Type[2] { typeof(string) , t.MakeByRefType() };
          var methodTryParse = t.GetMethod("TryParse" , p);
          if(methodTryParse != null) {
            var _a = new object[] { s , def };
            if((bool)methodTryParse.Invoke(null , _a))
              return _a[1];
            return def;
          }
          object[] invoke_param = new object[] { section , key  , def };
          if(t.IsArray) {
            // 配列型
            var generic_ra = typeof(IniFile).GetMethod(nameof(IniFile.ReadArray));
            var ra = generic_ra.MakeGenericMethod(t.GetElementType());
            return ra.Invoke(this , invoke_param);
          } else if(t.IsGenericType) {
            //Generic型
            Type genericType = t.GetGenericTypeDefinition();
            if(genericType == typeof(Dictionary<,>)) {
              // Dictionary<string,Value>
              var generic = typeof(IniFile).GetMethod(nameof(IniFile.ReadDictionary));
              var m = generic.MakeGenericMethod(t.GetGenericArguments()[1]);
              return m.Invoke(this , invoke_param);
            } else if(genericType == typeof(List<>)) {
              // List<Value>
              var generic = typeof(IniFile).GetMethod(nameof(IniFile.ReadList));
              var m = generic.MakeGenericMethod(t.GetGenericArguments()[0]);
              return m.Invoke(this , invoke_param);
            }
          }
          throw new NotImplementedException();
        }
        public T ReadObject<T>(string section , string key,T def) {
          return (T) ReadObject(typeof(T) , section , key,def);
        }
      }
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
      #region 読み込み
      public static object ReadObject(Type t , string section , string key , string filepath , object def) {
        string _default = String.IsNullOrEmpty(def as string) ? "" : def.ToString();
        string s = ReadString(section , key , filepath , _default);
        if(t == typeof(string)) {
          return s;
        } else if(t.IsEnum) {
          try {
            return Enum.Parse(t , s);
          } catch {
            return def;
          }
        }
        Type[] p = new Type[2] { typeof(string) , t.MakeByRefType() };
        var methodTryParse = t.GetMethod("TryParse" , p);
        if(methodTryParse != null) {
          var _a = new object[] { s , def };
          if((bool)methodTryParse.Invoke(null , _a))
            return _a[1];
          return def;
        }
        object[] invoke_param = new object[] { section , key , filepath , def };
        if(t.IsArray) {
          // 配列型
          var generic_ra = typeof(Ini).GetMethod(nameof(Ini.ReadArray));
          var ra = generic_ra.MakeGenericMethod(t.GetElementType());
          return ra.Invoke(null , invoke_param);
        }else if(t.IsGenericType) {
          //Generic型
          Type genericType = t.GetGenericTypeDefinition();
          if(genericType == typeof(Dictionary<,>)) {
            // Dictionary<string,Value>
            var generic = typeof(Ini).GetMethod(nameof(ReadDictionary));
            var m = generic.MakeGenericMethod(t.GetGenericArguments()[1]);
            return m.Invoke(null , invoke_param);
          }else if(genericType == typeof(List<>)) {
            // List<Value>
            var generic = typeof(Ini).GetMethod(nameof(ReadList));
            var m = generic.MakeGenericMethod(t.GetGenericArguments()[0]);
            return m.Invoke(null , invoke_param);
          }
        }
        throw new NotImplementedException();
      }
      public static T ReadObject<T>(string section , string key , string filepath , T def) {
        return (T)ReadObject(typeof(T) , section , key , filepath , def);
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
      #region  List
      public static List<T> ReadList<T>(string section , string key , string filepath , List<T> def) {
        int t;
        string sec = section + separator + key;
        var list = GetKeys(sec , filepath).FindAll((s) => Int32.TryParse(s , out t));
        list.Sort(new NaturalStringComparer());
        if(list.Count == 0)
          return def;

        List<T> result = new List<T>(list.Capacity);
        foreach(var item in list) {
          result.Add(ReadObject<T>(sec , item , filepath , default(T)));
        }
        return result;
      }

      public static void WriteList<T>(List<T> data,string section,string key,string filepath) {
        var sec = section + separator + key;
        int i = 0;
        foreach(var item in data) {
          WriteObject<T>(item , sec , i.ToString() , filepath);
        }
      }
      #endregion
      #region Dictionary
      public static Dictionary<string , V> ReadDictionary<V>(string section , string key , string filepath , Dictionary<string , V> def) {
        string sec = section + separator + key;
        var list = GetKeys(sec , filepath);
        if(list.Count == 0)
          return def;
        Dictionary<string , V> result = new Dictionary<string , V>(list.Capacity);
        foreach(var item in list)
          result.Add(item , ReadObject<V>(sec , item , filepath , default(V)));
        return result;
      }
      public static void WriteDictionary<T>(Dictionary<string , T> data , string section , string key , string filepath) {
        var sec = section + separator + key;
        foreach(var item in data) {
          WriteObject<T>(item.Value , sec , item.Key , filepath);
        }
      }
      #endregion
      #endregion
      #region 配列型
      public static T[] ReadArray<T>(string section , string key , string filepath , T[] def) {
        List<T> _def = new List<T>(def.Length);
        _def.AddRange(def);
        List<T> result = ReadList<T>(section , key , filepath , _def);
        return result.ToArray();
      }
      #endregion
      public static void WriteObject(Type t , object data , string section , string key , string filepath) {
        Type[] p = new Type[2] { typeof(string) , t.MakeByRefType() };
        if(t.GetMethod("TryParse" , p) != null || t.IsEnum) {
          SafeNativeMethods.WritePrivateProfileString(section , key , data.ToString() , filepath);
        } else if(t == typeof(string)) {
          SafeNativeMethods.WritePrivateProfileString(section , key , (string)data , filepath);
        } else if(t.IsArray) {
          Array x = (Array)data;
          string sec = section + separator + key;
          int i = 0;
          foreach(var item in x) {
            WriteObject(t.GetElementType() , item , sec , i.ToString() , filepath);
            i++;
          }
        } else if(t.IsGenericType) {
          Type genericType = t.GetGenericTypeDefinition();
          var invoke_param = new object[] { data , section , key , filepath };
          if(genericType == typeof(Dictionary<,>)) {
            var generic_write = typeof(Ini).GetMethod(nameof(WriteDictionary));
            var write = generic_write.MakeGenericMethod(t.GetGenericArguments()[1]);
            write.Invoke(null , invoke_param);
          } else if(genericType == typeof(List<>)) {
            var generic_write = typeof(Ini).GetMethod(nameof(WriteList));
            var write = generic_write.MakeGenericMethod(t.GetGenericArguments()[0]);
            write.Invoke(null , invoke_param);
          }
        } else
          throw new NotImplementedException();
      }
      public static void WriteObject<T>(T data , string section , string key , string filepath) {
        WriteObject(typeof(T) , data , section , key , filepath);
      }


      public static T Read<T>(string section , string filepath) {
        T ret = (T)Activator.CreateInstance(typeof(T));
        IniFile ini = new IniFile(filepath);

        foreach(var n in typeof(T).GetFields()) {
          Type t = n.FieldType;
          n.SetValue(ret , ini.ReadObject(t , section , n.Name  , n.GetValue(ret)));
        }

        var postd = typeof(T).GetMethod("OnPostDeserialize");
        if(postd != null)
          postd.Invoke(ret , null);

        return ret;
      }
      public static void Write<T>(string section , T data , string filepath) {
        var pres = typeof(T).GetMethod("OnPreSerialize");
        Type[] p = new Type[2] { typeof(string) , typeof(T).MakeByRefType() };
        if(pres != null)
          pres.Invoke(data , null);
        foreach(var n in typeof(T).GetFields()) {
          WriteObject(n.FieldType , n.GetValue(data) , section , n.Name , filepath);
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
    /// <param name="section">Rootセクション名</param>
    /// <returns></returns>
    public T ReadConfig<T>(string section) where T : new() {
      return SharedConfig.ReadConfig<T>("Config" ,Name + ".ini");
    }

    /// <summary>
    /// 設定値をiniから読み込む
    /// </summary>
    /// <typeparam name="T">受け取る型</typeparam>
    /// <returns></returns>
    public T ReadConfig<T>() where T : new() {
      return ReadConfig<T>("Config");
    }

    /// <summary>
    /// 設定値をiniへ書き込む
    /// </summary>
    /// <typeparam name="T">書き込む型</typeparam>
    /// <param name="data">書き込むデータ</param>
    /// <param name="section">Rootセクション名</param>
    public void SaveConfig<T>(T data,string section) {
      SharedConfig.SaveConfig("Config",Name + ".ini" , data);
    }

    /// <summary>
    /// 設定値をiniへ書き込む
    /// </summary>
    /// <typeparam name="T">書き込む型</typeparam>
    /// <param name="data">書き込むデータ</param>
    public void SaveConfig<T>(T data) {
      SaveConfig<T>(data , "Config");
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

#if !NOUNITY
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
#endif
#if CM3D2
    /// <summary>
    /// ゲーム内ダイアログを開く
    /// </summary>
    /// <param name="f_strMsg">表示するメッセージ</param>
    /// <param name="f_eType">ダイアログの種類</param>
    /// <param name="f_dgOk">OK/YES時の処理(不要時はnull)</param>
    /// <param name="f_dgCancel">CANCEL/NO時の処理(不要時はnull)</param>
    public static void ShowDialog(string f_strMsg , SystemDialog.TYPE f_eType , SystemDialog.OnClick f_dgOk = null , SystemDialog.OnClick f_dgCancel = null) {
      GameMain.Instance.SysDlg.Show(f_strMsg , f_eType , f_dgOk , f_dgCancel);
    }
#endif
    #endregion
  }
}
