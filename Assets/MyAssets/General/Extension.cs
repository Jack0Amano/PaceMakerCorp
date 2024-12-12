using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using TMPro;
using DG.Tweening;
using System.Linq.Expressions;
using System.Diagnostics;
using static UnityEngine.Debug;
using static Utility;
using System.Runtime.CompilerServices;
using UnityEngine.VFX;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


#if UNITY_EDITOR
using UnityEditor;
#endif

static class Utility
{

    /// <summary>
    /// 確率判定
    /// </summary>
    /// <param name="fPercent">確率 (0~1)</param>
    /// <returns>当選結果 [true]当選</returns>
    public static bool Probability(float fPercent)
    {
        float fProbabilityRate = UnityEngine.Random.value;

        if (fPercent == 1f && fProbabilityRate == fPercent)
        {
            return true;
        }
        else if (fProbabilityRate < fPercent)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// UUIDを作成する
    /// </summary>
    /// <returns></returns>
    public static string GetUUID()
    {
        Guid guidValue = Guid.NewGuid();
        return guidValue.ToString();
    }

    /// <summary>
    /// 複数個の要素をPrintする
    /// </summary>
    /// <param name="args"></param>
    public static void Print(params object[] args)
    {
        Log(string.Join("  ", args) + "\n");
    }

    ///// <summary>
    ///// 呼び出しメソッド名をLogに表示する
    ///// </summary>
    ///// <param name="callerMethodName"></param>
    //public static void Print([CallerMemberName] string callerMethodName = "")
    //{
    //    Log($"Called from: {callerMethodName}");
    //}

    /// <summary>
    /// DebugにWarningを出力する
    /// </summary>
    /// <param name="args"></param>
    public static void PrintWarning(params object[] args)
    {
        // StackFrameクラスをインスタンス化する
        int nFrame = 1; // フレーム数(1なら直接呼び出したメソッド)
        StackFrame objStackFrame = new StackFrame(nFrame);
        // 呼び出し元のメソッド名を取得する
        string strMethodName = objStackFrame.GetMethod().Name;
        // 呼び出し元のクラス名を取得する
        string strClassName = objStackFrame.GetMethod().ReflectedType.FullName;

        Print("Warning: ", strClassName, strMethodName, " : ", string.Join("  ", args));
    }

    /// <summary>
    /// Debug出力にErrorを出力する
    /// </summary>
    /// <param name="args"></param>
    public static void PrintError(params object[] args)
    {
        Unity.Logging.Log.Error(string.Join(" ", args));
        //LogError(string.Join("  ", args));
    }

    /// <summary>
    /// 要素の中で最大の物を取得する
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static T Max<T>(params T[] args)
    {
        return (T)args.Max();
    }

    /// <summary>
    /// 要素の中で最小のものを取得する
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static T Min<T>(params T[] args)
    {
        return (T)args.Min();
    }


    /// <summary>
    /// Elementsの中からOddsに即した確率で要素を取り出す ** Elements.Count == Odds.Count **
    /// </summary>
    /// <param name="elements">選択される要素郡</param>
    /// <param name="odds">要素郡のそれぞれの選ばれる確率</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T ChooseOne<T>(List<T> elements, List<float> odds)
    {
        if (elements.Count != odds.Count)
        {
            PrintError("Count of elements and odds must be same.");
            return elements.First();
        }

        if (elements.Count == 1)
        {
            return elements[0];
        }

        float num = 0;
        var _odds = odds.ConvertAll(n =>
        {
            var elem = num + n;
            num = n;
            return elem;
        });

        var selection = UnityEngine.Random.Range(0, _odds.Last());
        var selectedIndex = _odds.FindIndex(a => a > selection);
        return elements[selectedIndex];
    }

    /// <summary>
    /// ２つのベクトルのなす角を計算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float RadianOfTwoVector(Vector2 a, Vector2 b)
    {
        var child = a.x * b.x + a.y * b.y;
        var base1 = Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2);
        var base2 = Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2);
        var _base = Mathf.Sqrt(base1) * Mathf.Sqrt(base2);
        return Mathf.Acos(child / _base);
    }

    /// <summary>
    /// <c>origin</c>を原点としてWorld座標での<c>target</c>方向への角度
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float Angle_bug(Vector3 origin, Vector3 target)
    {
        var r = Mathf.Atan2(target.z - origin.z, target.x - origin.x);
        if (r < 0)
        {
            r += 2 * Mathf.PI;
        }
        return Mathf.Floor(r * 360 / (2 * Mathf.PI));
    }


    /// <summary>
    /// <c>origin</c>を原点としてWorld座標での<c>target</c>方向への角度 Z軸がY X軸がX
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float Angle(Vector3 origin, Vector3 target)
    {
        var r = Mathf.Atan2(target.x - origin.x, target.z - origin.z);
        if (r < 0)
        {
            r += 2 * Mathf.PI;
        }
        return Mathf.Floor(r * 360 / (2 * Mathf.PI));
    }

    /// <summary>
    /// 関数名
    /// </summary>
    public static string FuncName([CallerMemberName] string callerName = "")
    {
        return callerName;
    }

}

