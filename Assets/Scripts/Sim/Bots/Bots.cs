// the scripted solutions for every level, plus cheats that must NOT work
using System;
using static ShadowSet.Tools.Bot;

namespace ShadowSet.Tools
{
    public static class Bots
    {
        static float HX(Sim s) { return s.hero.x + Hero.W / 2; }
        // the solution: collect every key, then out through the door
        static Bot Keys(int level, params StepFn[] steps) { return new Bot(level, "solution, every key", true, steps); }

        // where the lamp has to be for a prop's shadow (part 0) to be centred on the hero, with a given offset
        static V2 LampFor(Sim s, int prop, float offY)
        {
            var p = s.level.props[prop]; var l = s.lamps[0]; float k = Sim.Scale(l, p); var part = p.parts[0];
            float cx = s.hero.x + Hero.W / 2, cy = s.hero.y + offY;
            // the shadow's centre is l + (partCentre - l) * k; solve for l
            float pcx = part.x + part.w / 2, pcy = part.y + part.h / 2;
            return new V2((cx - pcx * k) / (1 - k), (cy - pcy * k) / (1 - k));
        }
        // keep the veil's shade over the hero
        static StepFn Shade(int prop, float offY, Func<Sim, bool> until, float move = 0)
        {
            return Until(until, (s, b) => { var l = LampFor(s, prop, offY); b.lx = l.x; b.ly = l.y; return new Cmd { move = move }; });
        }
        // walk somewhere, keeping the veil's shade over the hero all the way
        static StepFn ShadedGoTo(int prop, float offY, float x)
        {
            return Until(s => System.Math.Abs(HX(s) - x) < 0.08f && s.hero.grounded,
                (s, b) => { var l = LampFor(s, prop, offY); b.lx = l.x; b.ly = l.y; float dx = x - HX(s); return new Cmd { move = System.Math.Abs(dx) < 0.08f ? 0 : System.Math.Sign(dx) * (System.Math.Abs(dx) < 0.4f ? 0.35f : 0.6f) }; });
        }
        // is that prop's shadow on its way up?
        static bool Rising(Sim s, int prop) { var p = s.level.props[prop]; return Sim.ShadowOf(s.lamps[0], p, p.parts[0], s.time + 0.05f).y > Sim.ShadowOf(s.lamps[0], p, p.parts[0], s.time).y; }
        static Box Shadow(Sim s, int prop, int lamp = 0) { foreach (var o in s.solids) if (o.lamp == lamp && o.prop == prop) return o.box; return new Box(); }
        // stand still while moving the lamp, until the hero gets somewhere
        static StepFn RideUntil(float lx, float ly, Func<Sim, bool> until, float ld = 0)
        {
            return Until(until, (s, b) => { b.lx = lx; b.ly = ly; if (ld > 0) b.ld = ld; return new Cmd(); });
        }

