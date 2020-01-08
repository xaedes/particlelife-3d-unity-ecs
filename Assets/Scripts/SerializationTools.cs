using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using System.Text;
using System.Globalization; // for culture neutral number serialization


public class GZipper
{
    public static NativeArray<byte> ZipNative(ref NativeArray<byte> bytes)
    {
        return new NativeArray<byte>(ZipNativeAsArray(ref bytes), Allocator.Persistent);
    }
    public static NativeArray<byte> UnzipNative(ref NativeArray<byte> bytes)
    {
        return new NativeArray<byte>(UnzipNativeAsArray(ref bytes), Allocator.Persistent);
    }
    public static byte[] ZipNativeAsArray(ref NativeArray<byte> bytes)
    {
        var bytes_arr = bytes.ToArray();
        return Zip(ref bytes_arr);
    }
    public static byte[] UnzipNativeAsArray(ref NativeArray<byte> bytes)
    {
        var bytes_arr = bytes.ToArray();
        return Unzip(ref bytes_arr);
    }

    // https://stackoverflow.com/a/7343623/798588
    public static void CopyTo(System.IO.Stream src, System.IO.Stream dest)
    {
        byte[] bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }
    public static byte[] Zip(ref byte[] bytes)
    {
        using (var msi = new System.IO.MemoryStream(bytes))
        using (var mso = new System.IO.MemoryStream())
        {
            using (var gs = new System.IO.Compression.GZipStream(mso, System.IO.Compression.CompressionMode.Compress))
            {
                //msi.CopyTo(gs);
                CopyTo(msi, gs);
            }

            return mso.ToArray();
        }
    }
    public static byte[] Unzip(ref byte[] bytes)
    {
        using (var msi = new System.IO.MemoryStream(bytes))
        using (var mso = new System.IO.MemoryStream())
        {
            using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress))
            {
                //gs.CopyTo(mso);
                CopyTo(gs, mso);
            }

            return mso.ToArray();
            //return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}


[Serializable]
public class SerializeGenericNativeArray
{
    public int Length;
    public string data;
    public SerializeNativeArrayInt starts;
    public SerializeNativeArrayInt lengths;

    public static SerializeGenericNativeArray Serialize<T>(ref NativeArray<T> arr) where T : struct
    {
        SerializeNativeArray<T> ser = new SerializeNativeArray<T>(ref arr);
        return new SerializeGenericNativeArray()
        {
            Length = ser.Length,
            data = ser.data,
            starts = ser.starts,
            lengths = ser.lengths,
        };
    }
}

public class SerializeNativeArray<T> where T : struct
{
    public int Length;
    public string data;
    public SerializeNativeArrayInt starts;
    public SerializeNativeArrayInt lengths;
    public SerializeNativeArray(ref NativeArray<T> arr)
    {
        //arr.
        Length = arr.Length;
        if (Length == 0)
        {
            data = "";
            starts = new SerializeNativeArrayInt();
            lengths = new SerializeNativeArrayInt();
        }
        else
        {
            NativeArray<int> arr_starts = new NativeArray<int>(Length, Allocator.Persistent);
            NativeArray<int> arr_lengths = new NativeArray<int>(Length, Allocator.Persistent);
            string jsonItem;

            // estimate string size by serializing one item
            jsonItem = JsonUtility.ToJson(arr[0]);
            StringBuilder sb = new StringBuilder(jsonItem.Length * Length + Length - 1);
            for (int i = 0; i < Length; i++)
            {
                arr_starts[i] = sb.Length;
                jsonItem = JsonUtility.ToJson(arr[0]);
                arr_lengths[i] = jsonItem.Length;
                sb.Append(jsonItem);
            }
            data = sb.ToString();
            starts = new SerializeNativeArrayInt(ref arr_starts);
            lengths = new SerializeNativeArrayInt(ref arr_lengths);
            arr_starts.Dispose();
            arr_lengths.Dispose();
        }
    }
}


[Serializable]
public class SerializeNativeArrayFloat
{
    public int NumItems = 0;
    public string data = "";