#region Get Min and Max
public static class IEnumerableExtensions
{
    /// <summary>
    /// 最小値を持つ要素を返します
    /// </summary>
    public static TSource FindMin<TSource, TResult>
    (
        this IEnumerable<TSource> self,
        Func<TSource, TResult> selector
    )
    {
        return self.First(c => selector(c).Equals(self.Min(selector)));
    }

    /// <summary>
    /// 最大値を持つ要素を返します
    /// </summary>
    public static TSource FindMax<TSource, TResult>
    (
        this IEnumerable<TSource> self,
        Func<TSource, TResult> selector
    )
    {
        return self.First(c => selector(c).Equals(self.Max(selector)));
    }
}
#endregion


public static class ListExtensions
{
    public static T get<T>(this IList<T> list, int index)
    {
        if (index < 0)
        {
            index += list.Count;
        }
        return list[index];
    }
    public static void set<T>(this IList<T> list, int index, T value)
    {
        if (index < 0)
        {
            index += list.Count;
        }
        list[index] = value;
    }

    /// <summary>
    /// 配列をスライスして取得
    /// </summary>
    /// <param name="list"></param>
    /// <param name="begin">Begin index</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> Slice<T>(this List<T> list, int begin)
    {
        return Slice(list, begin, list.Count-1);
    }

    /// <summary>
    /// 配列をスライスして取得
    /// </summary>
    /// <param name="list"></param>
    /// <param name="begin">Begin index</param>
    /// <param name="end">End index</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> Slice<T>(this List<T> list, int begin, int end)
    {
        if (begin < 0)
        {
            begin += list.Count;
        }
        if (end < 0)
        {
            end += list.Count;
        }
        var length = Math.Max(0, end - begin + 1);

        // endがlistからオーバーしている場合
        if (begin + length > list.Count)
            length = list.Count - begin;

        return list.GetRange(begin, length);
    }

    /// <summary>
    /// 与えられた要素を削除した新たなListを返す
    /// </summary>
    /// <param name="l"></param>
    /// <param name="arg"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> Removed<T>(this List<T> l, T arg)
    {
        var output = new List<T>(l);
        output.Remove(arg);
        return output;
    }

    /// <summary>
    /// List内容の可視化
    /// </summary>
    /// <param name="list"></param>
    /// <param name="toStr"></param>
    /// <param name="format"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string ToString<T>(this List<T> list, Func<T, string> toStr = null, string format = "{0},")
    {
        if (list.Count == 0)
            return "Empty";

        //default
        if (toStr == null) toStr = (t) => t.ToString();

        var strs = (from p in list select (toStr(p))).ToArray();
        var sb = new StringBuilder("[");
        foreach (var str in strs)
        {
            sb.AppendFormat(format, str);
        }
        sb.AppendLine("]");
        return sb.ToString();
    }

    /// <summary>
    /// ロバストなListのIndex index<0の時の巻き戻しは行わないため削除予定
    /// </summary>
    public static bool IndexAt_Bug<T>(this List<T> list, int index, out T output)
    {
        output = default;

        // Listのindexが小さすぎて存在しない
        if ((list.Count + index) < 0)
            return false;

        // Listのindexが大きすぎて存在しない
        if (list.Count <= index)
            return false;


        if (index < 0)
            output = list[list.Count + index];
        else
            output = list[index];

        return true;

    }

    /// <summary>
    /// ロバストなListのIndex
    /// </summary>
    public static bool IndexAt<T>(this List<T> list, int index, out T output)
    {
        output = default;
        if (index < 0)
            return false;
        // Listのindexが大きすぎて存在しない
        if (list.Count <= index)
            return false;

        output = list[index];

        return true;
    }

    /// <summary>
    /// ランダムに要素を一つ取り出す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool ChooseRandom<T>(this List<T> list, out T output)
    {
        output = default(T);

        if (list.Count == 0)
            return false;
        if (list.Count == 1)
        {
            output = list[0];
        }
        else
        {
            var rand = new System.Random();
            var index = rand.Next(0, list.Count - 1);
            output = list[index];
        }
        return true;
    }

    /// <summary>
    /// ランダムに並び替え
    /// </summary>
    public static List<T> Shuffle<T>(this List<T> list)
    {

        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        return list;
    }

