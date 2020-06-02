using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = System.Diagnostics.Debug;

public class PathIdTest
{
    [MenuItem("AssetBundle/PathID")]
    public static void BuildAssetBundleOptions()
    {
        var fileId = new int[] {2800000, 21300000};
        List<byte>   b = new List<byte>();
        const string guid = "1bb5f812f4373c143b24d9cdd79303bf";

        b.AddRange(System.Text.Encoding.ASCII.GetBytes(guid));
        b.AddRange(BitConverter.GetBytes((Int32)3));
        b.AddRange(BitConverter.GetBytes((Int64)2800000));
        //b.AddRange(StringToByteArray("00000000005FC608"));

        UnityEngine.Debug.Log(BitConverter.ToString(b.ToArray()).Replace("-", ""));

        var d = Md4.Md4Hash(b);
        UnityEngine.Debug.Log(BitConverter.ToString(d).Replace("-", ""));
    }

    public static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }
}


static class Md4
{
    public static byte[] Md4Hash(List<byte> bytes)
    {
        // get padded uints from bytes
        uint       bitCount = (uint) (bytes.Count) * 8;
        bytes.Add(128);
        while (bytes.Count % 64 != 56) bytes.Add(0);
        var uints = new List<uint>();
        for (int i = 0; i + 3 < bytes.Count; i += 4)
            uints.Add(bytes[i] | (uint) bytes[i + 1] << 8 | (uint) bytes[i + 2] << 16 | (uint) bytes[i + 3] << 24);
        uints.Add(bitCount);
        uints.Add(0);

        // run rounds
        uint                   a   = 0x67452301, b = 0xefcdab89, c = 0x98badcfe, d = 0x10325476;
        Func<uint, uint, uint> rol = (x, y) => x << (int) y | x >> 32 - (int) y;
        for (int q = 0; q + 15 < uints.Count; q += 16)
        {
            var  chunk = uints.GetRange(q, 16);
            uint aa    = a, bb = b, cc = c, dd = d;
            Action<Func<uint, uint, uint, uint>, uint[]> round = (f, y) =>
            {
                foreach (uint i in new[] {y[0], y[1], y[2], y[3]})
                {
                    a = rol(a + f(b, c, d) + chunk[(int) (i + y[4])] + y[12], y[8]);
                    d = rol(d + f(a, b, c) + chunk[(int) (i + y[5])] + y[12], y[9]);
                    c = rol(c + f(d, a, b) + chunk[(int) (i + y[6])] + y[12], y[10]);
                    b = rol(b + f(c, d, a) + chunk[(int) (i + y[7])] + y[12], y[11]);
                }
            };
            round((x, y, z) => (x & y) | (~x & z), new uint[] {0, 4, 8, 12, 0, 1, 2, 3, 3, 7, 11, 19, 0});
            round((x, y, z) => (x & y) | (x & z) | (y & z),
                new uint[] {0, 1, 2, 3, 0, 4, 8, 12, 3, 5, 9, 13, 0x5a827999});
            round((x, y, z) => x ^ y ^ z, new uint[] {0, 2, 1, 3, 0, 8, 4, 12, 3, 9, 11, 15, 0x6ed9eba1});
            a += aa;
            b += bb;
            c += cc;
            d += dd;
        }

        // return hex encoded string
        return new[] {a, b, c, d}.SelectMany(BitConverter.GetBytes).ToArray();
    }
}