    public DataSerializer.EncodingType encoding = DataSerializer.EncodingType.CommaSeperatedDecimals;
    public SerializeNativeArrayFloat()
    { }
    public SerializeNativeArrayFloat([ReadOnly] ref NativeArray<float> arr,
            DataSerializer.EncodingType encoding = DataSerializer.EncodingType.CommaSeperatedDecimals)
    {
        NumItems = arr.Length;
        this.encoding = encoding;
        if (NumItems == 0)
            data = "";
        else
            data = DataSerializer.encode(ref arr, encoding);
    }
    public bool readOut(ref NativeArray<float> arr)
    {
        bool success = DataSerializer.decode(ref data, ref arr, encoding);
        if (success && arr.Length == NumItems)
        {
            NumItems = arr.Length;
            return true;
        }
        return false;
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeNativeArrayFloat FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeNativeArrayFloat>(json);
        return result;
    }
}
[Serializable]
public class SerializeNativeArrayInt
{
    public int NumItems = 0;
    public string data = "";

    public DataSerializer.EncodingType encoding = DataSerializer.EncodingType.CommaSeperatedDecimals;
    public SerializeNativeArrayInt()
    { }
    public SerializeNativeArrayInt([ReadOnly] ref NativeArray<int> arr,
            DataSerializer.EncodingType encoding = DataSerializer.EncodingType.CommaSeperatedDecimals)
    {
        NumItems = arr.Length;
        this.encoding = encoding;
        if (NumItems == 0)
            data = "";
        else
            data = DataSerializer.encode(ref arr, encoding);
    }
    public bool readOut(ref NativeArray<int> arr)
    {
        bool success = DataSerializer.decode(ref data, ref arr, encoding);
        if (success && arr.Length == NumItems)
        {
            NumItems = arr.Length;
            return true;
        }
        return false;
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static SerializeNativeArrayInt FromJson([ReadOnly] ref string json)
    {
        var result = JsonUtility.FromJson<SerializeNativeArrayInt>(json);
        return result;
    }
}

public class DataSerializer
{
    public enum EncodingType : int
    {
        CommaSeperatedDecimals = 0,
        HexCodedBinary = 1,
        Base64CodedZippedBinary = 2,
        COUNT = 3,
    };

    #region decode overloads
    public static bool decode([ReadOnly] ref string data, ref NativeArray<float> arr,
        EncodingType encoding = EncodingType.CommaSeperatedDecimals)
    {
        switch (encoding)
        {
            case EncodingType.CommaSeperatedDecimals:
                return DataSerializer.decodeCommaSepDecimals(ref data, ref arr);
            //throw new Exception("not implemented encoding " + encoding);
            case EncodingType.HexCodedBinary:
                return DataSerializer.decodeBinaryHex(ref data, ref arr);
            case EncodingType.Base64CodedZippedBinary:
                //throw new Exception("not implemented encoding " + encoding);
                return DataSerializer.decodeBinaryGzipBase64(ref data, ref arr);
            default:
                throw new Exception("not implemented encoding " + encoding);
        }
    }
    public static bool decode([ReadOnly] ref string data, ref NativeArray<int> arr,
    EncodingType encoding = EncodingType.CommaSeperatedDecimals)
    {
        switch (encoding)
        {
            case EncodingType.CommaSeperatedDecimals:
                return DataSerializer.decodeCommaSepDecimals(ref data, ref arr);
                //throw new Exception("not implemented encoding " + encoding);
            case EncodingType.HexCodedBinary:
                return DataSerializer.decodeBinaryHex(ref data, ref arr);
            case EncodingType.Base64CodedZippedBinary:
                return DataSerializer.decodeBinaryGzipBase64(ref data, ref arr);
                //throw new Exception("not implemented encoding " + encoding);
            default:
                throw new Exception("not implemented encoding " + encoding);
        }
    }
    #endregion
    #region encode overloads
    public static string encode([ReadOnly] ref NativeArray<float> arr,
        EncodingType encoding = EncodingType.CommaSeperatedDecimals)
    {
        switch (encoding)
        {
            case EncodingType.CommaSeperatedDecimals:
                return DataSerializer.encodeCommaSepDecimals(ref arr);
            case EncodingType.HexCodedBinary:
                return DataSerializer.encodeBinaryHex(ref arr);
            case EncodingType.Base64CodedZippedBinary:
                return DataSerializer.encodeBinaryGzipBase64(ref arr);
            default:
                throw new Exception("not implemented encoding " + encoding);
        }
    }
    public static string encode([ReadOnly] ref NativeArray<int> arr,
        EncodingType encoding = EncodingType.CommaSeperatedDecimals)
    {
        switch (encoding)
        {
            case EncodingType.CommaSeperatedDecimals:
                return DataSerializer.encodeCommaSepDecimals(ref arr);
            case EncodingType.HexCodedBinary:
                return DataSerializer.encodeBinaryHex(ref arr);
            case EncodingType.Base64CodedZippedBinary:
                return DataSerializer.encodeBinaryGzipBase64(ref arr);
            default:
                throw new Exception("not implemented encoding " + encoding);
        }
    }
    #endregion