    /// <summary>
    /// Nullチェックを含めたList.Find
    /// </summary>
    public static bool TryFindFirst<T>(this IEnumerable<T> seq, Func<T, bool> filter, out T result)
    {
        result = default(T);
        foreach (var item in seq)
        {
            if (filter(item))
            {
                result = item;
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// <c>List<float></c>を最大値で正規化
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<float> Normalize(this IEnumerable<float> list)
    {
        var max = list.Max();
        return list.ToList().ConvertAll(e => e / max);
    }

    /// <summary>
    /// Corutineが<c>corutineReturn</c>を返したときにCoroutineが終了したとみなす
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="c"></param>
    /// <param name="corutineReturn"></param>
    /// <returns></returns>
    public static bool IsNotCompleted<T>(this IEnumerator c, T corutineReturn)
    {
        return !(c.Current is T b && b.Equals(corutineReturn));
    }

    /// <summary>
    /// Corutineが<c>corutineReturn</c>を返したときにCoroutineが終了したとみなす
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="corutineReturn"></param>
    /// <returns></returns>
    public static bool AreNotCompleted<T>(this List<IEnumerator> list, T corutineReturn)
    {
        return list.Find(c => c.IsNotCompleted(corutineReturn)) != null;
    }

    /// <summary>
    /// Listの最終Index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static int LastIndex<T>(this List<T> list)
    {
        return list.Count - 1;
    }

    /// <summary>
    /// 反転したリストを返す
    /// </summary>
    public static List<T> Reversed<T>(this List<T> list)
    {
        var output = new List<T>(list);
        output.Reverse();
        return output;
    }


}


public static partial class TupleEnumerable
{
    /// <summary>
    /// itemとindexのTuple形式のListに変換する
    /// </summary>
    public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        IEnumerable<(T item, int index)> impl()
        {
            var i = 0;
            foreach (var item in source)
            {
                yield return (item, i);
                ++i;
            }
        }

        return impl();
    }
}


public static class FloatExtensions
{

    /// <summary>
    /// 小数点dimension以下の数を切り捨てる
    /// </summary>
    /// <param name="dimension"></param>
    /// <returns></returns>
    public static float Floor(this float f, int dimension)
    {
        var _dimension = Mathf.Pow(10f, dimension);
        var s = f * _dimension;
        s = Mathf.Floor(s);
        return s / _dimension;
    }

    /// <summary>
    /// 値がRangeの範囲内にあるか判定する
    /// </summary>
    /// <param name="f"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool In(this float f, RangeAttribute range)
    {
        if (f < range.min || range.max < f)
            return false;
        else
            return true;
    }
}

public static class IntExtensions
{
    /// <summary>
    /// 値がRangeの範囲内にあるか判定する
    /// </summary>
    /// <param name="i"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool In(this int i, RangeAttribute range)
    {
        if (i < range.min || range.max < i)
            return false;
        else
            return true;
    }
}


public static class Vector2Extensions
{
    /// <summary>
    /// Vector2の各要素の合算を取得する
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float Sum(this Vector2 v)
    {
        return v.x + v.y;
    }

    /// <summary>
    /// Vector2の各要素の平均値を取得する
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float Mean(this Vector2 v)
    {
        return v.Sum() / 2;
    }
}


public static class HashSetExtensions
{
    /// <summary>
    /// HashSetにListを追加する
    /// </summary>
    /// <param name="list">追加するlist</param>
    /// <returns>重複なく追加できた数</returns>
    public static int AddRange<T>(this HashSet<T> value, List<T> list)
    {
        var addCount = 0;
        list.ForEach(l =>
        {
            if (value.Add(l))
                addCount++;
        });
        return addCount;
    }
}

public static class Vector3Extensions
{
    /// <summary>
    ///　ベクトルからスカラー量減算する
    /// </summary>    
    public static Vector3 Subtraction(this Vector3 v, float f)
    {
        return new Vector3(v.x - f, v.y - f, v.z - f);
    }

    /// <summary>
    /// ベクトルからスカラー量加算する
    /// </summary>
    public static Vector3 Add(this Vector3 v, float f)
    {
        return new Vector3(v.x + f, v.y + f, v.z + f);
    }

    /// <summary>
    ///　Vector3の要素ごとに割り算を実行する
    /// </summary>
    /// <param name="v"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static Vector3 Divide(this Vector3 v, Vector3 v2)
    {
        return new Vector3(v.x / v2.x, v.y / v2.y, v.z / v2.z);
    }

    /// <summary>
    /// Vector3の要素ごとに掛け算を実行する
    /// </summary>
    /// <param name="v"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static Vector3 Multiply(this Vector3 v, Vector3 v2)
    {
        return new Vector3(v.x * v2.x, v.y * v2.y, v.z * v2.z);
    }

    public static void Set(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        v.Set(x != null ? (float)x : v.x,
              y != null ? (float)y : v.y,
              z != null ? (float)z : v.z);
        
    }

    public static Vector2 xy(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.y);
    }


    public static Vector2 xz(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector2 zy(this Vector3 vector3)
    {
        return new Vector2(vector3.z, vector3.y);
    }

    /// <summary>
    /// Vector3をディープコピーしてZ軸を0にして返す
    /// </summary>
    public static Vector3 ZeroZ(this Vector3 vector3)
    {
        var output = vector3;
        output.z = 0;
        return output;
    }

    public static Color ToColor(this Vector3 v)
    {
        return new Color(v.x, v.y, v.z);
    }
}

public static class Vector4Extensions
{
    public static Color ToColor(this Vector4 v)
    {
        return new Color(v.x, v.y, v.z, v.w);
    }
}

public static class DictionaryExtensions
{
    /// <summary>
    /// 値を取得、keyがなければデフォルト値を設定し、デフォルト値を取得
    /// </summary>
    public static TV SafeAccess<TK, TV>(this Dictionary<TK, TV> dic, TK key, TV defaultValue = default(TV))
    {
        TV result;
        return dic.TryGetValue(key, out result) ? result : defaultValue;
    }

    /// <summary>
    /// Dictionary型のValueを指定してKeyの取得を試みる
    /// </summary>
    /// <param name="value">検索するVlaue</param>
    /// <param name="key">結果となるKey</param>
    /// <returns>検索の合否</returns>
    public static bool TryGetFirstKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value, out TKey key)
    {
        foreach (var pair in dictionary)
        {
            if (pair.Value == null)
                continue;

            if (pair.Value.Equals(value))
            {
                key = pair.Key;
                return true;
            }
        }

        key = default;
        return false;
    }

    public static string ToOpenedString<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToList().ToString<KeyValuePair<TKey, TValue>>();
    }
}

