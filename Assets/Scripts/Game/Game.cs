// runs the show: the title, the levels, the menus and the controls. the simulation steps
// at a fixed 120 times a second whatever the frame rate, and View smooths between steps.
using UnityEngine;

namespace ShadowSet
{
    public class Game : MonoBehaviour
    {
        enum State { Title, Play, Cleared, End }
        State state = State.Title;
        View view;
        Sfx sfx;
        Sim sim;
        int levelIndex;
        float acc, stateTime, tipTime, pauseFade;
        bool paused;
        Vector3[] prevLamps;
        float prevTime;
        Vector2 prevHero;
        Vector2 lampTarget;
        float depthTarget;
        Vector3 lastMouse;
        bool keyboardLamp;
        bool jumpQueued, switchQueued;
        int totalDeaths;
        float levelTime;
        GUIStyle big, mid, key;
        string shotPath; int shotFrames = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() != null) return;
            var go = new GameObject("Shadow Set");
            go.AddComponent<Game>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            view = gameObject.AddComponent<View>();
            view.Build();
            sfx = view.cam.gameObject.AddComponent<Sfx>();
            Sfx.Muted = PlayerPrefs.GetInt("muted", 0) == 1;
            // a title-screen level running behind the menu
            Load(0);
            state = State.Title;