    #region code endianess
    public static string encodeEndianess()
    {
        return System.BitConverter.IsLittleEndian ? "LE" : "BE";
    }
    public static bool decodeEndianess([ReadOnly] ref string data, ref bool out_isLittleEndian)
    {
        if (data.StartsWith("LE"))
        {
            out_isLittleEndian = true;
            return true;
        }
        else if (data.StartsWith("BE"))
        {
            out_isLittleEndian = false;
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion
    #region CommaSeperatedDecimals encoding
    public static bool decodeCommaSepDecimals([ReadOnly] ref string data, ref NativeArray<uint> arr)
    {
        string[] items = data.Split(',');
        if (!arr.IsCreated || arr.Length != items.Length)
            arr = new NativeArray<uint>(items.Length, Allocator.Persistent);
        for (int i = 0; i < items.Length; i++) 
        {
            arr[i] = Convert.ToUInt32(items[i], CultureInfo.InvariantCulture);
        }
        return true;
    }
    public static bool decodeCommaSepDecimals([ReadOnly] ref string data, ref NativeArray<float> arr)
    {
        string[] items = data.Split(',');
        if (!arr.IsCreated || arr.Length != items.Length)
            arr = new NativeArray<float>(items.Length, Allocator.Persistent);
        for (int i = 0; i < items.Length; i++)
        {
            arr[i] = Convert.ToSingle(items[i], CultureInfo.InvariantCulture);
        }
        return true;
    }
    public static bool decodeCommaSepDecimals([ReadOnly] ref string data, ref NativeArray<int> arr)
    {
        string[] items = data.Split(',');
        if (!arr.IsCreated || arr.Length != items.Length)
            arr = new NativeArray<int>(items.Length, Allocator.Persistent);
        for (int i = 0; i < items.Length; i++)
        {
            arr[i] = Convert.ToInt32(items[i], CultureInfo.InvariantCulture);
        }
        return true;
    }

    public static string encodeCommaSepDecimals([ReadOnly] ref NativeArray<float> arr)
    {
        StringBuilder sb = new StringBuilder(12 * arr.Length + arr.Length - 1);
        for (int i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i].ToString("G9", CultureInfo.InvariantCulture));
            if (i < arr.Length - 1) sb.Append(",");
        }
        return sb.ToString();
    }
    public static string encodeCommaSepDecimals([ReadOnly] ref NativeArray<int> arr)
    {
        StringBuilder sb = new StringBuilder(12 * arr.Length + arr.Length - 1);
        for (int i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i].ToString(CultureInfo.InvariantCulture));
            if (i < arr.Length - 1) sb.Append(",");
        }
        return sb.ToString();
    }
    #endregion