public static class TransformExtensions
{
    /// <summary>
    /// 拒否するような横に揺らすアニメーション
    /// </summary>
    public static Sequence DOShakeX(this Transform transform, float strength = 3, float decrease = 0.4f)
    {
        var previousX = transform.localPosition.x;
        var seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveX(transform.localPosition.x + strength, 0.1f));

        var moveXAbs = strength*2;
        var moveDirection = -1f;
        while(moveXAbs > 0.05f)
        {
            seq.Append(transform.DOLocalMoveX(transform.localPosition.x +  moveXAbs * moveDirection, 0.1f));
            moveDirection *= -1;
            moveXAbs -= decrease;
        }
        seq.Append(transform.DOLocalMoveX(previousX, 0.05f));
        return seq.Play();
    }
}

#region Read and Write binary
static class Binary
{
    /// <summary>
    /// オブジェクトの内容をファイルから読み込み復元する
    /// </summary>
    /// <param name="path">読み込むファイル名</param>
    /// <returns>復元されたオブジェクト</returns>
    public static object LoadFrom(string path)
    {
        FileStream fs = new FileStream(path,
            FileMode.Open,
            FileAccess.Read);
        BinaryFormatter f = new BinaryFormatter();
        //読み込んで逆シリアル化する
        object obj = f.Deserialize(fs);
        fs.Close();

        return obj;
    }

    /// <summary>
    /// オブジェクトの内容をファイルから読み込み復元する
    /// </summary>
    /// <param name="path">読み込むファイル名</param>
    /// <returns>復元されたオブジェクト</returns>
    public static T LoadFrom<T>(string path)
    {
        return (T)LoadFrom(path);
    }

    /// <summary>
    /// オブジェクトの内容をファイルに保存する
    /// </summary>
    /// <param name="obj">保存するオブジェクト</param>
    /// <param name="path">保存先のファイル名</param>
    public static void SaveTo(object obj, string path)
    {
        FileStream fs = new FileStream(path,
            FileMode.Create,
            FileAccess.Write);
        BinaryFormatter bf = new BinaryFormatter();
        //シリアル化して書き込む
        bf.Serialize(fs, obj);
        fs.Close();
    }
}
#endregion


#region  Serializable class
/// <summary>
/// シリアライズ可能でBinary Json保存に対応したVector3
/// </summary>
[Serializable]
public struct SerializableVector3
{
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float x;
    public float y;
    public float z;

    public static SerializableVector3 zero = new SerializableVector3() { x = 0, y = 0, z = 0 };

    internal Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    override public string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}

/// <summary>
/// シリアライズ可能でBinary Json保存に対応したQuaternion
/// </summary>
[Serializable]
public struct SerializableQuaternion
{
    public SerializableQuaternion(Quaternion rotation)
    {
        x = rotation.x;
        y = rotation.y;
        z = rotation.z;
        w = rotation.w;
    }

    public float x;
    public float y;
    public float z;
    public float w;

    public static SerializableQuaternion zero = new SerializableQuaternion() { x = 0, y = 0.7f, z = 0, w = 0.7f };

    internal Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ", " + w + ")";
    }
}

#endregion

public static class RectExtensions
{
    /// <summary>
    /// RectにWidthをsetする
    /// </summary>
    /// <param name="width"></param>
    public static void SetWidth(this Rect rect, float width)
    {
        rect.Set(rect.x, rect.y, width, rect.height);
    }
}

