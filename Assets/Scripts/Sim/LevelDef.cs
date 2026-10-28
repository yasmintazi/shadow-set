// what a level is made of. the wall is 32 x 18 units, measured from its bottom-left corner.
// everything you can stand on (apart from a few shelves nailed to the wall) is a shadow.
namespace ShadowSet
{
    // a paper cut-out hanging between the lamp and the wall, parallel to the wall.
    // because it's flat and parallel, its shadow is the same shape, scaled up around the lamp:
    // the closer it hangs to the lamp, the bigger its shadow and the faster the shadow moves
    public class PropDef
    {
        public string look = "plank";       // which stage prop it is: bridge, cloud, boat, crate, thorns, plank
        public float depth;                 // how far in front of the wall it hangs
        public Box[] parts;                 // its pieces, in wall coordinates, as if pressed flat on the wall
        public bool thorn;                  // a spiky cut-out: its shadow is deadly instead of solid
        public V2 swing;                    // props on a motor sway back and forth by this much...
        public float period;                // ...taking this long for a full swing (0 = it doesn't move)
        public float phase;                 // ...starting this far through the swing (0 to 1)
        public int tint;                    // which colour of paper it's cut from

        public V2 Offset(float time)
        {
            if (period <= 0) return new V2(0, 0);
            return swing * M.Sin(M.TAU * (time / period + phase));
        }
    }

    public class LampDef
    {
        public float x, y, depth;           // where it starts
        public Box area;                    // where on its plane it can be moved to
        public float minDepth, maxDepth;    // how near the wall it can go, and how far (the same = fixed)
        public LampDef(float x, float y, float depth, Box area, float minDepth = 0, float maxDepth = 0)
        {
            this.x = x; this.y = y; this.depth = depth; this.area = area;
            this.minDepth = minDepth > 0 ? minDepth : depth;
            this.maxDepth = maxDepth > 0 ? maxDepth : depth;
        }
    }

    public class LevelDef
    {
        public string name;
        public string[] tips = new string[0];   // shown at the start, one line per new idea
        public V2 start;                        // the hero's feet
        public Box exit;                        // the doorway out: locked until you've got every key
        public Box[] shelves = new Box[0];      // wooden shelves nailed to the wall: solid, and never move
        public PropDef[] props = new PropDef[0];
        public LampDef[] lamps;
        public Box[] keys = new Box[0];        // optional collectibles, drawn as moths on the wall
    }
}
