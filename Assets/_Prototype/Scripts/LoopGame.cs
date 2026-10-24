using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HorrorJam
{
    // "ดวงถึงฆาต" prototype — ลูป 4 นาที (17:56 → 18:00) ในซอยเดียว
    //
    // เรื่อง: อาจารย์เพียร (หมอดู) ผูกด้ายดำให้ภพ "เสริมดวง" — จริง ๆ ด้ายดำทำให้ภพหนีความตายได้ (ย้อนเวลา)
    //         ทุกครั้งที่หนี ดวงไหลไปต่ออายุหมอดู และความตายไปตกที่คนอื่นแทน
    //
    // กฎความตาย: ต้องมีคนตาย 1 คนเสมอ ตามลำดับสมุด ภพ → ต้น → ป้าศรี → ลุงชม
    //   - ภพหลบได้ = คนถัดไปที่ไม่ถูกป้องกันตายแทน
    //   - ป้องกันครบทุกคน = ความตายวนกลับมาหาภพ (แล้วย้อนเวลาอีก)
    //
    // 3 องก์:
    //   1. เอาตัวรอด — เรียนรู้ chain (กระถาง, สายไฟ, มอเตอร์ไซค์)
    //   2. ช่วยคนอื่น — สืบเรื่องหมอดู → เปิดกล่อง → เขียนชื่อจริงบนหุ่น → เผาด้วยไฟศาล = จุดพีค (ล้มเหลว ทุกคนตาย)
    //   3. ยอมรับ — เผาด้ายดำที่ข้อมือตัวเอง → ไม่ย้อนเวลาอีก → ภพตาย ทุกคนรอด (true ending)
    //
    // โลกจริงอยู่ใน scene (GameObject "World" — สร้างด้วย Tools/Horror Jam/Bake World To Scene)
    // ทุกลูป clone World ใหม่จาก template แล้วทิ้งอันเก่า => ไม่มี state ค้างข้ามลูปแน่นอน
    public class LoopGame : MonoBehaviour
    {

        const string Pliers = "คีม";
        const string SaiSin = "ด้ายสายสิญจน์";
        const string Doll = "หุ่นฟาง";

        [Tooltip("ว่างไว้ได้ — จะหา GameObject ชื่อ World ใน scene เอง ถ้าไม่มีจะสร้างตอน Play")]
        [SerializeField] GameObject worldTemplate;
        [Tooltip("ฟอนต์ไทย (เช่น Sarabun/Kanit) — ว่างไว้จะใช้ Leelawadee UI / Tahoma ของ Windows")]
        [SerializeField] Font thaiFont;

        [Header("ค่าจูนทั้งหมด (แก้ได้ระหว่าง Play ด้วย)")]
        public LoopTimings timing = new();
        public SenseSettings senses = new();
        public CameraSettings cameraSettings = new();
        public MoodSettings mood = new();
        public DialogueSettings dialogue = new();

        float LoopLength => timing.loopLength;

        static readonly Color Blood = new(0.6f, 0.05f, 0.05f);
        static readonly Color Burnt = new(0.12f, 0.1f, 0.1f);
        static readonly Color Peaceful = new(0.85f, 0.85f, 0.8f);
        static readonly Color OldHair = new(0.85f, 0.85f, 0.85f);
        static readonly Color YoungHair = new(0.05f, 0.04f, 0.04f);

        enum DeathStyle { Crushed, Electric, Burned, Caught, Hit, Heart }

        class Interaction
        {
            public Func<Vector3> position;
            public float radius;
            public Func<string> label;   // null = ไม่โชว์เลย
            public Func<bool> canUse;    // false = โชว์เป็นสีเทา (บอกว่ามีของ แต่ยังไม่ใช่ตอนนี้)
            public Action use;
            public int priority;         // สูงกว่าชนะ แม้อยู่ไกลกว่า (เช่น "เผาหุ่น" ต้องชนะ "อ่านสมุด")
            public bool onTable;         // ของบนโต๊ะหมอดู — ถ้าหมอดูเห็นตอนแตะ = ตาย
        }

        // ---- object ใน World (bind ใหม่ทุกลูป) ----
        Transform world;
        Camera cam;
        PlayerController player;
        Npc fortune, aunt, child, uncle, ghost;
        Transform ladder, ball, pot, notebook, bill, thread, box, boxLid, pliers, stallLamp, truck, bike, peakView;
        GameObject snappedWire, sparks, playerThread, handCandle, handDoll, phone, squeezeDoll;
        Light[] streetLights;
        float[] streetLightBase;
        Quaternion ladderRest;
        Text clockText, loopText, memoryText, heldText, promptText, noticeText, bigText;
        GameObject bigPanel;

        // markers
        Vector3 toiletDoor, tableSeat, shrine, auntDoor, auntRoof, potSpot, childA, childB, kickSpot;
        Vector3 alleyMouth, uncleEntry, unclePole, toolbox, puddleCenter, stallFront, bikeStart, bikeEnd;
        float puddleRadius;

        readonly List<(float time, Action action)> events = new();
        readonly List<Interaction> interactions = new();
        readonly List<(Vector3 position, GameObject label)> corpseLabels = new();
        System.Random rng;

        // ---- state ของลูปนี้ ----
        float t;
        int nextEvent;
        string held;
        bool dollRenamed, boxOpen, candleLit;
        bool auntOnRoof, auntCalled, auntSafe, childSafe, uncleSafe, wireFixed;
        bool fortuneAway, ladderUp, ladderFallen, puddleLive, ballKicked, phoneActive;
        bool accepted, acceptConfirm;
        bool locked, endReady, trueEnding;
        int lastMurmur, lastPhone, auntTalks;
        float flickerUntil, acceptConfirmUntil, camWide = 1f;
        Interaction focused;
        Transform camFocus;

        string big;
        float bigUntil;
        string notice;
        float noticeUntil;

        void Start()
        {
            if (worldTemplate == null)
            {
                var found = GameObject.Find(Names.World);
                worldTemplate = found != null ? found : AlleyBuilder.Build();
            }
            if (thaiFont == null)
                thaiFont = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Segoe UI" }, 32);
            UseWorld(worldTemplate);
            BuildLoop();
        }

        // เล่นบน World ตัวจริงใน scene (ไม่ clone) — เลือก object ใน Hierarchy ระหว่าง Play = ตัวที่ขยับอยู่จริง
        void UseWorld(GameObject target)
        {
            world = target.transform;
            world.gameObject.SetActive(true);
            // ฟอนต์ default ของ Unity ไม่มีภาษาไทย
            foreach (var text in world.GetComponentsInChildren<Text>(true)) text.font = thaiFont;
            if (world.Find(Names.Templates) == null)
            {
                // scene ที่ Bake ไว้ก่อนมี Templates — สร้างให้ชั่วคราว (Bake ใหม่เพื่อให้แก้ใน Editor ได้)
                AlleyBuilder.BuildTemplates(world);
                foreach (var text in world.Find(Names.Templates).GetComponentsInChildren<Text>(true)) text.font = thaiFont;
            }
            TakeSnapshot();
        }

        // ---- ย้อนเวลา = คืนทุกอย่างใน World กลับเป็นสถานะตอนกด Play ----
        struct TransformState
        {
            public Transform transform;
            public Vector3 position, scale;
            public Quaternion rotation;
            public bool active;
        }

        readonly List<TransformState> transformStates = new();
        readonly List<(Renderer renderer, Material[] materials)> rendererStates = new();
        readonly List<(Light light, float intensity, Color color, bool enabled)> lightStates = new();
        readonly List<GameObject> spawned = new();   // ของที่เกิดระหว่างลูป (ศพ, ด้ายผูกแขน, แสงวาบ) — ลบทิ้งตอนย้อนเวลา

        void TakeSnapshot()
        {
            transformStates.Clear();
            rendererStates.Clear();
            lightStates.Clear();
            foreach (var tr in world.GetComponentsInChildren<Transform>(true))
                transformStates.Add(new TransformState { transform = tr, position = tr.localPosition, rotation = tr.localRotation, scale = tr.localScale, active = tr.gameObject.activeSelf });
            foreach (var r in world.GetComponentsInChildren<Renderer>(true))
                rendererStates.Add((r, r.sharedMaterials));
            foreach (var l in world.GetComponentsInChildren<Light>(true))
                lightStates.Add((l, l.intensity, l.color, l.enabled));
        }

        void RestoreSnapshot()
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                go.SetActive(false);
                Destroy(go);
            }
            spawned.Clear();

            var controllers = world.GetComponentsInChildren<CharacterController>(true);
            foreach (var cc in controllers) cc.enabled = false;   // CharacterController บังคับตำแหน่งตัวเอง ต้องปิดก่อนย้าย
            foreach (var s in transformStates)
            {
                if (s.transform == null) continue;
                s.transform.localPosition = s.position;
                s.transform.localRotation = s.rotation;
                s.transform.localScale = s.scale;
            }
            foreach (var s in transformStates)
                if (s.transform != null && s.transform.gameObject.activeSelf != s.active)
                    s.transform.gameObject.SetActive(s.active);
            foreach (var (r, mats) in rendererStates)
                if (r != null) r.sharedMaterials = mats;
            foreach (var (l, intensity, color, enabled) in lightStates)
            {
                if (l == null) continue;
                l.intensity = intensity;
                l.color = color;
                l.enabled = enabled;
            }
            foreach (var cc in controllers) cc.enabled = true;
            foreach (var npc in world.GetComponentsInChildren<Npc>(true)) npc.ResetState();
            foreach (var p in world.GetComponentsInChildren<PlayerController>(true)) p.ResetState();
        }

        void BuildLoop()
        {
            StopAllCoroutines();
            RestoreSnapshot();
            Time.timeScale = 1f;

            events.Clear();
            interactions.Clear();
            corpseLabels.Clear();
            // seed คงที่ = เด็กเดินเส้นเดิมทุกลูป (ผู้เล่นต้องเชื่อว่าโลกคาดเดาได้ ไม่งั้นความรู้ไร้ค่า)
            rng = new System.Random(1356);

            t = 0f;
            nextEvent = 0;
            held = null;
            dollRenamed = boxOpen = candleLit = false;
            auntOnRoof = auntCalled = auntSafe = childSafe = uncleSafe = wireFixed = false;
            fortuneAway = ladderUp = ladderFallen = puddleLive = ballKicked = phoneActive = false;
            accepted = acceptConfirm = false;
            locked = endReady = trueEnding = false;
            lastMurmur = lastPhone = -1;
            auntTalks = 0;
            flickerUntil = 0f;
            camWide = 1f;
            notice = null;

            Bind();
            ApplyDoom();
            BuildCorpses();
            BuildInteractions();
            Schedule();
            events.Sort((a, b) => a.time.CompareTo(b.time));

            if (LoopState.Loop == 1)
                Big(dialogue.intro, 5f);
            else if (LoopState.Knows(Facts.Truth))
                Big($"17:56\nครั้งที่ {LoopState.Loop}\n<size=28>ด้ายดำยังอยู่ที่ข้อมือ</size>", 2.6f);
            else
                Big($"17:56\nครั้งที่ {LoopState.Loop}", 2.2f);
        }

        void Bind()
        {
            cam = Find(Names.Camera).GetComponent<Camera>();
            cam.fieldOfView = cameraSettings.fieldOfView;
            player = Find(Names.Player).GetComponent<PlayerController>();
            camFocus = player.transform;
            fortune = Find(Names.Fortune).GetComponent<Npc>();
            aunt = Find(Names.Aunt).GetComponent<Npc>();
            child = Find(Names.Child).GetComponent<Npc>();
            uncle = Find(Names.Uncle).GetComponent<Npc>();
            ghost = Find(Names.Ghost).GetComponent<Npc>();
            foreach (var npc in new[] { fortune, aunt, child, uncle, ghost }) npc.listener = player.transform;

            playerThread = Find(Names.PlayerThread).gameObject;
            handCandle = Find(Names.HandCandle).gameObject;
            handDoll = Find(Names.HandDoll).gameObject;
            phone = Find(Names.FortunePhone).gameObject;
            squeezeDoll = Find(Names.FortuneDoll).gameObject;

            ladder = Find(Names.Ladder);
            ladderRest = ladder.localRotation;
            ball = Find(Names.Ball);
            pot = Find(Names.Pot);
            notebook = Find(Names.Notebook);
            bill = Find(Names.Bill);
            thread = Find(Names.Thread);
            box = Find(Names.Box);
            boxLid = Find(Names.BoxLid);
            pliers = Find(Names.Pliers);
            stallLamp = Find(Names.StallLamp);
            truck = Find(Names.Truck);
            bike = Find(Names.Bike);
            snappedWire = Find(Names.SnappedWire).gameObject;
            sparks = Find(Names.Sparks).gameObject;
            streetLights = Find(Names.StreetLights).GetComponentsInChildren<Light>();
            streetLightBase = Array.ConvertAll(streetLights, l => l.intensity);

            var puddle = Find(Names.Puddle);
            puddleCenter = puddle.position;
            puddleRadius = puddle.lossyScale.x * 0.5f;

            toiletDoor = Marker(Names.ToiletDoor);
            tableSeat = Marker(Names.TableSeat);
            shrine = Marker(Names.Shrine);
            auntDoor = Marker(Names.AuntDoor);
            auntRoof = Marker(Names.AuntRoof);
            potSpot = Marker(Names.PotSpot);
            childA = Marker(Names.ChildAreaA);
            childB = Marker(Names.ChildAreaB);
            kickSpot = Marker(Names.KickSpot);
            alleyMouth = Marker(Names.AlleyMouth);
            uncleEntry = Marker(Names.UncleEntry);
            unclePole = Marker(Names.UnclePole);
            toolbox = Marker(Names.Toolbox);
            stallFront = Marker(Names.StallFront);
            bikeStart = Marker(Names.BikeStart);
            bikeEnd = Marker(Names.BikeEnd);
            peakView = Find(Names.Markers + Names.PeakView);

            var hud = Names.Hud + "/";
            clockText = Find(hud + "Clock").GetComponent<Text>();
            loopText = Find(hud + "Loop").GetComponent<Text>();
            memoryText = Find(hud + "Memory").GetComponent<Text>();
            heldText = Find(hud + "Held").GetComponent<Text>();
            promptText = Find(hud + "Prompt").GetComponent<Text>();
            noticeText = Find(hud + "Notice").GetComponent<Text>();
            bigPanel = Find(hud + "Big Panel").gameObject;
            bigText = Find(hud + "Big Panel/Big Text").GetComponent<Text>();

            // ยิ่งภพตาย หมอดูยิ่งหนุ่ม — foreshadow ว่าใครได้ประโยชน์จากการหนีความตาย
            float youth = LoopState.Knows(Facts.Truth) ? 1f : Mathf.Clamp01(LoopState.Corpses.Count / 6f);
            SetFortuneHair(Color.Lerp(OldHair, YoungHair, youth));
        }

        Transform Find(string path)
        {
            var found = world.Find(path);
            if (found == null)
                throw new Exception($"[LoopGame] ไม่เจอ '{path}' ใน World — ห้ามเปลี่ยนชื่อ object (ดู Names ใน AlleyBuilder.cs) หรือกด Tools/Horror Jam/Bake World To Scene ใหม่");
            return found;
        }

        Vector3 Marker(string name) => Find(Names.Markers + name).position;

        // =====================================================================
        // ตาราง — หัวใจของ Majora: ทุกอย่างเกิดเวลาเดิม ผู้เล่นเป็นคนเดียวที่เปลี่ยนมันได้
        // =====================================================================
        void Schedule()
        {
            // ป้าศรี: เข้าบ้าน → ขึ้นดาดฟ้ารดน้ำต้นไม้ของลูก
            At(timing.auntWalkHome, () => aunt.WalkTo(auntDoor, timing.auntWalkSpeed));
            At(timing.auntGoesInside, () => aunt.Show(false));
            At(timing.auntOnRoof, () =>
            {
                if (auntSafe) return;
                aunt.Show(true);
                aunt.Place(auntRoof);
                auntOnRoof = true;
            });
            At(timing.potOmen, () => { if (auntOnRoof) Omen("ตุ๊กแก... ตุ๊กแก...", 0f); });
            At(timing.potFall, () => { if (auntOnRoof) StartCoroutine(PotFalls()); });

            // ต้น: เตะบอลเล่นกลางซอย (เส้นทางสุ่มแต่ seed เดิม)
            for (float s = 1f; s < timing.childWanderUntil; s += timing.childWanderEvery)
                At(s, () => child.WalkTo(new Vector3(Rand(childA.x, childB.x), childA.y, Rand(childA.z, childB.z)), timing.childWanderSpeed));
            At(timing.childToKickSpot, () => child.WalkTo(kickSpot, timing.childRunSpeed));
            At(timing.childKick, () => StartCoroutine(KickBall()));

            // ลุงชม: มาซ่อมสายไฟ แต่ลืมคีม
            At(timing.uncleArrive, () =>
            {
                if (uncleSafe) return;
                uncle.Show(true);
                uncle.Place(uncleEntry);
                uncle.WalkTo(unclePole, timing.uncleWalkSpeed);
            });
            At(timing.ladderUp, () =>
            {
                if (wireFixed || !uncle.Visible) return;
                ladder.gameObject.SetActive(true);
                ladderUp = true;
                uncle.Say("ลุงชม: คีมกูหายไปไหนวะ... ขันน็อตมือเปล่าไม่ไหวแล้ว", 9f, senses.uncleComplaintRadius);
            });

            // หมอดู: คุยโทรศัพท์ (แอบฟังได้ถ้าไม่อยู่ในสายตา) → เข้าห้องน้ำ 14 วิ (ช่องเดียวที่แตะของบนโต๊ะได้)
            At(timing.phoneStart, () =>
            {
                if (fortuneAway || !fortune.Visible) return;
                phoneActive = true;
                phone.SetActive(true);
            });
            At(timing.fortuneLeaves, () => { EndPhone(); fortune.WalkTo(toiletDoor, timing.fortuneWalkSpeed); });
            At(timing.fortuneAway, () => { fortune.Show(false); fortuneAway = true; });
            At(timing.fortuneReturns, () =>
            {
                fortune.Show(true);
                fortuneAway = false;
                fortune.WalkTo(tableSeat, timing.fortuneWalkSpeed, 90f);
            });

            // สายไฟ: ลางเตือน (ไฟกะพริบ) แล้วตกลงน้ำขัง
            At(timing.wireOmen, () => { if (ladderFallen) Omen("ไฟทั้งซอยกะพริบ...", 5f); });
            At(timing.wireDrop, () =>
            {
                if (!ladderFallen) return;
                snappedWire.SetActive(true);
                sparks.SetActive(true);
                puddleLive = true;
            });

            // มอเตอร์ไซค์ส่งของ: ซิ่งลงมาทั้งซอย (ตอนที่ผู้เล่นอาจกำลังถือเทียนเดินช้า ๆ)
            At(timing.bikeOmen, () =>
            {
                bike.gameObject.SetActive(true);
                bike.position = bikeStart;
                Omen("แตรรถดังลั่นมาจากปากซอย! ไฟหน้าสาดเข้ามา", 0f);
            });
            At(timing.bikeRide, () => StartCoroutine(BikeRide()));
        }

        void At(float time, Action action) => events.Add((time, action));
        float Rand(float a, float b) => Mathf.Lerp(Mathf.Min(a, b), Mathf.Max(a, b), (float)rng.NextDouble());

        // =====================================================================
        // Loop
        // =====================================================================
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f5Key.wasPressedThisFrame)   // debug: ล้างความจำ เริ่มจากศูนย์
            {
                LoopState.Clear();
                BuildLoop();
                return;
            }

            UpdateCamera();
            UpdateCorpseLabels();

            if (locked)
            {
                if (endReady && kb.rKey.wasPressedThisFrame)
                {
                    if (trueEnding) LoopState.Clear();
                    else LoopState.Loop++;
                    BuildLoop();
                }
                return;
            }

            Time.timeScale = kb.tabKey.isPressed ? timing.fastForward : 1f;
            t += Time.deltaTime;
            while (nextEvent < events.Count && events[nextEvent].time <= t)
                events[nextEvent++].action();

            if (!ballKicked && child.Visible)
                ball.position = child.transform.position + child.transform.forward * 0.5f + Vector3.down * 0.55f;

            AuntMurmur();
            PhoneCall();
            Flicker();

            if (candleLit && player.IsRunning)
            {
                SetCandle(false);
                Notice("เทียนดับ! (ถือเทียนแล้ววิ่งไม่ได้)", 3f);
            }

            if (puddleLive && FlatDistance(player.transform.position, puddleCenter) < puddleRadius)
            {
                Die("เหยียบน้ำขังที่สายไฟขาดตกลงมา — ไฟดูด", DeathStyle.Electric);
                return;
            }

            UpdateInteraction(kb);

            if (t >= LoopLength)
            {
                t = LoopLength;
                locked = true;
                StopAllCoroutines();
                StartCoroutine(SixPm());
            }
        }

        void LateUpdate() => UpdateHud();

        void AuntMurmur()
        {
            if (!auntOnRoof || t < timing.murmurStart || t >= timing.murmurEnd) return;
            var lines = dialogue.auntMurmurs;
            int i = Mathf.Min(lines.Length - 1, (int)((t - timing.murmurStart) / timing.murmurLineSeconds));
            if (i != lastMurmur)
            {
                lastMurmur = i;
                aunt.Say(lines[i], timing.murmurLineSeconds, senses.auntMurmurHearRadius);
            }
            // ต้องยืนฟังจริงถึงประโยคที่มีชื่อ — ใต้ดาดฟ้าพอดี ซึ่งคือที่กระถางจะตก
            if (i >= 2 && aunt.PlayerHears) Learn(Facts.SonName);
        }

        void PhoneCall()
        {
            if (!phoneActive) return;
            if (t >= timing.phoneEnd || fortuneAway)
            {
                EndPhone();
                return;
            }
            // ต้องอยู่ใกล้ แต่ต้องอยู่นอกสายตา (ข้าง ๆ / หลังแผง) — ยืนหน้าโต๊ะ = เขาวางสาย
            if (FortuneSees(player.transform.position))
            {
                fortune.Say("หมอดู: ...แค่นี้ก่อน มีลูกค้า", 2.5f);
                EndPhone();
                return;
            }
            var lines = dialogue.phoneLines;
            int i = Mathf.Min(lines.Length - 1, (int)((t - timing.phoneStart) / timing.phoneLineSeconds));
            if (i != lastPhone)
            {
                lastPhone = i;
                fortune.Say(lines[i], timing.phoneLineSeconds, senses.phoneHearRadius);
            }
            if (i == lines.Length - 1 && fortune.PlayerHears) Learn(Facts.DollInBox);
        }

        void EndPhone()
        {
            phoneActive = false;
            phone.SetActive(false);
        }

        // สายตาหมอดู: กรวยด้านหน้า 100° ระยะ 4.5 ม.
        bool FortuneSees(Vector3 position)
        {
            if (!fortune.Visible || fortuneAway) return false;
            Vector3 d = position - fortune.transform.position;
            d.y = 0f;
            return d.magnitude < senses.fortuneSightRange && Vector3.Angle(fortune.transform.forward, d) < senses.fortuneSightHalfAngle;
        }

        void Learn(string fact)
        {
            if (LoopState.Learn(fact)) Notice("จำได้: " + Facts.Describe(fact), 5f);
        }

        // =====================================================================
        // 18:00
        // =====================================================================
        IEnumerator SixPm()
        {
            Time.timeScale = 1f;
            player.frozen = true;
            if (accepted)
            {
                yield return AcceptanceEnding(false);
                yield break;
            }

            Big("18:00", 1.4f);
            yield return new WaitForSeconds(1.4f);

            var order = new (string name, bool safe, Npc npc, string cause, Func<IEnumerator> scene)[]
            {
                ("ต้น", childSafe, child, dialogue.childDeath, ChildHitByTruck),
                ("ป้าศรี", auntSafe, aunt, dialogue.auntDeath, AuntFalls),
                ("ลุงชม", uncleSafe, uncle, dialogue.uncleDeath, UncleElectrocuted),
            };
            foreach (var v in order)
            {
                if (v.safe) continue;
                camFocus = v.npc.transform;
                yield return v.scene();
                yield return new WaitForSeconds(0.8f);
                LoopState.RecordDeath(v.name, v.cause);
                string hint = LoopState.Knows(Facts.Truth) ? "\n<size=26>(ทุกครั้งที่ภพหนี คนอื่นต้องจ่ายแทน)</size>" : "";
                Big($"18:00 — ภพรอด... แต่ {v.name} ตายแทน\n<size=30>สาเหตุ: {v.cause}</size>{hint}\n\n<size=26>[R] ย้อนกลับไป 17:56</size>", 9999f);
                endReady = true;
                yield break;
            }

            // ป้องกันครบทุกคน — ความตายไม่มีที่ลง เลยวนกลับมาหาเจ้าของดวง
            camFocus = player.transform;
            fortune.Say("หมอดู: ปกป้องได้ทุกคน... ยกเว้นตัวมึงเอง", 3.5f);
            yield return new WaitForSeconds(2.5f);
            yield return PlayerDeath("ทุกคนในซอยได้รับการปกป้อง — ความตายจึงวนกลับมาหาเจ้าของดวง", DeathStyle.Heart);
        }

        // =====================================================================
        // Death — ทุกการตายต้อง "เห็น" ก่อน แล้วค่อยบอกด้วยตัวหนังสือว่าใครตายเพราะอะไร
        // =====================================================================
        void Die(string cause, DeathStyle style)
        {
            if (locked) return;
            locked = true;
            StopAllCoroutines();
            StartCoroutine(PlayerDeath(cause, style));
        }

        void Caught() => Die("หมอดูเห็นเธอแตะของบนโต๊ะ — บีบหุ่นฟางในมือ", DeathStyle.Caught);

        IEnumerator PlayerDeath(string cause, DeathStyle style)
        {
            Time.timeScale = 1f;
            player.frozen = true;
            camFocus = player.transform;
            var body = player.transform;

            switch (style)
            {
                case DeathStyle.Crushed:
                    Paint(body, Blood);
                    Vector3 s0 = body.localScale, s1 = new Vector3(1.3f, 0.25f, 1.3f);
                    Vector3 p0 = body.position, p1 = new Vector3(p0.x, 0.25f, p0.z);
                    for (float k = 0f; k < 1f; k += Time.deltaTime / 0.12f)
                    {
                        body.localScale = Vector3.Lerp(s0, s1, k);
                        body.position = Vector3.Lerp(p0, p1, k);
                        yield return null;
                    }
                    break;
                case DeathStyle.Electric:
                    yield return Shake(body, 1.3f, new Color(0.5f, 0.85f, 1f), new Color(0.7f, 0.9f, 1f), Burnt);
                    yield return Collapse(body, Burnt);
                    break;
                case DeathStyle.Burned:
                    yield return Shake(body, 1.5f, new Color(1f, 0.45f, 0.1f), new Color(1f, 0.5f, 0.1f), Burnt);
                    yield return Collapse(body, Burnt);
                    break;
                case DeathStyle.Caught:
                    squeezeDoll.SetActive(true);
                    fortune.Say("หมอดู: อย่ามาแตะของกู", 2.5f);
                    yield return new WaitForSeconds(0.6f);
                    yield return Shake(body, 1.2f, new Color(0.5f, 0.1f, 0.6f), new Color(0.35f, 0.1f, 0.4f), Blood);
                    yield return Collapse(body, Blood);
                    break;
                case DeathStyle.Hit:
                    yield return Fling(body, new Vector3(1.4f, 0f, -4.5f), 0.7f);
                    break;
                case DeathStyle.Heart:
                    yield return Shake(body, 0.8f, new Color(0.6f, 0f, 0f), Peaceful, Blood);
                    yield return Collapse(body, new Color(0.5f, 0.52f, 0.55f));
                    break;
            }

            // ยอมรับดวงแล้ว — ไม่ย้อนเวลาอีก ไม่ว่าจะตายแบบไหน
            if (accepted)
            {
                yield return AcceptanceEnding(true);
                yield break;
            }

            yield return new WaitForSeconds(0.7f);
            LoopState.RecordDeath("ภพ (เธอ)", cause);
            Big($"ภพ ตาย\n<size=30>สาเหตุ: {cause}</size>", 3.2f);
            yield return new WaitForSeconds(3.2f);
            ReturnByDeath();
        }

        void ReturnByDeath()
        {
            var pos = player.transform.position;
            LoopState.Corpses.Add(new LoopState.Corpse { position = new Vector3(pos.x, 1f, pos.z), cause = LoopState.Deaths[LoopState.Deaths.Count - 1].cause, loop = LoopState.Loop });
            LoopState.Loop++;
            BuildLoop();
        }

        // =====================================================================
        // จุดพีค — เผาหุ่นสำเร็จ แต่ช่วยใครไม่ได้เลย
        // =====================================================================
        void StartPeak()
        {
            locked = true;
            StopAllCoroutines();
            StartCoroutine(Peak());
        }

        IEnumerator Peak()
        {
            Time.timeScale = 1f;
            player.frozen = true;
            held = null;
            handDoll.SetActive(false);
            SetCandle(false);
            camFocus = fortune.transform;

            // 1) ดูเหมือนได้ผล: หมอดูไหม้ แก่ลง
            var fire = SpawnFlash(fortune.transform, Vector3.up * 0.3f, new Color(1f, 0.45f, 0.1f));
            fire.intensity = 0f;
            fortune.Say("หมอดู: อ๊ากกกก! ไฟศาล...!", 2.5f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2f)
            {
                fire.intensity = UnityEngine.Random.Range(4f, 14f);
                SetFortuneHair(Color.Lerp(YoungHair, OldHair, k));
                fortune.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.82f, k);
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);

            // 2) ...แล้วหัวเราะ กลับมาหนุ่ม
            fortune.Say("หมอดู: ...ฮ่า ๆ ๆ ๆ ๆ ๆ", 2.5f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 1.5f)
            {
                fire.intensity = Mathf.Lerp(8f, 0f, k);
                SetFortuneHair(Color.Lerp(OldHair, YoungHair, k));
                fortune.transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.08f, k);
                yield return null;
            }
            Destroy(fire.gameObject);
            yield return new WaitForSeconds(0.6f);

            // 3) เฉลย
            var reveal = dialogue.peakReveal;
            for (int i = 0; i < reveal.Length; i++)
            {
                // บรรทัดที่ 2 = เฉลยด้ายดำ → ตัดไปที่ข้อมือภพ
                if (i == 1)
                {
                    camFocus = player.transform;
                    playerThread.GetComponent<Renderer>().sharedMaterial = Greybox.Glow(new Color(0.9f, 0.05f, 0.05f), 3f);
                }
                if (i == 3) camFocus = fortune.transform;
                yield return FortuneLine(reveal[i]);
            }

            // 4) 18:00 ทุก chain พร้อมกัน
            t = LoopLength;
            Big("18:00", 1.4f);
            yield return new WaitForSeconds(1.4f);
            camWide = cameraSettings.peakWide;
            camFocus = peakView;
            StartCoroutine(AuntFalls());
            StartCoroutine(ChildHitByTruck());
            StartCoroutine(UncleElectrocuted());
            yield return new WaitForSeconds(7f);

            camWide = 1f;
            camFocus = player.transform;
            yield return Shake(player.transform, 1f, new Color(0.6f, 0f, 0f), Blood, Burnt);
            yield return Collapse(player.transform, Blood);

            LoopState.RecordDeath("ต้น", "โดนรถกระบะชน — พร้อมกันทั้งซอย");
            LoopState.RecordDeath("ป้าศรี", "ตกจากดาดฟ้า — พร้อมกันทั้งซอย");
            LoopState.RecordDeath("ลุงชม", "ไฟแรงสูงดูด — พร้อมกันทั้งซอย");
            LoopState.RecordDeath("ภพ (เธอ)", "เผาหุ่นต่อหน้าหมอดู — ดวงที่หนีมาตลอดตามทัน");
            LoopState.Learn(Facts.Truth);
            Big(dialogue.peakTitle, 5.5f);
            yield return new WaitForSeconds(5.5f);
            ReturnByDeath();
        }

        IEnumerator FortuneLine(string line)
        {
            fortune.Say(line, dialogue.peakLineSeconds);
            yield return new WaitForSeconds(dialogue.peakLineSeconds);
        }

        // =====================================================================
        // องก์ 3 — ยอมรับ
        // =====================================================================
        IEnumerator BurnThread()
        {
            var r = playerThread.GetComponent<Renderer>();
            for (float k = 0f; k < 1.2f; k += Time.deltaTime)
            {
                r.sharedMaterial = Greybox.Glow(new Color(1f, 0.45f, 0.1f), UnityEngine.Random.Range(1f, 5f));
                yield return null;
            }
            playerThread.SetActive(false);
            Notice("ด้ายดำไหม้หมดแล้ว... ภพรู้สึกเบาอย่างประหลาด", 5f);

            // โลกกลับมามีสี: grain/vignette จาง, หมอกบาง
            var volume = world.GetComponentInChildren<Volume>();
            volume.profile.TryGet<Vignette>(out var vignette);
            volume.profile.TryGet<FilmGrain>(out var grain);
            volume.profile.TryGet<ColorAdjustments>(out var grade);
            float v0 = vignette != null ? vignette.intensity.value : 0f;
            float g0 = grain != null ? grain.intensity.value : 0f;
            float s0 = grade != null ? grade.saturation.value : 0f;
            float e0 = grade != null ? grade.postExposure.value : 0f;
            float fog0 = RenderSettings.fogDensity;
            Color ambient0 = RenderSettings.ambientLight;
            for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, mood.acceptFadeSeconds))
            {
                if (vignette != null) vignette.intensity.Override(Mathf.Lerp(v0, mood.acceptVignette, k));
                if (grain != null) grain.intensity.Override(Mathf.Lerp(g0, 0f, k));
                if (grade != null)
                {
                    grade.saturation.Override(Mathf.Lerp(s0, mood.acceptSaturation, k));
                    grade.postExposure.Override(Mathf.Lerp(e0, e0 + mood.acceptExposureAdd, k));
                }
                RenderSettings.fogDensity = Mathf.Lerp(fog0, mood.acceptFogDensity, k);
                RenderSettings.ambientLight = Color.Lerp(ambient0, mood.acceptAmbient, k);
                yield return null;
            }
        }

        IEnumerator AcceptanceEnding(bool alreadyDead)
        {
            locked = true;
            endReady = false;
            Time.timeScale = 1f;
            player.frozen = true;
            fortune.Show(true);

            if (!alreadyDead)
            {
                Big("18:00", 1.4f);
                yield return new WaitForSeconds(1.4f);
                // ภพเดินไปนั่งหน้าโต๊ะหมอดูเอง
                camFocus = player.transform;
                fortune.Place(tableSeat);
                fortune.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Vector3 dir = stallFront - player.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(dir);
                yield return Tween(player.transform, stallFront, Mathf.Clamp(dir.magnitude / 2.5f, 0.5f, 6f));
                player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                yield return FortuneLine("หมอดู: มึง... ยอมรับมันเหรอ? ไม่... ไม่ได้นะ!");
            }

            // หมอดูแก่ลงจนเป็นผง — ดวงที่ขโมยไปไหลกลับเจ้าของ
            camFocus = fortune.transform;
            fortune.Say("หมอดู: ดวงกู... ดวงกูไหลออกไป...!", 3f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2.5f)
            {
                SetFortuneHair(Color.Lerp(YoungHair, OldHair, k));
                fortune.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, k);
                yield return null;
            }
            Paint(fortune.transform, new Color(0.55f, 0.55f, 0.52f));
            for (float k = 0f; k < 1f; k += Time.deltaTime / 1.2f)
            {
                fortune.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.01f, k);
                yield return null;
            }
            fortune.Show(false);
            yield return new WaitForSeconds(0.8f);

            if (!alreadyDead)
            {
                camFocus = player.transform;
                yield return new WaitForSeconds(0.6f);
                yield return Collapse(player.transform, Peaceful);
                yield return new WaitForSeconds(1.2f);
            }

            // เอ๋กลับบ้าน
            aunt.Show(true);
            aunt.Place(auntDoor);
            aunt.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            ghost.Show(true);
            ghost.Place(new Vector3(0f, 1f, 19f));
            ghost.WalkTo(auntDoor + new Vector3(-1.3f, 0f, 0f), timing.ghostWalkSpeed, 90f);
            camWide = cameraSettings.endingWide;
            camFocus = aunt.transform;
            yield return new WaitForSeconds(3.5f);
            ghost.Say("เอ๋: แม่... ผมกลับบ้านแล้วครับ", 4.5f);
            yield return new WaitForSeconds(1.5f);
            aunt.Say("ป้าศรี: เอ๋...! ลูกแม่...", 4f);
            yield return new WaitForSeconds(3.5f);

            LoopState.RecordDeath("ภพ (เธอ)", "ยอมรับดวงของตัวเอง — จากไปอย่างสงบ");
            Big(dialogue.endingTitle + "\n\n<size=24>[R] เล่นใหม่ (ล้างความจำ)</size>", 9999f);
            trueEnding = true;
            endReady = true;
        }

        // =====================================================================
        // ฉากตายของ NPC (18:00 และจุดพีค)
        // =====================================================================
        IEnumerator ChildHitByTruck()
        {
            child.Show(true);
            ballKicked = true;
            child.Say("ต้น: บอลผม!", 2.5f);
            Vector3 ballTarget = new Vector3(alleyMouth.x + 0.6f, 0.18f, alleyMouth.z);
            StartCoroutine(Tween(ball, ballTarget, 1.2f));
            child.WalkTo(alleyMouth, timing.childChaseBallSpeed);
            for (float w = 0f; w < 6f && FlatDistance(child.transform.position, alleyMouth) > 0.3f; w += Time.deltaTime)
                yield return null;

            truck.gameObject.SetActive(true);
            Vector3 start = truck.position, end = new Vector3(14f, start.y, start.z);
            bool hit = false;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 1.1f)
            {
                truck.position = Vector3.Lerp(start, end, k);
                if (!hit && truck.position.x + 2.4f >= child.transform.position.x)
                {
                    hit = true;
                    StartCoroutine(Fling(child.transform, new Vector3(3.2f, 0f, 1f), 0.8f));
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator AuntFalls()
        {
            aunt.Show(true);
            aunt.Place(auntRoof);
            aunt.Say("ป้าศรี: เอ๋... แม่ได้ยินเสียงลูก...", 2.5f);
            Vector3 edge = potSpot - auntRoof;
            edge.y = 0f;
            aunt.transform.rotation = Quaternion.LookRotation(edge);
            yield return new WaitForSeconds(2.2f);

            // โน้มตัวออกจากขอบ แล้วร่วง
            Quaternion lean = aunt.transform.rotation * Quaternion.Euler(35f, 0f, 0f);
            yield return Rotate(aunt.transform, lean, 0.5f);
            Vector3 from = aunt.transform.position, to = new Vector3(potSpot.x, 0.5f, potSpot.z);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.7f)
            {
                aunt.transform.position = Vector3.Lerp(from, to, k * k);
                aunt.transform.Rotate(240f * Time.deltaTime, 0f, 0f, Space.Self);
                yield return null;
            }
            aunt.transform.position = to;
            yield return Collapse(aunt.transform, Blood);
        }

        IEnumerator UncleElectrocuted()
        {
            uncle.Show(true);
            ladder.gameObject.SetActive(true);
            ladder.localRotation = ladderRest;
            uncle.Place(unclePole);
            uncle.Say("ลุงชม: ได้บันไดใหม่แล้ว ต่อเส้นนี้ก็เสร็จ", 2.5f);
            yield return new WaitForSeconds(2f);
            yield return Tween(uncle.transform, unclePole + Vector3.up * 3.2f, 1.2f);
            uncle.Say("ลุงชม: อ๊ากกกก!", 1.5f);
            yield return Shake(uncle.transform, 1.4f, new Color(0.5f, 0.85f, 1f), new Color(0.7f, 0.9f, 1f), Burnt);
            yield return Tween(uncle.transform, new Vector3(unclePole.x + 0.6f, 0.5f, unclePole.z), 0.4f, accelerate: true);
            yield return Collapse(uncle.transform, Burnt);
        }

        // ---- animation helpers ----
        IEnumerator Shake(Transform who, float seconds, Color flashColor, Color paintA, Color paintB)
        {
            var flash = SpawnFlash(who, Vector3.up * 0.5f, flashColor);
            float peak = flash.intensity;
            Vector3 basePos = who.position;
            for (float w = 0f; w < seconds; w += Time.deltaTime)
            {
                who.position = basePos + UnityEngine.Random.insideUnitSphere * 0.08f;
                flash.intensity = UnityEngine.Random.Range(0f, peak);
                Paint(who, UnityEngine.Random.value > 0.5f ? paintA : paintB);
                yield return null;
            }
            who.position = basePos;
            Destroy(flash.gameObject);
        }

        IEnumerator Collapse(Transform who, Color color)
        {
            Paint(who, color);
            Quaternion from = who.rotation;
            Quaternion to = Quaternion.Euler(0f, who.eulerAngles.y, 90f);
            Vector3 p0 = who.position;
            Vector3 p1 = new Vector3(p0.x, 0.3f * who.lossyScale.y, p0.z);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f)
            {
                who.rotation = Quaternion.Slerp(from, to, k * k);
                who.position = Vector3.Lerp(p0, p1, k * k);
                yield return null;
            }
            who.rotation = to;
            who.position = p1;
        }

        IEnumerator Fling(Transform who, Vector3 offset, float seconds)
        {
            Paint(who, Blood);
            Vector3 from = who.position, to = from + offset;
            to.y = 0.4f;
            for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                who.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
                who.Rotate(0f, 0f, -720f * Time.deltaTime, Space.World);
                yield return null;
            }
            who.position = to;
            who.rotation = Quaternion.Euler(0f, 90f, 90f);
        }

        IEnumerator Tween(Transform who, Vector3 to, float seconds, bool accelerate = false)
        {
            Vector3 from = who.position;
            for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                who.position = Vector3.Lerp(from, to, accelerate ? k * k : k);
                yield return null;
            }
            who.position = to;
        }

        IEnumerator Rotate(Transform who, Quaternion to, float seconds)
        {
            Quaternion from = who.rotation;
            for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                who.rotation = Quaternion.Slerp(from, to, k);
                yield return null;
            }
            who.rotation = to;
        }

        // ตัวละครมีหลายชิ้น (หัว/ตัว/แขน) — ทาสีทุกชิ้นยกเว้นตา
        static void Paint(Transform who, Color color)
        {
            var mat = Greybox.Mat(color);
            foreach (var r in who.GetComponentsInChildren<MeshRenderer>())
                if (!r.name.StartsWith("Eye")) r.sharedMaterial = mat;
        }

        void SetFortuneHair(Color color)
        {
            var mat = Greybox.Mat(color);
            Find(Names.FortuneHair).GetComponent<Renderer>().sharedMaterial = mat;
            Find(Names.FortuneBeard).GetComponent<Renderer>().sharedMaterial = mat;
        }

        void SetCandle(bool lit)
        {
            candleLit = lit;
            handCandle.SetActive(lit);
        }

        // =====================================================================
        // Death chains ระหว่างลูป
        // =====================================================================
        IEnumerator PotFalls()
        {
            yield return Tween(pot, potSpot + Vector3.up * 0.25f, 0.45f, accelerate: true);
            pot.localScale = new Vector3(0.8f, 0.15f, 0.8f);
            if (FlatDistance(player.transform.position, potSpot) < senses.potHitRadius)
                Die("ยืนใต้ดาดฟ้าป้าศรี กระถางต้นไม้ตกใส่หัว", DeathStyle.Crushed);
        }

        IEnumerator KickBall()
        {
            ballKicked = true;
            child.Say("ต้น: ยิงงงง!", 2f);
            Vector3 from = ball.position;
            Vector3 to = ladder.position + new Vector3(0.4f, 0.18f, 0f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.7f)
            {
                ball.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                yield return null;
            }
            if (!ladderUp) yield break;

            // บันไดล้ม → ลุงหนี → สายไฟค้าง รอขาดตอน 160
            ladderUp = false;
            ladderFallen = true;
            uncle.Say("ลุงชม: เฮ้ยยย! บันไดกู!", 3f);
            yield return Rotate(ladder, ladder.rotation * Quaternion.Euler(0f, 0f, -100f), 0.8f);
            yield return new WaitForSeconds(3f);
            uncle.Say("ลุงชม: ไปยืมบันไดบ้านเฮียก่อน...", 3f);
            uncle.WalkTo(uncleEntry, timing.uncleWalkSpeed);
            yield return new WaitForSeconds(6f);
            uncle.Show(false);
        }

        IEnumerator BikeRide()
        {
            float duration = timing.bikeDuration;
            for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
            {
                bike.position = Vector3.Lerp(bikeStart, bikeEnd, k);
                Vector3 p = player.transform.position;
                if (Mathf.Abs(p.x - bike.position.x) < senses.bikeHitHalfWidth && Mathf.Abs(p.z - bike.position.z) < senses.bikeHitHalfLength)
                {
                    Die("มอเตอร์ไซค์ส่งของซิ่งเข้าซอย ชนเต็มแรง", DeathStyle.Hit);
                    yield break;
                }
                yield return null;
            }
            bike.gameObject.SetActive(false);
        }

        void Omen(string text, float flickerSeconds)
        {
            Notice($"ลาง: {text}", 3f);
            if (flickerSeconds > 0f) flickerUntil = Time.time + flickerSeconds;
        }

        void Flicker()
        {
            bool flickering = Time.time < flickerUntil;
            for (int i = 0; i < streetLights.Length; i++)
                streetLights[i].intensity = flickering && UnityEngine.Random.value > 0.5f ? 0.3f : streetLightBase[i];
            if (sparks.activeSelf)
                sparks.GetComponentInChildren<Light>().intensity = UnityEngine.Random.Range(2f, 12f);
        }

        // =====================================================================
        // Interactions — ไม่มี quest log: prompt บอกได้แค่ "ทำอะไรได้" ไม่บอก "ควรทำอะไร"
        // =====================================================================
        void BuildInteractions()
        {
            // ---- หมอดู ----
            Interact(() => fortune.transform.position, 2.4f,
                () => fortune.Visible && !fortuneAway ? "คุยกับหมอดู" : null,
                () => fortune.Say(
                    LoopState.Knows(Facts.Truth) ? "หมอดู: ยังไม่ยอมอีกเหรอ... ดีแล้ว หนีต่อไปเรื่อย ๆ"
                    : LoopState.Loop == 1 ? "หมอดู: ดวงมึงถึงฆาต หนีไม่พ้นหรอก"
                    : "หมอดู: (ยิ้ม) ...เจอกันอีกแล้วนะ", 3.5f));

            // เผาหุ่นต่อหน้าเจ้าของ — ต้องถือทั้งหุ่นและเทียนศาล
            Interact(() => fortune.transform.position, 2.8f,
                () => held == Doll && candleLit && fortune.Visible && !fortuneAway
                    ? (dollRenamed ? "เผาหุ่น 'สมพงษ์' ต่อหน้าหมอดู" : "เผาหุ่นต่อหน้าหมอดู") : null,
                () =>
                {
                    if (dollRenamed) StartPeak();
                    else Die("เผาหุ่นที่ยังมีชื่อตัวเองอยู่ — ไฟคุณไสยลามเข้าตัว", DeathStyle.Burned);
                },
                priority: 2);

            // ---- ของบนโต๊ะ (แตะตอนหมอดูเห็น = ตาย) ----
            Interact(() => notebook.position, 1.6f,
                () => "อ่านสมุดดวง",
                () =>
                {
                    bool fresh = !LoopState.Knows(Facts.DeathList);
                    Learn(Facts.DeathList);
                    Learn(Facts.BurnMethod);
                    if (!fresh) Notice("ชื่อเธออยู่บรรทัดแรก... ชื่อ 'เอ๋' ถูกขีดฆ่าไว้หน้าแรก");
                },
                onTable: true);

            Interact(() => bill.position, 1.6f,
                () => "อ่านบิลค่าไฟที่ทับไว้",
                () =>
                {
                    Notice("บิลค่าไฟ — ผู้ใช้ไฟ: นายสมพงษ์ แสงทอง (ค้างชำระ 3 เดือน)", 4f);
                    LoopState.Learn(Facts.RealName);
                },
                onTable: true);

            Interact(() => thread.position, 1.6f,
                () => thread.gameObject.activeSelf ? "หยิบด้ายสายสิญจน์" : null,
                () => { held = SaiSin; thread.gameObject.SetActive(false); },
                () => held == null,
                onTable: true);

            Interact(() => box.position, 1.4f,
                () => boxOpen ? null : LoopState.Knows(Facts.DeathDate) ? "หมุนรหัส 2-3-1-0 เปิดกล่อง" : "กล่องล็อก — รหัส 4 หลัก (วว/ดด)",
                () =>
                {
                    boxOpen = true;
                    boxLid.localRotation = Quaternion.Euler(-110f, 0f, 0f);
                    box.Find("Doll").gameObject.SetActive(false);
                    held = Doll;
                    dollRenamed = false;
                    handDoll.SetActive(true);
                    Notice("หุ่นฟางพันด้ายดำ... ป้ายกระดาษที่คอเขียนว่า 'ภพ'", 4f);
                },
                () => held == null && LoopState.Knows(Facts.DeathDate),
                onTable: true);

            // ---- เครื่องมือ ----
            Interact(() => toolbox, 1.8f,
                () => pliers.gameObject.activeSelf ? "หยิบคีมในกล่องเครื่องมือ" : null,
                () => { held = Pliers; pliers.gameObject.SetActive(false); },
                () => held == null);

            // ---- ศาลพระภูมิ (หลายอย่างในจุดเดียว เรียงตามความสำคัญ) ----
            Interact(() => shrine, 1.9f, ShrineLabel, UseShrine, priority: 1);

            // ---- ป้าศรี ----
            Interact(() => potSpot, 3.2f,
                () => !auntOnRoof ? null : LoopState.Knows(Facts.SonName) ? "ตะโกน \"แม่! เอ๋เองนะ!\"" : "ตะโกนเรียกป้า",
                () =>
                {
                    if (!LoopState.Knows(Facts.SonName))
                    {
                        aunt.Say("ป้าศรี: ใครเรียก! ไม่ว่าง!", 2.5f);
                        return;
                    }
                    aunt.Say("ป้าศรี: เอ๋...? ลูกแม่เหรอ!", 2.5f);
                    auntOnRoof = false;
                    auntSafe = auntCalled = true;
                    StartCoroutine(Later(1.8f, () =>
                    {
                        aunt.Place(auntDoor);
                        aunt.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                        aunt.Say("ป้าศรี: ...ไม่ใช่เอ๋หรอกเหรอ", 3f);
                    }));
                });

            Interact(() => aunt.transform.position, 2.2f,
                () =>
                {
                    if (!aunt.Visible || auntOnRoof) return null;
                    if (held == SaiSin && !auntSafe) return "ผูกด้ายสายสิญจน์ให้ป้า";
                    return auntCalled ? "ถามป้าศรีเรื่องเอ๋" : "คุยกับป้าศรี";
                },
                () =>
                {
                    if (held == SaiSin && !auntSafe) { Protect(aunt, () => auntSafe = true); return; }
                    if (!auntCalled)
                    {
                        aunt.Say("ป้าศรี: จะขึ้นไปรดน้ำต้นไม้ให้ลูกหน่อย", 3f);
                        return;
                    }
                    int i = Mathf.Min(auntTalks++, dialogue.auntStory.Length - 1);
                    aunt.Say(dialogue.auntStory[i], 5.5f);
                    if (i == 0) Learn(Facts.DeathDate);
                    if (i == 1) Learn(Facts.BurnMethod);
                });

            // ---- ต้น ----
            Interact(() => child.transform.position, 2.2f,
                () => !child.Visible || childSafe ? null : held == SaiSin ? "ผูกด้ายสายสิญจน์ให้ต้น" : "คุยกับต้น",
                () =>
                {
                    if (held == SaiSin) { Protect(child, () => childSafe = true); return; }
                    child.Say("ต้น: พี่ ๆ ดูผมยิงประตูนะ!", 2.5f);
                });

            // ---- ลุงชม ----
            Interact(() => uncle.transform.position, 2.4f,
                () =>
                {
                    if (!uncle.Visible || uncleSafe) return null;
                    if (held == Pliers) return "ยื่นคีมให้ลุง";
                    if (held == SaiSin) return "ผูกด้ายสายสิญจน์ให้ลุง";
                    return "คุยกับลุงชม";
                },
                () =>
                {
                    if (held == SaiSin) { Protect(uncle, () => uncleSafe = true); return; }
                    if (held != Pliers)
                    {
                        uncle.Say("ลุงชม: ถอยไปไอ้หนู ไฟแรงสูง", 2.5f);
                        return;
                    }
                    held = null;
                    uncleSafe = true;
                    wireFixed = true;
                    uncle.Say("ลุงชม: เฮ้ย คีมกู! ...ไปยุ่งกับไอ้เพียรมาเหรอ? ชื่อจริงมันสมพงษ์ ค้างค่าไฟกูสามเดือนแล้ว", 6f);
                    Learn(Facts.RealName);
                    StartCoroutine(UncleFinishes());
                });
        }

        string ShrineLabel()
        {
            if (LoopState.Knows(Facts.Truth) && !accepted)
                return acceptConfirm && Time.unscaledTime < acceptConfirmUntil
                    ? "ยืนยัน — เผาด้ายดำ ยอมรับดวงของตัวเอง"
                    : "เผาด้ายดำที่ข้อมือด้วยไฟศาล (จะไม่ย้อนกลับอีก)";
            if (held == Doll && !dollRenamed && LoopState.Knows(Facts.RealName))
                return "เขียนชื่อ 'สมพงษ์' ทับชื่อภพบนหุ่น (ใช้ถ่านธูป)";
            if (!LoopState.Knows(Facts.SonName) || !LoopState.Knows(Facts.DeathDate))
                return "อ่านผ้าที่ผูกศาลพระภูมิ";
            if (!candleLit && !accepted)
                return "จุดเทียนจากศาลถือไป";
            return "อ่านผ้าที่ผูกศาลพระภูมิ";
        }

        void UseShrine()
        {
            if (LoopState.Knows(Facts.Truth) && !accepted)
            {
                if (!acceptConfirm || Time.unscaledTime > acceptConfirmUntil)
                {
                    acceptConfirm = true;
                    acceptConfirmUntil = Time.unscaledTime + 4f;
                    Notice("ถ้าเผาด้ายดำ ภพจะไม่ย้อนกลับมาอีกแล้ว — กด E อีกครั้งเพื่อยืนยัน", 4f);
                    return;
                }
                accepted = true;
                StartCoroutine(BurnThread());
                return;
            }
            if (held == Doll && !dollRenamed && LoopState.Knows(Facts.RealName))
            {
                dollRenamed = true;
                handDoll.transform.Find("Name Tag").GetComponent<Renderer>().sharedMaterial = Greybox.Mat(new Color(0.2f, 0.15f, 0.1f));
                Notice("ขีดชื่อ 'ภพ' ทิ้ง แล้วเขียน 'สมพงษ์' ด้วยถ่านธูป", 3f);
                return;
            }
            if (!LoopState.Knows(Facts.SonName) || !LoopState.Knows(Facts.DeathDate) || candleLit || accepted)
            {
                Notice("ผ้าแพร: \"อุทิศให้ ด.ช.เอ๋ ลูกรักของแม่ศรี — ถึงแก่กรรม ๒๓ ต.ค. ๖๘ เวลา ๑๘.๐๐ น.\"", 5f);
                LoopState.Learn(Facts.SonName);
                LoopState.Learn(Facts.DeathDate);
                return;
            }
            SetCandle(true);
            Notice("จุดเทียนจากศาลแล้ว — เดินช้า ๆ ไม่งั้นเทียนดับ", 3f);
        }

        IEnumerator UncleFinishes()
        {
            yield return new WaitForSeconds(5f);
            ladder.gameObject.SetActive(false);
            ladderUp = false;
            uncle.WalkTo(uncleEntry, timing.uncleWalkSpeed);
            yield return new WaitForSeconds(6f);
            uncle.Show(false);
        }

        void Protect(Npc npc, Action markSafe)
        {
            held = null;
            markSafe();
            // ด้ายพันแขนเรืองแสง = มองไกล ๆ ก็รู้ว่าใครถูกป้องกันแล้ว (หน้าตา = World/Templates/Sai Sin Band)
            Spawn(Names.BandTemplate, npc.transform);
            npc.Say($"{npc.displayName}: อ้าว... ขอบใจนะ", 2.5f);
        }

        // clone ของจาก World/Templates (ปิดไว้ใน scene ให้ปรับหน้าตาใน Editor ได้)
        GameObject Spawn(string templatePath, Transform parent, Vector3? worldPosition = null)
        {
            var go = Instantiate(Find(templatePath).gameObject, parent, false);
            go.SetActive(true);
            spawned.Add(go);
            if (worldPosition.HasValue) go.transform.position = worldPosition.Value;
            return go;
        }

        Light SpawnFlash(Transform parent, Vector3 localPosition, Color color)
        {
            var light = Spawn(Names.FlashTemplate, parent).GetComponent<Light>();
            light.transform.localPosition = localPosition;
            light.color = color;
            return light;
        }

        void Interact(Func<Vector3> position, float radius, Func<string> label, Action use, Func<bool> canUse = null, int priority = 0, bool onTable = false)
        {
            Action guarded = use;
            if (onTable)
                guarded = () =>
                {
                    if (FortuneSees(player.transform.position)) Caught();
                    else use();
                };
            interactions.Add(new Interaction { position = position, radius = radius, label = label, use = guarded, canUse = canUse ?? (() => true), priority = priority, onTable = onTable });
        }

        void UpdateInteraction(Keyboard kb)
        {
            focused = null;
            float best = float.MaxValue;
            int bestPriority = int.MinValue;
            foreach (var it in interactions)
            {
                if (it.label() == null) continue;
                float d = FlatDistance(player.transform.position, it.position());
                if (d > it.radius) continue;
                if (it.priority < bestPriority || (it.priority == bestPriority && d >= best)) continue;
                best = d;
                bestPriority = it.priority;
                focused = it;
            }
            if (focused != null && kb.eKey.wasPressedThisFrame && focused.canUse()) focused.use();
        }

        IEnumerator Later(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        // =====================================================================
        // World extras
        // =====================================================================
        void ApplyDoom()
        {
            // ยิ่งตายบ่อย หมอกยิ่งหนาและแดง — ราคาของการตายแบบ Re:Zero
            float doom = Mathf.Clamp01(LoopState.Corpses.Count / (float)Mathf.Max(1, mood.deathsForFullDoom));
            // ambient สูงพอให้เห็นทุกอย่างในซอย (แบบ Twelve Minutes) — ความมืดเอามาจากขอบจอ ไม่ใช่กลางจอ
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(mood.ambient, mood.ambientDoom, doom);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(mood.fogColor, mood.fogColorDoom, doom);
            RenderSettings.fogDensity = Mathf.Lerp(mood.fogDensity, mood.fogDensityDoom, doom);
            cam.backgroundColor = RenderSettings.fogColor;

            // ค่าฐานมาจาก HorrorPostFX.asset (แก้ใน Inspector ได้) — ที่นี่แค่ "บวกความหม่น" ตามจำนวนครั้งที่ตาย
            // อ่าน sharedProfile ก่อน แล้วค่อยแตะ .profile (สำเนา runtime ไม่ไปแก้ asset)
            var volume = world.GetComponentInChildren<Volume>();
            if (volume == null || volume.sharedProfile == null) return;
            var baseProfile = volume.sharedProfile;
            var profile = volume.profile;
            if (baseProfile.TryGet<Vignette>(out var baseVignette) && profile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.Override(baseVignette.intensity.value + mood.vignetteDoomAdd * doom);
                vignette.color.Override(Color.Lerp(baseVignette.color.value, mood.vignetteDoomColor, doom));
            }
            if (baseProfile.TryGet<ColorAdjustments>(out var baseGrade) && profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.saturation.Override(baseGrade.saturation.value + mood.saturationDoomAdd * doom);
                grade.postExposure.Override(baseGrade.postExposure.value);
            }
            if (baseProfile.TryGet<FilmGrain>(out var baseGrain) && profile.TryGet<FilmGrain>(out var grain))
                grain.intensity.Override(baseGrain.intensity.value + mood.grainDoomAdd * doom);
        }

        // ศพของ "ตัวเอง" จากลูปก่อน — ชี้มือไปทางสิ่งที่ผู้เล่นยังไม่รู้ = ระบบใบ้ที่ไม่มี UI
        void BuildCorpses()
        {
            Vector3 lookAt =
                !LoopState.Knows(Facts.SonName) || !LoopState.Knows(Facts.DeathDate) ? shrine
                : !LoopState.Knows(Facts.RealName) ? toolbox
                : !LoopState.Knows(Facts.DeathList) ? notebook.position
                : !LoopState.Knows(Facts.Truth) ? box.position
                : shrine;
            var group = new GameObject("Corpses").transform;
            group.SetParent(world, false);
            spawned.Add(group.gameObject);

            // หน้าตาศพ = World/Templates/Corpse ใน scene (แก้ได้ใน Editor) — โค้ดแค่ clone + วาง + ใส่ข้อความ
            foreach (var c in LoopState.Corpses)
            {
                var body = Spawn(Names.CorpseTemplate, group, c.position);
                body.name = $"ศพลูป {c.loop}";
                Vector3 dir = lookAt - c.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) body.transform.rotation = Quaternion.LookRotation(dir);

                var label = body.transform.Find("Label");
                label.Find("Name").GetComponent<Text>().text = $"ศพของเธอ (ครั้งที่ {c.loop})";
                label.Find("Line").GetComponent<Text>().text = c.cause;
                corpseLabels.Add((c.position, label.gameObject));
            }
        }

        void UpdateCorpseLabels()
        {
            foreach (var (position, label) in corpseLabels)
                label.SetActive(FlatDistance(player.transform.position, position) < senses.corpseLabelRadius);
        }

        void UpdateCamera()
        {
            // มุมเกือบตั้งฉาก (~75°) แบบ Twelve Minutes: ล็อกกลางซอยแกน x ให้เห็นกำแพงทั้งสองฝั่งเสมอ
            // ตามผู้เล่นแค่แกน z — ภาพไม่ส่าย อ่านตำแหน่ง NPC/ของได้ง่าย
            bool followingPlayer = camFocus == null || camFocus == player.transform;
            Vector3 focus = camFocus != null ? camFocus.position : player.transform.position;
            if (!followingPlayer) focus.x *= 0.5f;
            else if (cameraSettings.lockToAlleyCenter) focus.x = 0f;
            focus.y = 0f;
            if (followingPlayer) focus.z = Mathf.Clamp(focus.z, cameraSettings.minZ, cameraSettings.maxZ);
            Vector3 want = focus + cameraSettings.offset * camWide;
            float blend = 1f - Mathf.Exp(-cameraSettings.followSharpness * Time.unscaledDeltaTime);
            cam.transform.position = Vector3.Lerp(cam.transform.position, want, blend);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(focus - want), blend);
        }

        // =====================================================================
        // HUD
        // =====================================================================
        void Big(string text, float seconds)
        {
            big = text;
            bigUntil = Time.unscaledTime + seconds;
        }

        void Notice(string text, float seconds = 4f)
        {
            notice = text;
            noticeUntil = Time.unscaledTime + seconds;
        }

        void UpdateHud()
        {
            if (world == null) return;

            int secs = Mathf.Clamp((int)t, 0, (int)LoopLength);
            int minutes = 56 + secs / 60;
            clockText.text = $"{17 + minutes / 60}:{minutes % 60:00}:{secs % 60:00}";
            clockText.color = t > LoopLength - 30f ? new Color(1f, 0.35f, 0.3f) : Color.white;
            loopText.text = accepted ? "" : $"ครั้งที่ {LoopState.Loop}";

            string memory = "";
            if (LoopState.Known.Count > 0)
            {
                memory = "ความจำ:";
                foreach (var fact in LoopState.Known) memory += "\n• " + Facts.Describe(fact);
            }
            if (LoopState.Deaths.Count > 0)
            {
                memory += (memory.Length > 0 ? "\n\n" : "") + "บันทึกความตาย:";
                for (int i = Mathf.Max(0, LoopState.Deaths.Count - 5); i < LoopState.Deaths.Count; i++)
                {
                    var d = LoopState.Deaths[i];
                    memory += $"\n• ครั้งที่ {d.loop}: {d.who} — {d.cause}";
                }
            }
            memoryText.text = memory;

            string hand = held == Doll ? $"หุ่นฟาง (ชื่อ: {(dollRenamed ? "สมพงษ์" : "ภพ")})" : held;
            if (candleLit) hand = (hand != null ? hand + " + " : "") + "เทียนศาล (ห้ามวิ่ง)";
            heldText.text = hand != null ? $"ในมือ: {hand}" : "";

            if (focused != null && !locked)
            {
                bool ok = focused.canUse();
                string label = focused.label();
                // บอกแค่ข้อเท็จจริงว่า "เขามองอยู่" — ผู้เล่นตัดสินใจเองว่าจะเสี่ยงไหม
                if (focused.onTable && FortuneSees(player.transform.position)) label += "  (หมอดูมองอยู่)";
                promptText.text = ok ? $"[E] {label}" : label;
                promptText.color = ok ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
            else promptText.text = "";

            noticeText.text = notice != null && Time.unscaledTime < noticeUntil ? notice : "";

            bool showBig = big != null && Time.unscaledTime < bigUntil;
            bigPanel.SetActive(showBig);
            if (showBig) bigText.text = big;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // =====================================================================
        // Debug — ใช้โดย PrototypeTestHarness (Editor) เท่านั้น ไม่มีใน gameplay
        // =====================================================================
        public Camera DebugCamera => cam;
        public Canvas DebugHud => clockText.canvas;
        public bool DebugLocked => locked;
        public string DebugState =>
            $"t={t:0.0} loop={LoopState.Loop} locked={locked} held={held ?? "-"} candle={candleLit} accepted={accepted} " +
            $"known=[{string.Join(",", LoopState.Known)}] prompt='{promptText.text}' big='{(Time.unscaledTime < bigUntil ? big?.Replace("\n", " / ") : "")}'";

        // สร้าง World ใหม่จากโค้ดปัจจุบัน แล้วปิดของใน scene ระหว่างเทสต์ (Play mode จบ = กลับเหมือนเดิม)
        public void DebugUseFreshWorld()
        {
            StopAllCoroutines();
            RestoreSnapshot();
            world.gameObject.SetActive(false);
            worldTemplate = AlleyBuilder.Build();
            UseWorld(worldTemplate);
            BuildLoop();
        }

        public void DebugSkipTo(float time) => t = Mathf.Max(t, time);

        public void DebugTeleport(Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = position;
            cc.enabled = true;
        }

        public void DebugLearnAllClues()
        {
            foreach (var fact in new[] { Facts.SonName, Facts.DeathDate, Facts.DeathList, Facts.BurnMethod, Facts.RealName, Facts.DollInBox })
                LoopState.Learn(fact);
        }

        public void DebugGiveDoll(bool renamed, bool candle)
        {
            held = Doll;
            dollRenamed = renamed;
            handDoll.SetActive(true);
            SetCandle(candle);
        }

        public void DebugNextLoop()
        {
            LoopState.Loop++;
            BuildLoop();
        }

        public void DebugStartPeak() => StartPeak();

        public void DebugAccept()
        {
            accepted = true;
            StartCoroutine(BurnThread());
        }
    }
}
