using System;

public class Utility {
    public static string StringRemoveInvalidChars(string str, char[] characters)
    {
        System.Text.StringBuilder buf = new System.Text.StringBuilder(str);
        foreach (char c in characters)
        {
            buf.Replace(c.ToString(), "");
        }
        return buf.ToString();
    }

    public static T GetEnumFromValue<T, S>(S value)
        where T : struct, IConvertible
        where S : IConvertible
    {
        //引数の型を取得
        Type valueType = value.GetType();

        //列挙型に対応する型の場合のみ
        if (valueType == typeof(sbyte) || valueType == typeof(byte) ||
            valueType == typeof(short) || valueType == typeof(ushort) ||
            valueType == typeof(int) || valueType == typeof(uint) ||
            valueType == typeof(long) || valueType == typeof(ulong))
        {
            //定義した列挙型に該当値が含まれている場合のみ
            if (Enum.IsDefined(typeof(T), value)) return (T)Enum.ToObject(typeof(T), value);
        }

        //条件を満たさない場合は、デフォルト値を返す
        return default(T);
    }
}
