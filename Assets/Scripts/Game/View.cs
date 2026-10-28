// builds the little theatre (the paper wall, the stage, curtains, the lamp) and, for each
// level, the cut-outs, shelves, door and moths. every frame it moves everything to match
// the simulation, and redraws the shadows from where the lamp is.
// wall coordinates (x 0-32, y 0-18) map to the world as (x - 16, y, -depth)
using System.Collections.Generic;
using UnityEngine;

namespace ShadowSet
{
    public class View : MonoBehaviour
    {
        public Camera cam;
        Transform room, levelRoot;
        Material edgeMat, wallMat, shadowMat, thornShadowMat, shelfMat, heroMat, glowMat, beamMat, rigMat, rayMat, doorMat, mothMat;
        readonly List<Transform> shadowQuads = new List<Transform>();
        readonly List<MeshRenderer> shadowRenderers = new List<MeshRenderer>();
        readonly List<GameObject> propObjects = new List<GameObject>();
        readonly List<Material> propMats = new List<Material>();
        readonly List<Transform> moths = new List<Transform>();
        readonly List<Transform> spotQuads = new List<Transform>(), spotCores = new List<Transform>();
        readonly List<Transform> thornHalos = new List<Transform>();      // a pulsing red warning round every thorny shadow
        Material haloMat;
        readonly List<LineRenderer> rays = new List<LineRenderer>();
        LampView[] lampViews = new LampView[0];
        Transform hero, door, doorGlow;
        MeshRenderer heroRenderer;
        Sim sim;
        float shake;
        Vector3 camHome;
        readonly Vector3 camLook = new Vector3(0.6f, 8.3f, 0);
        Vector3 sway;
        readonly List<Particle> particles = new List<Particle>();
        public float peek;          // 0-1: how see-through the cut-outs are (hold Shift)

        class LampView { public Transform root, bulb, glow, shade, cord, beam; public Transform[] rig; }
        class Particle { public Transform t; public Vector3 v; public float life, spin; }

        public static Vector3 W(float x, float y, float d = 0) { return new Vector3(x - 16, y, -d); }

