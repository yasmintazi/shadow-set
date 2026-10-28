// every texture and material in the game is made here, in code, when it starts:
// paper grain, wood, cardboard, thorns, the hero's frames, moths, curtains, glows
using UnityEngine;

namespace ShadowSet
{
    public static class Art
    {
        public static Texture2D Paper, Wood, Card, Thorns, Halo, HotDisc, SoftRect, Glow, Moth, Key, Padlock, Door, Curtain, Rope, Dots;
        public static Texture2D[] Cat;        // idle, run 1, run 2, jump, fall
        static Shader lit, unlit;

        public static readonly Color[] Tints =
        {
            new Color(0.93f, 0.62f, 0.32f),     // orange card
            new Color(0.45f, 0.70f, 0.90f),     // sky blue
            new Color(0.92f, 0.80f, 0.40f),     // mustard
            new Color(0.70f, 0.52f, 0.86f),     // violet
            new Color(0.75f, 0.12f, 0.16f),     // blood red (thorns)
            new Color(0.90f, 0.48f, 0.55f),     // rose
        };

        public static void Init()
        {
            if (Paper != null) return;
            lit = Shader.Find("ShadowSet/Lit");
            unlit = Shader.Find("ShadowSet/Unlit");
            Paper = Make(256, 256, true, (x, y) => { float n = Fbm(x / 256f * 8, y / 256f * 8, 5, 8); float f = Fbm(x / 256f * 40, y / 256f * 3, 2, 40) * 0.5f; float v = 0.9f + 0.1f * n - 0.04f * f; return new Color(v, v * 0.97f, v * 0.9f, 1); });
            Wood = Make(256, 64, true, (x, y) => { float g = Fbm(x / 256f * 2, y / 64f * 12, 4, 2) ; float r = Mathf.Sin((y / 64f * 6 + g * 3) * Mathf.PI) * 0.5f + 0.5f; float v = 0.75f + 0.25f * r; float plank = (y % 16 == 0) ? 0.6f : 1; return new Color(0.55f * v * plank, 0.36f * v * plank, 0.2f * v * plank, 1); });
            Card = Make(128, 128, true, (x, y) => { float n = Fbm(x / 128f * 6, y / 128f * 6, 4, 6); float fib = Fbm(x / 128f * 30, y / 128f * 4, 2, 30); float v = 0.82f + 0.12f * n + 0.06f * fib; return new Color(v, v, v, 1); });
            SoftRect = Make(64, 64, false, (x, y) => { float e = Mathf.Min(Mathf.Min(x + 0.5f, 63.5f - x), Mathf.Min(y + 0.5f, 63.5f - y)); return new Color(1, 1, 1, Mathf.Clamp01(e / 2.5f)); });
            Thorns = MakeThorns();
            // the lamp's burning circle: flat and white-hot inside, with a short soft edge
            HotDisc = Make(128, 128, false, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f; float a = Mathf.Clamp01((1 - d) / 0.08f); return new Color(1, 1, 1, a * (0.85f + 0.15f * (1 - d))); });
            Halo = Make(64, 64, false, (x, y) => { float e = Mathf.Min(Mathf.Min(x + 0.5f, 63.5f - x), Mathf.Min(y + 0.5f, 63.5f - y)) / 16f; e = Mathf.Clamp01(e); return new Color(1, 1, 1, e * e * (3 - 2 * e)); });
            Glow = Make(128, 128, false, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f; float a = Mathf.Clamp01(1 - d); return new Color(1, 1, 1, a * a * a); });
            Moth = MakeMoth();
            Key = MakeKey();
            Padlock = MakePadlock();
            Door = MakeDoor();
            Curtain = Make(256, 32, true, (x, y) => { float f = Mathf.Sin(x / 256f * Mathf.PI * 2 * 9 + Mathf.Sin(x / 40f) * 0.6f) * 0.5f + 0.5f; float v = 0.45f + 0.55f * f; return new Color(0.62f * v, 0.06f * v, 0.1f * v, 1); });
            Rope = Make(8, 64, true, (x, y) => { float v = 0.6f + 0.4f * Mathf.Sin((y + x * 2) * 0.8f); return new Color(0.5f * v, 0.45f * v, 0.38f * v, 1); });
            Dots = Make(64, 8, true, (x, y) => new Color(1, 1, 1, (x / 8) % 2 == 0 && y > 2 && y < 6 ? 1 : 0));
            Cat = new Texture2D[5];
            for (int i = 0; i < 5; i++) Cat[i] = MakeCat(i);
        }

        // ------------------------------------------------------------ materials
        public static Material Lit(Texture tex, Color color, bool transparent = false, float wrap = 0.3f)
        {
            var m = new Material(lit) { mainTexture = tex, color = color };
            m.SetFloat("_Wrap", wrap);
            if (transparent)
            {
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.renderQueue = 3000;
            }
            return m;
        }

        public enum Blend { Alpha, Add, Multiply }
        public static Material Unlit(Texture tex, Color color, Blend blend = Blend.Alpha, int queue = 3000)
        {
            var m = new Material(unlit) { mainTexture = tex, color = color, renderQueue = queue };
            if (blend == Blend.Add) { m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); }
            if (blend == Blend.Multiply) { m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.DstColor); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero); m.SetFloat("_Multiply", 1); }
            return m;
        }

        // a flat quad, centred on its pivot or with the pivot at the bottom-left
        public static GameObject Quad(string name, Transform parent, Material mat, bool cornerPivot = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = cornerPivot ? CornerQuad : CenterQuad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        static Mesh centerQuad, cornerQuad;
        public static Mesh CenterQuad { get { return centerQuad ?? (centerQuad = MakeQuad(-0.5f, -0.5f)); } }
        public static Mesh CornerQuad { get { return cornerQuad ?? (cornerQuad = MakeQuad(0, 0)); } }
        static Mesh MakeQuad(float ox, float oy)
        {
            var m = new Mesh();
            m.vertices = new[] { new Vector3(ox, oy, 0), new Vector3(ox + 1, oy, 0), new Vector3(ox, oy + 1, 0), new Vector3(ox + 1, oy + 1, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            return m;
        }

        // boxes and spheres are built here rather than with CreatePrimitive, which adds colliders
        // (and the web build leaves the physics engine out)
        public static GameObject Box(string name, Transform parent, Material mat) { return Mesh(name, parent, mat, CubeMesh); }
        public static GameObject Sphere(string name, Transform parent, Material mat) { return Mesh(name, parent, mat, SphereMesh); }

        static GameObject Mesh(string name, Transform parent, Material mat, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        static Mesh cube, sphere;
        static Mesh CubeMesh
        {
            get
            {
                if (cube != null) return cube;
                var vs = new System.Collections.Generic.List<Vector3>(); var ns = new System.Collections.Generic.List<Vector3>();
                var uv = new System.Collections.Generic.List<Vector2>(); var tris = new System.Collections.Generic.List<int>();
                Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (var n in dirs)
                {
                    var a = n.y != 0 ? Vector3.right : Vector3.up;
                    var u = Vector3.Cross(n, a);
                    int i = vs.Count;
                    vs.Add((n - u - a) * 0.5f); vs.Add((n + u - a) * 0.5f); vs.Add((n + u + a) * 0.5f); vs.Add((n - u + a) * 0.5f);
                    for (int k = 0; k < 4; k++) ns.Add(n);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                    tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                }
                cube = new Mesh();
                cube.SetVertices(vs); cube.SetNormals(ns); cube.SetUVs(0, uv); cube.SetTriangles(tris, 0);
                return cube;
            }
        }
        static Mesh SphereMesh
        {
            get
            {
                if (sphere != null) return sphere;
                int lat = 12, lon = 18;
                var vs = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var tris = new System.Collections.Generic.List<int>();
                for (int y = 0; y <= lat; y++)
                    for (int x = 0; x <= lon; x++)
                    {
                        float th = Mathf.PI * y / lat, ph = 2 * Mathf.PI * x / lon;
                        vs.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f);
                        uv.Add(new Vector2(x / (float)lon, y / (float)lat));
                    }
                for (int y = 0; y < lat; y++)
                    for (int x = 0; x < lon; x++)
                    {
                        int a = y * (lon + 1) + x, b = a + lon + 1;
                        tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                    }
                sphere = new Mesh();
                sphere.SetVertices(vs); sphere.SetUVs(0, uv); sphere.SetTriangles(tris, 0);
                sphere.RecalculateNormals();
                return sphere;
            }
        }

        // ------------------------------------------------------------ texture makers
        delegate Color Pixel(int x, int y);
        static Texture2D Make(int w, int h, bool repeat, Pixel f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 2 };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            t.SetPixels(px);
            t.Apply(true, true);
            return t;
        }

        static float Hash(int x, int y, int seed) { unchecked { int h = x * 374761393 + y * 668265263 + seed * 982451653; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float Noise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int p = Mathf.Max(1, period);
            float a = Hash(((x0 % p) + p) % p, ((y0 % p) + p) % p, 1), b = Hash((((x0 + 1) % p) + p) % p, ((y0 % p) + p) % p, 1);
            float c = Hash(((x0 % p) + p) % p, (((y0 + 1) % p) + p) % p, 1), d = Hash((((x0 + 1) % p) + p) % p, (((y0 + 1) % p) + p) % p, 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float Fbm(float x, float y, int octaves, int period)
        {
            float v = 0, amp = 0.5f, total = 0;
            for (int i = 0; i < octaves; i++) { v += Noise(x, y, period) * amp; total += amp; x *= 2; y *= 2; period *= 2; amp *= 0.5f; }
            return v / total;
        }

        // a bramble: a wobbly stem along the middle with thorns sticking out above and below
        // ------------------------------------------------------------ stage props
        // each cut-out is a recognisable prop. its top edge is always flat and runs the full width,
        // because that's where the solid shadow is: you can trust what you see to stand on.
        // the same picture is the card cut-out (in colour) and its shadow (its alpha)
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> props = new System.Collections.Generic.Dictionary<string, Texture2D>();
        public static Texture2D Prop(string look)
        {
            if (look == "thorns") return Thorns;
            Texture2D t;
            if (props.TryGetValue(look, out t)) return t;
            int w = 512, h = 64;
            var inside = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    inside[y * w + x] = PropShape(look, (x + 0.5f) / w, (y + 0.5f) / h);
            t = Make(w, h, false, (x, y) =>
            {
                if (!inside[y * w + x]) return new Color(1, 1, 1, 0);
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                // a dark rim where the card was cut, a few pixels in from the edge
                bool rim = false;
                for (int d = 1; d <= 3 && !rim; d++)
                    rim = Out(inside, w, h, x + d * 2, y) || Out(inside, w, h, x - d * 2, y) || Out(inside, w, h, x, y + d) || Out(inside, w, h, x, y - d);
                float grain = 0.88f + 0.12f * Fbm(u * 40, v * 5, 3, 40);
                float shade = rim ? 0.5f : grain * PropDetail(look, u, v);
                if (look == "veil" || look == "canopy")
                {
                    // gauze: an even, see-through wash with a firmer edge, and the faintest weave
                    float weave = 0.03f * Mathf.Sin(u * 300) * Mathf.Sin(v * 60);
                    return new Color(1, 1, 1, rim ? 0.9f : 0.78f + weave);
                }
                return new Color(shade, shade, shade, 1);
            });
            props[look] = t;
            return t;
        }
        static bool Out(bool[] inside, int w, int h, int x, int y) { return x < 0 || y < 0 || x >= w || y >= h || !inside[y * w + x]; }

        // is (u, v) inside the prop? u runs left to right, v bottom to top, both 0-1
        static bool PropShape(string look, float u, float v)
        {
            float c = 2 * u - 1;                                    // -1 at the left end, 1 at the right
            switch (look)
            {
                case "bridge":
                    // a deck along the top, on an arch: solid down to the ground at the ends
                    if (v > 0.62f) return true;
                    float arch = 0.62f * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(c / 0.86f, 2)));
                    return v > arch && Mathf.Abs(c) < 1;
                case "cloud":
                    // a flat top, puffs underneath, rounded ends
                    float puffs = 0.42f - 0.3f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 5.5f));
                    float end = Mathf.Abs(c) > 0.86f ? (Mathf.Abs(c) - 0.86f) / 0.14f : 0;
                    return v > puffs + end * end * 0.9f;
                case "boat":
                    // a deck on top of a curved hull that's deepest in the middle
                    float hull = 0.88f * Mathf.Pow(Mathf.Abs(c), 2.2f);
                    return v > hull && !(v < 0.3f && Mathf.Abs(c) > 0.95f);
                case "veil":
                case "canopy":
                    // a sheet of gauze. it fills its box (the shade is the whole box), with just a
                    // little scalloped hem, so what looks shady is exactly what protects you
                    return v > 0.04f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 9));
                case "scrap":
                    // a torn strip of card
                    return v > 0.25f + 0.2f * Mathf.Abs(Mathf.Sin(u * 37)) * Mathf.Abs(Mathf.Sin(u * 13));
                default:
                    return true;                                    // crates and planks fill their box
            }
        }

        // painted-on detail, darker lines: planks, rivets, the waterline
        static float PropDetail(string look, float u, float v)
        {
            switch (look)
            {
                case "bridge": return v > 0.62f && Mathf.Repeat(u * 18, 1) < 0.06f ? 0.75f : v > 0.6f && v < 0.66f ? 0.7f : 1;
                case "boat": return Mathf.Abs(v - 0.7f) < 0.04f ? 0.7f : v < 0.7f && Mathf.Repeat(u * 9, 1) < 0.04f ? 0.8f : 1;
                case "crate":
                    float cu = Mathf.Repeat(u * 2, 1);
                    bool frame = cu < 0.05f || cu > 0.95f || v < 0.12f || v > 0.88f;
                    bool brace = Mathf.Abs((cu - 0.5f) * 1.0f - (v - 0.5f)) < 0.06f;
                    return frame ? 0.6f : brace ? 0.72f : Mathf.Repeat(v * 4, 1) < 0.08f ? 0.82f : 1;
                case "cloud": return 1.05f - 0.15f * (1 - v);
                default: return Mathf.Repeat(u * 7, 1) < 0.03f ? 0.8f : 1;  // plank joins
            }
        }

        static Texture2D MakeThorns()
        {
            int w = 256, h = 64;
            return Make(w, h, false, (x, y) =>
            {
                float u = x / (float)w, v = y / (float)h;
                float stem = 0.5f + 0.06f * Mathf.Sin(u * 19) + 0.03f * Mathf.Sin(u * 53);
                float d = Mathf.Abs(v - stem);
                float a = d < 0.17f ? 1 : 0;
                // thorns every 1/9th, alternating up and down
                float cell = u * 9;
                int i = Mathf.FloorToInt(cell);
                float f = cell - i;
                bool up = i % 2 == 0;
                float tri = 1 - Mathf.Abs(f - 0.5f) * 2;                    // 0 at the edges of the cell, 1 in the middle
                float reach = 0.17f + 0.33f * tri * tri;
                if (up ? v > stem && v - stem < reach : v < stem && stem - v < reach) a = 1;
                float f2 = (cell + 0.5f) - Mathf.Floor(cell + 0.5f);
                float tri2 = 1 - Mathf.Abs(f2 - 0.5f) * 2;
                float reach2 = 0.17f + 0.22f * tri2 * tri2 * tri2;
                if (!up ? v > stem && v - stem < reach2 : v < stem && stem - v < reach2) a = 1;
                if (x < 2 || x > w - 3) a *= 0.5f;
                return new Color(1, 1, 1, a);
            });
        }

        // a golden key: a ring, a shaft and two teeth
        static Texture2D MakeKey()
        {
            return Make(64, 64, false, (x, y) =>
            {
                float u = (x - 31.5f) / 32f, v = (y - 31.5f) / 32f;
                float ring = Mathf.Sqrt(u * u + (v - 0.45f) * (v - 0.45f));
                bool on = ring < 0.36f && ring > 0.17f;
                on |= Mathf.Abs(u) < 0.08f && v < 0.12f && v > -0.85f;
                on |= u > 0 && u < 0.3f && ((v < -0.55f && v > -0.68f) || (v < -0.78f && v > -0.9f));
                if (!on) return new Color(0, 0, 0, 0);
                float shine = 0.75f + 0.25f * Mathf.Clamp01(1 - (u + v + 0.5f));
                return new Color(1f * shine, 0.78f * shine, 0.25f * shine, 1);
            });
        }

        // a padlock for the locked door
        static Texture2D MakePadlock()
        {
            return Make(64, 64, false, (x, y) =>
            {
                float u = (x - 31.5f) / 32f, v = (y - 31.5f) / 32f;
                bool body = Mathf.Abs(u) < 0.5f && v < 0.1f && v > -0.7f;
                float r = Mathf.Sqrt(u * u + (v - 0.1f) * (v - 0.1f));
                bool shackle = v > 0.1f && r < 0.4f && r > 0.25f;
                bool hole = Mathf.Abs(u) < 0.07f && v < -0.15f && v > -0.45f;
                if (hole) return new Color(0.15f, 0.1f, 0.05f, 1);
                if (body) return new Color(0.85f, 0.65f, 0.25f, 1);
                if (shackle) return new Color(0.7f, 0.7f, 0.72f, 1);
                return new Color(0, 0, 0, 0);
            });
        }

        static Texture2D MakeMoth()
        {
            return Make(64, 64, false, (x, y) =>
            {
                float u = (x - 31.5f) / 32f, v = (y - 31.5f) / 32f, au = Mathf.Abs(u);
                bool body = Ell(au, v, 0, -0.02f, 0.08f, 0.36f);
                bool fore = Ell(au, v, 0.36f, 0.14f, 0.34f, 0.26f);
                bool hind = Ell(au, v, 0.24f, -0.2f, 0.22f, 0.2f);
                bool feeler = Mathf.Abs(au - (v - 0.3f) * 0.6f) < 0.025f && v > 0.3f && v < 0.75f;
                if (!(body || fore || hind || feeler)) return new Color(0, 0, 0, 0);
                if (body || feeler) return new Color(0.45f, 0.38f, 0.3f, 1);
                float spot = Mathf.Abs(Vector2.Distance(new Vector2(au, v), new Vector2(0.3f, 0.2f)) - 0.08f) < 0.03f ? 0.65f : 1;
                float vein = 0.9f + 0.1f * Mathf.Sin((au + v) * 40);
                return new Color(0.98f * spot * vein, 0.9f * spot * vein, 0.72f * spot * vein, 1);
            });
        }

        // an arched doorway, bright in the middle
        static Texture2D MakeDoor()
        {
            return Make(64, 128, false, (x, y) =>
            {
                float u = (x - 31.5f) / 32f, v = y / 128f;
                bool inside = Mathf.Abs(u) < 0.92f && (v < 0.7f || u * u + ((v - 0.7f) / 0.3f) * ((v - 0.7f) / 0.3f) < 0.85f) && v > 0.02f;
                if (!inside) return new Color(0, 0, 0, 0);
                float glow = 1 - 0.35f * Mathf.Abs(u) - 0.25f * v;
                return new Color(glow, glow * 0.93f, glow * 0.7f, 1);
            });
        }

        // the hero: a shadow cat. frame 0 idle, 1-2 running, 3 jumping, 4 falling.
        // the eye is a hole the light shines through
        static Texture2D MakeCat(int frame)
        {
            int s = 128;
            return Make(s, s, false, (x, y) =>
            {
                float u = x / (float)s, v = y / (float)s;
                bool on = false;
                float lift = frame == 3 ? 0.03f : 0;
                // a long, low body, stretched out when running
                float stretch = frame == 1 || frame == 2 ? 0.03f : 0;
                on |= Ell(u, v, 0.47f, 0.3f + lift, 0.21f + stretch, 0.12f);
                // head, and two pointed ears
                on |= Ell(u, v, 0.7f, 0.43f + lift, 0.1f, 0.09f);
                on |= Tri(u, v, 0.62f, 0.49f + lift, 0.68f, 0.49f + lift, 0.63f, 0.62f + lift);
                on |= Tri(u, v, 0.71f, 0.5f + lift, 0.78f, 0.47f + lift, 0.77f, 0.6f + lift);
                // a long tail that curls up at the end, swishing as it goes
                float swish = frame == 0 ? 0 : frame == 1 ? 0.04f : frame == 2 ? -0.03f : 0.06f;
                for (float t = 0; t <= 1; t += 0.05f)
                {
                    float tx = 0.27f - 0.14f * t, ty = 0.33f + lift + 0.32f * t * t + swish * t;
                    on |= Ell(u, v, tx + 0.05f * t * t, ty, 0.028f, 0.028f);
                }
                // legs
                if (frame == 0) { on |= Rot(u, v, 0.33f, 0.07f, 0.035f, 0.1f, 0); on |= Rot(u, v, 0.4f, 0.07f, 0.035f, 0.1f, 0); on |= Rot(u, v, 0.58f, 0.07f, 0.035f, 0.1f, 0); on |= Rot(u, v, 0.64f, 0.07f, 0.035f, 0.1f, 0); }
                if (frame == 1) { on |= Rot(u, v, 0.3f, 0.12f, 0.035f, 0.1f, 0.9f); on |= Rot(u, v, 0.66f, 0.11f, 0.035f, 0.1f, -0.9f); }
                if (frame == 2) { on |= Rot(u, v, 0.42f, 0.07f, 0.035f, 0.1f, -0.3f); on |= Rot(u, v, 0.52f, 0.07f, 0.035f, 0.1f, 0.3f); }
                if (frame == 3) { on |= Rot(u, v, 0.3f, 0.2f, 0.035f, 0.1f, 1.2f); on |= Rot(u, v, 0.66f, 0.2f, 0.035f, 0.1f, -1.1f); }
                if (frame == 4) { on |= Rot(u, v, 0.36f, 0.1f, 0.035f, 0.1f, 0.3f); on |= Rot(u, v, 0.6f, 0.1f, 0.035f, 0.1f, -0.3f); }
                // whiskers, and a glowing eye where the light shines through
                if (v > 0.4f + lift && v < 0.42f + lift && u > 0.78f && u < 0.86f) on = true;
                if (Ell(u, v, 0.74f, 0.45f + lift, 0.022f, 0.02f)) return new Color(1, 0.9f, 0.5f, 1);
                return on ? new Color(0.09f, 0.07f, 0.12f, 1) : new Color(0.09f, 0.07f, 0.12f, 0);
            });
        }
        static bool Tri(float u, float v, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (u - bx) * (ay - by) - (ax - bx) * (v - by), d2 = (u - cx) * (by - cy) - (bx - cx) * (v - cy), d3 = (u - ax) * (cy - ay) - (cx - ax) * (v - ay);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        static bool Ell(float u, float v, float cx, float cy, float rx, float ry) { float a = (u - cx) / rx, b = (v - cy) / ry; return a * a + b * b < 1; }
        static bool Rot(float u, float v, float cx, float cy, float rx, float ry, float ang)
        {
            float du = u - cx, dv = v - cy, c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            float ru = du * c - dv * s, rv = du * s + dv * c;
            return Ell(ru, rv, 0, ry, rx, ry);
        }
    }
}