static class GameObjectExtensions
{
    /// <summary>
    /// GameObjectのChildrenをすべて削除する
    /// </summary>
    /// <param name="transform"></param>
    public static void RemoveAllChildren(this Transform transform)
    {
        var children = transform.GetChildren();
        children.ForEach(c => GameObject.DestroyImmediate(c));
    }

    /// <summary>
    /// 全てのChildObjectを取得する
    /// </summary>
    public static List<GameObject> GetChildren(this Transform t)
    {
        var output = new List<GameObject>();
        foreach(Transform child in t)
        {
            output.Add(child.gameObject);
        }
        return output;
    }

    /// <summary>
    /// 全てのChildObjectを取得する
    /// </summary>
    public static List<GameObject> GetChildren(this GameObject o)
    {
        var t = o.transform;
        var output = new List<GameObject>();
        foreach (Transform child in t)
        {
            output.Add(child.gameObject);
        }
        return output;
    }

    /// <summary>
    /// Layerがnameであるか判定する
    /// </summary>
    /// <param name="o"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool EqualLayer(this GameObject o, string name)
    {
        var layer = 1 << LayerMask.NameToLayer(name);
        return ((1 << o.layer) & layer) != 0;
    }
}

static class ColorExtensions
{
    public static Color Clone(this Color c)
    {
        return new Color(c.r, c.g, c.b, c.a);
    }

    public static Vector3 ToVector3(this Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }

    public static Vector4 ToVector4(this Color c)
    {
        return new Color(c.r, c.g, c.b, c.a);
    }

    #region Color用の拡張
    /// <summary>
    /// 0~256 の値でのcolor
    /// </summary>
    /// <param name="c"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Color Rgb256(int r, int g, int b)
    {
        return new Color(r / 256f, g / 256f, b / 256f);
    }

    /// <summary>
    /// HexからColorを作成
    /// </summary>
    /// <param name="c"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Color Hex(string value)
    {
        try
        {
            var c = new System.Drawing.ColorConverter();
            var _color = (System.Drawing.Color)c.ConvertFromString(value);
            var color = new Color(_color.R / 256f, _color.G / .256f, _color.B / 256f, _color.A / 256f);
            return new Color(_color.R / 256f, _color.G / 256f, _color.B / 256f, _color.A / 256f);
        }
        catch (FormatException ex)
        {
            UnityEngine.Debug.LogWarning(ex);
            return Color.white;
        }
    }

    #endregion
}


static class TextMeshProUGUIExtensions
{
    /// <summary>
    /// ToStringしてobjectをSetTextする
    /// </summary>
    /// <param name="text"></param>
    /// <param name="obj"></param>
    public static void SetText(this TextMeshProUGUI text, object obj)
    {
        text.SetText(obj.ToString());
    }

    /// <summary>
    /// floatの設定されているlabelにアニメーション付きで新たなvalueを設置する
    /// </summary>
    /// <param name="label"></param>
    /// <param name="value"></param>
    /// <param name="decimals"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static IEnumerator SetText(this TextMeshProUGUI label, float value, int decimals, float duration)
    {
        float oldValue;
        Print(label.text);
        if (label.text.Length == 0)
            oldValue = 0;
        else if (!float.TryParse(label.text, out oldValue))
            yield break;
        oldValue = (float)Math.Round(oldValue, decimals);
        value = (float)Math.Round(value, decimals);
        if (oldValue == value) yield break;
        var tick = 1f / MathF.Pow(10, decimals);
        var length = Mathf.Abs(oldValue - value) / tick;
        var count = 0;
        float currentValue;
        if (length > 100)
        {
            tick *= (length / 100);
            length = 100;
        }
        Print(tick, length, value, oldValue);
        while (true)
        {
            if (oldValue < value)
                currentValue = oldValue + (tick * count);
            else if (oldValue > value)
                currentValue = oldValue - (tick * count);
            else
                break;
            count++;
            if (MathF.Abs(currentValue - value) < tick)
                break;
            label.SetText(currentValue);
            yield return new WaitForSeconds(duration / length);
        }
        Print(value);
        label.SetText(value);
    }
}

#region Median
public static class LinQCustomMethods
{
    // メディアン算出メソッド（Generics）
    public static T Median<T>(this IEnumerable<T> src)
    {
        //ジェネリックの四則演算用クラス
        var ao = new ArithmeticOperation<T>();
        //昇順ソート
        var sorted = src.OrderBy(a => a).ToArray();
        if (!sorted.Any())
        {
            throw new InvalidOperationException("Cannot compute median for an empty set.");
        }
        int medianIndex = sorted.Length / 2;
        //要素数が偶数のとき、真ん中の2要素の平均を出力
        if (sorted.Length % 2 == 0)
        {
            //四則演算可能な時のみ算出
            if (ao.ArithmeticOperatable(typeof(T)))
            {
                return ao.Divide(ao.Add(sorted[medianIndex], sorted[medianIndex - 1]), (T)(object)2.0);
            }
            else throw new InvalidOperationException("Cannot compute arithmetic operation");
        }
        //奇数のときは、真ん中の値を出力
        else
        {
            return sorted[medianIndex];
        }
    }

