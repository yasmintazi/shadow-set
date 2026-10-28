// the levels. the wall is 32 wide and 18 tall, measured from its bottom-left corner, and the
// lamp is `depth` units in front of it. a prop hanging `d` in front of the wall throws a shadow
// k = depth / (depth - d) times its size, so (with the lamp 12 away):
//   d 3 -> k 1.33: the shadow moves a third as far as the lamp, the other way
//   d 6 -> k 2:    the shadow moves exactly as far as the lamp, the other way
//   d 8 -> k 3:    twice as far
//   d 9 -> k 4:    three times as far
// the hero jumps about 2.6 high and 4.5 across.
namespace ShadowSet
{
    public static class Levels
    {
        const float CHUNK = 1.3f;       // how thick a prop's shadow is, at least
        static Box B(float x, float y, float w, float h) { return new Box(x, y, w, h); }
        static V2 P(float x, float y) { return new V2(x, y); }

        // a cut-out whose shadow (with the lamp at lx, ly, depth D) lands with its bottom-left
        // corner at (sx, sy) and is sw x sh. much easier to design with than raw prop positions
        static PropDef Cut(string look, float d, float D, float lx, float ly, float sx, float sy, float sw, float sh, int tint = 0, bool thorn = false, float chunk = CHUNK)
        {
            float k = D / (D - d);
            // props are chunky so they read as props: they hang further down, never higher up,
            // so the edge you stand on stays exactly where it was designed
            if (!thorn && sh < chunk) { sy -= chunk - sh; sh = chunk; }
            return new PropDef
            {
                look = look, depth = d, tint = tint, thorn = thorn,
                parts = new[] { B(lx + (sx - lx) / k, ly + (sy - ly) / k, sw / k, sh / k) }
            };
        }

        // a see-through gauze cut-out: its shadow isn't solid, but it's shade from the spotlights
        static PropDef Gauze(string look, float d, float D, float lx, float ly, float sx, float sy, float sw, float sh, int tint = 1)
        {
            var g = Cut(look, d, D, lx, ly, sx, sy, sw, sh, tint);
            g.gauze = true;
            return g;
        }

        static SpotDef Spot(float x, float y, float w, float h, float swingX = 0, float period = 0, float phase = 0)
        {
            return new SpotDef { box = B(x, y, w, h), swing = P(swingX, 0), period = period, phase = phase };
        }

        static PropDef Moving(PropDef p, float sx, float sy, float period, float phase = 0)
        {
            p.swing = P(sx, sy); p.period = period; p.phase = phase;
            return p;
        }

