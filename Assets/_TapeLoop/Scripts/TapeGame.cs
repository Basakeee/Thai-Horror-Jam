using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HorrorJam;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TapeLoop
{
    // 0047 / Tape Loop — prototype
    // โต๊ะตัดต่อ (Edit) → กด Play เล่นเทป (Play) → ตาย = กลับมาแก้ / ถึง END = ม้วนถัดไปเล่นเอง (คนต่อไปตายแทน)
    // ม้วน 6 = จุดพีค (แก้ได้ แล้วทุกคนตายพร้อมกัน) · ม้วน 7 = ไม่ตัด → TRUE ENDING / ตัด → ตัวนับ 0048 กลับม้วน 1
    // UI โต๊ะตัดต่อใน prototype เป็น IMGUI — art pass ค่อยเปลี่ยนเป็นเครื่องเล่นเทปจริง (diegetic)
    public class TapeGame : MonoBehaviour
    {
        [Tooltip("ม้วนเทปทั้งหมด (Assets/_TapeLoop/Rolls) — ว่าง = ใช้ค่าตั้งต้นจาก TapeLevels.cs")]
        public List<TapeRoll> rolls = new();
        [Tooltip("บ้าน (House) ใน scene — ว่าง = หาเอง / สร้างตอน Play")]
        public GameObject house;

        [Header("กล้อง (top-down แบบกล้องวงจรปิดเพดาน)")]
        public Vector3 cameraOffset = new(0f, 11f, -3f);
        public float fieldOfView = 45f;

        [Header("การเล่น")]
        [Tooltip("ความเร็วเล่นเทป (1 = ตามจริง)")]
        public float playSpeed = 1f;
        [Tooltip("แพ้กี่ครั้งถึงได้ลายมือใบ้ 1 บรรทัด")]
        public int failsPerHint = 3;
        [Tooltip("ตัวนับรอบตอนเริ่มเกม")]
        public int startCounter = 47;
        [Tooltip("เริ่มที่ม้วนไหน (ไว้ทดสอบ)")]
        [Range(1, 7)] public int startRoll = 1;

        [Header("ภาพเทป (VHS ขาวดำ)")]
        public float tapeSaturation = -100f;
        public float tapeContrast = 35f;
        public float tapeExposure = 0.5f;
        public float grain = 0.55f;
        public float vignette = 0.4f;
        [Tooltip("ภาพเทปขาวดำ เหลือแต่สีแดง (เลือด, ไฟเตือน) — ปิด = ขาวดำล้วน")]
        public bool keepRedOnly = true;
        [Tooltip("ช่วงสีแดงที่เก็บไว้ (hue ±) — กว้างขึ้น = ส้ม/ชมพูติดมาด้วย")]
        [Range(0.005f, 0.1f)] public float redHueWidth = 0.025f;
        [Tooltip("เลนส์โค้ง (ลบ = ตาปลา)")]
        public float lensDistortion = -0.22f;
        [Tooltip("กล้องสั่น (เมตร)")]
        public float cameraShake = 0.05f;
        [Tooltip("แถบสัญญาณรบกวน VHS โผล่ทุก ๆ กี่วินาที (สุ่มรอบค่านี้)")]
        public float trackingEvery = 7f;

        [Header("ตาย")]
        [Tooltip("ความยาวฉากตายก่อนขึ้นข้อความ (วินาที)")]
        public float deathSceneSeconds = 2.6f;

        [Header("ใบ้ปริศนาเทป (ไม่บอกตรง ๆ — ให้ผู้เล่นคิดเอง)")]
        [Tooltip("กระดาษลายมือภพบนเครื่องเล่นเทป เห็นตั้งแต่ม้วนแรก")]
        [TextArea] public string tapeNote = "ทุกเทปมีตอนจบ… ถ้าเรายอมดูมัน";
        [Tooltip("บรรทัดสุดท้ายของโน้ตหลังจุดพีค (ก่อนเข้าม้วน 7)")]
        [TextArea] public string peakNoteLastLine = "ม้วนสุดท้ายไม่มีอะไรให้แก้ — มีแต่เรา";

        enum Mode { Edit, Play, Message, Sequence }

        Mode mode = Mode.Sequence;
        int rollIndex;
        TapeRoll Roll => rolls[rollIndex];
        List<Piece> pieces = new();
        readonly List<Piece> pile = new();
        int selected;
        float scrub;
        int fails, loops, counter;
        readonly HashSet<int> cleared = new();
        readonly HashSet<TapeEvent> seen = new();
        SimState previewState = new();
        TapePlayer player;
        TapeSegment lastSegment;
        float lastTime;
        bool spliceCaptionShown;

        string message;
        bool messageLow;
        float messageUntil;
        Action afterMessage;
        string overlay;
        bool waitForRestart;
        float glitchUntil;
        readonly List<(string text, float until)> subtitles = new();

        Transform rooms, actors;
        Camera cam;
        ColorAdjustments grade;
        ColorCurves curves;
        Transform currentRoom;
        string heldNow;
        readonly List<(GameObject go, bool negate, bool item, string key)> props = new();
        readonly Dictionary<Transform, (Renderer[] renderers, Material[][] materials, Vector3 scale)> actorLooks = new();
        readonly List<GoreRig> gores = new();
        Transform goreAnchor;
        Vector3 camBasePosition;
        Quaternion camBaseRotation;
        Texture2D scanlines;
        float trackingUntil, nextTracking;

        Font font;
        GUIStyle style;

        // =====================================================================
        void Start()
        {
            // ม้วนที่เสีย (asset หาย / script หาย / ไม่มีชิ้นเทป) → ใช้ค่าตั้งต้นแทน แล้วบอกให้ชัด ไม่ค้างเงียบ ๆ
            if (rolls.Count == 0 || rolls.Any(r => r == null || r.segments == null || r.segments.Count == 0))
            {
                if (rolls.Count > 0)
                    Debug.LogError("[Tape Loop] ม้วนเทปใน TapeGame เสีย (null หรือไม่มีชิ้นเทป) — ใช้ค่าตั้งต้นจาก TapeLevels.cs แทน · ลอง Tools/Tape Loop/Reset Rolls To Defaults");
                rolls = TapeLevels.CreateAll();
            }
            if (house == null) house = GameObject.Find(HouseBuilder.House) ?? HouseBuilder.Build();
            rooms = house.transform.Find(HouseBuilder.Rooms);
            actors = house.transform.Find(HouseBuilder.Actors);
            cam = house.transform.Find(HouseBuilder.Camera).GetComponent<Camera>();
            cam.fieldOfView = fieldOfView;

            foreach (var t in rooms.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool negate = n.StartsWith("!");
                string body = negate ? n.Substring(1) : n;
                if (body.StartsWith("flag:")) props.Add((t.gameObject, negate, false, body.Substring(5)));
                else if (body.StartsWith("item:")) props.Add((t.gameObject, negate, true, body.Substring(5)));
            }

            // จำหน้าตา/สเกลเดิมของตัวละคร — ศพไหม้/แบนแล้วต้องคืนได้ตอนเล่นใหม่
            foreach (Transform a in actors)
            {
                var renderers = a.GetComponentsInChildren<Renderer>(true);
                actorLooks[a] = (renderers, renderers.Select(r => r.sharedMaterials).ToArray(), a.localScale);
            }
            var goreRoot = new GameObject("Gore FX (runtime)").transform;
            goreRoot.SetParent(transform, false);
            for (int i = 0; i < 4; i++) gores.Add(GoreRig.Create(goreRoot, $"Gore {i}"));
            goreAnchor = new GameObject("Gore Anchor").transform;
            goreAnchor.SetParent(goreRoot, false);

            SetupLook();
            counter = startCounter;
            LoadRoll(Mathf.Clamp(startRoll, 1, rolls.Count) - 1);
        }

        void SetupLook()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.34f);
            RenderSettings.fog = false;

            var go = new GameObject("Tape Look (runtime)");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            grade = profile.Add<ColorAdjustments>(true);
            // ขาวดำแต่เก็บแดง: Hue vs Sat = 0 ทุกสี ยกเว้นช่วงแดงแคบ ๆ รอบ hue 0 (0.5 = ไม่เปลี่ยน)
            curves = profile.Add<ColorCurves>(true);
            var hueVsSat = new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 0f),
                new Keyframe(redHueWidth, 0f, 0f, 0f),
                new Keyframe(1f - redHueWidth, 0f, 0f, 0f),
                new Keyframe(1f, 0.5f, 0f, 0f));
            curves.hueVsSat.Override(new TextureCurve(hueVsSat, 0.5f, true, new Vector2(0f, 1f)));
            SetColorMode(false);
            grade.contrast.Override(tapeContrast);
            grade.postExposure.Override(tapeExposure);
            var g = profile.Add<FilmGrain>(true);
            g.type.Override(FilmGrainLookup.Medium4);
            g.intensity.Override(grain);
            profile.Add<Vignette>(true).intensity.Override(vignette);
            profile.Add<ChromaticAberration>(true).intensity.Override(0.25f);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(1f);
            // เลนส์กล้องวงจรปิดราคาถูก: โค้งแบบตาปลานิด ๆ
            profile.Add<LensDistortion>(true).intensity.Override(lensDistortion);

            // scanline ของจอ CRT/VHS (วาดทับทั้งจอใน OnGUI)
            scanlines = new Texture2D(1, 4) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            scanlines.SetPixels(new[] { new Color(0, 0, 0, 0.32f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0.12f) });
            scanlines.Apply();
        }

        // กล้องวงจรปิดติดเพดานไม่นิ่งสนิท — สั่นเล็ก ๆ ตลอด (ภาพดู "ถ่ายจริง" ไม่ใช่กล้องเกม)
        void LateUpdate()
        {
            if (cam == null) return;
            float t = Time.unscaledTime;
            var wobble = new Vector3(Mathf.PerlinNoise(t * 0.7f, 0f) - 0.5f, 0f, Mathf.PerlinNoise(0f, t * 0.6f) - 0.5f) * cameraShake;
            cam.transform.position = camBasePosition + wobble;
            cam.transform.rotation = camBaseRotation * Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t * 0.4f, 7f) - 0.5f) * cameraShake * 8f);
        }

        // =====================================================================
        // ม้วน
        // =====================================================================
        void LoadRoll(int index)
        {
            StopAllCoroutines();
            rollIndex = index;
            pieces = Roll.segments.Select(s => new Piece { segment = s.id, start = 0f, end = s.length }).ToList();
            pile.Clear();
            selected = 0;
            scrub = 0f;
            fails = 0;
            spliceCaptionShown = false;
            player = null;
            subtitles.Clear();
            overlay = null;
            SetColorMode(false);
            RenderPreview();
            ShowMessage($"ม้วนที่ {Roll.number} — {Roll.title}\n<size=26>{Roll.intro}</size>", 5f, () => mode = Mode.Edit);
        }

        void Update()
        {
            var kb = Keyboard.current;
            switch (mode)
            {
                case Mode.Message:
                    bool skip = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
                    if (Time.unscaledTime >= messageUntil || skip)
                    {
                        var then = afterMessage;
                        afterMessage = null;
                        mode = Mode.Edit;
                        then?.Invoke();
                    }
                    break;
                case Mode.Edit:
                    HandleKeys(kb);
                    RenderPreview();
                    break;
                case Mode.Play:
                    TickPlay();
                    break;
                case Mode.Sequence:
                    if (waitForRestart && kb != null && kb.rKey.wasPressedThisFrame)
                    {
                        waitForRestart = false;
                        counter = startCounter;
                        loops = 0;
                        cleared.Clear();
                        LoadRoll(0);
                    }
                    break;
            }
        }

        // =====================================================================
        // โต๊ะตัดต่อ
        // =====================================================================
        void HandleKeys(Keyboard kb)
        {
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) { StartPlay(); return; }
            if (kb.cKey.wasPressedThisFrame) Cut();
            if (kb.xKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame) Discard();
            if (kb.rKey.wasPressedThisFrame) ToggleReverse();
            if (kb.qKey.wasPressedThisFrame) Move(-1);
            if (kb.eKey.wasPressedThisFrame) Move(1);
            if (kb.aKey.wasPressedThisFrame) Select(selected - 1);
            if (kb.dKey.wasPressedThisFrame) Select(selected + 1);
            float speed = kb.leftShiftKey.isPressed ? 4f : 1.5f;
            if (kb.leftArrowKey.isPressed) scrub -= speed * Time.unscaledDeltaTime;
            if (kb.rightArrowKey.isPressed) scrub += speed * Time.unscaledDeltaTime;
        }

        Piece Selected => pieces.Count > 0 ? pieces[Mathf.Clamp(selected, 0, pieces.Count - 1)] : null;

        void Select(int index)
        {
            if (pieces.Count == 0) return;
            selected = Mathf.Clamp(index, 0, pieces.Count - 1);
            var p = Selected;
            scrub = p.reversed ? p.end : p.start;
        }

        void Cut()
        {
            var p = Selected;
            if (p == null || scrub <= p.start + 0.15f || scrub >= p.end - 0.15f)
            {
                Toast("เลื่อนหัวอ่าน (← →) ไปจุดที่จะตัดก่อน");
                return;
            }
            var right = p.Clone();
            right.start = scrub;
            p.end = scrub;
            // ชิ้นที่เล่นถอยหลัง: ส่วนท้าย (ช่วงหลัง) เล่นก่อน
            if (p.reversed) pieces.Insert(selected, right);
            else pieces.Insert(selected + 1, right);
            Toast("✂ ตัด");
        }

        void Discard()
        {
            var p = Selected;
            if (p == null) return;
            pieces.RemoveAt(selected);
            pile.Add(p);
            Select(Mathf.Min(selected, pieces.Count - 1));
        }

        void Restore(int pileIndex)
        {
            var p = pile[pileIndex];
            pile.RemoveAt(pileIndex);
            int at = pieces.Count == 0 ? 0 : selected + 1;
            pieces.Insert(at, p);
            Select(at);
        }

        void Move(int dir)
        {
            int to = selected + dir;
            if (Selected == null || to < 0 || to >= pieces.Count) return;
            (pieces[selected], pieces[to]) = (pieces[to], pieces[selected]);
            selected = to;
        }

        void ToggleReverse()
        {
            if (!Roll.allowReverse)
            {
                Toast("ยังกลับด้านเทปไม่ได้");
                return;
            }
            var p = Selected;
            if (p == null) return;
            p.reversed = !p.reversed;
            scrub = p.reversed ? p.end : p.start;
        }

        void ResetRoll()
        {
            pieces = Roll.segments.Select(s => new Piece { segment = s.id, start = 0f, end = s.length }).ToList();
            pile.Clear();
            Select(0);
        }

        float TapeLength => TapePlayer.TapeLength(Roll, pieces);
        bool OverBudget => Roll.maxLength > 0f && TapeLength > Roll.maxLength + 0.01f;

        // ม้วน 7: แตะเทปเมื่อไหร่ = "ตัด"
        bool EditedFinal()
        {
            var seg = Roll.segments[0];
            return pieces.Count != 1 || pile.Count > 0 || pieces[0].reversed || pieces[0].start > 0.01f || pieces[0].end < seg.length - 0.01f;
        }

        void RenderPreview()
        {
            var p = Selected;
            if (p == null) return;
            scrub = Mathf.Clamp(scrub, p.start, p.end);
            var sim = TapePlayer.Seek(Roll, pieces, selected, scrub);
            seen.UnionWith(sim.history);
            previewState = sim.state;
            // scrub เลยจุดที่มีคนตายในชิ้นนี้ → ค้างภาพศพ + ร่องรอย ณ จุดตาย
            if (sim.outcome == Outcome.Died && sim.CurrentSegment != null && sim.CurrentSegment.id == p.segment)
                RenderAt(sim.CurrentSegment, sim.time, sim.state, sim.deadWho, DeathStyles.Resolve(sim.deathEvent));
            else
                RenderAt(Roll.Segment(p.segment), scrub, sim.state);
        }

        // =====================================================================
        // เล่นเทป
        // =====================================================================
        void StartPlay()
        {
            if (pieces.Count == 0)
            {
                Toast("ไม่มีเทปบน timeline");
                return;
            }
            if (OverBudget)
            {
                Toast($"เทปไม่พอ — ยาว {TapeLength:0.0} วิ แต่ม้วนนี้มีแค่ {Roll.maxLength:0} วิ");
                return;
            }
            player = new TapePlayer(Roll, pieces);
            lastSegment = player.CurrentSegment;
            subtitles.Clear();
            mode = Mode.Play;
        }

        void TickPlay()
        {
            player.Advance(Time.deltaTime * playSpeed);
            seen.UnionWith(player.history);
            foreach (var c in player.captions) Subtitle(c);
            if (player.justCrossedSplice)
            {
                glitchUntil = Time.unscaledTime + 0.3f;
                if (!spliceCaptionShown && !string.IsNullOrEmpty(Roll.spliceCaption))
                {
                    spliceCaptionShown = true;
                    Subtitle(Roll.spliceCaption);
                }
            }
            if (player.CurrentSegment != null)
            {
                lastSegment = player.CurrentSegment;
                lastTime = player.time;
            }
            RenderAt(lastSegment, lastTime, player.state);

            if (player.outcome == Outcome.Died && !(Roll.isFinal && EditedFinal()))
            {
                mode = Mode.Sequence;
                StartCoroutine(DeathScene());
                return;
            }
            if (player.outcome != Outcome.Running) HandleOutcome();
        }

        // ฉากตาย — ต้องตรงกับคำบรรยาย:
        //   1) ห้องเปลี่ยนตามเหตุ (deathFlags: ชั้นล้ม / กระจกแตก / โทรศัพท์ตกอ่าง)
        //   2) ตกจากที่สูง (landRoom / fallsOut): เสียหลัก → ร่วงหลุดเฟรม → ตัดไปห้องที่ศพตก หรือเหลือแต่ร่องรอย
        //   3) ไม่งั้น: ดิ้น/กระตุก → ล้มในท่าตามสาเหตุ → ร่องรอยค่อย ๆ ขยาย
        IEnumerator DeathScene()
        {
            var e = player.deathEvent;
            var style = DeathStyles.Resolve(e);
            var actor = actors.Find(player.deadWho);
            glitchUntil = Time.unscaledTime + 0.25f;
            // เหยื่อไม่อยู่ในเฟรม (เช่น มิ้นท์ติดในห้อง ขณะเทปอยู่ที่ทางเดิน) → สลับไปกล้องห้องที่เธออยู่ แบบจอ CCTV
            if (actor != null && !actor.gameObject.activeSelf && StageVictim(player.deadWho))
            {
                glitchUntil = Time.unscaledTime + 0.5f;
                yield return new WaitForSeconds(0.35f);
            }

            if (e != null && !string.IsNullOrEmpty(e.deathFlags))
            {
                foreach (var f in Csv.Split(e.deathFlags))
                {
                    if (f.StartsWith("-")) player.state.flags.Remove(f.Substring(1));
                    else player.state.flags.Add(f);
                }
                ApplyProps(player.state);
                if (style == DeathStyle.Electric) yield return new WaitForSeconds(0.35f);   // เห็นโทรศัพท์ตกน้ำก่อน แล้วค่อยช็อต
            }

            if (actor != null && actor.gameObject.activeSelf)
            {
                var anim = actor.GetComponent<FigureAnimator>();
                if (anim != null) anim.enabled = false;
                Vector3 standPos = actor.position;
                float yaw = actor.eulerAngles.y;
                int seed = player.deadWho.GetHashCode();
                var gore = gores[0];
                bool falls = e != null && (!string.IsNullOrEmpty(e.landRoom) || e.fallsOut);

                if (falls)
                {
                    // เสียหลัก: โน้มไปข้างหน้า แขนแกว่งหาที่จับ
                    for (float k = 0f; k < 1f; k += Time.deltaTime / 0.4f)
                    {
                        actor.rotation = Quaternion.Euler(Mathf.Lerp(0f, 35f, k), yaw, 0f);
                        Joint(actor, TapeFigure.ShoulderL, new Vector3(-140f + Mathf.Sin(k * 25f) * 30f, 0f, 35f));
                        Joint(actor, TapeFigure.ShoulderR, new Vector3(-140f - Mathf.Sin(k * 25f) * 30f, 0f, -35f));
                        Joint(actor, TapeFigure.HipR, new Vector3(-50f * k, 0f, 0f));
                        yield return null;
                    }
                    // ร่วงลง — หลุดเฟรม
                    Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f)
                    {
                        actor.position = standPos + forward * (k * 1.2f) + Vector3.down * (k * k * 4f);
                        actor.rotation = Quaternion.Euler(Mathf.Lerp(35f, 130f, k), yaw, 0f);
                        yield return null;
                    }

                    if (!string.IsNullOrEmpty(e.landRoom) && rooms.Find(e.landRoom) != null)
                    {
                        // ตัดไปกล้องห้องที่ศพตก
                        glitchUntil = Time.unscaledTime + 0.5f;
                        yield return new WaitForSeconds(0.3f);
                        var land = rooms.Find(e.landRoom);
                        StageRoom(land);
                        Subtitle($"— สลับกล้อง: {e.landRoom} —");
                        PlaceActor(actor, land, e.landPosition, Vector2.zero);
                        FocusOn(e.landPosition);
                        if (anim != null) anim.enabled = false;
                        actor.rotation = Quaternion.Euler(0f, yaw, 0f);
                        standPos = actor.position;
                        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.18f)
                        {
                            DeathPose(actor, style, k, yaw, standPos);
                            yield return null;
                        }
                        DeathPose(actor, style, 1f, yaw, standPos);
                        yield return Bleed(gore, style, actor, seed, Mathf.Max(1.2f, deathSceneSeconds - 0.9f));
                    }
                    else
                    {
                        // ร่วงหายออกนอกเฟรม — เหลือแต่ร่องรอยที่ขอบ
                        actor.gameObject.SetActive(false);
                        goreAnchor.position = standPos;
                        goreAnchor.rotation = Quaternion.Euler(0f, yaw, 0f);
                        yield return Bleed(gore, style, goreAnchor, seed, Mathf.Max(1.2f, deathSceneSeconds - 0.9f));
                    }
                }
                else
                {
                    float struggle = style == DeathStyle.Burn ? 1.3f : style == DeathStyle.Electric ? 1.1f : 0f;
                    for (float k = 0f; k < struggle; k += Time.deltaTime)
                    {
                        var shake = UnityEngine.Random.insideUnitSphere * (style == DeathStyle.Electric ? 0.07f : 0.025f);
                        shake.y = 0f;
                        actor.position = standPos + shake;
                        Writhe(actor, style, k);
                        Char(actor, style == DeathStyle.Burn ? k / struggle * 0.6f : (UnityEngine.Random.value > 0.5f ? 0.55f : 0f));
                        gore.Show(style, actor, 0.15f * k / struggle, seed);
                        yield return null;
                    }

                    float fall = style == DeathStyle.Crush ? 0.22f : 0.5f;
                    for (float k = 0f; k < 1f; k += Time.deltaTime / fall)
                    {
                        DeathPose(actor, style, k * k, yaw, standPos);
                        gore.Show(style, actor, 0.15f + 0.1f * k, seed);
                        yield return null;
                    }
                    DeathPose(actor, style, 1f, yaw, standPos);
                    yield return Bleed(gore, style, actor, seed, Mathf.Max(0.8f, deathSceneSeconds - struggle - fall));
                }
            }
            yield return new WaitForSeconds(0.6f);
            HandleOutcome();
        }

        IEnumerator Bleed(GoreRig gore, DeathStyle style, Transform anchor, int seed, float seconds)
        {
            for (float k = 0.25f; k < 1f; k += Time.deltaTime / seconds)
            {
                gore.Show(style, anchor, k, seed);
                yield return null;
            }
            gore.Show(style, anchor, 1f, seed);
        }

        // ห้องตามสถานะปัจจุบันของเทป (ไม่มีตัวละคร) — ใช้ตอนตัดกล้องไปห้องอื่น
        void StageRoom(Transform room)
        {
            PointCamera(room);
            foreach (var g in gores) g.Hide();
            foreach (Transform a in actors) a.gameObject.SetActive(false);
            ApplyProps(player.state);
        }

        void ApplyProps(SimState state)
        {
            foreach (var (go, negate, item, key) in props)
            {
                bool on = item ? state.items.Contains(key) : state.flags.Contains(key);
                if (go.activeSelf != (on != negate)) go.SetActive(on != negate);
            }
        }

        // หาชิ้นเทปล่าสุด (ตามเวลา) ที่มีเหยื่ออยู่ในเฟรม → แสดงห้องนั้น ณ ท้ายชิ้น ด้วย state ปัจจุบัน (ไฟลุก ฯลฯ)
        bool StageVictim(string who)
        {
            TapeSegment best = null;
            foreach (var s in Roll.segments)
            {
                if (s.memory || !s.actors.Exists(a => a.actor == who && a.keys.Count > 0)) continue;
                if (best == null || s.ClockSeconds > best.ClockSeconds) best = s;
            }
            if (best == null) return false;
            var track = best.actors.Find(a => a.actor == who);
            lastSegment = best;
            lastTime = track.keys[^1].time;
            RenderAt(best, lastTime, player.state);
            Subtitle($"— สลับกล้อง: {best.room} —");
            return true;
        }

        void HandleOutcome()
        {
            var result = player.outcome;
            if (Roll.isFinal && EditedFinal())
            {
                StartCoroutine(LoopBack());
                return;
            }
            switch (result)
            {
                case Outcome.Died:
                    fails++;
                    string where = player.deathOffscreen ? "\n<size=22>(เกิดนอกจอ — ชิ้นที่ตัดทิ้งไม่ได้แปลว่ามันไม่เกิด)</size>" : "";
                    ShowMessage($"{player.deadWho} ตาย\n<size=28>{player.deathCause}</size>{where}", 4f, BackToEdit, low: true);
                    break;
                case Outcome.NoEnd:
                    fails++;
                    ShowMessage("เทปจบ — ยังไม่ถึง END\n<size=26>ยังไม่มีใครรอดจนจบม้วน</size>", 3f, BackToEdit);
                    break;
                case Outcome.Won:
                    cleared.Add(rollIndex);
                    if (Roll.isFinal) StartCoroutine(TrueEnding());
                    else if (Roll.isPeak) StartCoroutine(Peak());
                    else if (Roll.endShots != null && Roll.endShots.Count > 0) StartCoroutine(EndScene());
                    else ShowMessage($"END — {player.winCaption}\n\n<size=26>{Roll.carryOver}</size>", 7f, () => LoadRoll(rollIndex + 1));
                    break;
            }
        }

        void BackToEdit()
        {
            player = null;
            mode = Mode.Edit;
        }

        // =====================================================================
        // ภาพ
        // =====================================================================
        void RenderAt(TapeSegment seg, float t, SimState state, string deadWho = null, DeathStyle deadStyle = DeathStyle.Fall)
        {
            if (seg == null) return;
            var room = rooms.Find(seg.room);
            if (room == null) return;
            PointCamera(room);
            foreach (var g in gores) g.Hide();

            foreach (Transform a in actors) a.gameObject.SetActive(false);
            foreach (var track in seg.actors)
            {
                var actor = actors.Find(track.actor);
                if (actor == null || track.keys.Count == 0) continue;
                if (t < track.keys[0].time - 0.01f || t > track.keys[^1].time + 0.01f) continue;
                var local = Sample(track, t);
                PlaceActor(actor, room, local, Sample(track, t + 0.1f) - Sample(track, t - 0.1f));
                var anim = actor.GetComponent<FigureAnimator>();
                if (anim != null) anim.pose = ChoosePose(track, seg, state, local);
                if (track.actor == deadWho)
                {
                    if (anim != null) anim.enabled = false;
                    DeathPose(actor, deadStyle, 1f, actor.eulerAngles.y, actor.position);
                    gores[0].Show(deadStyle, actor, 1f, deadWho.GetHashCode());
                }
            }

            foreach (var (go, negate, item, key) in props)
            {
                bool on = item ? state.items.Contains(key) : state.flags.Contains(key);
                if (go.activeSelf != (on != negate)) go.SetActive(on != negate);
            }
            SetColorMode(seg.memory);
            AnimateFromEvents(seg, t, state);
        }

        // ท่าทางตามเหตุการณ์: หยิบ/วาง/กด = เอื้อมมือ · คำบรรยาย = คนในเฟรมพูด/ขยับ · ภพถือของ = ของในมือ
        void AnimateFromEvents(TapeSegment seg, float t, SimState state)
        {
            foreach (var track in seg.actors)
            {
                var anim = actors.Find(track.actor)?.GetComponent<FigureAnimator>();
                if (anim == null) continue;
                anim.reach = 0f;
                anim.talk = 0f;
            }
            foreach (var e in seg.events)
            {
                if (string.IsNullOrEmpty(e.caption) || e.effect == EffectType.Kill || e.effect == EffectType.Win) continue;
                float since = t - e.time;
                var doer = Doer(seg, e, t);
                if (doer == null) continue;
                if (e.effect == EffectType.Say)
                {
                    if (since >= 0f && since < 2f) doer.talk = Mathf.Max(doer.talk, 1f - since / 2f);
                }
                else if (since > -0.6f && since < 0.5f)
                    doer.reach = Mathf.Max(doer.reach, 1f - Mathf.Abs(since + 0.05f) / 0.55f);
            }

            ShowHeld(state);
        }

        void ShowHeld(SimState state)
        {
            var phop = actors.Find(TapeLevels.Phop);
            var arm = phop != null ? phop.Find(TapeFigure.ShoulderR) : null;
            if (arm == null) return;
            heldNow = state.items.FirstOrDefault();
            foreach (Transform c in arm)
                if (c.name.StartsWith("Held:")) c.gameObject.SetActive(heldNow != null && c.name == "Held:" + heldNow);
        }

        // ใครทำเหตุการณ์นี้: ชื่อขึ้นต้นคำบรรยาย > ภพ (ถ้าเป็นเรื่องของในมือ) > คนแรกในเฟรม
        FigureAnimator Doer(TapeSegment seg, TapeEvent e, float t)
        {
            FigureAnimator first = null, phop = null;
            foreach (var track in seg.actors)
            {
                if (track.keys.Count == 0 || t < track.keys[0].time - 0.01f || t > track.keys[^1].time + 0.01f) continue;
                var anim = actors.Find(track.actor)?.GetComponent<FigureAnimator>();
                if (anim == null) continue;
                if (e.caption.StartsWith(track.actor)) return anim;
                if (track.actor == TapeLevels.Phop) phop = anim;
                first ??= anim;
            }
            if (phop != null && (e.effect == EffectType.GiveItem || e.effect == EffectType.TakeItem)) return phop;
            return first;
        }

        // ท่าระหว่างชิ้นเทป: ตั้งใน Roll asset ได้ (ActorTrack.pose) — ถ้า Auto เดาจากตำแหน่ง/ของในมือ
        FigureAnimator.Pose ChoosePose(ActorTrack track, TapeSegment seg, SimState state, Vector2 local)
        {
            if (track.pose != FigureAnimator.Pose.Auto) return track.pose;
            string who = track.actor;
            if (who == TapeLevels.Phop && state.items.Count > 0) return FigureAnimator.Pose.Hold;
            if (seg.room == "CAM 05" && who == TapeLevels.Mom && Mathf.Abs(local.x) < 0.8f && Mathf.Abs(local.y) < 1.4f) return FigureAnimator.Pose.Bath;
            bool still = track.keys.TrueForAll(k => k.position == track.keys[0].position);
            if (seg.room == "CAM 08" && still && local.y >= 1.1f)
                return Roll.isFinal && who != TapeLevels.Phop ? FigureAnimator.Pose.Sleep : FigureAnimator.Pose.Sit;
            return FigureAnimator.Pose.Auto;
        }

        // fullColor = ภาพสี (ความทรงจำ / เช้าวันใหม่) · ไม่งั้น = ภาพเทป (ขาวดำ / ขาวดำ+แดง)
        void SetColorMode(bool fullColor)
        {
            if (fullColor)
            {
                grade.saturation.Override(0f);
                curves.hueVsSat.overrideState = false;
            }
            else if (keepRedOnly)
            {
                grade.saturation.Override(0f);
                curves.hueVsSat.overrideState = true;
            }
            else
            {
                grade.saturation.Override(tapeSaturation);
                curves.hueVsSat.overrideState = false;
            }
        }

        void PointCamera(Transform room)
        {
            currentRoom = room;
            camBasePosition = room.position + cameraOffset;
            camBaseRotation = Quaternion.LookRotation(room.position + new Vector3(0f, 0f, 0.4f) - camBasePosition);
            cam.transform.SetPositionAndRotation(camBasePosition, camBaseRotation);
        }

        // ศพที่อยู่ริมล่างของห้องจะโดนแถบข้อความทับ → เลื่อนกล้องตามไปเล็กน้อย (มุมเดิม)
        void FocusOn(Vector2 local)
        {
            if (local.y > -0.8f) return;
            camBasePosition += new Vector3(0f, 0f, local.y + 0.8f);
            cam.transform.position = camBasePosition;
        }

        // วางตัวละครยืนตรง (ท่านั่ง/นอนเป็นงานของ FigureAnimator, ท่าศพเป็นงานของ DeathPose)
        void PlaceActor(Transform actor, Transform room, Vector2 local, Vector2 velocity)
        {
            bool wasHidden = !actor.gameObject.activeSelf;
            ResetLook(actor);
            bool isVacuum = actor.name == TapeLevels.Vacuum;
            float height = isVacuum ? 0f : actor.localScale.y;
            float yaw = velocity.sqrMagnitude > 0.0004f ? Mathf.Atan2(velocity.x, velocity.y) * Mathf.Rad2Deg : actor.eulerAngles.y;
            actor.SetPositionAndRotation(room.position + new Vector3(local.x, height, local.y), Quaternion.Euler(0f, yaw, 0f));
            actor.gameObject.SetActive(true);
            var anim = actor.GetComponent<FigureAnimator>();
            if (anim != null)
            {
                anim.enabled = true;
                if (wasHidden) anim.Snap();
            }
        }

        // ---- ท่าศพ ----
        void DeathPose(Transform actor, DeathStyle style, float p, float yaw, Vector3 standPos)
        {
            if (!actorLooks.TryGetValue(actor, out var look)) return;
            float h = look.scale.y;
            var rig = actor.Find(TapeFigure.Rig);
            if (rig == null)
            {
                actor.rotation = Quaternion.Euler(0f, yaw, 90f * p);
                return;
            }
            rig.localPosition = Vector3.zero;
            rig.localRotation = Quaternion.identity;
            Vector3 hipL = Vector3.zero, hipR = Vector3.zero, shL = Vector3.zero, shR = Vector3.zero, neck = Vector3.zero, root;
            Vector3 scale = look.scale;
            switch (style)
            {
                case DeathStyle.Peaceful:   // นอนหงาย มือวางข้างตัว — ยอมรับแล้ว
                    root = new Vector3(-90f * p, yaw, 0f);
                    shL = new Vector3(0f, 0f, 8f * p);
                    shR = new Vector3(0f, 0f, -8f * p);
                    neck = new Vector3(0f, 12f * p, 0f);
                    break;
                case DeathStyle.Burn:       // ขดตัว ท่านักมวย (แขนงอเข้าหาตัว)
                    root = new Vector3(0f, yaw, 90f * p);
                    hipL = hipR = new Vector3(-95f * p, 0f, 0f);
                    shL = new Vector3(-125f * p, 0f, 12f * p);
                    shR = new Vector3(-125f * p, 0f, -12f * p);
                    neck = new Vector3(45f * p, 0f, 0f);
                    break;
                case DeathStyle.Electric:   // หงายแข็งทื่อ แขนกาง หัวแหงน
                    root = new Vector3(-90f * p, yaw, 0f);
                    shL = new Vector3(0f, 0f, 78f * p);
                    shR = new Vector3(0f, 0f, -78f * p);
                    hipL = new Vector3(12f * p, 0f, 8f * p);
                    hipR = new Vector3(12f * p, 0f, -8f * p);
                    neck = new Vector3(-35f * p, 0f, 0f);
                    break;
                case DeathStyle.Crush:      // หงาย แบนใต้ชั้น แขนขากาง
                    root = new Vector3(-90f * p, yaw, 0f);
                    shL = new Vector3(0f, 0f, 62f * p);
                    shR = new Vector3(0f, 0f, -62f * p);
                    hipL = new Vector3(0f, 0f, 16f * p);
                    hipR = new Vector3(0f, 0f, -16f * p);
                    neck = new Vector3(0f, 0f, 42f * p);
                    scale = Vector3.Scale(look.scale, new Vector3(1f, 1f, Mathf.Lerp(1f, 0.42f, p)));
                    break;
                case DeathStyle.Glass:      // คว่ำ แขนขาบิดคนละทิศ
                    root = new Vector3(0f, yaw, -90f * p);
                    hipL = new Vector3(22f * p, 0f, 0f);
                    hipR = new Vector3(-95f * p, 0f, 10f * p);
                    shL = new Vector3(-40f * p, 0f, 72f * p);
                    shR = new Vector3(-165f * p, 0f, -22f * p);
                    neck = new Vector3(-20f * p, 0f, -65f * p);
                    break;
                default:                    // ตกจากที่สูง: คอหัก ขาพับผิดรูป
                    root = new Vector3(0f, yaw, 90f * p);
                    hipL = new Vector3(-105f * p, 0f, 0f);
                    hipR = new Vector3(28f * p, 0f, -12f * p);
                    shL = new Vector3(-150f * p, 0f, 30f * p);
                    shR = new Vector3(22f * p, 0f, -62f * p);
                    neck = new Vector3(32f * p, 0f, 80f * p);
                    break;
            }
            Joint(actor, TapeFigure.HipL, hipL);
            Joint(actor, TapeFigure.HipR, hipR);
            Joint(actor, TapeFigure.ShoulderL, shL);
            Joint(actor, TapeFigure.ShoulderR, shR);
            Joint(actor, TapeFigure.Neck, neck);
            Joint(actor, TapeFigure.Torso, Vector3.zero);
            actor.localScale = scale;
            float floorY = standPos.y - h;
            actor.SetPositionAndRotation(new Vector3(standPos.x, Mathf.Lerp(standPos.y, floorY + 0.2f * h, p), standPos.z), Quaternion.Euler(root));
            if (style == DeathStyle.Burn) Char(actor, Mathf.Lerp(0.6f, 0.92f, p));
            else if (style == DeathStyle.Electric) Char(actor, 0.5f * p);
        }

        // ดิ้น (ไฟคลอก) / กระตุก (ไฟดูด) ก่อนล้ม
        void Writhe(Transform actor, DeathStyle style, float t)
        {
            if (style == DeathStyle.Electric)
            {
                float j() => UnityEngine.Random.Range(-12f, 12f);
                Joint(actor, TapeFigure.ShoulderL, new Vector3(j(), 0f, 60f + j()));
                Joint(actor, TapeFigure.ShoulderR, new Vector3(j(), 0f, -60f + j()));
                Joint(actor, TapeFigure.Neck, new Vector3(-30f + j(), j(), 0f));
                Joint(actor, TapeFigure.HipL, new Vector3(j(), 0f, 0f));
                Joint(actor, TapeFigure.HipR, new Vector3(j(), 0f, 0f));
                return;
            }
            Joint(actor, TapeFigure.ShoulderL, new Vector3(-90f + Mathf.Sin(t * 22f) * 60f, 0f, 30f + Mathf.Sin(t * 17f) * 30f));
            Joint(actor, TapeFigure.ShoulderR, new Vector3(-90f + Mathf.Sin(t * 19f + 1f) * 60f, 0f, -30f - Mathf.Sin(t * 23f) * 30f));
            Joint(actor, TapeFigure.Neck, new Vector3(Mathf.Sin(t * 14f) * 30f, Mathf.Sin(t * 11f) * 40f, 0f));
            Joint(actor, TapeFigure.Torso, new Vector3(Mathf.Sin(t * 9f) * 20f, 0f, Mathf.Sin(t * 13f) * 15f));
            Joint(actor, TapeFigure.HipL, new Vector3(Mathf.Sin(t * 16f) * 25f, 0f, 0f));
            Joint(actor, TapeFigure.HipR, new Vector3(-Mathf.Sin(t * 16f) * 25f, 0f, 0f));
        }

        static void Joint(Transform actor, string path, Vector3 euler)
        {
            var j = actor.Find(path);
            if (j != null) j.localRotation = Quaternion.Euler(euler);
        }

        // ไหม้: ค่อย ๆ ดำ (0 = ปกติ, 1 = ดำสนิท) — ปัดเป็นขั้น 0.1 ไม่ให้สร้าง material เยอะเกิน
        readonly HashSet<Transform> changedLooks = new();

        void Char(Transform actor, float amount)
        {
            if (!actorLooks.TryGetValue(actor, out var look)) return;
            float step = Mathf.Round(Mathf.Clamp01(amount) * 10f) / 10f;
            for (int i = 0; i < look.renderers.Length; i++)
            {
                var original = look.materials[i].Length > 0 ? look.materials[i][0] : null;
                if (original == null || look.renderers[i].name.StartsWith("Eye")) continue;
                look.renderers[i].sharedMaterial = Greybox.Mat(Color.Lerp(original.color, new Color(0.04f, 0.035f, 0.03f), step));
            }
            changedLooks.Add(actor);
        }

        void ResetLook(Transform actor)
        {
            if (!actorLooks.TryGetValue(actor, out var look)) return;
            actor.localScale = look.scale;
            if (!changedLooks.Remove(actor)) return;
            for (int i = 0; i < look.renderers.Length; i++) look.renderers[i].sharedMaterials = look.materials[i];
        }

        static Vector2 Sample(ActorTrack track, float t)
        {
            var keys = track.keys;
            if (t <= keys[0].time) return keys[0].position;
            for (int i = 1; i < keys.Count; i++)
            {
                if (t > keys[i].time) continue;
                float k = Mathf.InverseLerp(keys[i - 1].time, keys[i].time, t);
                return Vector2.Lerp(keys[i - 1].position, keys[i].position, k);
            }
            return keys[^1].position;
        }

        // ภาพนิ่งสำหรับฉาก sequence: ห้อง + flag + ตัวละคร (dead = ท่าศพ+ร่องรอย, null = ยืน/ท่าปกติ)
        void Pose(string roomName, string flags, params (string actor, float x, float z, DeathStyle? dead)[] who)
        {
            var room = rooms.Find(roomName);
            PointCamera(room);
            foreach (var g in gores) g.Hide();
            foreach (Transform a in actors) a.gameObject.SetActive(false);
            int goreIndex = 0;
            foreach (var w in who)
            {
                var actor = actors.Find(w.actor);
                PlaceActor(actor, room, new Vector2(w.x, w.z), Vector2.zero);
                var anim = actor.GetComponent<FigureAnimator>();
                if (anim != null) anim.pose = FigureAnimator.Pose.Auto;
                if (!w.dead.HasValue) continue;
                if (anim != null) anim.enabled = false;
                DeathPose(actor, w.dead.Value, 1f, actor.eulerAngles.y, actor.position);
                FocusOn(new Vector2(w.x, w.z));
                if (goreIndex < gores.Count) gores[goreIndex++].Show(w.dead.Value, actor, 1f, w.actor.GetHashCode());
            }
            var state = new SimState();
            state.flags.UnionWith(Csv.Split(flags));
            state.flags.Add("lights");
            foreach (var (go, negate, item, key) in props)
            {
                bool on = !item && state.flags.Contains(key);
                go.SetActive(on != negate);
            }
        }

        // =====================================================================
        // ฉากพิเศษ
        // =====================================================================
        IEnumerator Peak()
        {
            mode = Mode.Sequence;
            overlay = "ทุกคนนั่งด้วยกันบนโซฟา… ทุกคนรอด";
            yield return new WaitForSeconds(3f);

            glitchUntil = Time.unscaledTime + 1.2f;
            overlay = "ทุกจอในบ้านเปิดขึ้นเอง\n<size=26>ชิ้นเทปที่ตัดทิ้งทั้งหมด เริ่มเล่นพร้อมกัน</size>";
            yield return new WaitForSeconds(2.5f);

            var montage = new (string room, string actor, float x, float z, string flags, string caption, DeathStyle style)[]
            {
                // ต้องตรงกับฉากตายจริงของแต่ละม้วน (actor ว่าง = ไม่เห็นศพ เหลือแต่ร่องรอย)
                ("CAM 08", TapeLevels.Phop, 2.5f, -1.9f, "", "ภพ — ตกช่องบันได ศพที่ตีนบันได", DeathStyle.Fall),
                ("CAM 03", TapeLevels.Mint, 1.2f, -0.6f, "fire, smoke, doorLocked", "มิ้นท์ — ไฟคลอก", DeathStyle.Burn),
                ("CAM 05", TapeLevels.Mom, 0f, 0f, "phoneInTub", "แม่ — โทรศัพท์ตกอ่าง ไฟดูด", DeathStyle.Electric),
                ("CAM 06b", "", 1.8f, 0f, "crack, glassBroken", "พ่อ — กระจกแตก ร่วงลงไปสองชั้น", DeathStyle.Glass),
                ("CAM 02", TapeLevels.Phop, 2.2f, 2f, "shelfFallen", "ภพ — ชั้นล้มทับ", DeathStyle.Crush),
            };
            foreach (var m in montage)
            {
                if (string.IsNullOrEmpty(m.actor))
                {
                    Pose(m.room, m.flags);
                    goreAnchor.position = rooms.Find(m.room).position + new Vector3(m.x, 0f, m.z);
                    gores[0].Show(m.style, goreAnchor, 1f, 7);
                }
                else Pose(m.room, m.flags, (m.actor, m.x, m.z, m.style));
                overlay = m.caption;
                glitchUntil = Time.unscaledTime + 0.4f;
                yield return new WaitForSeconds(1.5f);
            }

            Pose("CAM 08", "fireLiving", (TapeLevels.Mint, 0f, 1.2f, DeathStyle.Burn), (TapeLevels.Mom, 0.9f, 1.6f, DeathStyle.Burn), (TapeLevels.Dad, -1.1f, 1.6f, DeathStyle.Fall), (TapeLevels.Phop, -0.6f, 0.2f, DeathStyle.Burn));
            SetColorMode(false);
            overlay = "ทุกคนตาย\n<size=28>ช่วยใครไม่ได้เลย</size>";
            yield return new WaitForSeconds(4f);

            Pose("CAM 08", "", (TapeLevels.Shadow, -1f, 2.1f, null));
            // หันหน้าเข้าเครื่องเล่นเทปก่อน แล้วค่อย ๆ หันมามองกล้อง (ตาเรืองแสงโผล่ตอนหันมา)
            var shadow = actors.Find(TapeLevels.Shadow);
            shadow.rotation = Quaternion.identity;
            StartCoroutine(TurnToCamera(shadow, 1.2f, 1.5f));
            overlay = "เงาที่นั่งหน้าเครื่องเล่นเทปตั้งแต่ม้วนแรก… หันมามองกล้อง\n<size=30>หน้าของมันคือภพ</size>";
            glitchUntil = Time.unscaledTime + 0.6f;
            yield return new WaitForSeconds(5f);

            overlay = $"<size=28>ลายมือบนฉลากม้วนสุดท้าย:</size>\n\n\"ทุกครั้งที่เราตัดตัวเองออก ความตายต้องหาคนใหม่\n{counter} ครั้งแล้ว ทุกคนตายครบทุกแบบแล้ว\n{peakNoteLastLine}\"";
            yield return new WaitForSeconds(8f);
            overlay = null;
            LoadRoll(rollIndex + 1);
        }

        // ผ่านม้วน: ตัดภาพจากกล้องหลายห้อง (คนที่รอด → สิ่งที่ทิ้งไว้) แล้วม้วนถัดไปเล่นเอง
        IEnumerator EndScene()
        {
            mode = Mode.Sequence;
            foreach (var shot in Roll.endShots)
            {
                if (shot.glitch) glitchUntil = Time.unscaledTime + 0.45f;
                StageShot(shot);
                overlay = shot.caption;
                yield return new WaitForSeconds(Mathf.Max(0.5f, shot.seconds));
            }
            overlay = null;
            LoadRoll(rollIndex + 1);
        }

        void StageShot(EndShot shot)
        {
            var room = rooms.Find(shot.room);
            if (room == null) return;
            PointCamera(room);
            foreach (var g in gores) g.Hide();
            foreach (Transform a in actors) a.gameObject.SetActive(false);
            foreach (var who in shot.actors)
            {
                var actor = actors.Find(who.actor);
                if (actor == null) continue;
                PlaceActor(actor, room, who.position, Vector2.zero);
                actor.rotation = Quaternion.Euler(0f, who.yaw, 0f);
                var anim = actor.GetComponent<FigureAnimator>();
                if (anim == null) continue;
                anim.pose = who.pose;
                anim.reach = 0f;
                anim.talk = 0f;
            }
            var state = new SimState();
            state.flags.UnionWith(Csv.Split(shot.flags));
            state.items.UnionWith(Csv.Split(shot.items));
            foreach (var (go, negate, item, key) in props)
            {
                bool on = item ? state.items.Contains(key) : state.flags.Contains(key);
                go.SetActive(on != negate);
            }
            ShowHeld(state);
            SetColorMode(shot.room == "MEMORY");
        }

        IEnumerator TurnToCamera(Transform who, float delay, float seconds)
        {
            yield return new WaitForSeconds(delay);
            var from = who.rotation;
            var to = Quaternion.Euler(0f, 180f, 0f);   // กล้องอยู่ด้าน -z ของห้อง
            for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                who.rotation = Quaternion.Slerp(from, to, k * k);
                yield return null;
            }
            who.rotation = to;
        }

        IEnumerator LoopBack()
        {
            mode = Mode.Sequence;
            overlay = "◄◄  เทปกรอกลับ";
            glitchUntil = Time.unscaledTime + 2f;
            yield return new WaitForSeconds(2.2f);
            counter++;
            loops++;
            overlay = $"ตัวนับบนเครื่อง: {counter:0000}\n<size=26>กระดาษบนเครื่องมีลายมือเพิ่มอีกบรรทัด</size>";
            yield return new WaitForSeconds(3.5f);
            overlay = null;
            LoadRoll(0);
        }

        IEnumerator TrueEnding()
        {
            mode = Mode.Sequence;
            overlay = "ภพหลับตา… เทปเล่นจนจบ";
            yield return new WaitForSeconds(3f);
            overlay = "เทปหยุดหมุน";
            Pose("CAM 08", "");
            foreach (Transform a in actors) a.gameObject.SetActive(false);
            cam.cullingMask = 0;
            yield return new WaitForSeconds(2.5f);
            cam.cullingMask = ~0;

            SetColorMode(true);
            Pose("CAM 08", "dawn",
                (TapeLevels.Mint, -0.6f, -0.4f, null), (TapeLevels.Mom, 0.4f, -0.2f, null), (TapeLevels.Dad, 1.2f, -0.6f, null),
                (TapeLevels.Phop, -1f, 1.6f, DeathStyle.Peaceful));
            overlay = "เช้าวันใหม่ — แอปกล้องในโทรศัพท์แม่ · CAM 08 · 06:12\n<size=28>พ่อ แม่ มิ้นท์ ตื่นมาครบ</size>";
            yield return new WaitForSeconds(6f);

            Pose("MEMORY", "",
                (TapeLevels.Mint, 0f, 0.6f, null), (TapeLevels.Mom, -1.2f, 1f, null), (TapeLevels.Dad, 1.2f, 1f, null), (TapeLevels.Phop, 0.8f, -1f, null));
            overlay = $"ภพยอมรับความตายของตัวเอง\n<size=30>ความตายหยุดวน — ทุกคนรอด</size>\n\n<size=26>TRUE ENDING · ตัวนับหยุดที่ {counter:0000}</size>\n\n<size=22>[R] เล่นใหม่</size>";
            waitForRestart = true;
        }

        // =====================================================================
        // UI (IMGUI — prototype)
        // =====================================================================
        // low = ข้อความไว้ล่างจอ (ตอนมีศพ/ร่องรอยให้ดู)
        void ShowMessage(string text, float seconds, Action then, bool low = false)
        {
            messageLow = low;
            message = text;
            messageUntil = Time.unscaledTime + seconds;
            afterMessage = then;
            mode = Mode.Message;
        }

        void Subtitle(string text) => subtitles.Add((text, Time.unscaledTime + 3.5f));
        void Toast(string text) => subtitles.Add((text, Time.unscaledTime + 2f));

        IEnumerable<string> LabelLines()
        {
            yield return "\"" + tapeNote + "\"";
            var final = rolls[^1];
            for (int i = 0; i < Mathf.Min(loops, final.hints.Length); i++) yield return "\"" + final.hints[i] + "\"";
            if (Roll.isFinal) yield break;
            int shown = failsPerHint > 0 ? Mathf.Min(Roll.hints.Length, fails / failsPerHint) : 0;
            for (int i = 0; i < shown; i++) yield return "\"" + Roll.hints[i] + "\"";
        }

        void OnGUI()
        {
            if (style == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Segoe UI Symbol", "Segoe UI" }, 22);
                style = new GUIStyle(GUI.skin.label) { font = font, fontSize = 22, wordWrap = true, richText = true };
            }
            float s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            if (Time.unscaledTime < glitchUntil) Glitch(w, h);
            VhsOverlay(w, h);

            // ---- overlay กล้องวงจรปิด ----
            if (mode != Mode.Sequence && rolls.Count > 0)
            {
                // หลังเล่นเทป (ข้อความผลลัพธ์) ยังเป็นภาพจากเทปที่เพิ่งเล่น ไม่ใช่ชิ้นที่เลือกบนโต๊ะ
                bool fromTape = mode == Mode.Play || (mode == Mode.Message && player != null);
                var seg = fromTape ? lastSegment : Selected != null ? Roll.Segment(Selected.segment) : null;
                float t = fromTape ? lastTime : scrub;
                if (seg != null)
                {
                    // หลังตัดกล้อง (เช่น ศพตกลงไปชั้นล่าง) ป้ายต้องตามกล้องที่เห็นอยู่ ไม่ใช่ห้องของชิ้นเทป
                    string room = fromTape && currentRoom != null ? currentRoom.name : seg.room;
                    string stamp = string.IsNullOrEmpty(seg.clock) ? "MEMORY" : $"{room} · {seg.ClockAt(t)}";
                    Text(new Rect(28, 20, 900, 40), stamp, 30, Color.white);
                }
                Text(new Rect(28, 58, 900, 30), $"ม้วน {Roll.number}/{rolls.Count} — {Roll.title}  ·  ความตายคืนนี้: {Roll.victim}", 20, new Color(1f, 1f, 1f, 0.7f));
                string state = mode == Mode.Play ? (player != null && player.Current != null && player.Current.reversed ? "◄◄ REV" : "▶ PLAY")
                    : fromTape ? "■ STOP" : "❚❚ EDIT";
                Text(new Rect(w - 360, 20, 330, 40), state, 30, mode == Mode.Play ? new Color(1f, 0.4f, 0.4f) : Color.white, TextAnchor.UpperRight);
                Text(new Rect(w - 360, 60, 330, 40), $"COUNTER {counter:0000}", 24, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperRight);
                Text(new Rect(w - 520, 100, 490, 200), string.Join("\n", LabelLines()), 22, new Color(1f, 0.95f, 0.75f), TextAnchor.UpperRight);
            }

            // ---- คำบรรยาย ----
            subtitles.RemoveAll(x => Time.unscaledTime > x.until);
            float subY = mode == Mode.Edit ? h - 330 : h - 140;
            for (int i = 0; i < subtitles.Count; i++)
                Text(new Rect(100, subY - (subtitles.Count - 1 - i) * 36, w - 200, 36), subtitles[i].text, 24, Color.white, TextAnchor.MiddleCenter);

            if (mode == Mode.Edit || mode == Mode.Play) ItemTags(s);   // ฉาก cinematic ไม่ต้องมีป้าย
            if (mode == Mode.Edit) EditorPanel(w, h);

            if (mode == Mode.Message && message != null) Band(w, h, message, messageLow);
            if (mode == Mode.Sequence && overlay != null) Band(w, h, overlay, low: true);
        }

        void EditorPanel(float w, float h)
        {
            float top = h - 290;
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, top, w, 290), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ---- timeline ----
            float x0 = 40, tlW = w - 80, y = top + 18, tlH = 58;
            float total = Mathf.Max(TapeLength, Roll.maxLength, 1f);
            float scale = tlW / total;
            float x = x0;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                var seg = Roll.Segment(p.segment);
                var rect = new Rect(x, y, Mathf.Max(18f, p.Length * scale - 2f), tlH);
                GUI.color = i == selected ? new Color(1f, 0.85f, 0.3f) : seg.memory ? new Color(0.9f, 0.6f, 0.75f) : new Color(0.75f, 0.75f, 0.75f);
                if (GUI.Button(rect, "")) Select(i);
                GUI.color = Color.white;
                Text(new Rect(rect.x + 6, rect.y + 2, rect.width - 8, 26), $"{p.segment}{(p.reversed ? " ◄" : "")}", 20, Color.black);
                Text(new Rect(rect.x + 6, rect.y + 28, rect.width - 8, 24), $"{p.start:0.0}–{p.end:0.0}", 15, new Color(0f, 0f, 0f, 0.7f));
                foreach (var e in seg.events)
                {
                    if (e.time < p.start || e.time > p.end || e.effect == EffectType.Say && !seen.Contains(e)) continue;
                    float ex = p.reversed ? rect.x + (p.end - e.time) * scale : rect.x + (e.time - p.start) * scale;
                    GUI.color = e.effect == EffectType.Kill ? Color.red : e.effect == EffectType.Win ? Color.green : new Color(0.2f, 0.2f, 0.2f);
                    GUI.DrawTexture(new Rect(ex, rect.yMax - 10, 3, 10), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                if (i == selected)
                {
                    float px = p.reversed ? rect.x + (p.end - scrub) * scale : rect.x + (scrub - p.start) * scale;
                    GUI.color = Color.red;
                    GUI.DrawTexture(new Rect(px - 1, rect.y - 6, 3, tlH + 12), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                x += p.Length * scale + Roll.spliceCost * scale;
            }
            if (Roll.maxLength > 0f)
            {
                GUI.color = OverBudget ? Color.red : new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(new Rect(x0 + Roll.maxLength * scale, y - 8, 3, tlH + 16), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // ---- scrub ----
            var sel = Selected;
            float rowY = y + tlH + 14;
            if (sel != null)
            {
                var seg = Roll.Segment(sel.segment);
                scrub = GUI.HorizontalSlider(new Rect(x0, rowY, 520, 20), scrub, sel.start, sel.end);
                Text(new Rect(x0 + 540, rowY - 8, 900, 30), $"ชิ้น {seg.id} — {seg.title} · หัวอ่าน {scrub:0.0}s / {seg.length:0.0}s", 20, Color.white);
            }

            // ---- ปุ่ม ----
            float by = rowY + 32, bx = x0;
            if (Btn(ref bx, by, 150, "✂ ตัด [C]")) Cut();
            if (Btn(ref bx, by, 130, "◄ ย้าย [Q]")) Move(-1);
            if (Btn(ref bx, by, 130, "ย้าย ► [E]")) Move(1);
            if (Roll.allowReverse && Btn(ref bx, by, 170, "◄◄ กลับด้าน [R]")) ToggleReverse();
            if (Btn(ref bx, by, 170, "ทิ้งลงกอง [X]")) Discard();
            if (Btn(ref bx, by, 150, "เริ่มม้วนใหม่")) ResetRoll();
            if (Btn(ref bx, by, 170, "▶ เล่น [Space]")) StartPlay();
            if (loops > 0 && cleared.Contains(rollIndex) && !Roll.isFinal && Btn(ref bx, by, 220, "ข้าม ►► (เคยผ่านแล้ว)")) LoadRoll(rollIndex + 1);

            // ---- กองข้างเครื่อง + ข้อมูล ----
            float py = by + 52, pxPos = x0;
            Text(new Rect(pxPos, py, 180, 34), "กองข้างเครื่อง:", 20, new Color(1f, 1f, 1f, 0.7f));
            pxPos += 170;
            for (int i = 0; i < pile.Count; i++)
            {
                var p = pile[i];
                if (Btn(ref pxPos, py, 150, $"{p.segment} {p.start:0.0}–{p.end:0.0}{(p.reversed ? " ◄" : "")}")) { Restore(i); break; }
            }

            string budget = Roll.maxLength > 0f ? $"เทป {TapeLength:0.0}/{Roll.maxLength:0} วิ (ต่อ 1 ครั้ง = {Roll.spliceCost:0} วิ)" : $"เทป {TapeLength:0.0} วิ";
            string hands = previewState.items.Count > 0 ? "ในมือภพ: " + string.Join(", ", previewState.items.Select(ItemName)) : "ในมือภพ: —";
            Text(new Rect(x0, py + 44, 900, 30), budget, 20, OverBudget ? new Color(1f, 0.35f, 0.3f) : Color.white);
            Text(new Rect(x0 + 520, py + 44, 900, 30), hands, 20, new Color(0.85f, 0.9f, 1f));
            Text(new Rect(w - 760, py + 44, 720, 30), "A/D เลือกชิ้น · ←/→ เลื่อนหัวอ่าน (Shift เร็ว)", 16, new Color(1f, 1f, 1f, 0.45f), TextAnchor.UpperRight);
        }

        static string ItemName(string id) => id switch
        {
            "phone" => "โทรศัพท์มิ้นท์ (ไฟฉาย)",
            "key" => "กุญแจฉุกเฉิน",
            "extinguisher" => "ถังดับเพลิง",
            "tablet" => "แท็บเล็ตคุมบ้าน",
            "strap" => "สายรัดของ",
            _ => id,
        };

        bool Btn(ref float x, float y, float width, string label)
        {
            var bs = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18 };
            bool hit = GUI.Button(new Rect(x, y, width, 40), label, bs);
            x += width + 10;
            return hit;
        }

        void Band(float w, float h, string text, bool low = false)
        {
            // low = ฉาก sequence: ข้อความไว้ล่างจอ ให้เห็นภาพ (ศพ, เงา, ครอบครัวตอนเช้า) เต็ม ๆ
            var rect = low ? new Rect(0, h * 0.66f, w, h * 0.3f) : new Rect(0, h * 0.28f, w, h * 0.44f);
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            Text(new Rect(80, rect.y, w - 160, rect.height), text, low ? 32 : 38, Color.white, TextAnchor.MiddleCenter);
        }

        // ป้ายระบุของแบบระบบกล้องตรวจจับวัตถุ: [ ] รอบของที่ยังไม่ถูกหยิบ + "ถือ:" ข้างภพ
        void ItemTags(float s)
        {
            if (cam == null || currentRoom == null) return;
            foreach (var (go, negate, item, key) in props)
            {
                if (!item || !negate || !go.activeInHierarchy || !go.transform.IsChildOf(currentRoom)) continue;
                Tag(go.transform.position, "▸ " + ItemName(key), s, new Color(1f, 0.95f, 0.6f), true);
            }
            var phop = actors.Find(TapeLevels.Phop);
            if (heldNow != null && phop != null && phop.gameObject.activeInHierarchy)
                Tag(phop.position + Vector3.up * 1.1f, "ถือ: " + ItemName(heldNow), s, new Color(0.75f, 0.9f, 1f), false);
        }

        void Tag(Vector3 world, string text, float s, Color color, bool bracket)
        {
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return;
            float x = sp.x / s, y = (Screen.height - sp.y) / s;
            if (bracket)
            {
                GUI.color = color;
                const float r = 18f, l = 7f;
                GUI.DrawTexture(new Rect(x - r, y - r, l, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - r, y - r, 2, l), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + r - l, y - r, l, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + r - 2, y - r, 2, l), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - r, y + r - 2, l, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - r, y + r - l, 2, l), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + r - l, y + r - 2, l, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + r - 2, y + r - l, 2, l), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Text(new Rect(x + 22f, y - 14f, 300f, 28f), text, 18, color);
        }

        // ภาพเทปวิดีโอ: scanline ทั้งจอ + แถบ tracking เลื่อนผ่านเป็นระยะ + REC กะพริบ
        void VhsOverlay(float w, float h)
        {
            if (scanlines != null)
                GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), scanlines, new Rect(0, 0, 1, h / 3f));

            float now = Time.unscaledTime;
            if (now > nextTracking)
            {
                trackingUntil = now + 0.45f;
                nextTracking = now + trackingEvery * UnityEngine.Random.Range(0.6f, 1.4f);
            }
            if (now < trackingUntil)
            {
                float k = 1f - (trackingUntil - now) / 0.45f;
                float y = Mathf.Lerp(-60f, h, k);
                for (int i = 0; i < 6; i++)
                {
                    GUI.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.04f, 0.16f));
                    GUI.DrawTexture(new Rect(UnityEngine.Random.Range(-40f, 40f), y + i * 7f, w, UnityEngine.Random.Range(2f, 6f)), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
            }

            if (mode == Mode.Play && Mathf.Repeat(now, 1f) < 0.6f)
                Text(new Rect(28, 86, 200, 30), "<color=#ff3030>●</color> REC", 20, Color.white);
        }

        void Glitch(float w, float h)
        {
            for (int i = 0; i < 7; i++)
            {
                GUI.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.05f, 0.25f));
                GUI.DrawTexture(new Rect(0, UnityEngine.Random.Range(0f, h), w, UnityEngine.Random.Range(2f, 18f)), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        void Text(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            style.fontSize = size;
            style.alignment = anchor;
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
        }

        // =====================================================================
        // Debug — ใช้โดย TapeLoopHarness
        // =====================================================================
        public string DebugMode => mode.ToString();
        public int DebugRoll => rollIndex + 1;
        public int DebugCounter => counter;
        public string DebugState => $"roll={rollIndex + 1} mode={mode} counter={counter} pieces=[{string.Join(" ", pieces)}] pile=[{string.Join(" ", pile)}]";

        public void DebugLoadRoll(int number)
        {
            LoadRoll(number - 1);
            messageUntil = 0f;
        }

        public void DebugSetPieces(List<Piece> list)
        {
            pieces = list.Select(p => p.Clone()).ToList();
            pile.Clear();
            var used = new HashSet<string>(pieces.Select(p => p.segment));
            foreach (var s in Roll.segments.Where(s => !used.Contains(s.id)))
                pile.Add(new Piece { segment = s.id, start = 0f, end = s.length });
            Select(0);
        }

        public void DebugSelect(int index, float t)
        {
            Select(index);
            scrub = t;
        }

        public void DebugPlay() => StartPlay();
        public void DebugSkipMessage() => messageUntil = 0f;
    }
}