    // メディアン算出（DateTime型のみ別メソッド）
    public static DateTime Median(this IEnumerable<DateTime> src)
    {
        //昇順ソート
        var sorted = src.OrderBy(a => a).ToArray();
        if (!sorted.Any())
        {
            throw new InvalidOperationException("Cannot compute median for an empty set.");
        }
        int medianIndex = sorted.Length / 2;
        //要素数が偶数のとき、真ん中の2要素の平均を出力
        if (sorted.Length % 2 == 0)
        {
            return sorted[medianIndex] + new TimeSpan((sorted[medianIndex - 1] - sorted[medianIndex]).Ticks / 2);
        }
        //奇数のときは、真ん中の値を出力
        else
        {
            return sorted[medianIndex];
        }
    }
}

//ジェネリック四則演算用クラス
public class ArithmeticOperation<T>
{
    /// <summary>
    /// 四則演算適用可能かを判定
    /// </summary>
    /// <param name="src">判定したいタイプ</param>
    /// <returns></returns>
    public bool ArithmeticOperatable(Type srcType)
    {
        //四則演算可能な型の一覧
        var availableT = new Type[]
        {
            typeof(int), typeof(uint), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(byte),
            typeof(decimal), typeof(double)
        };
        if (availableT.Contains(srcType)) return true;
        else return false;
    }

    /// <summary>
    /// 四則演算可能なクラスに対しての処理
    /// </summary>
    public ArithmeticOperation()
    {
        var availableT = new Type[]
        {
            typeof(int), typeof(uint), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(byte),
            typeof(decimal), typeof(double)
        };
        if (!availableT.Contains(typeof(T)))
        {
            throw new NotSupportedException();
        }
        var p1 = Expression.Parameter(typeof(T));
        var p2 = Expression.Parameter(typeof(T));
        Add = Expression.Lambda<Func<T, T, T>>(Expression.Add(p1, p2), p1, p2).Compile();
        Subtract = Expression.Lambda<Func<T, T, T>>(Expression.Subtract(p1, p2), p1, p2).Compile();
        Multiply = Expression.Lambda<Func<T, T, T>>(Expression.Multiply(p1, p2), p1, p2).Compile();
        Divide = Expression.Lambda<Func<T, T, T>>(Expression.Divide(p1, p2), p1, p2).Compile();
        Modulo = Expression.Lambda<Func<T, T, T>>(Expression.Modulo(p1, p2), p1, p2).Compile();
        Equal = Expression.Lambda<Func<T, T, bool>>(Expression.Equal(p1, p2), p1, p2).Compile();
        GreaterThan = Expression.Lambda<Func<T, T, bool>>(Expression.GreaterThan(p1, p2), p1, p2).Compile();
        GreaterThanOrEqual = Expression.Lambda<Func<T, T, bool>>(Expression.GreaterThanOrEqual(p1, p2), p1, p2).Compile();
        LessThan = Expression.Lambda<Func<T, T, bool>>(Expression.LessThan(p1, p2), p1, p2).Compile();
        LessThanOrEqual = Expression.Lambda<Func<T, T, bool>>(Expression.LessThanOrEqual(p1, p2), p1, p2).Compile();
    }
    public Func<T, T, T> Add { get; private set; }
    public Func<T, T, T> Subtract { get; private set; }
    public Func<T, T, T> Multiply { get; private set; }
    public Func<T, T, T> Divide { get; private set; }
    public Func<T, T, T> Modulo { get; private set; }
    public Func<T, T, bool> Equal { get; private set; }
    public Func<T, T, bool> GreaterThan { get; private set; }
    public Func<T, T, bool> GreaterThanOrEqual { get; private set; }
    public Func<T, T, bool> LessThan { get; private set; }
    public Func<T, T, bool> LessThanOrEqual { get; private set; }
}
#endregion

public static class ObjectExtensions
{
    /// <summary>
    /// Predicateの戻り値がFalseの場合_defaultを返す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="m"></param>
    /// <param name="_default"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    static public T Default<T>(this T m, T _default, Predicate<T> predicate)
    {
        if (predicate(m) == true)
            return m;
        else
            return _default;
    }
}

#region ReadOnlyインスペクタ
public class ReadOnlyAttribute : PropertyAttribute
{
}
 
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(_position, _property, _label);
        EditorGUI.EndDisabledGroup();
    }
}
#endif
#endregion


#region WaitFroSecondsの拡張