        public static readonly Bot[] All =
        {
            // ------------------------------------------------ 1. Curtain Up
            new Bot(0, "straight for the door, without the keys", false,
                Lamp(28, 10), GoTo(6),
                RideUntil(4, 10, s => s.hero.x + Hero.W > 15.1f), Stop(),
                JumpTo(17.6f, 0.6f),
                RideUntil(4, 10, s => HX(s) > 25.3f), Stop(),
                JumpTo(29, 0.6f), Walk(1, s => false)),
            new Bot(0, "stay on the shelf", false, Walk(1, s => false)),

            // ------------------------------------------------ 2. Stage Lift
            new Bot(1, "straight for the door, without the keys", false,
                Lamp(16, 16), GoTo(9),
                RideUntil(16, 2, s => s.hero.y > 8.95f), Stop(), GoTo(13.6f),
                Lamp(16, 10), GoTo(17),
                RideUntil(16, 2, s => s.hero.y > 14.6f), Stop(),
                JumpTo(23, 0.6f), Walk(1, s => false)),
            new Bot(1, "ride the slow lift into the ceiling", false,
                Lamp(16, 16), GoTo(9), RideUntil(16, 2, s => false)),

            // ------------------------------------------------ 3. Free Hand
            new Bot(2, "straight for the door, without the keys", false,
                Lamp(20, 16.5f), GoTo(5.5f),
                Lamp(16, 9), GoTo(12),
                Lamp(14, 13), JumpTo(15.5f, 0.3f),
                Lamp(6, 13), GoTo(24.6f), JumpTo(29, 0.6f), Walk(1, s => false)),
            new Bot(2, "stay on the first shadow", false,
                Lamp(20, 16.5f), GoTo(5.5f), Lamp(16, 9), GoTo(12), Lamp(6, 16.5f), Walk(1, s => false)),

            // ------------------------------------------------ 4. Parallax
            new Bot(3, "straight for the door, without the keys", false,
                Lamp(28, 12), JumpTo(6, 0.4f),
                Lamp(22, 12), GoTo(9.4f), JumpTo(14, 0.6f),
                Lamp(20, 12), GoTo(19.4f), JumpTo(24, 0.6f),
                Lamp(26, 12), GoTo(17.1f), JumpTo(13, 0.6f),
                Lamp(28, 12), GoTo(8.1f), JumpTo(3, 0.6f), Walk(-1, s => false)),
            new Bot(3, "jump the gaps without lining them up", false,
                Lamp(28, 12), JumpTo(6, 0.4f), GoTo(7.4f), JumpTo(12, 0.6f), Walk(1, s => false)),

            // ------------------------------------------------ 5. Bramble
            new Bot(4, "straight for the door, without the keys", false,
                Lamp(24, 15), GoTo(8.8f),
                Lamp(14, 15), Lamp(14, 7), Lamp(22, 7), GoTo(7.1f),
                Lamp(22, 3), Lamp(8, 3), GoTo(24.8f), JumpTo(28, 0.6f), Walk(1, s => false)),
            new Bot(4, "straight across", false,
                Lamp(24, 15), GoTo(8.8f), Lamp(8, 3), GoTo(24.8f), JumpTo(28, 0.6f), Walk(1, s => false)),
            new Bot(4, "up the right of the maze", false,
                Lamp(24, 15), GoTo(8.8f), Lamp(14, 15), Lamp(14, 3), Lamp(8, 3), GoTo(24.8f), JumpTo(28, 0.6f), Walk(1, s => false)),

            // ------------------------------------------------ 6. Limelight
            new Bot(5, "straight for the door, without the keys", false,
                GoTo(10.5f), Lamp(25, 3.5f), GoTo(18), Lamp(21, 3.5f), GoTo(23.4f), GoTo(30.5f), Walk(1, s => false)),
            new Bot(5, "jump up out of the shade, in the light", false,
                GoTo(10.5f), Lamp(25, 3.5f), GoTo(18), Lamp(21, 3.5f), GoTo(21), JumpTo(21, 0.6f), Walk(1, s => false)),

            // ------------------------------------------------ 7. Close-Up
            new Bot(6, "straight for the door, without the keys", false,
                Lamp(16, 3, 9), GoTo(26.5f), GoTo(31.2f),
                Lamp(27, 3, 9), Lamp(27, 3, 20), JumpTo(27, 0.3f),
                // zoom in, walking back towards the middle of the lift as it swells
                Until(s => s.lamps[0].d <= 8.51f, (s, b) => { b.ld = 8.5f; float dx = 27 - HX(s); return new Cmd { move = System.Math.Abs(dx) > 0.15f ? System.Math.Sign(dx) * 0.6f : 0 }; }),
                GoTo(22.7f), JumpTo(20, 0.6f), Walk(-1, s => false)),
            new Bot(6, "zoom without keeping to the middle", false,
                Lamp(16, 3, 9), GoTo(26.5f), GoTo(31.2f),
                Lamp(27, 3, 9), Lamp(27, 3, 20), JumpTo(27.6f, 0.3f), Lamp(27, 3, 8.5f), GoTo(22.7f), JumpTo(20, 0.6f), Walk(-1, s => false)),

            // ------------------------------------------------ 8. Double Act
            new Bot(7, "straight for the door, without the keys", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(22, 8),
                Switch(), Lamp(16, 8), GoTo(18.5f),
                Lamp(11, 4), GoTo(25.4f), JumpTo(29, 0.5f), Walk(1, s => false)),
            new Bot(7, "one lamp only", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(20, 8), GoTo(16.5f), Lamp(16, 4), GoTo(20.4f), JumpTo(30, 0.6f), Walk(1, s => false)),

            // ------------------------------------------------ 9. Clockwork
            new Bot(8, "straight for the door, without the keys", false,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5),
                // keep to the right-hand end of the swing until the lift comes up to meet it
                Until(s => Shadow(s, 0).Right > 18.6f && Shadow(s, 1).Top > 8.0f && Shadow(s, 1).Top < 9.0f && Rising(s, 1),
                      (s, b) => new Cmd { move = s.hero.x + Hero.W < Shadow(s, 0).Right - 0.4f ? 1 : 0 }),
                JumpTo(21.4f, 0.5f),
                GoTo(23), Wait(s => Shadow(s, 1).Top > 12.3f && Shadow(s, 3).x > 26.5f), JumpTo(28.2f, 0.4f),
                JumpTo(30.5f, 0.5f), Walk(1, s => false)),
            new Bot(8, "jump for the lift any old time", false,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5), WaitT(1.2f), JumpTo(21.4f, 0.5f), Walk(1, s => false)),

