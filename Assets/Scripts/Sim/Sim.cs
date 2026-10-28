// the whole game, minus the drawing: the lamps, the shadows they throw, and the hero
// running about on them. it steps at a fixed 120 times a second, so it plays the same
// on every computer, and it has no Unity types, so it can be tested on its own.
using System;
using System.Collections.Generic;

namespace ShadowSet
{
    public class Lamp
    {
        public LampDef def;
        public float x, y, d;           // d is how far it is from the wall
        public Lamp(LampDef def) { this.def = def; x = def.x; y = def.y; d = def.depth; }
    }

    // something solid (or deadly) on the wall this step. lamp/prop say whose shadow it is
    // (lamp -1 is a shelf or the frame of the wall)
    public struct Solid
    {
        public Box box;
        public int lamp, prop, part;
        public bool thorn;
    }

    public class Hero
    {
        public const float W = 0.7f, H = 1.0f;
        public float x, y, vx, vy;      // x, y is the bottom-left corner
        public bool grounded;
        public int groundLamp = -2, groundProp = -1;
        public float coyote, buffer;
        public bool jumping;
        public int facing = 1;
        public float landTimer, runTimer;   // for the animation
        public Box Box { get { return new Box(x, y, W, H); } }
    }

    public struct Controls
    {
        public float move;                  // -1 to 1
        public bool jumpPressed, jumpHeld;
        public bool aim;                    // is there a lamp target this step?
        public float lampX, lampY;          // where the player wants the lamp
        public float lampDepth;             // how far from the wall they want it (0 = leave it)
        public bool switchLamp;             // pick up the other lamp
    }

    public class Sim
    {
        public const float WALL_W = 32, WALL_H = 18;
        public const float STEP = 1f / 120;

        // the hero's movement, in wall units (the wall is 18 tall)
        public const float RUN = 6.8f, ACCEL = 70, AIR_ACCEL = 45, FRICTION = 80;
        public const float GRAVITY = 40, FALL_GRAVITY = 52, MAX_FALL = 24;
        public const float JUMP = 14.5f, JUMP_CUT = 0.45f;     // a jump is about 2.6 units high
        public const float COYOTE = 0.1f, BUFFER = 0.13f;
        // how fast you can drag the lamp around (so shadows can't teleport)
        public const float LAMP_SPEED = 11, LAMP_DEPTH_SPEED = 7;

        public LevelDef level;
        public Lamp[] lamps;
        public int active;              // the lamp the player is holding
        public Hero hero = new Hero();
        public float time;
        public readonly List<Solid> solids = new List<Solid>();
        public bool won, dead;
        public string cause = "";
        public float deadTimer;
        public int deaths;
        public bool[] keysGot;
        public int keyCount;
        public bool atLockedDoor;           // touching the door without all the keys (for the hint)

        // things that happened this step, for sounds and effects
        public bool evJump, evLand, evDie, evWin, evKey, evSwitch;

        public Sim(LevelDef level)
        {
            this.level = level;
            keysGot = new bool[level.keys.Length];
            Restart();
        }

        public void Restart()
        {
            lamps = new Lamp[level.lamps.Length];
            for (int i = 0; i < lamps.Length; i++) lamps[i] = new Lamp(level.lamps[i]);
            active = 0;
            hero = new Hero { x = level.start.x - Hero.W / 2, y = level.start.y };
            time = 0;
            dead = false;
            won = false;
            keysGot = new bool[level.keys.Length];      // a tumble drops the keys back where they were
            keyCount = 0;
            cause = "";
            BuildSolids();
        }

        // ------------------------------------------------------------ shadows
        public static float Scale(Lamp l, PropDef p) { return l.d / (l.d - p.depth); }

        // where a point on a prop lands on the wall, lit by this lamp
        public static V2 Project(Lamp l, PropDef p, V2 point, float time)
        {
            float k = Scale(l, p);
            V2 off = p.Offset(time);
            return new V2(l.x + (point.x + off.x - l.x) * k, l.y + (point.y + off.y - l.y) * k);
        }

        public static Box ShadowOf(Lamp l, PropDef p, Box part, float time)
        {
            float k = Scale(l, p);
            V2 c = Project(l, p, new V2(part.x, part.y), time);
            return new Box(c.x, c.y, part.w * k, part.h * k);
        }