static class WaitForSecondsExtensions
{
    /// <summary>
    /// <see cref="WaitForSecondsStopable(float, Trigger)"/>のTrigger用class (IEnumeratorは参照渡しができないため代案として)
    /// </summary>
    public class Trigger
    {
        /// <summary>
        /// 一時停止
        /// </summary>
        public bool pause = false;
        /// <summary>
        /// WaitForSecondsをcancelする
        /// </summary>
        public bool cancel = false;
    }

    /// <summary>
    /// 途中でPause可能なWaitForSecond
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="trigger"></param>
    /// <returns></returns>
    public static IEnumerator WaitForSecondsStopable(float seconds, Trigger trigger)
    {
        var stopTime = 0.0;
        var now = DateTime.Now;
        while ((DateTime.Now - now).TotalMilliseconds - stopTime < seconds * 1000)
        {
            if (trigger.cancel)
                break;

            if (trigger.pause)
            {
                var start = DateTime.Now;
                while (trigger.pause)
                    yield return null;
                stopTime += (DateTime.Now - start).TotalMilliseconds;
            }
            yield return null;
        }
    }
}
#endregion;


public static class MaterialExtensions
{
    /// <summary>
    /// AnimationCurveに即した形でMaterialのFloatを変更する
    /// </summary>
    /// <param name="material"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="animationCurve"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static IEnumerator SetFloat(this Material material, string name, float value, AnimationCurve animationCurve)
    {
        var start = Time.time;
        var t = 0f;
        var startValue = material.GetFloat(name);
        var endKey = animationCurve.keys[animationCurve.keys.Length - 1];
        
        while(t < endKey.time)
        {
            var rate = animationCurve.Evaluate(t);
            var _value = (1 - rate) * startValue + rate * value;
            material.SetFloat(name, _value);
            yield return null;
            t = Time.time - start;
        }
        material.SetFloat(name, value);
    }

    /// <summary>
    /// AnimationCurveに即した形でMaterialのColorを変更する
    /// </summary>
    /// <param name="material"></param>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <param name="animationCurve"></param>
    /// <returns></returns>
    public static IEnumerator SetColor(this Material material, string name, Color color, AnimationCurve animationCurve)
    {
        var start = Time.time;
        var t = 0f;
        var startColor = material.GetColor(name);
        var end = animationCurve.keys[animationCurve.keys.Length - 1].time;

        while(t < end)
        {
            var rate = animationCurve.Evaluate(t);
            var _value = (1 - rate) * startColor + rate * color;
            material.SetColor(name, _value);
            yield return null;
            t = Time.time - start;
        }
        material.SetColor(name, color);
    }
}

/// <summary>
/// Serialize可能なAnimationCurve
/// </summary>
[Serializable]
public class SerializableCurve
{
    SerializableKeyframe[] keys;
    string postWrapMode;
    string preWrapMode;

    [Serializable]
    public class SerializableKeyframe
    {
        public Single inTangent;
        public Single outTangent;
        public Int32 tangentMode;
        public Single time;
        public Single value;

        public SerializableKeyframe(Keyframe original)
        {
            inTangent = original.inTangent;
            outTangent = original.outTangent;
            tangentMode = original.tangentMode;
            time = original.time;
            value = original.value;
        }
    }

    public SerializableCurve(AnimationCurve original)
    {
        postWrapMode = getWrapModeAsString(original.postWrapMode);
        preWrapMode = getWrapModeAsString(original.preWrapMode);
        keys = new SerializableKeyframe[original.length];
        for (int i = 0; i < original.keys.Length; i++)
        {
            keys[i] = new SerializableKeyframe(original.keys[i]);
        }
    }

    public AnimationCurve toCurve()
    {
        if (keys == null)
            return null;
        if (keys.Length == 0)
            return null;

        AnimationCurve res = new AnimationCurve();
        res.postWrapMode = getWrapMode(postWrapMode);
        res.preWrapMode = getWrapMode(preWrapMode);
        Keyframe[] newKeys = new Keyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            SerializableKeyframe aux = keys[i];
            Keyframe newK = new Keyframe();
            newK.inTangent = aux.inTangent;
            newK.outTangent = aux.outTangent;
            newK.tangentMode = aux.tangentMode;
            newK.time = aux.time;
            newK.value = aux.value;
            newKeys[i] = newK;
        }
        res.keys = newKeys;
        return res;
    }

    private WrapMode getWrapMode(String mode)
    {
        if (mode.Equals("Clamp"))
        {
            return WrapMode.Clamp;
        }
        if (mode.Equals("ClampForever"))
        {
            return WrapMode.ClampForever;
        }
        if (mode.Equals("Default"))
        {
            return WrapMode.Default;
        }
        if (mode.Equals("Loop"))
        {
            return WrapMode.Loop;
        }
        if (mode.Equals("Once"))
        {
            return WrapMode.Once;
        }
        if (mode.Equals("PingPong"))
        {
            return WrapMode.PingPong;
        }
        LogError("Wat is this wrap mode???");
        return WrapMode.Default;
    }

    private string getWrapModeAsString(WrapMode mode)
    {
        if (mode.Equals(WrapMode.Clamp))
        {
            return "Clamp";
        }
        if (mode.Equals(WrapMode.ClampForever))
        {
            return "ClampForever";
        }
        if (mode.Equals(WrapMode.Default))
        {
            return "Default";
        }
        if (mode.Equals(WrapMode.Loop))
        {
            return "Loop";
        }
        if (mode.Equals(WrapMode.Once))
        {
            return "Once";
        }
        if (mode.Equals(WrapMode.PingPong))
        {
            return "PingPong";
        }
        LogError("Wat is this wrap mode???");
        return "f you";
    }
}