            // for testing: ShadowSet.exe -shot <level> <seconds> <file.png> [-peek]
            // plays the level's scripted solution for that long, then takes a picture
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-titleshot" && i + 1 < args.Length) { titleShot = args[i + 1]; Invoke("TitleShot", 6); }
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-shot" && i + 3 < args.Length)
                {
                    Load(int.Parse(args[i + 1]));
                    state = State.Play;
                    float seconds = float.Parse(args[i + 2], System.Globalization.CultureInfo.InvariantCulture);
                    shotPath = args[i + 3];
                    shotFrames = 30;
                    shotPeek = System.Array.IndexOf(args, "-peek") >= 0;
                    var bot = Tools.Bot.Solution(levelIndex);
                    if (bot != null)
                    {
                        bot.Begin(sim);
                        for (int t = 0; t < seconds * 120 && !sim.won; t++) { Snapshot(); sim.Step(bot.Next(sim, t)); }
                        var l = sim.lamps[sim.active];
                        lampTarget = new Vector2(l.x, l.y); depthTarget = l.d;
                    }
                    tipTime = 100;
                }
        }

        bool shotPeek;
        string titleShot;
        void TitleShot() { ScreenCapture.CaptureScreenshot(titleShot); Invoke("Quit", 0.5f); }
        Tools.Bot demo;
        int demoTick;

        void Load(int i)
        {
            levelIndex = Mathf.Clamp(i, 0, Levels.All.Length - 1);
            sim = new Sim(Levels.All[levelIndex]);
            view.Load(sim);
            lampTarget = new Vector2(sim.lamps[0].x, sim.lamps[0].y);
            depthTarget = sim.lamps[0].d;
            keyboardLamp = true;            // don't jump the lamp to wherever the mouse happens to be
            lastMouse = Input.mousePosition;
            tipTime = 0;
            levelTime = 0;
            Snapshot();
        }

        void Snapshot()
        {
            prevLamps = new Vector3[sim.lamps.Length];
            for (int i = 0; i < prevLamps.Length; i++) prevLamps[i] = new Vector3(sim.lamps[i].x, sim.lamps[i].y, sim.lamps[i].d);
            prevTime = sim.time;
            prevHero = new Vector2(sim.hero.x, sim.hero.y);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            stateTime += dt;
            view.FitCamera();

            if (Input.GetKeyDown(KeyCode.M)) { Sfx.Muted = !Sfx.Muted; PlayerPrefs.SetInt("muted", Sfx.Muted ? 1 : 0); }

            if (state == State.Title)
            {
                Demo(dt);
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartLevel(PlayerPrefs.GetInt("reached", 0));
                return;
            }
            if (state == State.End)
            {
                Demo(dt);
                if (stateTime > 1.5f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) { state = State.Title; stateTime = 0; }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
            pauseFade = Mathf.MoveTowards(pauseFade, paused ? 1 : 0, dt * 6);
            if (paused) { view.Sync(1, prevLamps, prevTime, prevHero, 0); return; }

            if (state == State.Cleared)
            {
                view.Sync(1, prevLamps, prevTime, prevHero, dt);
                sfx.Hum(0);
                if (stateTime > 2.2f || (stateTime > 0.6f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))))
                {
                    if (levelIndex + 1 >= Levels.All.Length) { state = State.End; stateTime = 0; Load(0); demo = null; }
                    else StartLevel(levelIndex + 1);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.R)) { sim.Restart(); Snapshot(); }
            if (Input.GetKeyDown(KeyCode.H)) tipTime = 0;
            if (Input.GetKeyDown(KeyCode.N)) StartLevel(levelIndex + 1);   // skip ahead (for testing)
            tipTime += dt;
            levelTime += dt;

            if (shotPath == null) { ReadControls(dt); Step(dt); }
            Sync(dt);

            if (shotPath != null) view.peek = shotPeek ? 1 : 0;
            if (shotFrames >= 0 && --shotFrames == 0) { ScreenCapture.CaptureScreenshot(shotPath); }
            if (shotFrames < 0 && shotPath != null) { shotPath = null; Invoke("Quit", 0.5f); }
        }

        void Quit() { Application.Quit(); }

        void StartLevel(int i)
        {
            if (i >= Levels.All.Length) i = 0;
            Load(i);
            state = State.Play;
            stateTime = 0;
            paused = false;
            sfx.Click();
        }

        // ------------------------------------------------------------ controls
        float move;
        bool jumpHeld;

        void ReadControls(float dt)
        {
            move = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += 1;
            bool jumpKey = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Z);
            if (jumpKey) jumpQueued = true;
            jumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z);
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetMouseButtonDown(1)) switchQueued = true;
            view.peek = Mathf.MoveTowards(view.peek, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 1 : 0, dt * 5);

            var lamp = sim.lamps[sim.active];
            // the lamp follows the mouse, or IJKL if you'd rather keep both hands on the keys
            var mouse = Input.mousePosition;
            if ((mouse - lastMouse).sqrMagnitude > 4) { keyboardLamp = false; lastMouse = mouse; }
            Vector2 keys = Vector2.zero;
            if (Input.GetKey(KeyCode.J)) keys.x -= 1;
            if (Input.GetKey(KeyCode.L)) keys.x += 1;
            if (Input.GetKey(KeyCode.I)) keys.y += 1;
            if (Input.GetKey(KeyCode.K)) keys.y -= 1;
            if (keys != Vector2.zero)
            {
                if (!keyboardLamp) lampTarget = new Vector2(lamp.x, lamp.y);
                keyboardLamp = true;
                lampTarget += keys.normalized * Sim.LAMP_SPEED * 0.8f * dt;
            }
            if (!keyboardLamp) lampTarget = view.ScreenToWall(mouse, lamp.d);
            var a = lamp.def.area;
            lampTarget = new Vector2(Mathf.Clamp(lampTarget.x, a.x, a.Right), Mathf.Clamp(lampTarget.y, a.y, a.Top));

            // nearer the wall or further away: the scroll wheel, or Q and E
            float wheel = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.Q)) wheel -= dt * 8;
            if (Input.GetKey(KeyCode.E)) wheel += dt * 8;
            depthTarget = Mathf.Clamp(depthTarget + wheel * 0.6f, lamp.def.minDepth, lamp.def.maxDepth);
        }

        void Step(float dt)
        {
            acc += dt;
            int steps = 0;
            while (acc >= Sim.STEP && steps < 24)
            {
                acc -= Sim.STEP;
                steps++;
                Snapshot();
                var c = new Controls
                {
                    move = move, jumpPressed = jumpQueued, jumpHeld = jumpHeld || jumpQueued,
                    aim = true, lampX = lampTarget.x, lampY = lampTarget.y, lampDepth = depthTarget, switchLamp = switchQueued
                };
                jumpQueued = false;
                switchQueued = false;
                int before = sim.active;
                sim.Step(c);
                if (sim.active != before)
                {
                    var l = sim.lamps[sim.active];
                    lampTarget = new Vector2(l.x, l.y);
                    depthTarget = l.d;
                    keyboardLamp = true;
                    lastMouse = Input.mousePosition;
                }
                Events();
                if (sim.won) { state = State.Cleared; stateTime = 0; totalDeaths += sim.deaths; PlayerPrefs.SetInt("reached", Mathf.Max(PlayerPrefs.GetInt("reached", 0), Mathf.Min(levelIndex + 1, Levels.All.Length - 1))); break; }
            }
            if (steps == 24) acc = 0;
        }

        void Events()
        {
            if (sim.evJump) sfx.Jump();
            if (sim.evLand) sfx.Land();
            if (sim.evKey) sfx.Moth();      // the key chime
            if (sim.evSwitch) sfx.Swap();
            if (sim.evWin) sfx.Win();
            if (sim.evDie)
            {
                sfx.Die();
                view.Burst(new Color(0.1f, 0.08f, 0.14f), 22, 7);
                view.Shake(1);
            }
        }

        void Sync(float dt)
        {
            float alpha = Mathf.Clamp01(acc / Sim.STEP);
            view.Sync(alpha, prevLamps, prevTime, prevHero, dt);
            var l = sim.lamps[sim.active];
            sfx.Hum(new Vector2(lampTarget.x - l.x, lampTarget.y - l.y).magnitude * 8);
        }

        // behind the title and end screens, the first scene plays itself, over and over
        void Demo(float dt)
        {
            if (demo == null || sim.won || sim.dead)
            {
                if (sim.won || sim.dead || sim.level != Levels.All[0]) Load(0);
                demo = Tools.Bot.Solution(0);
                demo.Begin(sim);
                demoTick = 0;
                demoWait = 1.5f;
            }
            if (demoWait > 0) { demoWait -= dt; Sync(dt); sfx.Hum(0); return; }
            acc += dt;
            int steps = 0;
            while (acc >= Sim.STEP && steps++ < 24)
            {
                acc -= Sim.STEP;
                Snapshot();
                sim.Step(demo.Next(sim, demoTick++));
                if (sim.won) { demo = null; break; }
            }
            Sync(dt);
            sfx.Hum(0);
        }
        float demoWait;

        // ------------------------------------------------------------ the words on screen
        void Styles()
        {
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = true };
            mid = new GUIStyle(big) { fontStyle = FontStyle.Normal };
            key = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        void Text(Rect r, string s, GUIStyle st, Color c, int size, float alpha = 1)
        {
            st.fontSize = size;
            var shadow = new Color(0, 0, 0, 0.75f * alpha);
            st.normal.textColor = shadow;
            GUI.Label(new Rect(r.x + size * 0.06f, r.y + size * 0.08f, r.width, r.height), s, st);
            c.a *= alpha;
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }

        void OnGUI()
        {
            Styles();
            float W = Screen.width, H = Screen.height, u = H / 100f;
            var cream = new Color(1, 0.94f, 0.82f);
            var gold = new Color(1, 0.8f, 0.4f);


            if (state == State.Title)
            {
                Dim(0.45f);
                Text(new Rect(0, H * 0.16f, W, 14 * u), "SHADOW SET", big, cream, (int)(11 * u));
                Text(new Rect(0, H * 0.30f, W, 5 * u), "a platformer where the light is the level", mid, gold, (int)(3.2f * u));
                Text(new Rect(0, H * 0.42f, W, 4 * u), "Move the lamp with the <b>mouse</b>. Shadows are solid: run and jump on them.", mid, cream, (int)(2.6f * u));
                Text(new Rect(0, H * 0.47f, W, 4 * u), "<b>A D</b> or the <b>arrow keys</b> to run,  <b>Space</b> to jump.", mid, cream, (int)(2.6f * u));
                float blink = 0.6f + 0.4f * Mathf.Sin(Time.time * 3);
                int reached = PlayerPrefs.GetInt("reached", 0);
                Text(new Rect(0, H * 0.56f, W, 5 * u), reached > 0 ? "Press <b>Space</b> to carry on from scene " + (reached + 1) : "Press <b>Space</b> to begin", mid, gold, (int)(3.4f * u), blink);
                // scene select
                float bw = 9 * u, gap = 1.2f * u, total = Levels.All.Length * bw + (Levels.All.Length - 1) * gap, x0 = (W - total) / 2, y0 = H * 0.68f;
                Text(new Rect(0, y0 - 5 * u, W, 4 * u), "or pick a scene", mid, cream, (int)(2.2f * u), 0.7f);
                for (int i = 0; i < Levels.All.Length; i++)
                {
                    var r = new Rect(x0 + i * (bw + gap), y0, bw, bw);
                    bool open = true;      // every scene is open, so it's easy to show any of them
                    GUI.enabled = open;
                    key.fontSize = (int)(3 * u);
                    if (GUI.Button(r, (i + 1).ToString(), key)) StartLevel(i);
                    GUI.enabled = true;
                }
                Text(new Rect(0, H * 0.92f, W, 4 * u), "M: sound " + (Sfx.Muted ? "off" : "on") + "      Esc: pause", mid, cream, (int)(1.8f * u), 0.6f);
                return;
            }

            if (state == State.End)
            {
                Dim(0.5f);
                Text(new Rect(0, H * 0.25f, W, 12 * u), "Curtain call", big, cream, (int)(9 * u));
                Text(new Rect(0, H * 0.42f, W, 5 * u), "You made it through every scene.", mid, gold, (int)(3.2f * u));
                Text(new Rect(0, H * 0.5f, W, 5 * u), "Tumbles: " + totalDeaths, mid, cream, (int)(2.6f * u));
                if (stateTime > 1.5f) Text(new Rect(0, H * 0.62f, W, 5 * u), "Press Space", mid, gold, (int)(2.6f * u), 0.6f + 0.4f * Mathf.Sin(Time.time * 3));
                return;
            }

            // scene title and tips while it starts
            var level = sim.level;
            Text(new Rect(2 * u, 1.5f * u, W, 4 * u), "<b>Scene " + (levelIndex + 1) + "</b>  ·  " + level.name, new GUIStyle(mid) { alignment = TextAnchor.UpperLeft }, cream, (int)(2.6f * u));
            string right = "tumbles " + sim.deaths + (level.keys.Length > 0 ? "     keys " + sim.keyCount + "/" + level.keys.Length : "");
            Text(new Rect(0, 1.5f * u, W - 2 * u, 4 * u), right, new GUIStyle(mid) { alignment = TextAnchor.UpperRight }, cream, (int)(2.2f * u), 0.8f);

            float tipAlpha = Mathf.Clamp01(1 - (tipTime - 7) / 1.5f) * Mathf.Clamp01(tipTime * 3);
            if (tipAlpha > 0)
            {
                for (int i = 0; i < level.tips.Length; i++)
                    Text(new Rect(0, H * 0.075f + i * 4.2f * u, W, 4 * u), level.tips[i], mid, gold, (int)(2.7f * u), tipAlpha);
            }

            string controls = "Mouse: lamp    A D: run    Space: jump    Shift: see the light    R: restart    H: tips";
            if (level.lamps[0].maxDepth > level.lamps[0].minDepth) controls = "Mouse: lamp    Wheel / Q E: nearer, further    A D: run    Space: jump    Shift: see the light    R: restart";
            if (level.lamps.Length > 1) controls = "Mouse: lamp    Right click / Tab: other lamp    A D: run    Space: jump    Shift: see the light    R: restart";
            Text(new Rect(0, H - 4.5f * u, W, 4 * u), controls, mid, cream, (int)(1.9f * u), 0.55f);

            if (sim.atLockedDoor)
                Text(new Rect(0, H * 0.4f, W, 8 * u), "Locked! Find every key first (" + sim.keyCount + "/" + level.keys.Length + ")", big, new Color(1, 0.8f, 0.4f), (int)(3.6f * u));
            if (sim.dead)
                Text(new Rect(0, H * 0.4f, W, 8 * u), sim.cause == "squashed" ? "Squashed!" : sim.cause == "thorns" ? "Ouch, thorns!" : "Off the edge!", big, new Color(1, 0.55f, 0.45f), (int)(5 * u), Mathf.Clamp01(sim.deadTimer * 3));

            if (state == State.Cleared)
            {
                float a = Mathf.Clamp01(stateTime * 3);
                Dim(0.35f * a);
                Text(new Rect(0, H * 0.33f, W, 10 * u), "Bravo!", big, cream, (int)(8 * u), a);
                Text(new Rect(0, H * 0.46f, W, 5 * u), levelTime.ToString("0.0") + " seconds    ·    " + sim.deaths + (sim.deaths == 1 ? " tumble" : " tumbles"), mid, gold, (int)(2.8f * u), a);
            }

            if (pauseFade > 0)
            {
                Dim(0.55f * pauseFade);
                Text(new Rect(0, H * 0.3f, W, 10 * u), "Paused", big, cream, (int)(7 * u), pauseFade);
                if (paused)
                {
                    key.fontSize = (int)(2.6f * u);
                    float bw = 30 * u, bh = 6 * u, x = (W - bw) / 2, y = H * 0.45f;
                    if (GUI.Button(new Rect(x, y, bw, bh), "Carry on", key)) paused = false;
                    if (GUI.Button(new Rect(x, y + bh * 1.25f, bw, bh), "Restart scene", key)) { sim.Restart(); Snapshot(); paused = false; }
                    if (GUI.Button(new Rect(x, y + bh * 2.5f, bw, bh), "Back to the title", key)) { paused = false; Load(0); demo = null; state = State.Title; }
                    if (GUI.Button(new Rect(x, y + bh * 3.75f, bw, bh), "Quit", key)) Application.Quit();
                }
            }
        }

        void Dim(float a)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