        public void Build()
        {
            Art.Init();
            room = new GameObject("Room").transform;
            room.SetParent(transform, false);

            // the camera, looking at the wall from the seats
            cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetParent(transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.03f, 0.045f);
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 120;
            // a seat a little left of centre and up in the circle, so you can see the cut-outs
            // hanging at their different depths, and the lamp out in front of them
            camHome = new Vector3(-5.5f, 13.5f, -35);
            cam.transform.position = camHome;
            cam.transform.LookAt(camLook);
            cam.gameObject.AddComponent<AudioListener>();

            // the paper wall, with a thin wooden frame round it
            wallMat = Art.Lit(Art.Paper, new Color(0.98f, 0.93f, 0.83f), false, 0.0f);
            wallMat.mainTextureScale = new Vector2(4, 2.25f);
            var wall = Art.Quad("Wall", room, wallMat, true);
            wall.transform.position = W(0, 0);
            wall.transform.localScale = new Vector3(Sim.WALL_W, Sim.WALL_H, 1);
            var frameMat = Art.Lit(Art.Wood, new Color(0.55f, 0.4f, 0.3f));
            Frame(-0.5f, -0.5f, 33, 0.5f, frameMat); Frame(-0.5f, 18, 33, 0.5f, frameMat);
            Frame(-0.5f, -0.5f, 0.5f, 19, frameMat); Frame(32, -0.5f, 0.5f, 19, frameMat);
            // the dark stage under the screen, and the floorboards further down
            var stageMat = Art.Lit(Art.Wood, new Color(0.45f, 0.36f, 0.3f));
            stageMat.mainTextureScale = new Vector2(8, 6);
            var floor = Art.Box("Stage", room, stageMat);
            floor.transform.position = new Vector3(0, -3.6f, -14);
            floor.transform.localScale = new Vector3(60, 0.4f, 30);
            // curtains either side, and a pelmet along the top
            var curtainMat = Art.Lit(Art.Curtain, Color.white, false, 0.6f);
            curtainMat.mainTextureScale = new Vector2(3, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                var c = Art.Quad("Curtain", room, curtainMat);
                c.transform.position = new Vector3(side * 21.5f, 9, -3);
                c.transform.localScale = new Vector3(9, 30, 1);
                c.transform.rotation = Quaternion.Euler(0, side * -25, 0);
            }
            var pelmet = Art.Quad("Pelmet", room, curtainMat);
            pelmet.transform.position = new Vector3(0, 21.6f, -4);
            pelmet.transform.localScale = new Vector3(60, 4, 1);

            shadowMat = Art.Unlit(Art.SoftRect, new Color(0.13f, 0.11f, 0.2f, 1), Art.Blend.Multiply, 2900);
            thornShadowMat = Art.Unlit(Art.Thorns, new Color(0.35f, 0.02f, 0.05f, 1), Art.Blend.Multiply, 2901);
            shelfMat = Art.Lit(Art.Wood, Color.white, false, 0.5f);
            heroMat = Art.Unlit(Art.Cat[0], Color.white, Art.Blend.Alpha, 3100);
            glowMat = Art.Unlit(Art.Glow, new Color(1, 0.8f, 0.5f, 0.6f), Art.Blend.Add, 3200);
            beamMat = Art.Unlit(Art.Glow, new Color(1, 0.85f, 0.6f, 0.035f), Art.Blend.Add, 3150);
            rigMat = Art.Unlit(Art.Dots, new Color(1, 0.75f, 0.4f, 0.22f), Art.Blend.Add, 3050);
            rayMat = Art.Unlit(Texture2D.whiteTexture, new Color(1, 0.85f, 0.5f, 0.25f), Art.Blend.Add, 3060);
            doorMat = Art.Unlit(Art.Door, new Color(1, 0.95f, 0.8f, 1), Art.Blend.Alpha, 2950);
            mothMat = Art.Unlit(Art.Moth, new Color(1, 0.95f, 0.85f, 1), Art.Blend.Alpha, 3090);
            edgeMat = Art.Lit(Texture2D.whiteTexture, new Color(0.25f, 0.18f, 0.12f, 0.8f), true, 0.6f);
            edgeMat.renderQueue = 2995;

            var h = Art.Quad("Hero", transform, heroMat);
            hero = h.transform;
            heroRenderer = h.GetComponent<MeshRenderer>();
            FitCamera();
        }

        // every prop's shadow is its silhouette: one multiply material per kind of prop
        readonly Dictionary<string, Material> shadowMats = new Dictionary<string, Material>();
        Material ShadowMat(PropDef p)
        {
            if (p.thorn) return thornShadowMat;
            Material m;
            if (!shadowMats.TryGetValue(p.look, out m))
                // gauze shade is a cool blue wash, drawn over the spotlights so it visibly blocks them
                shadowMats[p.look] = m = p.gauze
                    ? Art.Unlit(Art.Prop(p.look), new Color(0.3f, 0.38f, 0.72f, 1), Art.Blend.Multiply, 2905)
                    : Art.Unlit(Art.Prop(p.look), shadowMat.color, Art.Blend.Multiply, 2900);
            return m;
        }

        void Frame(float x, float y, float w, float h, Material m)
        {
            var b = Art.Box("Frame", room, m);
            b.transform.position = W(x + w / 2, y + h / 2, 0.15f);
            b.transform.localScale = new Vector3(w, h, 0.3f);
        }