public static class DateTimeExtensions
{
    /// <summary>
    /// English形式のMonthを取得
    /// </summary>
    public static string MonthEN(this DateTime dateTime)
    {
        string[] month = new string[]
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };
        return month[dateTime.Month];
    }
}


public static class AnimationCurveExtensions
{
    /// <summary>
    /// Timeの最大値を取得する
    /// </summary>
    public static float GetMaxTime(this AnimationCurve curve)
    {
        return curve.keys.ToList().Max(k => k.time);
    }

    /// <summary>
    /// Timeの最小値を取得する
    /// </summary>
    public static float GetMinTime(this AnimationCurve curve)
    {
        return curve.keys.ToList().Min(k => k.time);
    }

    /// <summary>
    /// Valueの最大値を取得する
    /// </summary>
    public static float GetMaxValue(this AnimationCurve curve)
    {
        return curve.keys.ToList().Max(k => k.value);
    }

    /// <summary>
    /// Valueの最小値を取得する
    /// </summary>
    public static float GetMinValue(this AnimationCurve curve)
    {
        return curve.keys.ToList().Min(k => k.value);
    }

    /// <summary>
    /// TimeのLastを取得する
    /// </summary>
    public static float GetLastTime(this AnimationCurve curve)
    {
        return curve.keys.ToList().Last().time;
    }

    /// <summary>
    /// ValueのLastを取得する
    /// </summary>
    public static float GetLastValue(this AnimationCurve curve)
    {
        return curve.keys.ToList().Last().value;
    }
}

public static class VisualEffectExtension
{
    /// <summary>
    /// Get float value from VisualEffect if it has
    /// </summary>
    public static bool GetFloat(this VisualEffect vfx, string name, out float value)
    {
        if (vfx.HasFloat(name))
        {
            value = vfx.GetFloat(name);
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }


    /// <summary>
    /// Get bool value from VisualEffect if it has 
    /// </summary>
    public static bool GetBool(this VisualEffect vfx, string name, out bool value)
    {
        if (vfx.HasBool(name))
        {
            value = vfx.GetBool(name);
            return true;
        }
        else
        {
            value = false;
            return false;
        }
    }

    /// <summary>
    /// Get vector2 value from VisualEffect if it has
    /// </summary>
    public static bool GetVector2(this VisualEffect vfx, string name, out Vector2 value)
    {
        if (vfx.HasVector2(name))
        {
            value = vfx.GetVector2(name);
            return true;
        }
        else
        {
            value = Vector2.zero;
            return false;
        }
    }

    /// <summary>
    /// Get vector3 value from VisualEffect if it has
    /// </summary>
    public static bool GetVector3(this VisualEffect vfx, string name, out Vector3 value)
    {
        if (vfx.HasVector3(name))
        {
            value = vfx.GetVector3(name);
            return true;
        }
        else
        {
            value = Vector3.zero;
            return false;
        }
    }

    /// <summary>
    /// Get vector4 value from VisualEffect if it has
    /// </summary>
    public static bool GetVector4(this VisualEffect vfx, string name, out Vector4 value)
    {
        if (vfx.HasVector4(name))
        {
            value = vfx.GetVector4(name);
            return true;
        }
        else
        {
            value = Vector4.zero;
            return false;
        }
    }
}

public static class AddressablesExtensions
{
    /// <summary>
    /// 指定されたアドレスに紐づくアセットが存在する場合 true を返します
    /// </summary>
    public static async Task<bool> Exists(object key)
    {
        var handle = Addressables.LoadResourceLocationsAsync(key);

        await handle.Task;

        return
            handle.Status == AsyncOperationStatus.Succeeded &&
            handle.Result != null &&
            0 < handle.Result.Count;
    }
}

public static class ScriptableObjectExtensions
{
    /// <summary>
    /// ScriptableObjectをディープコピーする
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static T DeepCopy<T>(this T obj) where T : ScriptableObject
    {
        T copy = ScriptableObject.CreateInstance<T>();
        EditorUtility.CopySerialized(obj, copy);
        return copy;
    }
}