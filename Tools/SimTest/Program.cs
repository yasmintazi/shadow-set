// the level-design tool: draws levels to PNGs and plays them with scripted bots.
//   SimTest draw <level> <lampX> <lampY> [depth] out.png
//   SimTest sweep <level> out.png         every shadow, for lamp positions across its whole area
//   SimTest bots [level]                  play the scripted solutions (and the cheats that shouldn't work)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace ShadowSet.Tools
{
    public static class Program
    {
        const int S = 40, PAD = 60;

        public static int Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (args.Length == 0) { Console.WriteLine("draw | sweep | bots"); return 1; }
            if (args[0] == "draw")
            {
                var sim = new Sim(Levels.All[int.Parse(args[1])]);
                var l = sim.lamps[0];
                l.x = float.Parse(args[2]); l.y = float.Parse(args[3]);
                if (args.Length > 5) l.d = float.Parse(args[4]);
                sim.BuildSolids();
                Draw(sim, null).Save(args[args.Length - 1]);
                return 0;
            }
            if (args[0] == "sweep")
            {
                var sim = new Sim(Levels.All[int.Parse(args[1])]);
                Draw(sim, sim).Save(args[2]);
                return 0;
            }
            if (args[0] == "bots")
            {
                int only = args.Length > 1 ? int.Parse(args[1]) : -1;
                bool ok = true;
                foreach (var b in Bots.All)
                {
                    if (only >= 0 && b.level != only) continue;
                    var r = b.Run();
                    bool pass = r.won == b.shouldWin && (!b.allMoths || r.sim.mothCount == r.sim.level.moths.Length);
                    ok &= pass;
                    Console.WriteLine((pass ? "ok   " : "FAIL ") + "level " + (b.level + 1) + " " + b.name + ": " + r.summary);
                    if (!pass && r.sim != null) Draw(r.sim, null).Save("fail_" + b.level + "_" + b.name.Replace(' ', '_') + ".png");
                }
                return ok ? 0 : 2;
            }
            return 1;
        }

        static float PX(float x) { return PAD + x * S; }
        static float PY(float y) { return PAD + (Sim.WALL_H - y) * S; }

        public static Canvas Draw(Sim sim, Sim sweep)
        {
            var c = new Canvas((int)(Sim.WALL_W * S) + PAD * 2, (int)(Sim.WALL_H * S) + PAD * 2 + 40, 0x202024);
            c.Rect(PX(0), PY(Sim.WALL_H), PX(Sim.WALL_W), PY(0), 0xEFE6D2);
            // a grid every unit, labelled every 2
            for (int x = 0; x <= Sim.WALL_W; x++) c.Rect(PX(x), PY(Sim.WALL_H), PX(x) + 1, PY(0), 0xC9BFA9, x % 2 == 0 ? 0.6f : 0.25f);
            for (int y = 0; y <= Sim.WALL_H; y++) c.Rect(PX(0), PY(y), PX(Sim.WALL_W), PY(y) + 1, 0xC9BFA9, y % 2 == 0 ? 0.6f : 0.25f);
            for (int x = 0; x <= Sim.WALL_W; x += 2) c.Text(x.ToString(), PX(x) - 3, PY(0) + 6, 0xAAAAAA);
            for (int y = 0; y <= Sim.WALL_H; y += 2) c.Text(y.ToString(), PX(0) - 30, PY(y) - 5, 0xAAAAAA);

            var level = sim.level;
            if (sweep != null)
            {
                // shadows for lamp positions all over its area, faintly, so you can see where they can reach
                foreach (var lamp in sweep.lamps)
                {
                    var a = lamp.def.area;
                    for (int i = 0; i <= 8; i++) for (int j = 0; j <= 8; j++) for (int k = 0; k <= 2; k++)
                            {
                                lamp.x = a.x + a.w * i / 8; lamp.y = a.y + a.h * j / 8;
                                lamp.d = lamp.def.minDepth + (lamp.def.maxDepth - lamp.def.minDepth) * k / 2;
                                foreach (var p in level.props)
                                    foreach (var part in p.parts)
                                    {
                                        var b = Sim.ShadowOf(lamp, p, part, 0);
                                        c.Rect(PX(b.x), PY(b.Top), PX(b.Right), PY(b.y), p.thorn ? 0xC03030u : 0x303050u, 0.025f);
                                    }
                            }
                    lamp.x = lamp.def.x; lamp.y = lamp.def.y; lamp.d = lamp.def.depth;
                }
                sweep.BuildSolids();
            }

            foreach (var s in sim.solids)
            {
                var b = s.box;
                if (s.lamp == -1 && s.prop < 0) continue;
                uint col = s.lamp == -1 ? 0x8A5A2Bu : s.thorn ? 0xB02020u : 0x2A2A3Au;
                c.Rect(PX(b.x), PY(b.Top), PX(b.Right), PY(b.y), col, s.lamp == -1 ? 1 : 0.85f);
                if (s.lamp >= 0) c.Text(s.prop.ToString(), PX(b.x) + 3, PY(b.Top) + 3, 0xFFFFFF);
            }
            // the props themselves, as if pressed flat on the wall, outlined in their colours, with their depths
            foreach (var p in level.props)
                foreach (var part in p.parts)
                {
                    var off = p.Offset(sim.time);
                    c.Outline(PX(part.x + off.x), PY(part.Top + off.y), PX(part.Right + off.x), PY(part.y + off.y), p.thorn ? 0xFF6060u : 0x3080FFu, 0.9f);
                    c.Text(p.depth.ToString("0.#"), PX(part.x + off.x) + 2, PY(part.y + off.y) - 13, 0x3080FF);
                }
            var e = level.exit;
            c.Rect(PX(e.x), PY(e.Top), PX(e.Right), PY(e.y), 0x30B060, 0.8f);
            foreach (var m in level.moths) c.Dot(PX(m.Center.x), PY(m.Center.y), 7, 0xE0B020);
            // the lamps and where they can go
            for (int i = 0; i < sim.lamps.Length; i++)
            {
                var l = sim.lamps[i];
                var a = l.def.area;
                c.Outline(PX(a.x) - 1, PY(a.Top) - 1, PX(a.Right) + 2, PY(a.y) + 2, 0xFFA020, 0.8f);
                c.Dot(PX(l.x), PY(l.y), 8, i == sim.active ? 0xFFC020u : 0xC08020u);
                c.Text(l.d.ToString("0.0"), PX(l.x) + 10, PY(l.y) - 5, 0xFF9000);
            }
            var h = sim.hero;
            c.Rect(PX(h.x), PY(h.y + Hero.H), PX(h.x + Hero.W), PY(h.y), sim.dead ? 0xFF0000u : 0x1060FFu);
            c.Text(sim.time.ToString("0.00"), 10, c.h - 30, 0xFFFFFF, 3);
            return c;
        }
    }
}
