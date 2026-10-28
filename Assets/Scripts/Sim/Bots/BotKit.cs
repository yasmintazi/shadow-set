// scripted players. a bot is a list of steps; each step looks at the game and says what to
// press this tick, or returns null to move on to the next step. the lamp target is kept
// between steps, so the hero and the lamp can move at the same time.
using System;
using System.Collections.Generic;

namespace ShadowSet.Tools
{
    public class Mem { public int f; public bool a, b, c; public float v; }
    public class Cmd { public float move; public bool press, hold; }
    public delegate Cmd StepFn(Sim s, Mem m, Bot b);

    public class Result { public bool won; public string summary; public Sim sim; }

    public class Bot
    {
        public int level;
        public string name;
        public bool shouldWin = true;
        public bool allMoths;                   // this run must catch every moth too
        public List<StepFn> steps = new List<StepFn>();
        public float lx = -1, ly = -1, ld;      // where the bot wants the lamp
        public bool sw;                         // switch lamps this tick
        public int maxTicks = 120 * 90;
        public static bool Trace = Environment.GetEnvironmentVariable("TRACE") != null, TraceAll = Environment.GetEnvironmentVariable("TRACE") == "2";

        public Bot(int level, string name, bool shouldWin, params StepFn[] steps)
        {
            this.level = level; this.name = name; this.shouldWin = shouldWin; this.steps.AddRange(steps);
        }

        // playing along with a running game (the title screen's demo, and the screenshot tool)
        int step; Mem mem; List<string> log;
        public void Begin(Sim sim) { lx = sim.lamps[0].x; ly = sim.lamps[0].y; ld = 0; step = 0; mem = new Mem(); log = new List<string>(); }
        public Controls Next(Sim sim, int t)
        {
            Cmd cmd = null;
            for (int guard = 0; guard < 8 && step < steps.Count; guard++)
            {
                cmd = steps[step](sim, mem, this);
                if (cmd != null) break;
                step++; mem = new Mem();
                log.Add(step + "@" + (t / 120f).ToString("0.0") + "(" + sim.hero.x.ToString("0.0") + "," + sim.hero.y.ToString("0.0") + ")");
            }
            if (cmd == null) cmd = new Cmd();
            var c = new Controls { move = cmd.move, jumpPressed = cmd.press, jumpHeld = cmd.hold || cmd.press, aim = true, lampX = lx, lampY = ly, lampDepth = ld, switchLamp = sw };
            sw = false;
            return c;
        }

        public static Bot Solution(int level)
        {
            foreach (var b in Bots.All) if (b.level == level && b.shouldWin && !b.allMoths) return b;
            return null;
        }

        public Result Run()
        {
            var sim = new Sim(Levels.All[level]);
            Begin(sim);
            for (int t = 0; t < maxTicks; t++)
            {
                sim.Step(Next(sim, t));
                if (Trace && (t % 6 == 0 || TraceAll)) Console.WriteLine("  t" + (t / 120f).ToString("0.00") + " step " + step + " hero " + sim.hero.Box + (sim.hero.grounded ? " g" + sim.hero.groundLamp + ":" + sim.hero.groundProp : "") + " lamp " + L(sim));
                if (sim.won) return new Result { won = true, summary = "won in " + (t / 120f).ToString("0.0") + "s, moths " + sim.mothCount + "/" + sim.level.moths.Length + "   " + string.Join(" ", log), sim = sim };
                if (sim.dead) return new Result { won = false, summary = "died (" + sim.cause + ") at step " + step + " t=" + (t / 120f).ToString("0.0") + " hero " + sim.hero.Box + " lamp " + L(sim) + "   " + string.Join(" ", log), sim = sim };
            }
            return new Result { won = false, summary = "timed out at step " + step + " hero " + sim.hero.Box + " lamp " + L(sim) + "   " + string.Join(" ", log), sim = sim };
        }

        static string L(Sim s) { var l = s.lamps[s.active]; return "(" + l.x.ToString("0.0") + "," + l.y.ToString("0.0") + " d" + l.d.ToString("0.0") + ")"; }

        // -------------------------------------------------------- steps
        static float HX(Sim s) { return s.hero.x + Hero.W / 2; }

        // aim the lamp somewhere and carry on straight away
        public static StepFn Aim(float x, float y, float d = 0) { return (s, m, b) => { b.lx = x; b.ly = y; if (d > 0) b.ld = d; return null; }; }
        // aim the lamp and wait (standing still) until it gets there
        public static StepFn Lamp(float x, float y, float d = 0)
        {
            return (s, m, b) =>
            {
                b.lx = x; b.ly = y; if (d > 0) b.ld = d;
                var l = s.lamps[s.active];
                bool there = Math.Abs(l.x - M.Clamp(x, l.def.area.x, l.def.area.Right)) < 0.01f && Math.Abs(l.y - M.Clamp(y, l.def.area.y, l.def.area.Top)) < 0.01f
                             && (d <= 0 || Math.Abs(l.d - M.Clamp(d, l.def.minDepth, l.def.maxDepth)) < 0.01f);
                return there ? null : new Cmd();
            };
        }
        // leave the lamp where it is right now
        public static StepFn Stop() { return (s, m, b) => { b.lx = s.lamps[s.active].x; b.ly = s.lamps[s.active].y; return null; }; }
        public static StepFn Switch() { return (s, m, b) => { if (m.a) return null; m.a = true; b.sw = true; b.lx = s.lamps[(s.active + 1) % s.lamps.Length].x; b.ly = s.lamps[(s.active + 1) % s.lamps.Length].y; return new Cmd(); }; }
        public static StepFn Wait(Func<Sim, bool> until) { return (s, m, b) => until(s) ? null : new Cmd(); }
        public static StepFn WaitT(float seconds) { return (s, m, b) => ++m.f > seconds * 120 ? null : new Cmd(); }
        public static StepFn Walk(int dir, Func<Sim, bool> until) { return (s, m, b) => until(s) ? null : new Cmd { move = dir }; }
        // walk until the middle of the hero is at x, then stop
        public static StepFn GoTo(float x)
        {
            return (s, m, b) =>
            {
                m.f++;
                float dx = x - HX(s);
                if (Math.Abs(dx) < 0.06f && Math.Abs(s.hero.vx) < 0.5f && s.hero.grounded) return null;
                if (m.f > 600) return null;
                if (Math.Abs(dx) < 0.06f) return new Cmd();
                return new Cmd { move = Math.Sign(dx) * (Math.Abs(dx) < 0.4f ? 0.35f : 1) };
            };
        }
        // jump, steering the middle of the hero to x, holding the button for `hold` seconds;
        // finished once landed
        public static StepFn JumpTo(float x, float hold = 0.5f)
        {
            return (s, m, b) =>
            {
                m.f++;
                var c = new Cmd();
                float dx = x - HX(s);
                if (Math.Abs(dx) > 0.05f) c.move = Math.Sign(dx) * (Math.Abs(dx) < 0.3f ? 0.4f : 1);
                if (m.f == 1) { c.press = true; return c; }
                c.hold = m.f < hold * 120;
                if (m.f > 6 && s.hero.grounded) return null;
                if (m.f > 600) return null;
                return c;
            };
        }
        // do something every tick until a condition holds (for moving and steering at once)
        public static StepFn Until(Func<Sim, bool> until, Func<Sim, Bot, Cmd> act) { return (s, m, b) => until(s) ? null : act(s, b); }
    }
}