        public static readonly LevelDef[] All =
        {
            // 1. ride a shadow across. the lamp slides along a rail; its shadow slides the other way
            new LevelDef
            {
                name = "Curtain Up",
                tips = new[] { "Move the mouse to slide the lamp. Shadows are solid.", "Stand on a shadow and move the lamp to ride it." },
                start = P(2, 5.5f),
                exit = B(29.6f, 7.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 5, 4, 0.5f), B(27, 7, 5, 0.5f), B(15.6f, 5.6f, 0.6f, 1.8f) },
                props = new[] { Cut("bridge", 6, 12, 16, 10, 16, 4.8f, 6, 0.7f) },
                lamps = new[] { new LampDef(16, 10, 12, B(4, 10, 24, 0)) },
                moths = new[] { B(15.5f, 8.6f, 0.8f, 0.8f), B(16f, 10.8f, 0.8f, 0.8f), B(27.5f, 5.8f, 0.8f, 0.8f) }
            },
            // 2. lifts. the lamp moves up and down; a cut-out nearer the lamp moves faster
            new LevelDef
            {
                name = "Stage Lift",
                tips = new[] { "Lower the lamp and the shadows rise.", "Cut-outs nearer the lamp throw faster shadows. Don't get squashed!" },
                start = P(2, 1.5f),
                exit = B(28.5f, 15.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 1, 7, 0.5f), B(6.5f, 11, 6, 0.5f), B(12, 8.5f, 3, 0.5f), B(20, 15, 12, 0.5f) },
                props = new[]
                {
                    Cut("cloud", 6, 12, 16, 16, 7, 0.8f, 5, 0.7f, 0),     // a slow lift, in the column under the ceiling
                    Cut("cloud", 9, 12, 16, 10, 15, 8.2f, 4, 0.8f, 1)     // a fast lift on the right
                },
                lamps = new[] { new LampDef(16, 16, 12, B(16, 2, 0, 14)) },
                moths = new[] { B(8.5f, 11.8f, 0.8f, 0.8f), B(17f, 16.9f, 0.8f, 0.8f), B(14.2f, 10.6f, 0.8f, 0.8f) }
            },
            // 3. the lamp goes anywhere. one shadow lifts you over the wall; it can't get under the
            // beam on the far side, but the slow one at the bottom can
            new LevelDef
            {
                name = "Free Hand",
                tips = new[] { "Now the lamp goes anywhere you like.", "Some shadows can't go everywhere. Change ride when you need to." },
                start = P(2, 4.5f),
                exit = B(29.6f, 2.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 4, 4, 0.5f), B(10, 0, 1, 12), B(8, 14.2f, 9, 0.5f), B(21, 5, 1, 13), B(27, 2, 5, 0.5f) },
                props = new[]
                {
                    Cut("boat", 6, 12, 20, 16.5f, 4, 3.8f, 5, 0.7f, 0),
                    Cut("bridge", 3, 12, 13, 10, 14, 2.8f, 9, 0.7f, 2)
                },
                lamps = new[] { new LampDef(20, 16.5f, 12, B(6, 3, 14, 13.5f)) },
                moths = new[] { B(24.2f, 6.6f, 0.8f, 0.8f), B(10.4f, 13.2f, 0.8f, 0.8f), B(4.2f, 15.4f, 0.8f, 0.8f) }
            },
            // 4. four stepping stones hanging at four depths, so they slide at four speeds.
            // line each one up with the next, climb, then come back along the top
            new LevelDef
            {
                name = "Parallax",
                tips = new[] { "Each cut-out hangs at its own depth, so each shadow slides at its own speed.", "Line them up into a staircase." },
                start = P(1.5f, 6.5f),
                exit = B(1.2f, 13.7f, 1.4f, 2.2f),
                shelves = new[] { B(0, 6, 3, 0.5f), B(0, 13.2f, 6, 0.5f) },
                props = new[]
                {
                    Cut("crate", 3, 12, 28, 12, 4.5f, 6.0f, 3.5f, 0.5f, 0, false, 0.75f),
                    Cut("cloud", 9, 12, 22, 12, 12.5f, 7.8f, 3.5f, 0.5f, 1, false, 0.75f),
                    Cut("boat", 6, 12, 20, 12, 22.5f, 9.6f, 3.5f, 0.5f, 2, false, 0.75f),
                    Cut("crate", 8, 12, 26, 12, 11.5f, 11.4f, 3.5f, 0.5f, 3, false, 0.75f)
                },
                lamps = new[] { new LampDef(28, 12, 12, B(4, 12, 24, 0)) },
                moths = new[] { B(23f, 14.2f, 0.8f, 0.8f), B(30f, 7.8f, 0.8f, 0.8f), B(19.6f, 12.4f, 0.8f, 0.8f) }
            },
            // 5. thorny branches, hung near the wall so their shadows barely move: a maze. your lift
            // moves three times as fast as the maze, so you steer it through the gaps
            new LevelDef
            {
                name = "Bramble",
                tips = new[] { "Thorny cut-outs throw thorny shadows. Don't touch them!", "Shadows near the wall hardly move. Steer your lift through the maze." },
                start = P(2, 1.5f),
                exit = B(28.5f, 15.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 1, 4.5f, 0.5f), B(26, 15, 6, 0.5f) },
                props = new[]
                {
                    Cut("cloud", 6, 12, 24, 15, 4.5f, 0.9f, 5, 0.6f, 0),
                    Cut("thorns", 3, 12, 24, 15, 2, 4.5f, 12, 1, 4, true),
                    Cut("thorns", 3, 12, 22, 7, 9, 11, 13, 1, 4, true)
                },
                lamps = new[] { new LampDef(24, 15, 12, B(8, 3, 16, 12)) },
                moths = new[] { B(6.2f, 14.8f, 0.8f, 0.8f), B(13f, 2.7f, 0.8f, 0.8f), B(8f, 9.9f, 0.8f, 0.8f) }
            },
            // 6. spotlights. the hero is a shadow, so a bright beam burns. a gauze veil's shadow isn't
            // solid, but it's shade: park it over the first beam, carry it across the second, then
            // ride a boat that has its own canopy over the pit
            new LevelDef
            {
                name = "Limelight",
                tips = new[] { "You're a shadow: the spotlights burn! Keep in the shade.", "Gauze shadows aren't solid, but they're shade. Carry it with you." },
                start = P(2, 2.5f),
                exit = B(30.3f, 2.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 2, 24, 0.5f), B(29.5f, 2, 2.5f, 0.5f) },
                props = new[]
                {
                    Gauze("veil", 6, 12, 16, 10, 7.5f, 2.3f, 4.5f, 3.2f),
                    Cut("boat", 8, 12, 6, 10, 21, 1.2f, 4, 1.3f, 1),
                    Gauze("canopy", 8, 12, 6, 10, 20.75f, 2.3f, 4.5f, 3.5f)
                },
                spots = new[] { Spot(8, 2.5f, 3, 7), Spot(14, 2.5f, 8, 7), Spot(23.5f, 0, 5.5f, 10) },
                lamps = new[] { new LampDef(16, 10, 12, B(3, 10, 25, 0)) },
                moths = new[] { B(12.2f, 5.6f, 0.8f, 0.8f), B(17.6f, 3.9f, 0.8f, 0.8f), B(26.3f, 3.8f, 0.8f, 0.8f) }
            },
            // 6. the lamp comes nearer or goes further. near, the shadows swell outwards from it;
            // a plank becomes a bridge, a scrap becomes a lift, and you have to keep to the middle of it
            new LevelDef
            {
                name = "Close-Up",
                tips = new[] { "Scroll (or Q and E) to bring the lamp nearer: shadows grow.", "Shadows grow outwards from the lamp. Stay near the middle!" },
                start = P(2, 4.5f),
                exit = B(18.5f, 14.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 4, 6.6f, 0.5f), B(25, 4, 7, 0.5f), B(17, 14, 6, 0.5f) },
                props = new[]
                {
                    Prop("bridge", 6, 0, B(13, 3.2f, 6, 0.3f)),
                    Prop("cloud", 7.5f, 1, B(26.4f, 4.1f, 1.2f, 0.15f)),
                    Prop("thorns", 4, 4, B(27.92f, 7.05f, 1.24f, 1.05f), true)
                },
                lamps = new[] { new LampDef(16, 3, 20, B(12, 3, 16, 0), 8.5f, 20) },
                moths = new[] { B(11.6f, 7.1f, 0.8f, 0.8f), B(30.6f, 15.2f, 0.8f, 0.8f), B(20.4f, 7.2f, 0.8f, 0.8f) }
            },
            // 7. two lamps, one each side. each throws its own shadow of the same cut-out, so
            // you park one lamp to hold a shadow still, and carry on with the other
            new LevelDef
            {
                name = "Double Act",
                tips = new[] { "Two lamps! Right click (or Tab) to pick up the other one.", "The lamp you put down stays put, and so does its shadow." },
                start = P(2, 3.5f),
                exit = B(29.3f, 15, 1.4f, 2.2f),
                shelves = new[] { B(0, 3, 4, 0.5f), B(15, 0, 1, 11), B(27, 14.5f, 5, 0.5f) },
                props = new[] { Prop("bridge", 6, 3, B(16, 9.2f, 2.5f, 0.3f)) },
                lamps = new[]
                {
                    new LampDef(28, 14, 12, B(16, 4, 12, 10)),
                    new LampDef(10, 14, 12, B(4, 4, 12, 10))
                },
                moths = new[] { B(15.1f, 15.5f, 0.8f, 0.8f), B(3.6f, 16.1f, 0.8f, 0.8f), B(27.5f, 6f, 0.8f, 0.8f) }
            },
            // 8. cut-outs on motors swing all by themselves. time your jumps, or nudge the lamp
            // to hold a shadow still for a moment
            new LevelDef
            {
                name = "Clockwork",
                tips = new[] { "These cut-outs are on motors. Their shadows swing on their own.", "Nudge the lamp to hold a shadow steady, or time your jumps." },
                start = P(2, 8.5f),
                exit = B(29.8f, 14, 1.4f, 2.2f),
                shelves = new[] { B(0, 8, 4, 0.5f), B(29, 13.5f, 3, 0.5f) },
                props = new[]
                {
                    Moving(Cut("boat", 6, 12, 16, 10, 7, 7.9f, 6, 0.6f, 0), 3, 0, 4, 0),
                    Moving(Cut("cloud", 8, 12, 16, 10, 20, 7.9f, 4, 0.6f, 1), 0, 4 / 3f, 4, 0.75f),
                    Moving(Cut("thorns", 9, 12, 16, 10, 18.6f, 9, 0.8f, 1.2f, 4, true), 0, 1, 2, 0.25f),
                    Moving(Cut("boat", 4, 12, 16, 10, 22.5f, 11.9f, 5, 0.6f, 2), 3, 0, 4, 0.75f)
                },
                lamps = new[] { new LampDef(16, 10, 12, B(14, 9, 4, 2)) },
                moths = new[] { B(21.6f, 14.6f, 0.8f, 0.8f), B(4.8f, 11.3f, 0.8f, 0.8f), B(29.2f, 16.3f, 0.8f, 0.8f) }
            },
            // a follow spot sweeps the whole wall. climb the steps, hiding under the veil's shade
            // whenever it comes past, and only jump while it's away
            new LevelDef
            {
                name = "Follow Spot",
                tips = new[] { "A follow spot sweeps the stage. Hide in the veil's shade when it passes.", "Jumping takes you out of the shade: wait till the light's gone by." },
                start = P(2, 2.5f),
                exit = B(29.5f, 10.5f, 1.4f, 2.2f),
                shelves = new[] { B(0, 2, 5, 0.5f), B(7.5f, 4, 4, 0.5f), B(14, 6, 4, 0.5f), B(20.5f, 8, 4, 0.5f), B(27, 10, 5, 0.5f) },
                props = new[] { Gauze("veil", 6, 12, 16, 10, 7, 4.3f, 4.5f, 3.5f) },
                spots = new[] { Spot(13.5f, 0, 5, 18, 12, 8, 0) },
                lamps = new[] { new LampDef(23, 12, 12, B(4, 6, 20, 8)) },
                moths = new[] { B(9.1f, 7.6f, 0.8f, 0.8f), B(15.6f, 9.6f, 0.8f, 0.8f), B(22.1f, 11.6f, 0.8f, 0.8f) }
            },
            // 9. the finale: three rooms. zoom the lift over the first wall, zoom the bridge across
            // the second under a thorn piston, then steer the fast lift round a bramble to the door
            new LevelDef
            {
                name = "Grand Finale",
                tips = new[] { "Everything you've learned, all at once.", "Move, zoom, time it. Take a bow at the top!" },
                start = P(1, 3.3f),
                exit = B(30, 16, 1.4f, 2),
                shelves = new[] { B(0, 2.8f, 2, 0.5f), B(9.5f, 0, 1.5f, 11), B(20.5f, 0, 1.5f, 11), B(28.5f, 15.5f, 3.5f, 0.5f) },
                props = new[]
                {
                    Cut("cloud", 6, 9, 13, 6, 5, 10.4f, 5, 0.6f, 0),
                    Cut("bridge", 4, 9, 16, 9, 10.5f, 10.4f, 10.5f, 0.6f, 2),
                    Moving(Cut("thorns", 4.5f, 9, 16, 9, 15, 12.5f, 1.5f, 1.5f, 4, true), 0, 1.25f, 2.4f, 0),
                    Cut("cloud", 6, 9, 16, 9, 22.5f, 10.4f, 3, 0.6f, 1),
                    Cut("thorns", 2, 9, 16, 9, 22, 13.8f, 3, 0.8f, 4, true)
                },
                lamps = new[] { new LampDef(19, 12, 12, B(13, 6, 6, 6), 9, 15) },
                moths = new[] { B(4.6f, 13f, 0.8f, 0.8f), B(13f, 13.4f, 0.8f, 0.8f), B(28.6f, 17.1f, 0.8f, 0.8f) }
            },
        };

        static PropDef Prop(string look, float d, int tint, Box part, bool thorn = false, float chunkScale = 2)
        {
            // as chunky as Cut's props, measured at the scale the shadow is designed for
            if (!thorn && part.h < CHUNK / chunkScale) { float h = CHUNK / chunkScale; part = B(part.x, part.Top - h, part.w, h); }
            return new PropDef { look = look, depth = d, tint = tint, thorn = thorn, parts = new[] { part } };
        }
    }
}
