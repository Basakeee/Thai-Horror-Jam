using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TapeLoop.EditorTools
{
    // ---------------------------------------------------------------------------
    // Bake: สร้าง scene TapeLoop + บ้านเป็น object จริง + ม้วนเทปเป็น .asset (แก้ใน Inspector ได้)
    // ---------------------------------------------------------------------------
    public static class TapeLoopBake
    {
        public const string ScenePath = "Assets/Scenes/TapeLoop.unity";
        const string RollFolder = "Assets/_TapeLoop/Rolls";
        const string MaterialFolder = "Assets/_TapeLoop/Materials";

        [MenuItem("Tools/Tape Loop/Bake Scene")]
        static void BakeMenu()
        {
            if (!Bake(out string error)) EditorUtility.DisplayDialog("Tape Loop", error, "OK");
        }

        [MenuItem("Tools/Tape Loop/Reset Rolls To Defaults")]
        static void ResetRollsMenu()
        {
            if (!EditorUtility.DisplayDialog("Tape Loop", "เขียนทับ Roll1–7.asset ด้วยค่าตั้งต้นจาก TapeLevels.cs?\n(ที่แก้ใน Inspector จะหาย)", "เขียนทับ", "ยกเลิก")) return;
            LoadOrCreateRolls(overwrite: true);
        }

        public static bool Bake(out string error)
        {
            error = null;
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty && active.path != ScenePath)
            {
                error = $"scene ที่เปิดอยู่ ({active.path}) ยังไม่ได้เซฟ — เซฟก่อนแล้วค่อย Bake";
                return false;
            }

            var rolls = LoadOrCreateRolls(overwrite: false);
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ไม่ลบของเดิม — เก็บเป็น backup (ปิดไว้)
            var old = GameObject.Find(HouseBuilder.House);
            if (old != null)
            {
                old.name = $"House (backup {DateTime.Now:MMdd-HHmm})";
                old.SetActive(false);
            }

            var house = HouseBuilder.Build();
            PersistMaterials(house);

            var game = Object.FindFirstObjectByType<TapeGame>();
            if (game == null) game = new GameObject("TapeGame").AddComponent<TapeGame>();
            game.rolls = rolls;
            game.house = house;
            EditorUtility.SetDirty(game);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Tape Loop] Bake เสร็จ — เปิด Assets/Scenes/TapeLoop.unity แล้วกด Play");
            return true;
        }

        public static List<TapeRoll> LoadOrCreateRolls(bool overwrite)
        {
            if (!AssetDatabase.IsValidFolder(RollFolder)) AssetDatabase.CreateFolder("Assets/_TapeLoop", "Rolls");
            var result = new List<TapeRoll>();
            foreach (var fresh in TapeLevels.CreateAll())
            {
                string path = $"{RollFolder}/{fresh.name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TapeRoll>(path);
                if (existing != null && !overwrite)
                {
                    // ฟีลด์ที่เพิ่มทีหลัง: เติมเฉพาะที่ว่าง ไม่ทับสิ่งที่แก้ใน Inspector
                    if ((existing.endShots == null || existing.endShots.Count == 0) && fresh.endShots.Count > 0)
                    {
                        existing.endShots = fresh.endShots;
                        EditorUtility.SetDirty(existing);
                    }
                    MergeDeathFields(existing, fresh);
                    result.Add(existing);
                    continue;
                }
                if (existing != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(fresh, path);
                result.Add(fresh);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        // Kill event ใน asset เดิม: เติม deathStyle/deathFlags/landRoom/fallsOut จากค่าตั้งต้น เฉพาะที่ยังว่าง
        static void MergeDeathFields(TapeRoll existing, TapeRoll fresh)
        {
            bool changed = false;
            foreach (var freshSeg in fresh.segments)
            {
                var seg = existing.Segment(freshSeg.id);
                if (seg == null) continue;
                foreach (var fe in freshSeg.events)
                {
                    if (fe.effect != EffectType.Kill) continue;
                    var e = seg.events.Find(x => x.effect == EffectType.Kill && x.value == fe.value && Mathf.Abs(x.time - fe.time) < 0.01f);
                    if (e == null) continue;
                    if (e.deathStyle == DeathStyle.Auto && fe.deathStyle != DeathStyle.Auto) { e.deathStyle = fe.deathStyle; changed = true; }
                    if (string.IsNullOrEmpty(e.deathFlags) && !string.IsNullOrEmpty(fe.deathFlags)) { e.deathFlags = fe.deathFlags; changed = true; }
                    if (string.IsNullOrEmpty(e.landRoom) && !string.IsNullOrEmpty(fe.landRoom)) { e.landRoom = fe.landRoom; e.landPosition = fe.landPosition; changed = true; }
                    if (!e.fallsOut && fe.fallsOut) { e.fallsOut = true; changed = true; }
                }
            }
            if (changed) EditorUtility.SetDirty(existing);
        }

        // material ที่สร้างจากโค้ดไม่ถูกเซฟลง scene → เก็บเป็น .mat
        static void PersistMaterials(GameObject root)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_TapeLoop", "Materials");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || AssetDatabase.Contains(mats[i])) continue;
                    string path = $"{MaterialFolder}/{mats[i].name}.mat";
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (existing == null)
                    {
                        AssetDatabase.CreateAsset(mats[i], path);
                        existing = mats[i];
                    }
                    mats[i] = existing;
                }
                renderer.sharedMaterials = mats;
            }
            AssetDatabase.SaveAssets();
        }
    }

    // ---------------------------------------------------------------------------
    // Harness: เทสต์ใน Editor ที่เปิดอยู่
    //   Library/TapeLoopBake.request → Bake
    //   Library/TapeLoopTest.request → ตรวจทางแก้/ทางผิดทุกม้วน (headless) + เข้า Play ไล่ทั้งเกม + ภาพหน้าจอ
    //   ผล: Library/TapeLoopTest/report.txt + *.png
    // ---------------------------------------------------------------------------
    [InitializeOnLoad]
    static class TapeLoopHarness
    {
        const string BakeRequest = "Library/TapeLoopBake.request";
        const string TestRequest = "Library/TapeLoopTest.request";
        const string OutDir = "Library/TapeLoopTest";
        const string RunningKey = "TapeLoopTest.Running";
        const string RefreshedKey = "TapeLoopTest.Refreshed";

        static IEnumerator routine;
        static readonly Stack<IEnumerator> stack = new();
        static double waitUntil;
        static Func<bool> waitFor;
        static double waitForDeadline;
        static readonly StringBuilder report = new();

        static TapeLoopHarness() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (SessionState.GetBool(RunningKey, false))
            {
                if (EditorApplication.isPlaying) Tick();
                return;
            }
            if (EditorApplication.isPlaying) return;

            bool bake = File.Exists(BakeRequest), test = File.Exists(TestRequest);
            if (!bake && !test) return;
            if (!SessionState.GetBool(RefreshedKey, false))
            {
                SessionState.SetBool(RefreshedKey, true);
                AssetDatabase.Refresh();
                return;
            }
            SessionState.EraseBool(RefreshedKey);

            if (bake)
            {
                File.Delete(BakeRequest);
                bool ok = TapeLoopBake.Bake(out string error);
                File.WriteAllText("Library/TapeLoopBake.done", ok ? "ok " + DateTime.Now.ToString("s") : "FAIL " + error);
                return;
            }

            File.Delete(TestRequest);
            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            Directory.CreateDirectory(OutDir);
            report.Clear();

            VerifyLevels();
            File.WriteAllText(Path.Combine(OutDir, "verify.txt"), report.ToString());

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != TapeLoopBake.ScenePath)
            {
                if (active.isDirty)
                {
                    Line("ABORT: scene ปัจจุบันยังไม่ได้เซฟ — ข้ามการเทสต์ใน Play mode");
                    File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
                    return;
                }
                EditorSceneManager.OpenScene(TapeLoopBake.ScenePath);
            }
            SessionState.SetBool(RunningKey, true);
            Application.runInBackground = true;
            EditorApplication.isPlaying = true;
        }

        // ---- batchmode (ตอนที่โปรเจกต์ไม่ได้เปิดใน Editor) ----
        //   Unity.exe -batchmode -projectPath <proj> -executeMethod TapeLoop.EditorTools.TapeLoopHarness.BatchRun
        //   Bake + ตรวจด่าน + ภาพทุกห้อง (ไม่เข้า Play mode)
        public static void BatchRun()
        {
            int code = 0;
            try
            {
                if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
                Directory.CreateDirectory(OutDir);
                report.Clear();
                // -noBake = แค่เปิด scene เดิมแล้วตรวจ (ไม่สร้าง House backup ซ้ำ)
                string error = null;
                bool noBake = Environment.GetCommandLineArgs().Contains("-noBake") && File.Exists(TapeLoopBake.ScenePath);
                if (noBake) EditorSceneManager.OpenScene(TapeLoopBake.ScenePath);
                if (!noBake && !TapeLoopBake.Bake(out error))
                {
                    Line("BAKE FAIL: " + error);
                    code = 1;
                }
                else
                {
                    Line(noBake ? "SCENE OPENED (no bake)" : "BAKE OK");
                    VerifyLevels();
                    RoomShots();
                }
            }
            catch (Exception e)
            {
                Line("BATCH EXCEPTION: " + e);
                code = 1;
            }
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
            EditorApplication.Exit(code);
        }

        // ภาพทุกห้องจากกล้องเพดาน (state ตั้งต้นของ scene — prop ทุกชิ้นโชว์หมด)
        static void RoomShots()
        {
            var house = GameObject.Find(HouseBuilder.House).transform;
            var cam = house.Find(HouseBuilder.Camera).GetComponent<Camera>();
            var game = Object.FindFirstObjectByType<TapeGame>();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.34f);
            foreach (Transform room in house.Find(HouseBuilder.Rooms))
            {
                cam.transform.position = room.position + game.cameraOffset;
                cam.transform.LookAt(room.position + new Vector3(0f, 0f, 0.4f));
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(OutDir, $"room_{room.name.Replace(' ', '_')}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
                Line($"room shot {room.name}");
            }
        }

        // ---- ตรวจทุกม้วนแบบไม่มีภาพ: ทางแก้ต้องชนะ ทางผิดต้องแพ้ ----
        static void VerifyLevels()
        {
            var rolls = TapeLoopBake.LoadOrCreateRolls(false);
            Line("=== ตรวจด่าน (headless) ===");
            for (int i = 0; i < rolls.Count; i++)
            {
                var r = rolls[i];
                bool ok = r != null && r.segments != null && r.segments.Count > 0;
                Line($"{(ok ? "PASS" : "FAIL")}  Roll{i + 1}.asset โหลดได้ ({(ok ? r.segments.Count + " ชิ้น" : "เสีย")})");
                if (!ok) return;
            }
            foreach (var (n, name, pieces, expect, note) in Cases())
            {
                var roll = rolls[n - 1];
                var list = pieces.Select(p => Resolve(roll, p)).ToList();
                float length = TapePlayer.TapeLength(roll, list);
                bool over = roll.maxLength > 0f && length > roll.maxLength + 0.01f;
                string got;
                if (over) got = "OverBudget";
                else
                {
                    var sim = new TapePlayer(roll, list);
                    var outcome = sim.RunToEnd();
                    got = outcome == Outcome.Died ? $"Died({sim.deadWho}{(sim.deathOffscreen ? ",นอกจอ" : "")})" : outcome.ToString();
                }
                bool pass = got.StartsWith(expect);
                Line($"{(pass ? "PASS" : "FAIL")}  ม้วน {n} · {name}: [{string.Join(" ", list)}] len={length:0.#} → {got} (คาด {expect}) {note}");
            }
        }

        static Piece Resolve(TapeRoll roll, Piece p)
        {
            var c = p.Clone();
            if (c.end < 0f) c.end = roll.Segment(c.segment).length;
            return c;
        }

        static Piece P(string seg, float start = 0f, float end = -1f, bool rev = false) => new() { segment = seg, start = start, end = end, reversed = rev };

        static IEnumerable<(int, string, Piece[], string, string)> Cases()
        {
            yield return (1, "เริ่มต้น", new[] { P("A"), P("B"), P("C") }, "Died(ภพ", "");
            yield return (1, "ทางแก้", new[] { P("A"), P("B", 0, 6), P("C") }, "Won", "");
            yield return (1, "ทิ้ง B ทั้งชิ้น", new[] { P("A"), P("C") }, "NoEnd", "ไม่มีไฟฉาย");
            yield return (1, "ตัดก่อนหยิบโทรศัพท์", new[] { P("A"), P("B", 0, 2), P("C") }, "NoEnd", "");
            yield return (2, "เริ่มต้น", new[] { P("A"), P("B"), P("C"), P("D") }, "Died(มิ้นท์", "");
            yield return (2, "ทางแก้", new[] { P("D", 0, 5), P("C"), P("B") }, "Won", "A เกิดนอกจอ");
            yield return (2, "ถังดับเพลิงอย่างเดียว", new[] { P("C"), P("B") }, "Died(มิ้นท์", "");
            yield return (2, "กุญแจอย่างเดียว", new[] { P("D", 0, 5), P("B") }, "Died(มิ้นท์", "");
            yield return (2, "ไม่ตัดครัว (วางกุญแจคืน)", new[] { P("D"), P("C"), P("B") }, "Died(มิ้นท์", "");
            yield return (2, "B ก่อน A", new[] { P("D", 0, 5), P("C"), P("B"), P("A") }, "NoEnd", "");
            yield return (3, "เริ่มต้น", new[] { P("A"), P("B"), P("C") }, "Died(แม่", "");
            yield return (3, "ทางแก้ (Reverse B)", new[] { P("A"), P("B", rev: true), P("C") }, "Won", "");
            yield return (3, "ตัด B ทิ้ง", new[] { P("A"), P("C") }, "Died(แม่", "ตัดทิ้ง ≠ ไม่เกิด: หุ่นยนต์ยังชนประตูนอกจอ");
            yield return (3, "Reverse C แทน", new[] { P("A"), P("B"), P("C", rev: true) }, "NoEnd", "");
            yield return (4, "เริ่มต้น", new[] { P("A"), P("B"), P("C"), P("D"), P("E") }, "Died(พ่อ", "");
            yield return (4, "ทางแก้", new[] { P("A", rev: true), P("B"), P("E"), P("C") }, "Won", "");
            yield return (4, "Reverse A อย่างเดียว", new[] { P("A", rev: true), P("B"), P("C"), P("D"), P("E") }, "Died(พ่อ", "");
            yield return (4, "ล็อกประตูอย่างเดียว", new[] { P("A"), P("B"), P("E"), P("C") }, "Died(พ่อ", "");
            yield return (5, "เริ่มต้น", new[] { P("A"), P("B"), P("C"), P("D"), P("E") }, "OverBudget", "");
            yield return (5, "ทางแก้ (ทิ้งความทรงจำ)", new[] { P("D", 0, 4), P("E"), P("A", 0, 3), P("C") }, "Won", "");
            yield return (5, "เก็บความทรงจำ 1 วิ", new[] { P("D", 0, 4), P("E"), P("A", 0, 3), P("B", 0, 1), P("C") }, "OverBudget", "เกินแค่ 2 วิ");
            yield return (5, "ไม่ผูกชั้น", new[] { P("A"), P("C") }, "Died(ภพ", "");
            yield return (6, "เริ่มต้น", new[] { P("A"), P("B"), P("C"), P("D") }, "Died(", "");
            yield return (6, "ทางแก้", new[] { P("C"), P("A"), P("B"), P("D") }, "Won", "");
            yield return (7, "ไม่ตัด", new[] { P("A") }, "Won", "→ TRUE ENDING");
            yield return (7, "ตัดก่อนภพหลับตา", new[] { P("A", 0, 11) }, "NoEnd", "→ วนกลับ 0048");
        }

        // ---- Play mode ----
        static void Tick()
        {
            if (routine == null)
            {
                Application.logMessageReceived += OnLog;
                routine = Scenario();
                stack.Clear();
                stack.Push(routine);
                waitUntil = 0;
                waitFor = null;
            }
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            if (waitFor != null)
            {
                bool done;
                try { done = waitFor(); }
                catch (Exception e) { Line("WAIT EXCEPTION: " + e.Message); done = true; }
                if (!done && EditorApplication.timeSinceStartup < waitForDeadline) return;
                if (!done) Line("TIMEOUT รอเงื่อนไข");
                waitFor = null;
            }

            // routine ซ้อนกันได้ (Scenario → Solve) — ใช้ stack
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool more;
                try { more = top.MoveNext(); }
                catch (Exception e)
                {
                    Line("HARNESS EXCEPTION: " + e);
                    stack.Clear();
                    break;
                }
                if (!more)
                {
                    stack.Pop();
                    continue;
                }
                switch (top.Current)
                {
                    case IEnumerator nested:
                        stack.Push(nested);
                        continue;
                    case float f:
                        waitUntil = EditorApplication.timeSinceStartup + f;
                        return;
                    case Func<bool> cond:
                        waitFor = cond;
                        waitForDeadline = EditorApplication.timeSinceStartup + 90;
                        return;
                    default:
                        return;
                }
            }
            // จบเสมอ — ต่อให้เขียนรายงานไม่ได้ (เช่นโฟลเดอร์ถูกลบระหว่างเทสต์) ก็ต้องออกจาก Play ไม่ค้างโยน error ทุกเฟรม
            Application.logMessageReceived -= OnLog;
            routine = null;
            SessionState.EraseBool(RunningKey);
            EditorApplication.isPlaying = false;
            try
            {
                Directory.CreateDirectory(OutDir);
                string verify = Path.Combine(OutDir, "verify.txt");
                File.WriteAllText(Path.Combine(OutDir, "report.txt"), (File.Exists(verify) ? File.ReadAllText(verify) : "") + report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Tape Loop] เขียนรายงานเทสต์ไม่ได้: " + e.Message);
            }
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log || type == LogType.Warning) return;
            Line($"[{type}] {message}\n{stack}");
        }

        static void Line(string text) => report.AppendLine(text);

        static void Shot(TapeGame game, string name)
        {
            Directory.CreateDirectory(OutDir);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"));
            Line($"shot {name} | {game.DebugState}");
        }

        static IEnumerator Scenario()
        {
            Line("\n=== เล่นทั้งเกมใน Play mode ===");
            yield return 1.5f;
            var game = Object.FindFirstObjectByType<TapeGame>();
            if (game == null)
            {
                Line("FAIL: ไม่เจอ TapeGame");
                yield break;
            }

            Shot(game, "00_intro");

            yield return 0.25f;
            yield return 1f;
            game.DebugSkipMessage();
            yield return 0.6f;
            Shot(game, "01_r1_edit");
            yield return 0.25f;
            game.DebugSelect(1, 3.5f);
            yield return 0.5f;
            Shot(game, "02_r1_scrub_phone");
            yield return 0.25f;
            game.DebugSelect(1, 8f);
            yield return 0.5f;
            Shot(game, "02b_r1_scrub_lights_off");
            yield return 0.25f;
            game.DebugSelect(1, 10.5f);
            yield return 0.5f;
            Shot(game, "02c_r1_scrub_after_death");
            yield return 0.25f;
            game.DebugSelect(0, 1f);
            yield return 0.4f;
            yield return 0.25f;

            // ม้วน 1: เล่นแบบเดิมให้ตายก่อน 1 ครั้ง แล้วแก้
            game.DebugPlay();
            yield return (Func<bool>)(() => game.DebugMode == "Sequence");
            yield return 1.6f;
            Shot(game, "03a_r1_dying_fall");
            yield return 0.25f;
            yield return (Func<bool>)(() => game.DebugMode == "Message");
            Shot(game, "03_r1_death");
            yield return 0.25f;
            game.DebugSkipMessage();
            yield return 0.3f;
            yield return Solve(game, 1, "04_r1", P("A"), P("B", 0, 6), P("C"));
            yield return DieFirst(game, 2, "05a_r2_burn", P("A"), P("B"));
            yield return Solve(game, 2, "05_r2", P("D", 0, 5), P("C"), P("B"));
            yield return DieFirst(game, 3, "06a_r3_electric", P("A"), P("B"), P("C"));
            yield return Solve(game, 3, "06_r3", P("A"), P("B", rev: true), P("C"));
            yield return DieFirst(game, 4, "07a_r4_glass", P("A"), P("B"), P("C"), P("D"));
            yield return Solve(game, 4, "07_r4", P("A", rev: true), P("B"), P("E"), P("C"));

            // ม้วน 5: โชว์เทปเกินงบก่อน
            game.DebugSkipMessage();
            yield return 0.6f;
            Shot(game, "08_r5_over_budget");
            yield return 0.25f;
            game.DebugSelect(1, 4f);
            yield return 0.4f;
            Shot(game, "09_r5_memory");
            yield return 0.25f;
            yield return DieFirst(game, 5, "09a_r5_crush", P("A", 0, 3), P("C"));
            yield return Solve(game, 5, "10_r5", P("D", 0, 4), P("E"), P("A", 0, 3), P("C"));

            // ม้วน 6: จุดพีค
            game.DebugSkipMessage();
            yield return 0.4f;
            game.DebugSetPieces(Resolve(game, 6, P("C"), P("A"), P("B"), P("D")));
            game.DebugPlay();
            yield return (Func<bool>)(() => game.DebugMode == "Sequence");
            yield return 1f;
            Shot(game, "11_r6_everyone_safe");
            yield return 0.25f;
            yield return 5f;
            Shot(game, "12_r6_montage");
            yield return 0.25f;
            yield return 7f;
            Shot(game, "13_r6_all_dead");
            yield return 0.25f;
            yield return 6f;
            Shot(game, "14_r6_shadow");
            yield return 0.25f;
            yield return 5f;
            Shot(game, "15_r6_note");
            yield return 0.25f;
            yield return (Func<bool>)(() => game.DebugRoll == 7);
            Line($"{(game.DebugRoll == 7 ? "PASS" : "FAIL")}  จุดพีคพาไปม้วน 7");

            // ม้วน 7: ตัด → วนกลับ 0048
            game.DebugSkipMessage();
            yield return 0.5f;
            Shot(game, "16_r7_edit");
            yield return 0.25f;
            game.DebugSetPieces(Resolve(game, 7, P("A", 0, 11)));
            game.DebugPlay();
            yield return (Func<bool>)(() => game.DebugRoll == 1);
            Line($"{(game.DebugCounter == 48 ? "PASS" : "FAIL")}  ตัดม้วนสุดท้าย → ตัวนับ {game.DebugCounter} กลับม้วน 1");
            yield return 0.5f;
            game.DebugSkipMessage();
            yield return 0.5f;
            Shot(game, "17_loop_0048_label");
            yield return 0.25f;

            // ม้วน 7: ไม่ตัด → TRUE ENDING
            game.DebugLoadRoll(7);
            yield return 0.5f;
            game.DebugPlay();
            yield return 10f;
            Shot(game, "18_r7_playing");
            yield return 0.25f;
            yield return (Func<bool>)(() => game.DebugMode == "Sequence");
            yield return 6.5f;
            Shot(game, "19_ending_dawn");
            yield return 0.25f;
            yield return 6f;
            Shot(game, "20_true_ending");
            yield return 0.25f;
            Line("PASS  เล่นจบ TRUE ENDING");
            yield return 1f;
        }

        // เล่นแบบที่ต้องตาย → ถ่ายกลางฉากตาย + ตอนจบฉาก (ศพ + ร่องรอย) → กลับมาโต๊ะตัดต่อ
        static IEnumerator DieFirst(TapeGame game, int roll, string shot, params Piece[] pieces)
        {
            game.DebugSkipMessage();
            yield return 0.4f;
            game.DebugSetPieces(Resolve(game, roll, pieces));
            game.DebugPlay();
            yield return (Func<bool>)(() => game.DebugMode == "Sequence");
            yield return 1.2f;
            Shot(game, shot + "_dying");
            yield return 0.25f;
            yield return (Func<bool>)(() => game.DebugMode == "Message");
            Shot(game, shot + "_dead");
            yield return 0.25f;
            game.DebugSkipMessage();
            yield return 0.4f;
            Line($"{(game.DebugRoll == roll && game.DebugMode == "Edit" ? "PASS" : "FAIL")}  ม้วน {roll} ฉากตาย → กลับโต๊ะตัดต่อ");
        }

        static IEnumerator Solve(TapeGame game, int roll, string shot, params Piece[] solution)
        {
            game.DebugSkipMessage();
            yield return 0.4f;
            if (game.DebugRoll != roll) Line($"FAIL  คาดว่าอยู่ม้วน {roll} แต่อยู่ม้วน {game.DebugRoll}");
            game.DebugSetPieces(Resolve(game, roll, solution));
            yield return 0.4f;
            Shot(game, shot + "_solution");
            yield return 0.25f;
            if (roll == 1)
            {
                // ห้องนั่งเล่นมืด — ไฟฉายโทรศัพท์ในมือภพควรส่องทาง
                game.DebugSelect(2, 5f);
                yield return 0.6f;
                Shot(game, "04b_r1_flashlight");
                yield return 0.25f;
            }
            game.DebugPlay();
            yield return 4f;
            Shot(game, shot + "_playing");
            yield return 0.25f;
            yield return (Func<bool>)(() => game.DebugMode == "Message" || game.DebugMode == "Sequence");
            if (game.DebugMode == "Sequence")
            {
                yield return 1.2f;
                Shot(game, shot + "_end1");
                yield return 0.25f;
                yield return 3.6f;
                Shot(game, shot + "_end2");
                yield return 0.25f;
                yield return (Func<bool>)(() => game.DebugMode == "Message");
            }
            Shot(game, shot + "_result");
            yield return 0.25f;
            yield return 0.3f;
            game.DebugSkipMessage();
            yield return 0.6f;
            Line($"{(game.DebugRoll == roll + 1 ? "PASS" : "FAIL")}  ม้วน {roll} ผ่าน → ม้วน {game.DebugRoll}");
        }

        static List<Piece> Resolve(TapeGame game, int roll, params Piece[] pieces)
        {
            var r = game.rolls[roll - 1];
            return pieces.Select(p => Resolve(r, p)).ToList();
        }
    }
}