        // frame the wall (plus a bit round it) whatever shape the window is
        public void FitCamera()
        {
            float dist = Mathf.Abs(camHome.z);
            float aspect = Mathf.Max(0.5f, cam.aspect);
            float halfH = Mathf.Max(11.6f, 19.6f / aspect);
            cam.fieldOfView = 2 * Mathf.Atan(halfH / dist) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------ per level
        public void Load(Sim s)
        {
            sim = s;
            if (levelRoot) Destroy(levelRoot.gameObject);
            levelRoot = new GameObject("Level").transform;
            levelRoot.SetParent(transform, false);
            foreach (var t in shadowQuads) Destroy(t.gameObject);
            shadowQuads.Clear(); shadowRenderers.Clear(); propObjects.Clear(); propMats.Clear(); moths.Clear(); rays.Clear();
            foreach (var p in particles) Destroy(p.t.gameObject);
            particles.Clear();
            var level = s.level;

            // shelves nailed to the wall
            foreach (var b in level.shelves)
            {
                var go = Art.Box("Shelf", levelRoot, shelfMat);
                go.transform.position = W(b.x + b.w / 2, b.y + b.h / 2, 0.25f);
                go.transform.localScale = new Vector3(b.w, b.h, 0.5f);
            }

            // the cut-outs: card shapes hanging on rods from the flies
            for (int pi = 0; pi < level.props.Length; pi++)
            {
                var p = level.props[pi];
                var holder = new GameObject("Prop " + pi + " (" + p.look + ")");
                holder.transform.SetParent(levelRoot, false);
                var col = Art.Tints[p.tint % Art.Tints.Length];
                var mat = Art.Lit(Art.Prop(p.look), new Color(col.r, col.g, col.b, 0.85f), true, 0.6f);
                propMats.Add(mat);
                foreach (var part in p.parts)
                {
                    var q = Art.Quad("Card", holder.transform, mat, true);
                    q.transform.localPosition = W(part.x, part.y, p.depth);
                    q.transform.localScale = new Vector3(part.w, part.h, 1);
                    // a rod up to the flies
                    var rod = Art.Box("Rod", holder.transform, Art.Lit(Art.Rope, new Color(0.3f, 0.28f, 0.25f, 0.6f), true));
                    float top = part.Top, len = 21 - top;
                    rod.transform.localPosition = W(part.x + part.w / 2, top + len / 2, p.depth);
                    rod.transform.localScale = new Vector3(0.05f, len, 0.05f);
                }
                propObjects.Add(holder);
            }

            // a shadow quad for every part of every prop, from every lamp
            int count = 0;
            foreach (var p in level.props) count += p.parts.Length;
            count *= level.lamps.Length;
            for (int i = 0; i < count; i++)
            {
                var q = Art.Quad("Shadow", transform, shadowMat, true);
                shadowQuads.Add(q.transform);
                shadowRenderers.Add(q.GetComponent<MeshRenderer>());
            }

            // the spotlights: a hot white glow on the wall, too bright for a shadow to survive
            spotQuads.Clear(); spotCores.Clear(); thornHalos.Clear();
            foreach (var sp in level.spots)
            {
                var q = Art.Quad("Spotlight", levelRoot, Art.Unlit(Art.Halo, new Color(1, 0.93f, 0.75f, 0.75f), Art.Blend.Add, 2880), true);
                spotQuads.Add(q.transform);
                var core = Art.Quad("SpotCore", levelRoot, Art.Unlit(Art.Glow, new Color(1, 0.97f, 0.85f, 0.5f), Art.Blend.Add, 2881));
                spotCores.Add(core.transform);
            }
            haloMat = Art.Unlit(Art.Halo, new Color(1, 0.12f, 0.08f, 0.5f), Art.Blend.Add, 2899);
            for (int li = 0; li < level.lamps.Length; li++)
                foreach (var p in level.props)
                    if (p.thorn) foreach (var part in p.parts) thornHalos.Add(Art.Quad("ThornWarning", levelRoot, haloMat, true).transform);

            // the way out: a lit doorway cut in the wall
            door = Art.Quad("Door", levelRoot, doorMat, true).transform;
            door.position = W(level.exit.x - 0.2f, level.exit.y, 0.01f);
            door.localScale = new Vector3(level.exit.w + 0.4f, level.exit.h + 0.2f, 1);
            doorGlow = Art.Quad("DoorGlow", levelRoot, Art.Unlit(Art.Glow, new Color(1, 0.85f, 0.55f, 0.5f), Art.Blend.Add, 3210)).transform;
            doorGlow.position = W(level.exit.Center.x, level.exit.Center.y, 0.05f);
            doorGlow.localScale = Vector3.one * 5;

            foreach (var m in level.moths)
            {
                var q = Art.Quad("Moth", levelRoot, mothMat);
                q.transform.position = W(m.Center.x, m.Center.y, 0.03f);
                q.transform.localScale = Vector3.one * 0.9f;
                var halo = Art.Quad("MothGlow", q.transform, Art.Unlit(Art.Glow, new Color(1, 0.9f, 0.6f, 0.35f), Art.Blend.Add, 3085));
                halo.transform.localPosition = new Vector3(0, 0, 0.01f);
                halo.transform.localScale = Vector3.one * 2.2f;
                moths.Add(q.transform);
            }

            // the lamps
            foreach (var lv in lampViews) if (lv != null && lv.root) Destroy(lv.root.gameObject);
            lampViews = new LampView[level.lamps.Length];
            for (int i = 0; i < lampViews.Length; i++) lampViews[i] = MakeLamp(level.lamps[i]);

            // the light rays you see while peeking: four per cut-out, from the lamp past its corners
            for (int i = 0; i < count * 4 && i < 64; i++)
            {
                var lr = new GameObject("Ray").AddComponent<LineRenderer>();
                lr.transform.SetParent(levelRoot, false);
                lr.sharedMaterial = rayMat;
                lr.positionCount = 3;
                lr.startWidth = lr.endWidth = 0.03f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.enabled = false;
                rays.Add(lr);
            }
        }

        LampView MakeLamp(LampDef def)
        {
            var v = new LampView();
            v.root = new GameObject("Lamp").transform;
            v.root.SetParent(levelRoot, false);
            var brass = Art.Unlit(Texture2D.whiteTexture, new Color(0.16f, 0.12f, 0.09f, 1), Art.Blend.Alpha, 2990);
            var shadeGo = new GameObject("Shade");
            shadeGo.transform.SetParent(v.root, false);
            shadeGo.AddComponent<MeshFilter>().sharedMesh = ConeMesh(0.25f, 0.75f, 0.8f, 20, true);
            shadeGo.AddComponent<MeshRenderer>().sharedMaterial = brass;
            v.shade = shadeGo.transform;
            v.shade.localPosition = new Vector3(0, 0, -0.35f);
            var bulb = Art.Sphere("Bulb", v.root, Art.Unlit(Texture2D.whiteTexture, new Color(1, 0.95f, 0.8f, 1), Art.Blend.Alpha, 3000));
            bulb.transform.localScale = Vector3.one * 0.42f;
            v.bulb = bulb.transform;
            v.glow = Art.Quad("Glow", v.root, glowMat).transform;
            v.glow.localScale = Vector3.one * 4.5f;
            var cord = Art.Box("Cord", v.root, Art.Lit(Texture2D.whiteTexture, new Color(0.15f, 0.13f, 0.12f)));
            v.cord = cord.transform;
            // a faint cone of light from the lamp to the wall
            var beam = new GameObject("Beam");
            beam.transform.SetParent(v.root, false);
            beam.AddComponent<MeshFilter>().sharedMesh = ConeMesh(0.005f, 1f, 1f, 32, false);
            beam.AddComponent<MeshRenderer>().sharedMaterial = beamMat;
            v.beam = beam.transform;
            // the rig: a dotted outline of where the lamp can go
            v.rig = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var q = Art.Quad("Rig", levelRoot, rigMat);
                v.rig[i] = q.transform;
            }
            return v;
        }

