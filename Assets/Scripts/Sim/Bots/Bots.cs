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
                JumpTo(29, 0.6f), Lamp(4, 10), GoTo(29.6f), JumpTo(25.5f, 0.6f),
                Lamp(13, 10), GoTo(12.9f), JumpTo(10.5f, 0.6f), GoTo(10), Wait(s => false)),
            new Bot(0, "stay on the shelf", false, Walk(1, s => false)),
            new Bot(0, "jump from the low shadow up to the high one", false,
                Lamp(28, 10), GoTo(6), GoTo(4.3f), JumpTo(1.5f, 0.6f), Lamp(13, 10), GoTo(12.9f), JumpTo(10.5f, 0.6f), GoTo(10), Wait(s => false)),

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
                Lamp(20, 9), Lamp(16, 9), GoTo(12),
                Lamp(14, 13), JumpTo(15.5f, 0.3f),
                Lamp(6, 13), GoTo(24.6f), JumpTo(29, 0.6f), Walk(1, s => false)),
            new Bot(2, "stay on the first shadow", false,
                Lamp(20, 16.5f), GoTo(5.5f), Lamp(20, 9), Lamp(16, 9), GoTo(12), Lamp(6, 16.5f), Walk(1, s => false)),
            new Bot(2, "lift straight through the thorn", false,
                Lamp(20, 16.5f), GoTo(5.5f), Lamp(16, 9), GoTo(12), Wait(s => false)),

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
                Lamp(8, 15), GoTo(23.2f),
                Lamp(18, 15), Lamp(18, 7), Lamp(10, 7), GoTo(24.9f),
                Lamp(10, 3), Lamp(24, 3), GoTo(7.2f), JumpTo(4, 0.6f), Walk(-1, s => false)),
            new Bot(4, "straight across", false,
                Lamp(8, 15), GoTo(23.2f), Lamp(24, 3), GoTo(7.2f), JumpTo(4, 0.6f), Walk(-1, s => false)),
            new Bot(4, "up the left of the maze", false,
                Lamp(8, 15), GoTo(23.2f), Lamp(18, 15), Lamp(18, 3), Lamp(24, 3), GoTo(7.2f), JumpTo(4, 0.6f), Walk(-1, s => false)),

            // ------------------------------------------------ 6. Puppet Stairs
            new Bot(5, "straight for the door, without the keys", false,
                GoTo(5.5f), JumpTo(8.5f, 0.5f), JumpTo(11.5f, 0.5f), Lamp(24, 9.5f), Lamp(21, 9.5f), GoTo(13.4f), Lamp(16, 9.5f),
                JumpTo(17, 0.3f), Lamp(5, 9.5f), JumpTo(30.5f, 0.5f), Lamp(5, 5.5f), GoTo(29.5f), JumpTo(26, 0.5f), Walk(-1, s => false)),
            new Bot(5, "stay on the top step under the thorns", false,
                GoTo(5.5f), JumpTo(8.5f, 0.5f), JumpTo(11.5f, 0.5f), Lamp(24, 9.5f), Lamp(21, 9.5f), GoTo(15.5f), Lamp(5, 9.5f), Walk(1, s => false)),
            new Bot(5, "drop a step before clearing the wall", false,
                GoTo(5.5f), JumpTo(8.5f, 0.5f), JumpTo(11.5f, 0.5f), Lamp(24, 9.5f), Lamp(21, 9.5f), GoTo(13.4f), JumpTo(12, 0.3f), Lamp(5, 9.5f), Walk(1, s => false)),

            // ------------------------------------------------ 7. Double Act
            new Bot(6, "straight for the door, without the keys", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(21, 8),
                Switch(), Lamp(16, 8), GoTo(19),
                Switch(), Lamp(21, 4), Switch(), Lamp(16, 4), GoTo(13),
                Switch(), Lamp(28, 4), GoTo(4.5f), JumpTo(2, 0.6f), Walk(-1, s => false)),
            new Bot(6, "one lamp only", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(21, 8), GoTo(15.9f), Lamp(21, 4), Wait(s => false)),
            new Bot(6, "lift straight up to the door", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 4), GoTo(4.5f), JumpTo(2, 0.6f), Walk(-1, s => false)),
            new Bot(6, "jump back across without the left shadow", false,
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(21, 8),
                Switch(), Lamp(16, 8), GoTo(19), Lamp(16, 4), GoTo(16.4f), JumpTo(11, 0.6f), Walk(-1, s => false)),

            // ------------------------------------------------ 8. Clockwork
            new Bot(7, "straight for the door, without the keys", false,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5),
                // keep to the right-hand end of the swing until the lift comes up to meet it
                Until(s => Shadow(s, 0).Right > 18.6f && Shadow(s, 1).Top > 8.0f && Shadow(s, 1).Top < 9.0f && Rising(s, 1),
                      (s, b) => new Cmd { move = s.hero.x + Hero.W < Shadow(s, 0).Right - 0.4f ? 1 : 0 }),
                JumpTo(21.4f, 0.5f),
                GoTo(23), Wait(s => Shadow(s, 1).Top > 12.3f && Shadow(s, 3).x > 26.5f), JumpTo(28.2f, 0.4f),
                JumpTo(30.5f, 0.5f), Walk(1, s => false)),
            new Bot(7, "jump for the lift any old time", false,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5), WaitT(1.2f), JumpTo(21.4f, 0.5f), Walk(1, s => false)),

            // ------------------------------------------------ 9. Close-Up
            new Bot(8, "don't zoom", false, Walk(1, s => false)),
            new Bot(8, "ride the cloud without keeping to the middle", false,
                Lamp(16, 3, 9), GoTo(16), Lamp(16, 3, 20), GoTo(13.4f), JumpTo(16.6f, 0.6f), Lamp(16, 3, 9), Walk(-1, s => false)),

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
                JumpTo(15.9f, 0.5f), JumpTo(17.6f, 0.4f),
                RideUntil(4, 10, s => HX(s) > 27.9f), Stop(), RideUntil(12, 10, s => HX(s) < 25.3f), Stop(),
                // up on to the shelf, then the high shadow back over to the far left and on to the door
                JumpTo(29, 0.6f), Lamp(4, 10), GoTo(29.6f), JumpTo(25.5f, 0.6f),
                Lamp(28, 10), JumpTo(1.4f, 0.6f),
                Lamp(13, 10), GoTo(12.9f), JumpTo(10.5f, 0.6f), Walk(-1, s => false)),
            Keys(1,
                Lamp(16, 16), GoTo(9), RideUntil(16, 2, s => s.hero.y > 8.95f), Stop(), GoTo(14.6f), JumpTo(14.6f, 0.6f),
                Lamp(16, 10), GoTo(17), Lamp(16, 7.5f), GoTo(17.4f), Lamp(16, 9.17f), GoTo(15.4f), JumpTo(10, 0.6f), GoTo(8.8f),
                GoTo(12), JumpTo(16.6f, 0.6f), RideUntil(16, 2, s => s.hero.y > 14.6f), Stop(), GoTo(18.2f), JumpTo(23, 0.6f), Walk(1, s => false)),
            Keys(2,
                Lamp(20, 16.5f), GoTo(5.5f), Lamp(20, 8), GoTo(4.6f), JumpTo(4.6f, 0.6f),
                Lamp(20, 9), Lamp(16, 9), GoTo(10.4f), JumpTo(10.4f, 0.12f), GoTo(12),
                Lamp(14, 13), JumpTo(15.5f, 0.3f), Lamp(6, 13), GoTo(24.6f), Lamp(6, 3), JumpTo(24.6f, 0.6f),
                Lamp(6, 13), GoTo(24.6f), JumpTo(29, 0.6f), Walk(1, s => false)),
            Keys(3,
                Lamp(28, 12), JumpTo(6, 0.4f), Lamp(22, 12), GoTo(9.4f), JumpTo(14, 0.6f),
                Lamp(16.5f, 12), GoTo(30.8f), Lamp(20, 12), GoTo(19.4f), JumpTo(24, 0.6f),
                Lamp(24, 12), GoTo(19.9f), JumpTo(19.9f, 0.6f),
                Lamp(26, 12), GoTo(17.1f), JumpTo(13, 0.6f), Lamp(21.25f, 12), GoTo(23.4f), JumpTo(23.4f, 0.6f),
                Lamp(28, 12), GoTo(8.1f), JumpTo(3, 0.6f), Walk(-1, s => false)),
            Keys(4,
                Lamp(8, 15), GoTo(23.2f), Lamp(12.6f, 15), JumpTo(18.6f, 0.03f),
                Lamp(18, 15), Lamp(18, 7), Lamp(10, 7), GoTo(24.9f),
                Lamp(10, 3), JumpTo(25.4f, 0.6f), Lamp(24, 3), GoTo(7.2f), JumpTo(4, 0.6f), Walk(-1, s => false)),
            Keys(5,
                GoTo(5.5f), JumpTo(8.5f, 0.5f), JumpTo(11.5f, 0.5f), JumpTo(11.4f, 0.6f),
                Lamp(24, 9.5f), Lamp(21, 9.5f), GoTo(13.4f),
                // carry the stairs past the wall, drop to the middle step, ride under the thorns, climb back up
                Lamp(16, 9.5f), JumpTo(17, 0.3f), Lamp(5, 9.5f), JumpTo(30.5f, 0.5f),
                Lamp(5, 5.5f), JumpTo(31.3f, 0.6f), GoTo(29.5f), JumpTo(26, 0.5f), Walk(-1, s => false)),
            Keys(6,
                // ride the left shadow up under the thorns, park it against the wall, walk over on to the right one
                Lamp(28, 14), JumpTo(6, 0.4f), Lamp(28, 8), Lamp(21, 8),
                Switch(), Lamp(16, 8), GoTo(19),
                // park the left shadow again, up above the thorns, then take the right one round both far corners
                Switch(), Lamp(21, 4), Switch(),
                Lamp(8, 13.4f), GoTo(27.9f), Lamp(4, 4), GoTo(29.9f), JumpTo(29.9f, 0.6f),
                // and back over to the left one where they meet, and up to the door
                Lamp(16, 4), GoTo(16), JumpTo(16, 0.6f), GoTo(13),
                Switch(), Lamp(28, 4), GoTo(4.5f), JumpTo(2, 0.6f), Walk(-1, s => false)),
            Keys(7,
                Wait(s => Shadow(s, 0).x < 3.5f), GoTo(5), Wait(s => Shadow(s, 0).x < 1.3f), JumpTo(5.2f, 0.6f),
                Until(s => Shadow(s, 0).Right > 18.6f && Shadow(s, 1).Top > 8.0f && Shadow(s, 1).Top < 9.0f && Rising(s, 1),
                      (s, b) => new Cmd { move = s.hero.x + Hero.W < Shadow(s, 0).Right - 0.4f ? 1 : 0 }),
                JumpTo(21.4f, 0.5f), Wait(s => Shadow(s, 1).Top > 12.3f), JumpTo(22, 0.6f),
                GoTo(23), Wait(s => Shadow(s, 1).Top > 12.3f && Shadow(s, 3).x > 26.5f), JumpTo(28.2f, 0.4f),
                JumpTo(29.3f, 0.5f), JumpTo(29.3f, 0.6f), Walk(1, s => false)),
            Keys(8,
                Lamp(16, 3, 9), GoTo(12), JumpTo(12, 0.6f), Lamp(16, 3, 8.5f), GoTo(25.7f), JumpTo(25.8f, 0.6f),
                GoTo(16), Lamp(16, 3, 20), GoTo(13.4f), JumpTo(16, 0.6f),
                // zoom back in, walking back towards the middle of the cloud as it swells
                Until(s => s.lamps[0].d <= 9.01f, (s, b) => { b.ld = 9; float dx = 16 - HX(s); return new Cmd { move = System.Math.Abs(dx) > 0.15f ? System.Math.Sign(dx) * 0.6f : 0 }; }),
                GoTo(18.6f), JumpTo(18.6f, 0.6f), GoTo(12.8f), JumpTo(9, 0.6f), Walk(-1, s => false)),
            Keys(9,
                Lamp(19, 12, 12), GoTo(3.5f), Lamp(13, 6, 9), GoTo(5.2f), JumpTo(5.2f, 0.6f), GoTo(10.3f),
                Lamp(16, 9, 9), GoTo(13.2f), Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), JumpTo(13.2f, 0.6f),
                Wait(s => Shadow(s, 2).y > 12.4f && Rising(s, 2)), GoTo(21.2f), GoTo(25),
                Lamp(15, 9, 9), Lamp(15, 6.5f, 9), JumpTo(28.9f, 0.6f), JumpTo(28.9f, 0.6f), Walk(1, s => false)),
        };

    }
}
