namespace UnityEngine {
  public static class Mathf {
    public const float Deg2Rad = 0.0174532924f;
    public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v;
    public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v;
    public static float Clamp01(float v)=>Clamp(v,0f,1f);
    public static int FloorToInt(float v)=>(int)System.Math.Floor(v);
    public static int Max(int a,int b)=>a>b?a:b; public static float Max(float a,float b)=>a>b?a:b;
    public static int Min(int a,int b)=>a<b?a:b; public static float Min(float a,float b)=>a<b?a:b;
  }
  public static class Debug { public static void LogWarning(object o){ System.Console.WriteLine("WARN "+o);} public static void Log(object o){System.Console.WriteLine(o);} }
  public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
  public class TextAsset { public string text; }
  public static class Resources { public static T Load<T>(string p) where T:class => null; }
  public class SerializeFieldAttribute : System.Attribute {}
  public class TooltipAttribute : System.Attribute { public TooltipAttribute(string s){} }
  public class HeaderAttribute : System.Attribute { public HeaderAttribute(string s){} }
}
namespace Ursine.Text { public static class Loc { public static string T(string k, params object[] a)=>k; public static System.Collections.Generic.List<string> Lines(string k)=>new System.Collections.Generic.List<string>{k}; public static void Load(string j){} } }