        // a cone (or a tube that opens out) along +z, from radius r0 at z=0 to r1 at z=len
        static Mesh ConeMesh(float r0, float r1, float len, int n, bool doubleSided)
        {
            var m = new Mesh();
            var vs = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                vs.Add(dir * r0); vs.Add(dir * r1 + Vector3.forward * len);
                uv.Add(new Vector2(0.5f, 0.5f)); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 2;
                tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
                if (doubleSided) tris.AddRange(new[] { a, a + 2, a + 1, a + 2, a + 3, a + 1 });
            }
            m.SetVertices(vs); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return m;
        }

        // ------------------------------------------------------------ every frame
        // `prev` is the lamps and clock one step ago, so motion can be smoothed between steps
        public void Sync(float alpha, Vector3[] prevLamps, float prevTime, Vector2 prevHero, float dt)
        {
            if (sim == null) return;
            var level = sim.level;
            float time = Mathf.Lerp(prevTime, sim.time, alpha);
            var lamps = new Lamp[sim.lamps.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                var l = sim.lamps[i];
                var p = prevLamps != null && i < prevLamps.Length ? prevLamps[i] : new Vector3(l.x, l.y, l.d);
                lamps[i] = new Lamp(l.def) { x = Mathf.Lerp(p.x, l.x, alpha), y = Mathf.Lerp(p.y, l.y, alpha), d = Mathf.Lerp(p.z, l.d, alpha) };
            }

            // the lamps, and the light they throw on everything (through the shaders)
            for (int i = 0; i < lamps.Length; i++)
            {
                var v = lampViews[i];
                var pos = W(lamps[i].x, lamps[i].y, lamps[i].d);
                v.root.position = pos;
                bool held = i == sim.active;
                float flicker = 1 + Mathf.Sin(Time.time * 23) * 0.01f + Mathf.Sin(Time.time * 7.3f) * 0.01f;
                v.glow.rotation = cam.transform.rotation;
                v.glow.localScale = Vector3.one * (held ? 4.5f : 3.2f) * flicker;
                v.cord.position = new Vector3(pos.x, (pos.y + 0.25f + 23) / 2, pos.z);
                v.cord.localScale = new Vector3(0.05f, 23 - pos.y - 0.25f, 0.05f);
                v.beam.localPosition = Vector3.zero;
                v.beam.localScale = new Vector3(lamps[i].d * 0.9f, lamps[i].d * 0.9f, lamps[i].d);
                // the rig outline
                var a = lamps[i].def.area;
                float pad = 0.4f;
                SetBar(v.rig[0], a.x - pad, a.Top + pad, a.Right + pad, a.Top + pad, lamps[i].d);
                SetBar(v.rig[1], a.x - pad, a.y - pad, a.Right + pad, a.y - pad, lamps[i].d);
                SetBar(v.rig[2], a.x - pad, a.y - pad, a.x - pad, a.Top + pad, lamps[i].d);
                SetBar(v.rig[3], a.Right + pad, a.y - pad, a.Right + pad, a.Top + pad, lamps[i].d);
                foreach (var r in v.rig) r.gameObject.SetActive(held);
                Shader.SetGlobalVector("_LampPos" + i, new Vector4(pos.x, pos.y, pos.z, (held ? 2.0f : 1.4f) * flicker));
            }
            if (lamps.Length < 2) Shader.SetGlobalVector("_LampPos1", Vector4.zero);
            Shader.SetGlobalVector("_LampColor", new Color(1, 0.86f, 0.64f));
            Shader.SetGlobalVector("_Ambient", new Color(0.13f, 0.13f, 0.21f));

            // shadows, recomputed from the smoothed lamp positions
            int n = 0;
            for (int li = 0; li < lamps.Length; li++)
                for (int pi = 0; pi < level.props.Length; pi++)
                {
                    var p = level.props[pi];
                    foreach (var part in p.parts)
                    {
                        var b = Sim.ShadowOf(lamps[li], p, part, time);
                        var q = shadowQuads[n];
                        q.position = W(b.x, b.y, 0.01f + 0.001f * n);
                        q.localScale = new Vector3(b.w, b.h, 1);
                        shadowRenderers[n].sharedMaterial = ShadowMat(p);
                        n++;
                    }
                }

            // spotlights (some of them sweep about), and the thorns' warning glow
            for (int i = 0; i < spotQuads.Count; i++)
            {
                var b = level.spots[i].At(time);
                float pad = 0.5f;
                spotQuads[i].position = W(b.x - pad, b.y - pad, 0.02f);
                spotQuads[i].localScale = new Vector3(b.w + 2 * pad, b.h + 2 * pad, 1);
                spotCores[i].position = W(b.Center.x, b.Center.y, 0.025f);
                spotCores[i].localScale = new Vector3(b.w * 1.3f, b.h * 1.1f, 1);
            }
            int hi = 0;
            for (int li = 0; li < lamps.Length; li++)
                foreach (var p in level.props)
                    if (p.thorn)
                        foreach (var part in p.parts)
                        {
                            var b = Sim.ShadowOf(lamps[li], p, part, time);
                            float pad = 0.45f;
                            thornHalos[hi].position = W(b.x - pad, b.y - pad, 0.009f);
                            thornHalos[hi].localScale = new Vector3(b.w + 2 * pad, b.h + 2 * pad, 1);
                            hi++;
                        }
            if (haloMat != null) { var hc = haloMat.color; hc.a = 0.35f + 0.25f * Mathf.Sin(Time.time * 5); haloMat.color = hc; }

            // the cut-outs, swaying on their motors, and faded right down while peeking
            int ri = 0;
            for (int pi = 0; pi < level.props.Length; pi++)
            {
                var p = level.props[pi];
                var off = p.Offset(time);
                propObjects[pi].transform.localPosition = new Vector3(off.x, off.y, 0);
                var c = propMats[pi].color;
                c.a = Mathf.Lerp(0.8f, 0.12f, peek);
                propMats[pi].color = c;
                foreach (var part in p.parts)
                {
                    var corners = new[] { new V2(part.x, part.y), new V2(part.Right, part.y), new V2(part.x, part.Top), new V2(part.Right, part.Top) };
                    foreach (var corner in corners)
                    {
                        if (ri >= rays.Count) break;
                        var lamp = lamps[sim.active];
                        var on = Sim.Project(lamp, p, corner, time);
                        var lr = rays[ri++];
                        lr.enabled = peek > 0.05f;
                        if (!lr.enabled) continue;
                        lr.SetPosition(0, W(lamp.x, lamp.y, lamp.d));
                        lr.SetPosition(1, W(corner.x + off.x, corner.y + off.y, p.depth));
                        lr.SetPosition(2, W(on.x, on.y, 0.02f));
                        var col = rayMat.color; col.a = 0.3f * peek; rayMat.color = col;
                    }
                }
            }
            for (; ri < rays.Count; ri++) rays[ri].enabled = false;

            // the hero
            var hpos = Vector2.Lerp(prevHero, new Vector2(sim.hero.x, sim.hero.y), alpha);
            var h = sim.hero;
            int frame = !h.grounded ? (h.vy > 0 ? 3 : 4) : Mathf.Abs(h.vx) > 0.5f ? 1 + (int)(h.runTimer * 1.6f) % 2 : 0;
            heroMat.mainTexture = Art.Cat[frame];
            float squash = h.landTimer > 0 ? 1 - h.landTimer * 1.2f : 1;
            float size = 1.85f;
            hero.position = W(hpos.x + Hero.W / 2 - h.facing * 0.05f, hpos.y + size * 0.5f * squash - size * 0.07f, 0.04f);
            hero.localScale = new Vector3(size * h.facing * (2 - squash), size * squash, 1);
            heroRenderer.enabled = !sim.dead && !sim.won;

            // moths flutter where they are until you catch them
            for (int i = 0; i < moths.Count; i++)
            {
                var m = level.moths[i];
                bool got = sim.mothsGot[i];
                var t = moths[i];
                if (got) { t.localScale = Vector3.Lerp(t.localScale, Vector3.zero, dt * 8); continue; }
                float ph = i * 1.7f;
                t.position = W(m.Center.x + Mathf.Sin(Time.time * 1.3f + ph) * 0.25f, m.Center.y + Mathf.Sin(Time.time * 2.1f + ph) * 0.2f, 0.03f);
                t.localScale = new Vector3(1.2f * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 11 + ph))), 1.2f, 1);
            }

            doorGlow.rotation = Quaternion.identity;
            doorGlow.localScale = Vector3.one * (4.5f + Mathf.Sin(Time.time * 2) * 0.4f);

            // particles and camera shake
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.life -= dt;
                p.v.y -= 18 * dt;
                p.t.position += p.v * dt;
                p.t.Rotate(0, 0, p.spin * dt);
                p.t.localScale *= 1 - dt * 1.5f;
                if (p.life <= 0) { Destroy(p.t.gameObject); particles.RemoveAt(i); }
            }
            shake = Mathf.Max(0, shake - dt * 3);
            // the camera leans a touch with the lamp, so the depth reads
            var heldLamp = lamps[sim.active];
            sway = Vector3.Lerp(sway, new Vector3((heldLamp.x - 16) * 0.12f, (heldLamp.y - 9) * 0.06f, 0), dt * 2);
            cam.transform.position = camHome + sway + (Vector3)(Random.insideUnitCircle * shake * 0.25f);
            cam.transform.LookAt(camLook + sway * 0.3f);
        }

        void SetBar(Transform t, float x0, float y0, float x1, float y1, float d)
        {
            var a = W(x0, y0, d); var b = W(x1, y1, d);
            t.position = (a + b) / 2;
            float len = Vector3.Distance(a, b);
            bool horizontal = Mathf.Abs(y1 - y0) < 0.001f;
            t.rotation = Quaternion.Euler(0, 0, horizontal ? 0 : 90);
            t.localScale = new Vector3(Mathf.Max(len, 0.3f), 0.18f, 1);
            var mr = t.GetComponent<MeshRenderer>();
            mr.material.mainTextureScale = new Vector2(Mathf.Max(len, 0.3f) * 1.2f, 1);
        }

        // the hero bursts into scraps of shadow
        public void Burst(Color col, int count, float speed)
        {
            var h = sim.hero;
            var mat = Art.Unlit(Art.SoftRect, col, Art.Blend.Alpha, 3120);
            for (int i = 0; i < count; i++)
            {
                var q = Art.Quad("Bit", levelRoot, mat).transform;
                q.position = W(h.x + Hero.W / 2, h.y + Hero.H / 2, 0.05f);
                q.localScale = Vector3.one * Random.Range(0.12f, 0.3f);
                particles.Add(new Particle { t = q, v = (Vector3)(Random.insideUnitCircle * speed) + Vector3.up * speed * 0.5f, life = Random.Range(0.4f, 0.9f), spin = Random.Range(-400f, 400f) });
            }
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        // where a ray from the camera through a screen point meets the plane `depth` in front of the wall
        public Vector2 ScreenToWall(Vector3 screen, float depth)
        {
            var ray = cam.ScreenPointToRay(screen);
            float z = -depth;
            if (Mathf.Abs(ray.direction.z) < 1e-4f) return Vector2.zero;
            float t = (z - ray.origin.z) / ray.direction.z;
            var p = ray.origin + ray.direction * t;
            return new Vector2(p.x + 16, p.y);
        }

        public Vector2 WallToScreen(float x, float y, float d = 0)
        {
            var s = cam.WorldToScreenPoint(W(x, y, d));
            return new Vector2(s.x, Screen.height - s.y);
        }
    }
}