        // the point on a prop whose shadow is at `wallPoint`
        static V2 Unproject(Lamp l, PropDef p, V2 wallPoint, float time)
        {
            float k = Scale(l, p);
            V2 off = p.Offset(time);
            return new V2(l.x + (wallPoint.x - l.x) / k - off.x, l.y + (wallPoint.y - l.y) / k - off.y);
        }

        public void BuildSolids()
        {
            solids.Clear();
            // the frame of the wall: the sides and the top. the bottom is open: below the wall is the dark stage
            solids.Add(new Solid { box = new Box(-10, -40, 10, 80), lamp = -1, prop = -1 });
            solids.Add(new Solid { box = new Box(WALL_W, -40, 10, 80), lamp = -1, prop = -1 });
            solids.Add(new Solid { box = new Box(-10, WALL_H, WALL_W + 20, 10), lamp = -1, prop = -1 });
            for (int i = 0; i < level.shelves.Length; i++)
                solids.Add(new Solid { box = level.shelves[i], lamp = -1, prop = i });
            for (int li = 0; li < lamps.Length; li++)
                for (int pi = 0; pi < level.props.Length; pi++)
                {
                    PropDef p = level.props[pi];
                    for (int k = 0; k < p.parts.Length; k++)
                        solids.Add(new Solid { box = ShadowOf(lamps[li], p, p.parts[k], time), lamp = li, prop = pi, part = k, thorn = p.thorn });
                }
        }

        // ------------------------------------------------------------ stepping
        public void Step(Controls c)
        {
            evJump = evLand = evDie = evWin = evKey = evSwitch = false;
            if (won) return;
            if (dead)
            {
                deadTimer -= STEP;
                if (deadTimer <= 0) Restart();
                return;
            }

            if (c.switchLamp && lamps.Length > 1) { active = (active + 1) % lamps.Length; evSwitch = true; }

            // remember where the hero's feet were on the shadow they're standing on, before it moves
            Hero h = hero;
            bool riding = h.groundProp >= 0 && h.groundLamp >= 0;
            V2 foot = new V2(h.x + Hero.W / 2, h.y), propPoint = new V2();
            if (riding) propPoint = Unproject(lamps[h.groundLamp], level.props[h.groundProp], foot, time);

            MoveLamp(c);
            time += STEP;
            BuildSolids();

            // the shadow under the hero carries them along with it
            if (riding)
            {
                V2 to = Project(lamps[h.groundLamp], level.props[h.groundProp], propPoint, time);
                h.x += to.x - foot.x;
                h.y += to.y - foot.y;
            }
            // and shadows sweeping into the hero push them out of the way, or crush them
            if (!PushOut()) { Die("squashed"); return; }

            MoveHero(c);
            if (dead) return;

            Box hb = h.Box;
            foreach (Solid s in solids)
                if (s.thorn && hb.Overlaps(s.box, 0.14f)) { Die("thorns"); return; }
            if (h.y < -2.5f) { Die("fell"); return; }
            for (int i = 0; i < level.keys.Length; i++)
                if (!keysGot[i] && hb.Overlaps(level.keys[i])) { keysGot[i] = true; keyCount++; evKey = true; }
            bool unlocked = keyCount >= level.keys.Length;
            atLockedDoor = hb.Overlaps(level.exit, 0.2f) && !unlocked;
            if (hb.Overlaps(level.exit, 0.2f) && unlocked) { won = true; evWin = true; }
        }

        void MoveLamp(Controls c)
        {
            Lamp l = lamps[active];
            LampDef d = l.def;
            if (c.aim)
            {
                float tx = M.Clamp(c.lampX, d.area.x, d.area.Right), ty = M.Clamp(c.lampY, d.area.y, d.area.Top);
                V2 delta = new V2(tx - l.x, ty - l.y);
                float len = delta.Length, max = LAMP_SPEED * STEP;
                if (len > max) delta = delta * (max / len);
                l.x += delta.x;
                l.y += delta.y;
            }
            if (c.lampDepth > 0)
            {
                float td = M.Clamp(c.lampDepth, d.minDepth, d.maxDepth);
                l.d = M.Approach(l.d, td, LAMP_DEPTH_SPEED * STEP);
            }
        }

