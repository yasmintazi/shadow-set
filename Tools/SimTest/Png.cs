// a tiny PNG writer and canvas, so the level-design tool can draw pictures of the wall
using System;
using System.IO;
using System.IO.Compression;

namespace ShadowSet.Tools
{
    public class Canvas
    {
        public readonly int w, h;
        public readonly byte[] px;      // RGB
        public Canvas(int w, int h, uint bg)
        {
            this.w = w; this.h = h; px = new byte[w * h * 3];
            for (int i = 0; i < w * h; i++) { px[i * 3] = (byte)(bg >> 16); px[i * 3 + 1] = (byte)(bg >> 8); px[i * 3 + 2] = (byte)bg; }
        }

        public void Blend(int x, int y, uint c, float a)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = (y * w + x) * 3;
            px[i] = (byte)(px[i] + ((int)((c >> 16) & 255) - px[i]) * a);
            px[i + 1] = (byte)(px[i + 1] + ((int)((c >> 8) & 255) - px[i + 1]) * a);
            px[i + 2] = (byte)(px[i + 2] + ((int)(c & 255) - px[i + 2]) * a);
        }

        public void Rect(float x0, float y0, float x1, float y1, uint c, float a = 1)
        {
            int ax = (int)Math.Floor(Math.Min(x0, x1)), bx = (int)Math.Ceiling(Math.Max(x0, x1));
            int ay = (int)Math.Floor(Math.Min(y0, y1)), by = (int)Math.Ceiling(Math.Max(y0, y1));
            for (int y = ay; y < by; y++) for (int x = ax; x < bx; x++) Blend(x, y, c, a);
        }

        public void Outline(float x0, float y0, float x1, float y1, uint c, float a = 1)
        {
            Rect(x0, y0, x1, y0 + 1, c, a); Rect(x0, y1 - 1, x1, y1, c, a);
            Rect(x0, y0, x0 + 1, y1, c, a); Rect(x1 - 1, y0, x1, y1, c, a);
        }

        public void Line(float x0, float y0, float x1, float y1, uint c, float a = 1)
        {
            int n = (int)Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) + 1;
            for (int i = 0; i <= n; i++) { float t = (float)i / n; Blend((int)(x0 + (x1 - x0) * t), (int)(y0 + (y1 - y0) * t), c, a); }
        }

        public void Dot(float x, float y, float r, uint c, float a = 1)
        {
            for (int yy = (int)(y - r); yy <= (int)(y + r); yy++)
                for (int xx = (int)(x - r); xx <= (int)(x + r); xx++)
                    if ((xx - x) * (xx - x) + (yy - y) * (yy - y) <= r * r) Blend(xx, yy, c, a);
        }

        // 3x5 digits and a few letters, for labels
        static readonly string[] Glyphs = {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111" };
        public void Text(string s, float x, float y, uint c, int scale = 2)
        {
            foreach (char ch in s)
            {
                if (ch >= '0' && ch <= '9')
                {
                    string g = Glyphs[ch - '0'];
                    for (int r = 0; r < 5; r++) for (int q = 0; q < 3; q++)
                            if (g[r * 3 + q] == '1') Rect(x + q * scale, y + r * scale, x + (q + 1) * scale, y + (r + 1) * scale, c);
                }
                else if (ch == '.') Rect(x + scale, y + 4 * scale, x + 2 * scale, y + 5 * scale, c);
                else if (ch == '-') Rect(x, y + 2 * scale, x + 3 * scale, y + 3 * scale, c);
                x += 4 * scale;
            }
        }

        public void Save(string path)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                var ihdr = new byte[13];
                BE(ihdr, 0, (uint)w); BE(ihdr, 4, (uint)h);
                ihdr[8] = 8; ihdr[9] = 2;
                Chunk(ms, "IHDR", ihdr);
                var raw = new byte[(w * 3 + 1) * h];
                for (int y = 0; y < h; y++) { raw[y * (w * 3 + 1)] = 0; Buffer.BlockCopy(px, y * w * 3, raw, y * (w * 3 + 1) + 1, w * 3); }
                byte[] z;
                using (var zs = new MemoryStream())
                {
                    zs.WriteByte(0x78); zs.WriteByte(0x9C);
                    using (var d = new DeflateStream(zs, CompressionMode.Compress, true)) d.Write(raw, 0, raw.Length);
                    uint a = 1, b = 0;
                    foreach (byte v in raw) { a = (a + v) % 65521; b = (b + a) % 65521; }
                    var ad = new byte[4]; BE(ad, 0, (b << 16) | a); zs.Write(ad, 0, 4);
                    z = zs.ToArray();
                }
                Chunk(ms, "IDAT", z);
                Chunk(ms, "IEND", new byte[0]);
                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        static void BE(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
        static uint[] crcTable;
        static uint Crc(byte[] data, int start, int len)
        {
            if (crcTable == null)
            {
                crcTable = new uint[256];
                for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; crcTable[n] = c; }
            }
            uint crc = 0xFFFFFFFF;
            for (int i = start; i < start + len; i++) crc = crcTable[(crc ^ data[i]) & 255] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }
        static void Chunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4]; BE(len, 0, (uint)data.Length); s.Write(len, 0, 4);
            var td = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++) td[i] = (byte)type[i];
            Buffer.BlockCopy(data, 0, td, 4, data.Length);
            s.Write(td, 0, td.Length);
            var crc = new byte[4]; BE(crc, 0, Crc(td, 0, td.Length)); s.Write(crc, 0, 4);
        }
    }
}