    #region HexCodedBinary encoding
    public static bool decodeBinaryHex([ReadOnly] ref string data, ref NativeArray<uint> arr)
    {
        bool isLittleEndian = false;
        if (!decodeEndianess(ref data, ref isLittleEndian))
        {
            Debug.LogError("deserialization fail: could not decode endianess.");
            return false;
        }
        if (isLittleEndian != System.BitConverter.IsLittleEndian)
        {
            Debug.LogError("deserialization fail: different endianess, conversion not implemented.");
            return false;
        }
        int numValues = (data.Length - 2) / 8;
        
        if (!arr.IsCreated || arr.Length != numValues)
            arr = new NativeArray<uint>(numValues, Allocator.Persistent);
        for (int i=0; i<numValues;i++)
        {
            string hexCoded = data.Substring(2 + i * 8, 8);
            arr[i] = Convert.ToUInt32(hexCoded, 16);
        }
        return true;
    }
    public static bool decodeBinaryHex([ReadOnly] ref string data, ref NativeArray<float> arr)
    {
        NativeArray<uint> uint_arr = new NativeArray<uint>();
        if(!decodeBinaryHex(ref data, ref uint_arr))
        {
            Debug.LogError("deserialization fail: could not decode binary content as uint array.");
            return false;
        }
        arr = uint_arr.Reinterpret<float>(4);
        return true;
    }
    public static bool decodeBinaryHex([ReadOnly] ref string data, ref NativeArray<int> arr)
    {
        NativeArray<uint> uint_arr = new NativeArray<uint>();
        if(!decodeBinaryHex(ref data, ref uint_arr))
        {
            Debug.LogError("deserialization fail: could not decode binary content as uint array.");
            return false;
        }
        arr = uint_arr.Reinterpret<int>(4);
        return true;
    }
    public static string encodeBinaryHex([ReadOnly] ref NativeArray<float> arr)
    {
        NativeArray<uint> uint_arr = arr.Reinterpret<uint>(4);
        StringBuilder sb = new StringBuilder(8 * arr.Length);
        sb.Append(DataSerializer.encodeEndianess());
        for (int i = 0; i < arr.Length; i++)
        {
            // 8 hexadecimal digits
            sb.Append(uint_arr[i].ToString("X8"));
        }
        return sb.ToString();
    }
    public static string encodeBinaryHex([ReadOnly] ref NativeArray<int> arr)
    {
        NativeArray<uint> uint_arr = arr.Reinterpret<uint>(4);
        StringBuilder sb = new StringBuilder(8 * arr.Length);
        sb.Append(DataSerializer.encodeEndianess());
        for (int i = 0; i < arr.Length; i++)
        {
            // 8 hexadecimal digits
            sb.Append(uint_arr[i].ToString("X8"));
        }
        return sb.ToString();
    }
    #endregion

    #region Base64CodedZippedBinary encoding
    public static bool decodeBinaryGzipBase64([ReadOnly] ref string data, ref NativeArray<uint> arr)
    {
        bool isLittleEndian = false;
        if (!decodeEndianess(ref data, ref isLittleEndian))
        {
            Debug.LogError("deserialization fail: could not decode endianess.");
            return false;
        }
        if (isLittleEndian != System.BitConverter.IsLittleEndian)
        {
            Debug.LogError("deserialization fail: different endianess, conversion not implemented.");
            return false;
        }
        byte[] bytes = Convert.FromBase64String(data.Substring(2));
        byte[] unzipped = GZipper.Unzip(ref bytes);
        int numValues = unzipped.Length / 4;
        uint[] uints = new uint[numValues];
        System.Buffer.BlockCopy(unzipped, 0, uints, 0, unzipped.Length);
        if (!arr.IsCreated || arr.Length != numValues)
            arr = new NativeArray<uint>(numValues, Allocator.Persistent);
        arr.CopyFrom(uints);
        return true;
    }
    public static bool decodeBinaryGzipBase64([ReadOnly] ref string data, ref NativeArray<float> arr)
    {
        NativeArray<uint> uint_arr = new NativeArray<uint>();
        if (!decodeBinaryGzipBase64(ref data, ref uint_arr))
        {
            Debug.LogError("deserialization fail: could not decode binary content as uint array.");
            return false;
        }
        arr = uint_arr.Reinterpret<float>(4);
        return true;
    }
    public static bool decodeBinaryGzipBase64([ReadOnly] ref string data, ref NativeArray<int> arr)
    {
        NativeArray<uint> uint_arr = new NativeArray<uint>();
        if (!decodeBinaryGzipBase64(ref data, ref uint_arr))
        {
            Debug.LogError("deserialization fail: could not decode binary content as uint array.");
            return false;
        }
        arr = uint_arr.Reinterpret<int>(4);
        return true;
    }
    public static string encodeBinaryGzipBase64([ReadOnly] ref NativeArray<float> arr)
    {
        byte[] bytes = new byte[arr.Length * 4];
        System.Buffer.BlockCopy(arr.ToArray(), 0, bytes, 0, bytes.Length);
        byte[] zipped = GZipper.Zip(ref bytes);
        return DataSerializer.encodeEndianess() + Convert.ToBase64String(zipped);
    }
    public static string encodeBinaryGzipBase64([ReadOnly] ref NativeArray<int> arr)
    {
        byte[] bytes = new byte[arr.Length * 4];
        System.Buffer.BlockCopy(arr.ToArray(), 0, bytes, 0, bytes.Length);
        byte[] zipped = GZipper.Zip(ref bytes);
        return DataSerializer.encodeEndianess() + Convert.ToBase64String(zipped);
    }

    #endregion
}