        // nudge the hero out of anything solid they're inside. false if there's no way out
        bool PushOut()
        {
            Hero h = hero;
            for (int iter = 0; iter < 4; iter++)
            {
                bool any = false;
                foreach (Solid s in solids)
                {
                    if (s.thorn) continue;
                    Box hb = h.Box, b = s.box;
                    if (!hb.Overlaps(b, 0.001f)) continue;
                    any = true;
                    float left = hb.Right - b.x, right = b.Right - hb.x, down = hb.Top - b.y, up = b.Top - hb.y;
                    float min = Math.Min(Math.Min(left, right), Math.Min(down, up));
                    if (min == up) { h.y += up; if (h.vy < 0) h.vy = 0; h.grounded = true; h.groundLamp = s.lamp; h.groundProp = s.lamp >= 0 ? s.prop : -1; }
                    else if (min == down) { h.y -= down; if (h.vy > 0) h.vy = 0; }
                    else if (min == left) { h.x -= left; }
                    else { h.x += right; }
                }
                if (!any) return true;
            }
            Box fb = h.Box;
            foreach (Solid s in solids)
                if (!s.thorn && fb.Overlaps(s.box, 0.01f)) return false;
            return true;
        }

        void MoveHero(Controls c)
        {
            Hero h = hero;
            float target = c.move * RUN;
            float accel = h.grounded ? (Math.Abs(c.move) > 0.01f ? ACCEL : FRICTION) : AIR_ACCEL;
            h.vx = M.Approach(h.vx, target, accel * STEP);
            if (c.move > 0.01f) h.facing = 1; else if (c.move < -0.01f) h.facing = -1;

            h.coyote = h.grounded ? COYOTE : Math.Max(0, h.coyote - STEP);
            h.buffer = c.jumpPressed ? BUFFER : Math.Max(0, h.buffer - STEP);
            if (h.buffer > 0 && h.coyote > 0)
            {
                h.vy = JUMP;
                h.buffer = 0;
                h.coyote = 0;
                h.jumping = true;
                h.grounded = false;
                h.groundLamp = -2; h.groundProp = -1;
                evJump = true;
            }
            if (h.jumping && !c.jumpHeld && h.vy > 0) { h.vy *= JUMP_CUT; h.jumping = false; }
            if (h.vy <= 0) h.jumping = false;

            h.vy -= (h.vy > 0 ? GRAVITY : FALL_GRAVITY) * STEP;
            if (h.vy < -MAX_FALL) h.vy = -MAX_FALL;

            // across, then up or down, stopping at anything solid. only things the hero
            // was clear of before the move stop them (PushOut deals with anything they're in)
            float x0 = h.x, y0 = h.y;
            h.x += h.vx * STEP;
            foreach (Solid s in solids)
            {
                if (s.thorn || !h.Box.Overlaps(s.box, 0.001f)) continue;
                // a lip only a little higher than your feet is just stepped up on to
                if (h.vy <= 0 && s.box.Top - y0 <= 0.2f && s.box.Top > y0) { h.y = s.box.Top; continue; }
                if (h.vx > 0 && x0 + Hero.W <= s.box.x + 0.01f) h.x = s.box.x - Hero.W;
                else if (h.vx < 0 && x0 >= s.box.Right - 0.01f) h.x = s.box.Right;
                else continue;
                h.vx = 0;
            }

            bool wasGrounded = h.grounded;
            float fallSpeed = h.vy;
            h.grounded = false;
            h.y += h.vy * STEP;
            foreach (Solid s in solids)
            {
                if (s.thorn || !h.Box.Overlaps(s.box, 0.001f)) continue;
                if (h.vy <= 0 ? y0 < s.box.Top - 0.01f : y0 + Hero.H > s.box.y + 0.01f) continue;
                if (h.vy <= 0)
                {
                    h.y = s.box.Top;
                    h.grounded = true;
                    h.groundLamp = s.lamp;
                    h.groundProp = s.lamp >= 0 ? s.prop : -1;
                }
                else h.y = s.box.y - Hero.H;
                h.vy = 0;
            }
            if (!h.grounded) { h.groundLamp = -2; h.groundProp = -1; }
            if (h.grounded && !wasGrounded && fallSpeed < -6) { evLand = true; h.landTimer = 0.15f; }
            h.landTimer = Math.Max(0, h.landTimer - STEP);
            h.runTimer += Math.Abs(h.vx) * STEP;
        }

        void Die(string why)
        {
            dead = true;
            cause = why;
            deadTimer = 0.7f;
            deaths++;
            evDie = true;
        }
    }
}