            // ------------------------------------------------ 10. Grand Finale
            new Bot(9, "straight for the door, without the keys", false,
                Lamp(19, 12, 12), GoTo(3.5f),
                Lamp(13, 6, 9), GoTo(10.3f),
                Lamp(16, 9, 9), Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), GoTo(21.2f), GoTo(25),
                Lamp(15, 9, 9), Lamp(15, 6.5f, 9), JumpTo(30, 0.6f), Walk(1, s => false)),
            new Bot(9, "no zoom for the first wall", false,
                Lamp(19, 12, 12), GoTo(3.5f), Lamp(13, 6, 12), GoTo(10.3f), Walk(1, s => false)),
            new Bot(9, "straight up the last lift", false,
                Lamp(19, 12, 12), GoTo(3.5f), Lamp(13, 6, 9), GoTo(10.3f),
                Lamp(16, 9, 9), Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), GoTo(21.2f), GoTo(23.2f),
                Lamp(16, 6.5f, 9), Walk(1, s => false)),

            // ------------------------------------------------ the solutions: every key, then the door
            Keys(0,
                Lamp(28, 10), GoTo(6), RideUntil(4, 10, s => s.hero.x + Hero.W > 15.1f), Stop(),
                JumpTo(15.9f, 0.5f), JumpTo(15.9f, 0.6f), JumpTo(17.6f, 0.4f),
                RideUntil(4, 10, s => HX(s) > 27.9f), Stop(), RideUntil(12, 10, s => HX(s) < 25.3f), Stop(),
                JumpTo(29, 0.6f), Walk(1, s => false)),
            Keys(1,
                Lamp(16, 16), GoTo(9), RideUntil(16, 2, s => s.hero.y > 8.95f), Stop(), GoTo(14.6f), JumpTo(14.6f, 0.6f),
                Lamp(16, 10), GoTo(17), Lamp(16, 7.5f), GoTo(17.4f), Lamp(16, 9.17f), GoTo(15.4f), JumpTo(10, 0.6f), GoTo(8.8f),
                GoTo(12), JumpTo(16.6f, 0.6f), RideUntil(16, 2, s => s.hero.y > 14.6f), Stop(), GoTo(18.2f), JumpTo(23, 0.6f), Walk(1, s => false)),
            Keys(2,
                Lamp(20, 16.5f), GoTo(5.5f), Lamp(20, 8), GoTo(4.6f), JumpTo(4.6f, 0.6f),
                Lamp(16, 9), GoTo(10.4f), JumpTo(10.4f, 0.12f), GoTo(12),
                Lamp(14, 13), JumpTo(15.5f, 0.3f), Lamp(6, 13), GoTo(24.6f), Lamp(6, 3), JumpTo(24.6f, 0.6f),
                Lamp(6, 13), GoTo(24.6f), JumpTo(29, 0.6f), Walk(1, s => false)),
            Keys(3,
                Lamp(28, 12), JumpTo(6, 0.4f), Lamp(22, 12), GoTo(9.4f), JumpTo(14, 0.6f),
                Lamp(16.5f, 12), GoTo(30.8f), Lamp(20, 12), GoTo(19.4f), JumpTo(24, 0.6f),
                Lamp(24, 12), GoTo(19.9f), JumpTo(19.9f, 0.6f),
                Lamp(26, 12), GoTo(17.1f), JumpTo(13, 0.6f), Lamp(21.25f, 12), GoTo(23.4f), JumpTo(23.4f, 0.6f),
                Lamp(28, 12), GoTo(8.1f), JumpTo(3, 0.6f), Walk(-1, s => false)),
            Keys(4,
                Lamp(24, 15), GoTo(8.8f), Lamp(19.4f, 15), JumpTo(13.4f, 0.03f),
                Lamp(14, 15), Lamp(14, 7), Lamp(22, 7), GoTo(7.1f),
                Lamp(22, 3), JumpTo(6.6f, 0.6f), Lamp(8, 3), GoTo(24.8f), JumpTo(28, 0.6f), Walk(1, s => false)),
            Keys(5,
                GoTo(10.5f), Lamp(25, 3.5f), GoTo(16.6f), JumpTo(16.6f, 0.6f), GoTo(18),
                Lamp(21, 3.5f), GoTo(20.6f), JumpTo(20.6f, 0.02f), GoTo(23.4f), GoTo(25), GoTo(27.4f), JumpTo(27.4f, 0.6f), Walk(1, s => false)),
            Keys(6,
                Lamp(16, 3, 9), GoTo(12), JumpTo(12, 0.6f), GoTo(16), Lamp(16, 3, 20), Lamp(16, 3, 9), GoTo(26.5f), GoTo(31.2f),
                Lamp(27, 3, 9), Lamp(27, 3, 20), JumpTo(27, 0.3f),
                Until(s => s.lamps[0].d <= 8.51f, (s, b) => { b.ld = 8.5f; float dx = 27 - HX(s); return new Cmd { move = System.Math.Abs(dx) > 0.15f ? System.Math.Sign(dx) * 0.6f : 0 }; }),
                GoTo(30.6f), JumpTo(30.6f, 0.6f), GoTo(22.7f), JumpTo(20, 0.6f), Walk(-1, s => false)),
            Keys(7,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 4), GoTo(4.45f), JumpTo(4.45f, 0.6f), Lamp(28, 8), Lamp(22, 8),
                Switch(), Lamp(16, 8), GoTo(18.5f), Lamp(8, 13.4f), GoTo(27.6f),
                Lamp(16, 4), GoTo(16.4f), JumpTo(15.6f, 0.6f), Lamp(16, 8), JumpTo(17.5f, 0.5f),
                Lamp(11, 4), GoTo(25.4f), JumpTo(29, 0.5f), Walk(1, s => false)),
            Keys(8,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5), Wait(s => Shadow(s, 0).x < 1.3f), JumpTo(5.2f, 0.6f),
                Until(s => Shadow(s, 0).Right > 18.6f && Shadow(s, 1).Top > 8.0f && Shadow(s, 1).Top < 9.0f && Rising(s, 1),
                      (s, b) => new Cmd { move = s.hero.x + Hero.W < Shadow(s, 0).Right - 0.4f ? 1 : 0 }),
                JumpTo(21.4f, 0.5f), Wait(s => Shadow(s, 1).Top > 12.3f), JumpTo(22, 0.6f),
                GoTo(23), Wait(s => Shadow(s, 1).Top > 12.3f && Shadow(s, 3).x > 26.5f), JumpTo(28.2f, 0.4f),
                JumpTo(29.3f, 0.5f), JumpTo(29.3f, 0.6f), Walk(1, s => false)),
            Keys(9,
                Lamp(19, 12, 12), GoTo(3.5f), Lamp(13, 6, 9), GoTo(5.2f), JumpTo(5.2f, 0.6f), GoTo(10.3f),
                Lamp(16, 9, 9), GoTo(13.2f), Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), JumpTo(13.2f, 0.6f),
                Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), GoTo(21.2f), GoTo(25),
                Lamp(15, 9, 9), Lamp(15, 6.5f, 9), JumpTo(28.9f, 0.6f), JumpTo(28.9f, 0.6f), Walk(1, s => false)),
        };

    }
}
