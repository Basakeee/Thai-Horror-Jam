using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HorrorJam
{
    // สร้างซอยทั้งหมดเป็น GameObject จริง — ใช้ทั้งตอน Bake ลง scene (Editor) และ fallback ตอน Play ถ้ายังไม่ได้ Bake
    // ชื่อใน Names คือ "สัญญา" ระหว่าง builder กับ LoopGame: เปลี่ยนตำแหน่ง/หน้าตาได้อิสระ แต่ห้ามเปลี่ยนชื่อ
    public static class Names
    {
        public const string World = "World";
        public const string Camera = "Main Camera";
        public const string StreetLights = "Street Lights";

        public const string Player = "People/ภพ";
        public const string Fortune = "People/หมอดู";
        public const string Aunt = "People/ป้าศรี";
        public const string Child = "People/ต้น";
        public const string Uncle = "People/ลุงชม";
        public const string Ghost = "People/เอ๋";

        public const string PlayerThread = Player + "/Black Thread";
        public const string HandCandle = Player + "/Hand Candle";
        public const string HandDoll = Player + "/Hand Doll";
        public const string FortuneHair = Fortune + "/Hair";
        public const string FortuneBeard = Fortune + "/Beard";
        public const string FortunePhone = Fortune + "/Phone";
        public const string FortuneDoll = Fortune + "/Squeeze Doll";

        public const string Notebook = "Props/Fortune Stall/Notebook";
        public const string Bill = "Props/Fortune Stall/Bill";
        public const string Thread = "Props/Fortune Stall/Sai Sin";
        public const string Box = "Props/Fortune Stall/Box";
        public const string BoxLid = "Props/Fortune Stall/Box/Lid";
        public const string StallLamp = "Props/Fortune Stall/Lamp";
        public const string ShrineCandle = "Props/Shrine/Candle";
        public const string Bike = "Props/Delivery Bike";
        public const string Pot = "Props/Aunt House/Pot";
        public const string Ladder = "Props/Pole/Ladder";
        public const string Wire = "Props/Pole/Wire";
        public const string SnappedWire = "Props/Pole/Snapped Wire";
        public const string Sparks = "Props/Pole/Sparks";
        public const string Puddle = "Props/Puddle";
        public const string Pliers = "Props/Toolbox/Pliers";
        public const string Ball = "Props/Ball";
        public const string Truck = "Props/Truck";

        // Markers = จุดที่ตาราง NPC ใช้ — ลากย้ายใน Editor ได้ ตารางจะตามไปเอง
        public const string Markers = "Markers/";
        public const string TableSeat = "Table Seat";
        public const string ToiletDoor = "Toilet Door";
        public const string Shrine = "Shrine";
        public const string AuntStart = "Aunt Start";
        public const string AuntDoor = "Aunt Door";
        public const string AuntRoof = "Aunt Roof";
        public const string PotSpot = "Pot Spot";
        public const string ChildAreaA = "Child Area A";
        public const string ChildAreaB = "Child Area B";
        public const string KickSpot = "Kick Spot";
        public const string AlleyMouth = "Alley Mouth";
        public const string UncleEntry = "Uncle Entry";
        public const string UnclePole = "Uncle Pole";
        public const string Toolbox = "Toolbox";
        public const string StallFront = "Stall Front";
        public const string BikeStart = "Bike Start";
        public const string BikeEnd = "Bike End";
        public const string PeakView = "Peak View";

        public const string Hud = "HUD";

        // Templates = ของที่โค้ด clone ตอนเล่น (ปิดไว้ใน scene) — แก้หน้าตาได้ใน Editor
        public const string Templates = "Templates";
        public const string CorpseTemplate = "Templates/Corpse";
        public const string BandTemplate = "Templates/Sai Sin Band";
        public const string FlashTemplate = "Templates/Flash Light";
    }

    public static class AlleyBuilder
    {
        static Font BuiltinFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static GameObject Build()
        {
            var world = new GameObject(Names.World).transform;

            var camGo = new GameObject(Names.Camera) { tag = "MainCamera" };
            camGo.transform.SetParent(world, false);
            // มุมเกือบตั้งฉากแบบ Twelve Minutes — LoopGame คุมจริงจาก cameraSettings ใน Inspector
            camGo.transform.position = new Vector3(0f, 14f, 4.6f - 3.8f);
            camGo.transform.LookAt(new Vector3(0f, 0f, 4.6f));
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.fieldOfView = 45f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            AlleyDressing.PostFx(world);

            var moonGo = new GameObject("Moon");
            moonGo.transform.SetParent(world, false);
            moonGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.5f, 0.8f);
            moon.intensity = 0.75f;
            moon.shadows = LightShadows.Soft;

            BuildAlley(Group(world, "Alley"));
            BuildProps(Group(world, "Props"));
            BuildMarkers(Group(world, "Markers"));
            BuildPeople(Group(world, "People"));
            AlleyDressing.Dress(world);

            var lights = Group(world, Names.StreetLights);
            foreach (var p in new[] { new Vector3(-3.4f, 4.5f, 6f), new Vector3(3.4f, 4.5f, 12f), new Vector3(-3.4f, 4.5f, 19f), new Vector3(3.4f, 4.5f, 27f) })
            {
                var lamp = Greybox.PointLight(lights, p - new Vector3(Mathf.Sign(p.x) * 0.3f, 0.2f, 0f), new Color(1f, 0.72f, 0.45f), 22f, 12f);
                lamp.shadows = LightShadows.None;   // point light เงา = 6 shadow map/ดวง ล้น atlas — ให้ key light (spot) ทอดเงาแทน
            }

            // Key lights: วงไฟอุ่นส่องลงจุดที่มีเรื่องเกิด — ผู้เล่นมองจอแล้วรู้ทันทีว่าควรไปดูตรงไหน
            var keys = Group(world, "Key Lights");
            var warm = new Color(1f, 0.82f, 0.6f);
            Greybox.KeyLight(keys, "Key Stall", new Vector3(-2.5f, 0f, 4.2f), 6f, new Color(1f, 0.7f, 0.45f), 70f, 80f, castShadows: true);
            Greybox.KeyLight(keys, "Key Shrine", new Vector3(2.6f, 0f, 9f), 6f, new Color(1f, 0.6f, 0.5f), 50f, 65f);
            Greybox.KeyLight(keys, "Key Aunt House", new Vector3(2.2f, 0f, 14f), 6f, warm, 55f, 75f, castShadows: true);
            Greybox.KeyLight(keys, "Key Play Area", new Vector3(0f, 0f, 18.5f), 7f, warm, 65f, 90f, castShadows: true);
            Greybox.KeyLight(keys, "Key Pole", new Vector3(-1.2f, 0f, 23f), 7f, new Color(0.8f, 0.9f, 1f), 60f, 80f, castShadows: true);
            Greybox.KeyLight(keys, "Key Toolbox", new Vector3(2f, 0f, 28f), 6f, warm, 50f, 75f);
            Greybox.KeyLight(keys, "Key Alley Mouth", new Vector3(0f, 0f, 33.5f), 7f, new Color(1f, 0.75f, 0.5f), 60f, 90f);
            Greybox.KeyLight(keys, "Key Entrance", new Vector3(0f, 0f, 1.5f), 6f, warm, 45f, 80f);

            BuildHud(world);
            BuildTemplates(world);
            return world.gameObject;
        }

        // ของที่เกิดระหว่างเล่น: ศพจากลูปก่อน, ด้ายสายสิญจน์ที่แขน NPC, แสงวาบตอนไฟดูด/ไฟไหม้
        public static void BuildTemplates(Transform world)
        {
            var t = Group(world, Names.Templates);

            // ศพ: ร่างซีดของภพ ยกแขนขวาชี้ (LoopGame หมุนให้ชี้ไปทาง clue)
            var ghostly = new Color(0.45f, 0.5f, 0.56f);
            var corpse = Figure(t, "Corpse", Vector3.zero, ghostly, new Color(0.25f, 0.28f, 0.32f), new Color(0.6f, 0.63f, 0.66f), new Color(0.15f, 0.16f, 0.18f));
            var arm = corpse.transform.Find("Arm R");
            arm.localPosition = new Vector3(0.38f, 0.35f, 0.3f);
            arm.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var label = WorldLabel(corpse.transform, 1.4f);
            label.line.fontSize = 26;
            label.line.color = new Color(0.75f, 0.8f, 0.9f);

            // ตำแหน่ง local นี้ = ตำแหน่งบนแขนขวาของ NPC ที่ถูกผูก
            Greybox.GlowBox(t, "Sai Sin Band", new Vector3(0.38f, 0.05f, 0f), new Vector3(0.22f, 0.07f, 0.22f), new Color(0.95f, 0.95f, 0.85f), 1.5f);

            // intensity = ความสว่างสูงสุดตอนกะพริบ, สีถูกตั้งตามสถานการณ์ (ฟ้า = ไฟดูด, ส้ม = ไฟไหม้)
            Greybox.PointLight(t, Vector3.zero, Color.white, 14f, 5f).name = "Flash Light";

            t.gameObject.SetActive(false);
        }

        static void BuildAlley(Transform t)
        {
            var ground = Greybox.Box(t, "Ground", new Vector3(0f, -0.05f, 16f), new Vector3(9f, 0.1f, 38f), Color.black);
            ground.GetComponent<Renderer>().sharedMaterial = Greybox.Mat(new Color(0.36f, 0.34f, 0.32f), 0.35f);   // ถนนเปียกหลังฝน
            Greybox.Box(t, "Road", new Vector3(0f, -0.04f, 34.5f), new Vector3(30f, 0.1f, 4f), new Color(0.1f, 0.1f, 0.11f), false);
            Greybox.Box(t, "Road Line", new Vector3(0f, 0.02f, 34.5f), new Vector3(30f, 0.01f, 0.15f), new Color(0.8f, 0.7f, 0.2f), false);
            Greybox.Box(t, "End South", new Vector3(0f, 3.5f, -1.5f), new Vector3(9f, 7f, 1f), new Color(0.2f, 0.19f, 0.18f));
            // ปากซอยฝั่งเหนือเปิดออกถนน แต่มีกำแพงล่องหนกันผู้เล่นเดินออก
            var mouth = new GameObject("Alley Mouth Blocker");
            mouth.transform.SetParent(t, false);
            mouth.transform.localPosition = new Vector3(0f, 1.5f, 32f);
            mouth.AddComponent<BoxCollider>().size = new Vector3(9f, 3f, 0.5f);

            Color[] shop = { new(0.5f, 0.46f, 0.41f), new(0.42f, 0.47f, 0.45f), new(0.52f, 0.44f, 0.4f) };
            for (int i = 0; i < 8; i++)
            {
                float z = i * 4f + 1f;
                Greybox.Box(t, $"Shop L{i}", new Vector3(-4.5f, 3.5f, z), new Vector3(1f, 7f, 4f), shop[i % 3]);
                Greybox.Box(t, $"Shop R{i}", new Vector3(4.5f, 3.5f, z), new Vector3(1f, 7f, 4f), shop[(i + 1) % 3]);
            }
        }

        static void BuildProps(Transform t)
        {
            // แผงหมอดู: ชิดกำแพงซ้าย หมอดูนั่งหลังโต๊ะหันหน้าออกซอย
            var stall = Group(t, "Fortune Stall");
            Greybox.Box(stall, "Table", new Vector3(-2.9f, 0.4f, 4f), new Vector3(0.9f, 0.8f, 1.9f), new Color(0.45f, 0.1f, 0.1f));
            // ของบนโต๊ะเรียงตาม z ห่างกัน — ผู้เล่นเลือกชิ้นด้วยการเดินขึ้นลง
            Greybox.Box(stall, "Notebook", new Vector3(-2.9f, 0.83f, 3.3f), new Vector3(0.45f, 0.06f, 0.35f), new Color(0.08f, 0.08f, 0.08f), false);
            Greybox.Box(stall, "Bill", new Vector3(-2.85f, 0.82f, 3.9f), new Vector3(0.28f, 0.01f, 0.36f), new Color(0.92f, 0.92f, 0.88f), false);
            Greybox.Box(stall, "Sai Sin", new Vector3(-2.9f, 0.83f, 4.45f), new Vector3(0.3f, 0.05f, 0.3f), new Color(0.92f, 0.9f, 0.82f), false);
            // กล่องรหัสโผล่จากใต้โต๊ะ มีหุ่นฟางข้างใน
            var box = Greybox.Box(stall, "Box", new Vector3(-2.3f, 0.18f, 4.75f), new Vector3(0.45f, 0.36f, 0.4f), new Color(0.3f, 0.2f, 0.12f));
            var lid = new GameObject("Lid").transform;
            lid.SetParent(box.transform, false);
            lid.localPosition = new Vector3(0f, 0.5f, -0.5f);   // pivot ที่บานพับ
            Greybox.Box(lid, "Board", new Vector3(0f, 0.04f, 0.5f), new Vector3(1.05f, 0.12f, 1.05f), new Color(0.25f, 0.15f, 0.08f), false);
            Greybox.GlowBox(box.transform, "Code Lock", new Vector3(0.52f, 0.1f, 0f), new Vector3(0.05f, 0.3f, 0.35f), new Color(0.8f, 0.7f, 0.3f), 0.8f);
            StrawDoll(box.transform, "Doll", new Vector3(0f, 0.2f, 0f), 1.4f);
            Greybox.Shape(PrimitiveType.Cylinder, stall, "Umbrella Pole", new Vector3(-3.7f, 1.3f, 4f), new Vector3(0.06f, 1.3f, 0.06f), new Color(0.3f, 0.3f, 0.3f));
            Greybox.ShadowOnly(Greybox.Shape(PrimitiveType.Cylinder, stall, "Umbrella", new Vector3(-3.4f, 2.6f, 4f), new Vector3(2.4f, 0.05f, 2.4f), new Color(0.55f, 0.08f, 0.08f)));
            var lamp = Greybox.Shape(PrimitiveType.Sphere, stall, "Lamp", new Vector3(-2.9f, 2.2f, 4f), Vector3.one * 0.22f, new Color(1f, 0.85f, 0.5f));
            Greybox.PointLight(lamp.transform, Vector3.zero, new Color(1f, 0.55f, 0.25f), 2.5f, 4f);

            Greybox.Box(t, "Toilet Door", new Vector3(-3.95f, 1.1f, 8f), new Vector3(0.1f, 2.2f, 1.1f), new Color(0.1f, 0.12f, 0.2f), false);

            var shrine = Group(t, "Shrine");
            Greybox.Shape(PrimitiveType.Cylinder, shrine, "Post", new Vector3(3.2f, 0.6f, 9f), new Vector3(0.15f, 0.6f, 0.15f), new Color(0.75f, 0.7f, 0.6f), true);
            Greybox.Box(shrine, "House", new Vector3(3.2f, 1.45f, 9f), new Vector3(0.7f, 0.5f, 0.6f), new Color(0.85f, 0.65f, 0.15f), false);
            Greybox.Box(shrine, "Cloth", new Vector3(3.1f, 0.9f, 9f), new Vector3(0.05f, 0.3f, 0.35f), new Color(0.9f, 0.3f, 0.6f), false);
            Greybox.PointLight(shrine, new Vector3(2.7f, 1.6f, 9f), new Color(1f, 0.3f, 0.3f), 1.5f, 3f);
            var candle = Greybox.GlowBox(shrine, "Candle", new Vector3(3.05f, 1.3f, 9.32f), new Vector3(0.05f, 0.18f, 0.05f), new Color(1f, 0.6f, 0.25f), 4f);
            Greybox.PointLight(candle.transform, Vector3.up * 0.8f, new Color(1f, 0.55f, 0.2f), 1.2f, 2f);

            var house = Group(t, "Aunt House");
            Greybox.Box(house, "Door", new Vector3(3.95f, 1.1f, 14f), new Vector3(0.1f, 2.2f, 1.2f), new Color(0.35f, 0.15f, 0.1f), false);
            Greybox.Box(house, "Balcony", new Vector3(3.4f, 5.8f, 14f), new Vector3(1.4f, 0.2f, 2.2f), new Color(0.4f, 0.38f, 0.35f), false);
            Greybox.Box(house, "Plants", new Vector3(3.6f, 6.15f, 14.6f), new Vector3(0.3f, 0.5f, 0.6f), new Color(0.2f, 0.45f, 0.2f), false);
            Greybox.Box(house, "Pot", new Vector3(2.85f, 6.15f, 14f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.55f, 0.3f, 0.2f), false);

            var pole = Group(t, "Pole");
            Greybox.Shape(PrimitiveType.Cylinder, pole, "Post", new Vector3(-3.3f, 4f, 24f), new Vector3(0.25f, 4f, 0.25f), new Color(0.5f, 0.5f, 0.5f), true);
            Greybox.ShadowOnly(Greybox.Box(pole, "Wire", new Vector3(0.35f, 7.3f, 24f), new Vector3(7.3f, 0.04f, 0.04f), Color.black, false));
            var snapped = Greybox.Box(pole, "Snapped Wire", new Vector3(-1.6f, 3.7f, 23.2f), new Vector3(0.05f, 7.4f, 0.05f), Color.black, false);
            snapped.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            snapped.SetActive(false);
            var sparks = Greybox.Box(pole, "Sparks", new Vector3(-0.8f, 0.1f, 23f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.6f, 0.9f, 1f), false);
            Greybox.PointLight(sparks.transform, Vector3.zero, new Color(0.5f, 0.85f, 1f), 8f, 6f);
            sparks.SetActive(false);
            // บันได: pivot ที่ฐาน ล้มหมุนรอบพื้นได้
            var ladder = new GameObject("Ladder");
            ladder.transform.SetParent(pole, false);
            ladder.transform.localPosition = new Vector3(-2.6f, 0f, 23.4f);
            ladder.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Greybox.Box(ladder.transform, "Rails", new Vector3(0f, 2.4f, 0f), new Vector3(0.5f, 4.8f, 0.08f), new Color(0.75f, 0.6f, 0.2f), false);
            ladder.SetActive(false);

            Greybox.Shape(PrimitiveType.Cylinder, t, "Puddle", new Vector3(0f, 0.01f, 22f), new Vector3(5.2f, 0.005f, 5.2f), Color.black)
                .GetComponent<Renderer>().sharedMaterial = Greybox.Mat(new Color(0.22f, 0.3f, 0.4f), 0.7f);

            var toolbox = Greybox.Box(t, "Toolbox", new Vector3(2.8f, 0.25f, 28f), new Vector3(0.7f, 0.5f, 0.4f), new Color(0.75f, 0.12f, 0.1f));
            var pliers = Greybox.Box(toolbox.transform, "Pliers", Vector3.zero, Vector3.one, new Color(0.2f, 0.4f, 0.9f), false);
            pliers.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            pliers.transform.localScale = new Vector3(0.17f, 0.12f, 0.87f);

            Greybox.Shape(PrimitiveType.Sphere, t, "Ball", new Vector3(0f, 0.18f, 17.5f), Vector3.one * 0.35f, Color.white);

            // รถกระบะ — ซ่อนไว้นอกจอ ใช้ในฉากตายของต้น
            var truck = Group(t, "Truck");
            truck.localPosition = new Vector3(-14f, 0f, 34.5f);
            Greybox.Box(truck, "Bed", new Vector3(-0.6f, 0.75f, 0f), new Vector3(2.8f, 0.9f, 1.9f), new Color(0.75f, 0.75f, 0.72f), false);
            Greybox.Box(truck, "Cab", new Vector3(1.5f, 1.05f, 0f), new Vector3(1.4f, 1.5f, 1.9f), new Color(0.8f, 0.8f, 0.78f), false);
            var head = Greybox.PointLight(truck, new Vector3(2.6f, 0.8f, 0f), new Color(1f, 0.95f, 0.8f), 10f, 10f);
            head.name = "Headlight";
            truck.gameObject.SetActive(false);

            // มอเตอร์ไซค์ส่งของ — วิ่งลงมาทั้งซอยตอน 17:59:10 (หันหน้า -z)
            var bike = Group(t, "Delivery Bike");
            bike.localPosition = new Vector3(0.3f, 0f, 33f);
            bike.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Greybox.Box(bike, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 0.45f, 1.4f), new Color(0.15f, 0.15f, 0.18f), false);
            Greybox.Box(bike, "Delivery Box", new Vector3(0f, 1.25f, -0.55f), new Vector3(0.55f, 0.5f, 0.5f), new Color(0.9f, 0.45f, 0.1f), false);
            foreach (float wz in new[] { -0.6f, 0.6f })
                Greybox.Shape(PrimitiveType.Cylinder, bike, "Wheel", new Vector3(0f, 0.3f, wz), new Vector3(0.6f, 0.06f, 0.6f), Color.black)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Greybox.GlowBox(bike, "Headlamp", new Vector3(0f, 0.85f, 0.72f), new Vector3(0.2f, 0.15f, 0.05f), new Color(1f, 0.95f, 0.8f), 6f);
            var beam = new GameObject("Beam").AddComponent<Light>();
            beam.transform.SetParent(bike, false);
            beam.transform.localPosition = new Vector3(0f, 1f, 0.8f);
            beam.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            beam.type = LightType.Spot;
            beam.spotAngle = 55f;
            beam.range = 16f;
            beam.intensity = 25f;
            beam.color = new Color(1f, 0.95f, 0.85f);
            var rider = Figure(bike, "Rider", new Vector3(0f, 1.25f, -0.1f), new Color(0.9f, 0.45f, 0.1f), new Color(0.15f, 0.15f, 0.2f), Skin, HairBlack);
            Greybox.Shape(PrimitiveType.Sphere, rider.transform, "Helmet", new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.42f, 0.5f), new Color(0.9f, 0.45f, 0.1f));
            bike.gameObject.SetActive(false);
        }

        // หุ่นฟางคุณไสย: ตัวฟาง + หัว + ด้ายดำพันเอว + ป้ายชื่อกระดาษ
        public static GameObject StrawDoll(Transform parent, string name, Vector3 position, float scale)
        {
            var doll = Group(parent, name);
            doll.localPosition = position;
            doll.localScale = Vector3.one * scale;
            var straw = new Color(0.75f, 0.62f, 0.3f);
            Greybox.Shape(PrimitiveType.Capsule, doll, "Straw Body", new Vector3(0f, 0f, 0f), new Vector3(0.12f, 0.12f, 0.1f), straw);
            Greybox.Shape(PrimitiveType.Sphere, doll, "Straw Head", new Vector3(0f, 0.17f, 0f), Vector3.one * 0.11f, straw);
            Greybox.Box(doll, "Straw Arms", new Vector3(0f, 0.05f, 0f), new Vector3(0.26f, 0.03f, 0.03f), straw, false);
            Greybox.Box(doll, "Black Thread", new Vector3(0f, -0.02f, 0f), new Vector3(0.13f, 0.025f, 0.11f), new Color(0.02f, 0.02f, 0.02f), false);
            Greybox.Box(doll, "Name Tag", new Vector3(0f, 0.04f, 0.055f), new Vector3(0.07f, 0.09f, 0.005f), new Color(0.9f, 0.88f, 0.8f), false);
            return doll.gameObject;
        }

        static void BuildMarkers(Transform t)
        {
            Marker(t, Names.TableSeat, new Vector3(-3.55f, 1f, 4f));
            Marker(t, Names.ToiletDoor, new Vector3(-3.4f, 1f, 8f));
            Marker(t, Names.Shrine, new Vector3(3.2f, 0f, 9f));
            Marker(t, Names.AuntStart, new Vector3(2f, 1f, 11f));
            Marker(t, Names.AuntDoor, new Vector3(3.3f, 1f, 14f));
            Marker(t, Names.AuntRoof, new Vector3(3.4f, 6.9f, 14f));
            Marker(t, Names.PotSpot, new Vector3(2.3f, 0f, 14f));
            Marker(t, Names.ChildAreaA, new Vector3(-2f, 0.75f, 15f));
            Marker(t, Names.ChildAreaB, new Vector3(2f, 0.75f, 20.5f));
            Marker(t, Names.KickSpot, new Vector3(-0.6f, 0.75f, 18.8f));
            Marker(t, Names.AlleyMouth, new Vector3(0f, 0.75f, 34f));
            Marker(t, Names.UncleEntry, new Vector3(-2.4f, 1f, 31f));
            Marker(t, Names.UnclePole, new Vector3(-2.1f, 1f, 22.9f));
            Marker(t, Names.Toolbox, new Vector3(2.8f, 0f, 28f));
            Marker(t, Names.StallFront, new Vector3(-1.85f, 1f, 4f));
            Marker(t, Names.BikeStart, new Vector3(0.3f, 0f, 33f));
            Marker(t, Names.BikeEnd, new Vector3(0.3f, 0f, -0.5f));
            Marker(t, Names.PeakView, new Vector3(0f, 0f, 19f));
        }

        static readonly Color Skin = new(0.78f, 0.6f, 0.45f);
        static readonly Color HairBlack = new(0.06f, 0.05f, 0.05f);

        static void BuildPeople(Transform t)
        {
            // ภพ: เสื้อนักศึกษาขาว กางเกงดำ — สว่างสุดในฉาก ผู้เล่นหาตัวเองเจอเสมอ
            var player = Figure(t, "ภพ", new Vector3(-1.3f, 1f, 4f), new Color(0.9f, 0.9f, 0.88f), new Color(0.1f, 0.1f, 0.12f), Skin, HairBlack);
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.9f;
            player.AddComponent<PlayerController>();
            // ด้ายดำที่หมอดูผูกให้ "เสริมดวง" — ของสำคัญที่สุดในเกม อยู่ใต้จมูกผู้เล่นตั้งแต่วินาทีแรก
            Greybox.Box(player.transform, "Black Thread", new Vector3(-0.38f, -0.15f, 0f), new Vector3(0.21f, 0.06f, 0.21f), new Color(0.02f, 0.02f, 0.02f), false);
            var handCandle = Greybox.GlowBox(player.transform, "Hand Candle", new Vector3(0.38f, -0.2f, 0.22f), new Vector3(0.05f, 0.2f, 0.05f), new Color(1f, 0.6f, 0.25f), 4f);
            Greybox.PointLight(handCandle.transform, Vector3.up * 0.8f, new Color(1f, 0.55f, 0.2f), 2f, 3.5f);
            handCandle.SetActive(false);
            StrawDoll(player.transform, "Hand Doll", new Vector3(0.45f, -0.25f, 0.25f), 1f).SetActive(false);

            // หมอดูผมขาวตอนเริ่มเกม — ยิ่งภพตาย ผมยิ่งดำ (ดูดดวงไปต่ออายุ)
            var oldHair = new Color(0.85f, 0.85f, 0.85f);
            var fortune = MakeNpc(Figure(t, "หมอดู", new Vector3(-3.55f, 1f, 4f), new Color(0.35f, 0.12f, 0.4f), new Color(0.2f, 0.1f, 0.2f), new Color(0.7f, 0.55f, 0.42f), oldHair));
            fortune.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Greybox.Box(fortune.transform, "Sash", new Vector3(0f, 0.2f, 0f), new Vector3(0.64f, 0.12f, 0.48f), new Color(0.75f, 0.1f, 0.1f), false);
            Greybox.Shape(PrimitiveType.Sphere, fortune.transform, "Beard", new Vector3(0f, 0.6f, 0.15f), new Vector3(0.25f, 0.25f, 0.15f), oldHair);
            Greybox.Box(fortune.transform, "Phone", new Vector3(0.24f, 0.7f, 0.08f), new Vector3(0.05f, 0.18f, 0.1f), new Color(0.05f, 0.05f, 0.05f), false).SetActive(false);
            StrawDoll(fortune.transform, "Squeeze Doll", new Vector3(0f, 0.15f, 0.4f), 1.3f).SetActive(false);

            // วิญญาณเอ๋ — โผล่เฉพาะฉากจบ
            var ghost = MakeNpc(Figure(t, "เอ๋", new Vector3(0f, 1f, 30f), Color.white, Color.white, Color.white, Color.white));
            foreach (var r in ghost.GetComponentsInChildren<MeshRenderer>())
                if (!r.name.StartsWith("Eye")) r.sharedMaterial = Greybox.Glow(new Color(0.55f, 0.75f, 0.85f), 1.2f);
            ghost.SetActive(false);

            var aunt = MakeNpc(Figure(t, "ป้าศรี", new Vector3(2f, 1f, 11f), new Color(0.7f, 0.4f, 0.5f), new Color(0.3f, 0.2f, 0.35f), Skin, new Color(0.55f, 0.55f, 0.55f)));
            Greybox.Shape(PrimitiveType.Sphere, aunt.transform, "Bun", new Vector3(0f, 0.92f, -0.17f), Vector3.one * 0.2f, new Color(0.55f, 0.55f, 0.55f));
            Greybox.Box(aunt.transform, "Apron", new Vector3(0f, -0.05f, 0.21f), new Vector3(0.45f, 0.6f, 0.04f), new Color(0.85f, 0.85f, 0.8f), false);

            var child = MakeNpc(Figure(t, "ต้น", new Vector3(0f, 0.75f, 17f), new Color(0.25f, 0.5f, 0.85f), new Color(0.15f, 0.2f, 0.35f), Skin, HairBlack));
            child.transform.localScale = Vector3.one * 0.75f;

            var uncle = MakeNpc(Figure(t, "ลุงชม", new Vector3(-2.4f, 1f, 31f), new Color(0.3f, 0.35f, 0.3f), new Color(0.25f, 0.22f, 0.18f), new Color(0.65f, 0.48f, 0.35f), HairBlack));
            Greybox.Shape(PrimitiveType.Sphere, uncle.transform, "Helmet", new Vector3(0f, 0.86f, 0f), new Vector3(0.5f, 0.3f, 0.5f), new Color(0.95f, 0.75f, 0.1f));
            Greybox.Box(uncle.transform, "Tool Belt", new Vector3(0f, -0.08f, 0f), new Vector3(0.64f, 0.1f, 0.48f), new Color(0.35f, 0.22f, 0.1f), false);
            uncle.SetActive(false);
        }

        // คน low-poly จาก primitive: root อยู่กลางลำตัว (y=1) เท้าแตะพื้น — โค้ดทั้งหมดอ้าง root ไม่อ้างชิ้นส่วน
        public static GameObject Figure(Transform parent, string name, Vector3 position, Color shirt, Color pants, Color skin, Color hair)
        {
            var root = Group(parent, name);
            root.localPosition = position;
            Greybox.Shape(PrimitiveType.Capsule, root, "Legs", new Vector3(0f, -0.5f, 0f), new Vector3(0.48f, 0.45f, 0.4f), pants);
            Greybox.Shape(PrimitiveType.Capsule, root, "Body", new Vector3(0f, 0.12f, 0f), new Vector3(0.62f, 0.45f, 0.46f), shirt);
            Greybox.Shape(PrimitiveType.Capsule, root, "Arm L", new Vector3(-0.38f, 0.1f, 0f), new Vector3(0.17f, 0.38f, 0.17f), shirt);
            Greybox.Shape(PrimitiveType.Capsule, root, "Arm R", new Vector3(0.38f, 0.1f, 0f), new Vector3(0.17f, 0.38f, 0.17f), shirt);
            Greybox.Shape(PrimitiveType.Sphere, root, "Head", new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.44f, 0.42f), skin);
            Greybox.Shape(PrimitiveType.Sphere, root, "Hair", new Vector3(0f, 0.8f, -0.04f), new Vector3(0.45f, 0.34f, 0.44f), hair);
            // ตา = รู้ว่าหันไปทางไหน (สำคัญตอนอ่าน schedule ของ NPC)
            Greybox.Box(root, "Eye L", new Vector3(-0.08f, 0.74f, 0.19f), new Vector3(0.06f, 0.06f, 0.04f), new Color(0.02f, 0.02f, 0.02f), false);
            Greybox.Box(root, "Eye R", new Vector3(0.08f, 0.74f, 0.19f), new Vector3(0.06f, 0.06f, 0.04f), new Color(0.02f, 0.02f, 0.02f), false);
            return root.gameObject;
        }

        static GameObject MakeNpc(GameObject figure)
        {
            var label = WorldLabel(figure.transform, 1.5f);
            var c = figure.AddComponent<Npc>();
            c.displayName = figure.name;
            c.nameText = label.name;
            c.nameText.text = figure.name;
            c.lineText = label.line;
            return figure;
        }

        // ป้ายลอยเหนือหัว: ชื่อ (ล่าง) + คำพูด (บน)
        public static (Text name, Text line) WorldLabel(Transform parent, float height)
        {
            var canvasGo = new GameObject("Label", typeof(RectTransform), typeof(Canvas), typeof(FaceCamera));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.localPosition = new Vector3(0f, height, 0f);
            rt.sizeDelta = new Vector2(500f, 160f);
            var face = canvasGo.GetComponent<FaceCamera>();
            face.useWorldOffset = true;
            face.worldOffset = new Vector3(0f, height + 0.4f, 0.9f);
            rt.localScale = Vector3.one * 0.01f;

            var name = UiText(rt, "Name", new Vector2(0f, 0f), new Vector2(1f, 0.3f), 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f));
            var line = UiText(rt, "Line", new Vector2(0f, 0.3f), new Vector2(1f, 1f), 34, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.75f));
            return (name, line);
        }

        static void BuildHud(Transform world)
        {
            var canvasGo = new GameObject(Names.Hud, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(world, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            Anchor(UiText(root, "Clock", Vector2.zero, Vector2.zero, 48, TextAnchor.UpperCenter, Color.white), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(600f, 70f))
                .gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            Anchor(UiText(root, "Loop", Vector2.zero, Vector2.zero, 22, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.5f)), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 34f));
            Anchor(UiText(root, "Memory", Vector2.zero, Vector2.zero, 25, TextAnchor.UpperLeft, new Color(0.92f, 0.95f, 1f)), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(820f, 520f))
                .gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            Anchor(UiText(root, "Held", Vector2.zero, Vector2.zero, 26, TextAnchor.LowerLeft, new Color(0.9f, 0.85f, 0.6f)), new Vector2(0f, 0f), new Vector2(24f, 60f), new Vector2(600f, 40f));
            Anchor(UiText(root, "Controls", Vector2.zero, Vector2.zero, 18, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.45f)), new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(900f, 30f))
                .text = "WASD เดิน · Shift วิ่ง · E ใช้ · Tab เร่งเวลา · F5 ล้างความจำ";
            Anchor(UiText(root, "Prompt", Vector2.zero, Vector2.zero, 30, TextAnchor.MiddleCenter, Color.white), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1200f, 44f));
            Anchor(UiText(root, "Notice", Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleCenter, new Color(0.75f, 0.9f, 1f)), new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(1300f, 90f));

            var panel = new GameObject("Big Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0f, 0.28f);
            prt.anchorMax = new Vector2(1f, 0.72f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            UiText(prt, "Big Text", Vector2.zero, Vector2.one, 44, TextAnchor.MiddleCenter, Color.white);
            panel.SetActive(false);
        }

        internal static Text UiText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = BuiltinFont;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            go.GetComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static Text Anchor(Text text, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return text;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Marker(Transform parent, string name, Vector3 position) => Group(parent, name).position = position;
    }
}
