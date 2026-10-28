// plain maths for the simulation, with no Unity types, so the whole game can also be
// run and tested outside Unity (see Tools/SimTest)
using System;

namespace ShadowSet
{
    public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
        public static V2 operator +(V2 a, V2 b) { return new V2(a.x + b.x, a.y + b.y); }
        public static V2 operator -(V2 a, V2 b) { return new V2(a.x - b.x, a.y - b.y); }
        public static V2 operator *(V2 a, float k) { return new V2(a.x * k, a.y * k); }
        public float Length { get { return (float)Math.Sqrt(x * x + y * y); } }
        public override string ToString() { return "(" + x.ToString("0.00") + ", " + y.ToString("0.00") + ")"; }
    }

    // an axis-aligned rectangle on the wall, measured from its bottom-left corner
    public struct Box
    {
        public float x, y, w, h;
        public Box(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public float Right { get { return x + w; } }
        public float Top { get { return y + h; } }
        public V2 Center { get { return new V2(x + w / 2, y + h / 2); } }

        // do the two overlap by more than `inset` on every side?
        public bool Overlaps(Box o, float inset = 0)
        {
            return x + inset < o.Right && o.x + inset < Right && y + inset < o.Top && o.y + inset < Top;
        }

        public bool Contains(V2 p) { return p.x >= x && p.x <= Right && p.y >= y && p.y <= Top; }
        public override string ToString() { return "[" + x.ToString("0.00") + ", " + y.ToString("0.00") + " " + w.ToString("0.00") + "x" + h.ToString("0.00") + "]"; }
    }

    public static class M
    {
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static float Approach(float v, float target, float step) { return v < target ? Math.Min(v + step, target) : Math.Max(v - step, target); }
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Abs(float a) { return a < 0 ? -a : a; }
        public const float TAU = 6.2831853f;
    }
}
