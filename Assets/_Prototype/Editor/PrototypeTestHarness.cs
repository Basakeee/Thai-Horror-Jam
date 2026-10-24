using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorrorJam.EditorTools
{
    // เทสต์ prototype ใน Editor ที่เปิดอยู่ (batchmode ใช้ไม่ได้เพราะ project ถูกล็อก)
    //   สร้างไฟล์ Library/HorrorJamTest.request → harness เข้า Play mode, ไล่ฉากทั้ง 3 องก์, ถ่ายภาพ
    //   ผลลัพธ์: Library/HorrorJamTest/report.txt + *.png
    // ใช้ World ใหม่จากโค้ดปัจจุบันเสมอ (DebugUseFreshWorld) — ไม่แตะ World ที่ Bake ไว้ใน scene
    [InitializeOnLoad]
    static class PrototypeTestHarness
    {
        const string RequestPath = "Library/HorrorJamTest.request";
        const string BakeRequestPath = "Library/HorrorJamBake.request";
        const string BakeDonePath = "Library/HorrorJamBake.done";
        const string OutDir = "Library/HorrorJamTest";
        const string RunningKey = "HorrorJamTest.Running";
        const string RefreshedKey = "HorrorJamTest.Refreshed";
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        static IEnumerator routine;
        static double waitUntil;
        static readonly StringBuilder report = new();

        static PrototypeTestHarness()
        {
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (SessionState.GetBool(RunningKey, false))
            {
                if (!EditorApplication.isPlaying) return;
                Tick();
                return;
            }

            // บาสกำลังเล่นอยู่ — ไม่แทรก
            if (EditorApplication.isPlaying) return;

            bool bake = File.Exists(BakeRequestPath);
            if (!bake && !File.Exists(RequestPath)) return;
            // Refresh ก่อน 1 รอบ เพื่อ compile โค้ดที่แก้จากนอก Editor (ถ้า compile จะ reload แล้วกลับมาที่นี่)
            if (!SessionState.GetBool(RefreshedKey, false))
            {
                SessionState.SetBool(RefreshedKey, true);
                AssetDatabase.Refresh();
                return;
            }
            SessionState.EraseBool(RefreshedKey);

            // Bake ก่อนเทสต์เสมอ (World เดิมเก็บเป็น backup) — ใช้ตอนโค้ด builder เปลี่ยนแต่ใน scene ยังเป็นรุ่นเก่า
            if (bake)
            {
                File.Delete(BakeRequestPath);
                if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
                BakeWorld.Rebuild();
                File.WriteAllText(BakeDonePath, System.DateTime.Now.ToString("s"));
                return;   // ถ้ามี test request ด้วย รอบถัดไปจะ Refresh แล้วเทสต์ต่อ
            }

            File.Delete(RequestPath);
            Begin();
        }

        static void Begin()
        {
            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            Directory.CreateDirectory(OutDir);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (active.isDirty)
                {
                    File.WriteAllText(Path.Combine(OutDir, "report.txt"), "ABORT: scene ปัจจุบันยังไม่ได้เซฟ — ไม่เปิด Prototype ทับ");
                    return;
                }
                EditorSceneManager.OpenScene(ScenePath);
            }

            SessionState.SetBool(RunningKey, true);
            // ต้องตั้งก่อนเข้า Play — ไม่งั้น Editor ที่ไม่ได้ focus จะค้าง
            Application.runInBackground = true;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (routine == null)
            {
                report.Clear();
                Application.logMessageReceived += OnLog;
                routine = Scenario();
                waitUntil = 0;
            }
            if (EditorApplication.timeSinceStartup < waitUntil) return;

            bool more;
            try { more = routine.MoveNext(); }
            catch (System.Exception e)
            {
                Line("HARNESS EXCEPTION: " + e);
                more = false;
            }
            if (more)
            {
                waitUntil = EditorApplication.timeSinceStartup + (routine.Current is float f ? f : 0f);
                return;
            }
            Finish();
        }

        static void Finish()
        {
            Application.logMessageReceived -= OnLog;
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
            routine = null;
            SessionState.EraseBool(RunningKey);
            EditorApplication.isPlaying = false;
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log || message.Contains("shadow atlas")) return;
            Line($"[{type}] {message}\n{stack}");
        }

        static void Line(string text) => report.AppendLine(text);

        // =====================================================================
        static IEnumerator Scenario()
        {
            yield return 1f;
            var game = Object.FindFirstObjectByType<LoopGame>();
            if (game == null)
            {
                Line("FAIL: ไม่เจอ LoopGame ใน scene");
                yield break;
            }
            // เทสต์บน World ตัวจริงใน scene (สิ่งที่บาสเห็นตอนกด Play เอง)
            yield return 1f;
            Shot(game, "01a_start_1s");
            yield return 3.5f;
            Shot(game, "01b_start_4s");

            // ---- องก์ 1: แอบฟังป้า → กระถางตก ----
            game.DebugSkipTo(62f);
            game.DebugTeleport(new Vector3(2.3f, 1f, 14f));
            yield return 13f;
            Shot(game, "02_eavesdrop_aunt");
            Check(game, LoopState.Knows(Facts.SonName), "แอบฟังป้าใต้ดาดฟ้า → ได้ชื่อเอ๋");
            game.DebugSkipTo(88.2f);
            yield return 1.1f;
            Shot(game, "03_pot_omen");
            yield return 1.3f;
            Shot(game, "04_pot_death");
            Check(game, game.DebugLocked, "ยืนใต้ดาดฟ้าตอน 1:30 → ตายเพราะกระถาง");
            yield return 7f;
            Shot(game, "05_loop2_corpse");
            Check(game, LoopState.Loop == 2 && LoopState.Corpses.Count == 1, "ย้อนเวลาเป็นลูป 2 + มีศพ");

            // ---- องก์ 2: แอบฟังโทรศัพท์ / โต๊ะตอนหมอดูไม่อยู่ ----
            game.DebugSkipTo(99f);
            game.DebugTeleport(new Vector3(-3.0f, 1f, 6.6f));
            yield return 11f;
            Shot(game, "06_phone_eavesdrop");
            Check(game, LoopState.Knows(Facts.DollInBox), "แอบฟังโทรศัพท์จากข้างแผง → รู้ว่าหุ่นอยู่ในกล่อง");
            game.DebugSkipTo(124f);
            game.DebugTeleport(new Vector3(-1.9f, 1f, 4.3f));
            yield return 2.5f;
            Shot(game, "07_fortune_away_table");

            // ---- chain สายไฟ + มอเตอร์ไซค์ ----
            game.DebugSkipTo(139f);
            game.DebugTeleport(new Vector3(2.6f, 1f, 19f));
            yield return 3f;
            Shot(game, "08_ladder_fall");
            game.DebugSkipTo(158f);
            yield return 3f;
            Shot(game, "09_wire_sparks");
            game.DebugSkipTo(186f);
            game.DebugTeleport(new Vector3(3.1f, 1f, 20f));
            yield return 2f;
            Shot(game, "10_bike_omen");
            yield return 2.6f;
            Shot(game, "11_bike_pass");
            Check(game, !game.DebugLocked, "ยืนชิดกำแพงตอนมอเตอร์ไซค์ผ่าน → รอด");

            // ---- 18:00 ไม่มีใครถูกป้องกัน → ต้นตายแทน ----
            game.DebugSkipTo(239.5f);
            yield return 5f;
            Shot(game, "12_six_pm_truck");
            yield return 4f;
            Shot(game, "13_bad_ending");

            // ---- จุดพีค ----
            game.DebugNextLoop();
            yield return 2.5f;
            game.DebugLearnAllClues();
            game.DebugSkipTo(142f);
            game.DebugTeleport(new Vector3(-1.9f, 1f, 4.0f));
            game.DebugGiveDoll(renamed: true, candle: true);
            yield return 3f;
            Shot(game, "14_ready_to_burn");
            game.DebugStartPeak();
            yield return 2f;
            Shot(game, "15_peak_burning");
            yield return 2.8f;
            Shot(game, "16_peak_laugh");
            yield return 5f;
            Shot(game, "17_peak_thread_reveal");
            yield return 12f;
            Shot(game, "18_peak_everyone_dies");
            yield return 7f;
            Shot(game, "19_peak_text");
            yield return 8f;
            Shot(game, "20_after_peak_loop");
            Check(game, LoopState.Knows(Facts.Truth) && !game.DebugLocked, "จุดพีคจบ → ได้ความจริง + เข้าลูปใหม่");

            // ---- องก์ 3: ยอมรับ ----
            game.DebugAccept();
            yield return 4.5f;
            Shot(game, "21_accept_world_brightens");
            game.DebugSkipTo(239.5f);
            yield return 4f;
            Shot(game, "22_walk_to_stall");
            yield return 7f;
            Shot(game, "23_fortune_turns_to_dust");
            yield return 6f;
            Shot(game, "24_ghost_returns");
            yield return 6f;
            Shot(game, "25_true_ending");
            Check(game, game.DebugLocked, "true ending แสดงผล");
            yield return 1f;
        }

        static void Check(LoopGame game, bool ok, string what) => Line($"{(ok ? "PASS" : "FAIL")}  {what}   | {game.DebugState}");

        // render กล้องเกม + HUD ลง PNG (HUD เปลี่ยนเป็น ScreenSpaceCamera ชั่วคราว ให้ติดในภาพ)
        static void Shot(LoopGame game, string name)
        {
            const int w = 1600, h = 900;
            var cam = game.DebugCamera;
            var hud = game.DebugHud;
            var mode = hud.renderMode;
            hud.renderMode = RenderMode.ScreenSpaceCamera;
            hud.worldCamera = cam;
            hud.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            hud.renderMode = mode;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Line($"shot {name}   | {game.DebugState}");
        }
    }
}
