using HorrorJam;
using UnityEngine;

namespace TapeLoop
{
    // สร้างบ้านทาวน์โฮมเป็น object จริง (Tools/Tape Loop/Bake Scene) — แต่ละห้อง = ภาพกล้องเพดาน 1 ตัว
    // ห้องวางห่างกันตามแกน x ให้กล้อง top-down เห็นทีละห้อง
    //
    // กฎตั้งชื่อ prop (เพิ่มของเองใน Editor ได้):
    //   "flag:fire"     โชว์เมื่อ flag fire ถูกตั้ง        "!flag:crack"  โชว์เมื่อ crack ไม่ถูกตั้ง
    //   "item:key"      โชว์เมื่อภพถือ key                 "!item:key"    โชว์เมื่อภพยังไม่ได้หยิบ key
    public static class HouseBuilder
    {
        public const string House = "House";
        public const string Rooms = "Rooms";
        public const string Actors = "Actors";
        public const string Camera = "Tape Camera";
        public const float RoomSpacing = 16f;

        static readonly Color Floor = new(0.62f, 0.6f, 0.57f);
        static readonly Color Wall = new(0.85f, 0.85f, 0.83f);
        static readonly Color Wood = new(0.45f, 0.33f, 0.24f);
        static readonly Color Dark = new(0.08f, 0.08f, 0.09f);
        static readonly Color White = new(0.92f, 0.92f, 0.9f);

        public static GameObject Build()
        {
            var house = new GameObject(House).transform;
            var rooms = Group(house, Rooms);
            int i = 0;
            Transform Room(string cam, string label) => NewRoom(rooms, cam, label, i++);

            // ---- ชั้น 3 ----
            var r01 = Room("CAM 01", "ห้องนอนภพ");
            Box(r01, "Bed", new Vector3(-2.5f, 0.25f, 1.2f), new Vector3(1.6f, 0.5f, 2.2f), new Color(0.3f, 0.35f, 0.5f));
            Box(r01, "Desk", new Vector3(2.6f, 0.4f, 2.3f), new Vector3(1.6f, 0.8f, 0.7f), Wood);
            Box(r01, "Laptop", new Vector3(2.6f, 0.82f, 2.3f), new Vector3(0.5f, 0.03f, 0.35f), Dark);

            var r02 = Room("CAM 02", "ห้องเก็บของ");
            var shelfUnit = new GameObject("!flag:shelfFallen").transform;
            shelfUnit.SetParent(r02, false);
            Box(shelfUnit, "Shelf", new Vector3(2.2f, 1.1f, 2.5f), new Vector3(2.6f, 2.2f, 0.6f), Wood);
            for (int b = 0; b < 3; b++)
            {
                var box = Box(shelfUnit, "flag:shelfTilted", new Vector3(1.4f + b * 0.8f, 2.4f, 2.5f), new Vector3(0.6f, 0.5f, 0.5f), new Color(0.6f, 0.5f, 0.35f));
                box.transform.localRotation = Quaternion.Euler(0f, 0f, 12f + b * 5f);
            }
            Box(shelfUnit, "flag:shelfSecured", new Vector3(2.2f, 1.6f, 2.15f), new Vector3(2.7f, 0.08f, 0.05f), new Color(0.95f, 0.8f, 0.1f));
            Extinguisher(r02, "!item:extinguisher", new Vector3(0f, 0f, 2.6f), 1f, wallMounted: true);
            Box(r02, "!item:tablet", new Vector3(1.2f, 1.25f, 2.3f), new Vector3(0.4f, 0.03f, 0.3f), Dark);
            Box(r02, "Boxes", new Vector3(-2.6f, 0.4f, -2f), new Vector3(1.4f, 0.8f, 1f), new Color(0.6f, 0.5f, 0.35f));

            // ---- ชั้น 2 ----
            var r03 = Room("CAM 03", "ห้องนอนมิ้นท์");
            Box(r03, "Bed", new Vector3(-2.5f, 0.25f, 1.2f), new Vector3(1.4f, 0.5f, 2f), new Color(0.9f, 0.6f, 0.7f));
            Shape(PrimitiveType.Sphere, r03, "Rabbit Doll", new Vector3(-2.2f, 0.6f, 2f), Vector3.one * 0.3f, White);
            Box(r03, "Scooter", new Vector3(1.6f, 0.3f, -1.3f), new Vector3(0.3f, 0.6f, 1.1f), new Color(0.2f, 0.6f, 0.9f));
            Box(r03, "Charger", new Vector3(2.2f, 0.1f, -1.9f), new Vector3(0.2f, 0.1f, 0.2f), Dark);
            for (int s = 0; s < 3; s++)
                Shape(PrimitiveType.Sphere, r03, "flag:smoke", new Vector3(1.4f + s * 0.4f, 1.2f + s * 0.3f, -1.1f), Vector3.one * (0.6f + s * 0.2f), new Color(0.4f, 0.4f, 0.4f));
            Glow(r03, "flag:fire", new Vector3(1.6f, 0.6f, -1.3f), new Vector3(1.2f, 1.2f, 1.6f), new Color(1f, 0.5f, 0.1f), 3f, 8f);
            Box(r03, "Door", new Vector3(-2f, 1f, -2.95f), new Vector3(1f, 2f, 0.1f), White);
            Glow(r03, "flag:doorLocked", new Vector3(-1.4f, 1.2f, -2.85f), new Vector3(0.12f, 0.12f, 0.05f), new Color(1f, 0.1f, 0.1f), 4f, 2f);

            var r04 = Room("CAM 04", "ทางเดินชั้น 2 / บันไดลอย");
            Box(r04, "Mint Door", new Vector3(-2f, 1f, 2.95f), new Vector3(1f, 2f, 0.1f), new Color(0.9f, 0.6f, 0.7f));
            Box(r04, "Bath Door", new Vector3(2f, 1f, 2.95f), new Vector3(1f, 2f, 0.1f), new Color(0.6f, 0.75f, 0.9f));
            Box(r04, "Stair Void", new Vector3(3.3f, 0.01f, 0f), new Vector3(1.4f, 0.02f, 5.8f), Dark, false);
            for (int s = 0; s < 6; s++)
                Box(r04, "Step", new Vector3(2.5f, 0.05f, -2.5f + s * 1f), new Vector3(0.9f, 0.1f, 0.5f), Wood);
            Shape(PrimitiveType.Cylinder, r04, "flag:vacuumStairs", new Vector3(2.5f, 0.15f, 0f), new Vector3(0.6f, 0.08f, 0.6f), Dark);
            Box(r04, "Vacuum Dock", new Vector3(-3.3f, 0.1f, -1.9f), new Vector3(0.5f, 0.2f, 0.3f), White);

            var r05 = Room("CAM 05", "ห้องน้ำ");
            Box(r05, "Tub", new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 2.8f), White);
            Box(r05, "Water", new Vector3(0f, 0.62f, 0f), new Vector3(1.4f, 0.03f, 2.6f), new Color(0.22f, 0.28f, 0.34f), false);
            Box(r05, "Shelf", new Vector3(2.2f, 1.2f, 1.6f), new Vector3(1f, 0.08f, 0.4f), White);
            Box(r05, "flag:phoneOnShelf", new Vector3(2.2f, 1.28f, 1.6f), new Vector3(0.15f, 0.03f, 0.3f), Dark);
            Box(r05, "Cable", new Vector3(1.6f, 0.9f, 1.4f), new Vector3(0.03f, 0.6f, 0.03f), Dark, false);
            var inTub = new GameObject("flag:phoneInTub").transform;
            inTub.SetParent(r05, false);
            Box(inTub, "Phone In Water", new Vector3(0.3f, 0.645f, 0.6f), new Vector3(0.15f, 0.03f, 0.3f), Dark);
            Box(inTub, "Cable Into Tub", new Vector3(1.1f, 0.9f, 1.0f), new Vector3(1.4f, 0.03f, 0.03f), Dark).transform.localRotation = Quaternion.Euler(0f, 30f, -25f);
            Glow(inTub, "Sparks In Water", new Vector3(0.3f, 0.67f, 0.6f), new Vector3(0.25f, 0.05f, 0.25f), new Color(0.7f, 0.9f, 1f), 3f, 1.2f);
            Box(r05, "Door", new Vector3(-2.5f, 1f, -2.95f), new Vector3(1f, 2f, 0.1f), new Color(0.6f, 0.75f, 0.9f));

            var r06 = Room("CAM 06", "นั่งเล่นชั้น 2");
            Box(r06, "Sofa", new Vector3(-2f, 0.35f, 0.5f), new Vector3(1f, 0.7f, 2.4f), new Color(0.5f, 0.5f, 0.55f));
            Box(r06, "Balcony Door", new Vector3(3.9f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 1.6f), new Color(0.75f, 0.85f, 0.95f));
            Glow(r06, "flag:balconyLocked", new Vector3(3.8f, 1.2f, 0.9f), new Vector3(0.05f, 0.12f, 0.12f), new Color(1f, 0.1f, 0.1f), 4f, 2f);

            var r06b = Room("CAM 06b", "ระเบียงกระจก");
            var glassRail = new GameObject("!flag:glassBroken").transform;
            glassRail.SetParent(r06b, false);
            for (int g = 0; g < 3; g++)
            {
                Box(glassRail, "!flag:crack", new Vector3(2.2f, 0.6f, -1.6f + g * 1.6f), new Vector3(0.05f, 1.2f, 1.5f), new Color(0.75f, 0.85f, 0.95f));
                Box(glassRail, "flag:crack", new Vector3(2.2f, 0.6f, -1.6f + g * 1.6f), new Vector3(0.06f, 1.2f, 1.5f), White);
            }
            Box(r06b, "Drop", new Vector3(3.4f, 0.01f, 0f), new Vector3(2f, 0.02f, 6f), Dark, false);
            Shape(PrimitiveType.Cylinder, r06b, "Plant", new Vector3(-2.5f, 0.4f, 2f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.3f, 0.5f, 0.3f));

            // ---- ชั้น 1 ----
            var r08 = Room("CAM 08", "ห้องนั่งเล่น");
            Box(r08, "TV", new Vector3(-1f, 0.8f, 2.9f), new Vector3(2f, 1.1f, 0.1f), Dark);
            Box(r08, "TV Stand", new Vector3(-1f, 0.25f, 2.6f), new Vector3(2.4f, 0.5f, 0.5f), Wood);
            Box(r08, "VCR", new Vector3(-1f, 0.55f, 2.6f), new Vector3(0.7f, 0.12f, 0.4f), new Color(0.2f, 0.2f, 0.2f));
            Box(r08, "Sofa", new Vector3(0f, 0.35f, 1.4f), new Vector3(2.8f, 0.7f, 0.9f), new Color(0.5f, 0.5f, 0.55f));
            Box(r08, "Parcel", new Vector3(-1.4f, 0.2f, -2.5f), new Vector3(0.6f, 0.4f, 0.5f), new Color(0.65f, 0.5f, 0.3f));
            Strap(r08, "!item:strap", new Vector3(-0.85f, 0.03f, -2.5f), 1f);
            for (int s = 0; s < 4; s++)
                Box(r08, "Step", new Vector3(3.2f, 0.05f + s * 0.2f, -2.5f + s * 0.5f), new Vector3(1f, 0.1f, 0.45f), Wood);
            Box(r08, "flag:scooterCharging", new Vector3(1.6f, 0.3f, -1f), new Vector3(0.3f, 0.6f, 1.1f), new Color(0.2f, 0.6f, 0.9f));
            Glow(r08, "flag:fireLiving", new Vector3(1.4f, 0.6f, -0.4f), new Vector3(1.6f, 1.2f, 1.8f), new Color(1f, 0.5f, 0.1f), 3f, 9f);
            Glow(r08, "flag:dawn", new Vector3(0f, 0.02f, -1f), new Vector3(6f, 0.01f, 3f), new Color(1f, 0.85f, 0.6f), 0.8f, 0f);

            var r09 = Room("CAM 09", "ครัว");
            Box(r09, "Counter", new Vector3(0f, 0.45f, 2.5f), new Vector3(7f, 0.9f, 0.8f), White);
            Box(r09, "Drawer", new Vector3(1f, 0.6f, 2.1f), new Vector3(0.8f, 0.3f, 0.05f), Wood);
            Key(r09, "!item:key", new Vector3(1f, 0.92f, 2.35f), 1f);
            Box(r09, "Stove", new Vector3(2f, 0.45f, -2.4f), new Vector3(1.2f, 0.9f, 0.8f), Dark);
            Glow(r09, "flag:stoveOn", new Vector3(2f, 0.92f, -2.4f), new Vector3(0.6f, 0.02f, 0.5f), new Color(1f, 0.2f, 0.1f), 3f, 3f);
            Glow(r09, "flag:fireKitchen", new Vector3(2f, 1.3f, -2.2f), new Vector3(1.4f, 1.2f, 1.2f), new Color(1f, 0.5f, 0.1f), 3f, 9f);
            Box(r09, "Table", new Vector3(-1.5f, 0.4f, -0.5f), new Vector3(1.6f, 0.8f, 1.2f), Wood);

            // ---- ความทรงจำ (ภาพสี) ----
            var mem = Room("MEMORY", "วันเกิดมิ้นท์ (ปีที่แล้ว)");
            Box(mem, "Party Table", new Vector3(0f, 0.4f, 1.5f), new Vector3(2.6f, 0.8f, 1.2f), new Color(0.95f, 0.75f, 0.85f));
            Shape(PrimitiveType.Cylinder, mem, "Cake", new Vector3(0f, 0.95f, 1.5f), new Vector3(0.6f, 0.15f, 0.6f), new Color(1f, 0.95f, 0.8f));
            Glow(mem, "Candles", new Vector3(0f, 1.15f, 1.5f), new Vector3(0.3f, 0.05f, 0.05f), new Color(1f, 0.7f, 0.3f), 4f, 3f);
            for (int b = 0; b < 5; b++)
                Shape(PrimitiveType.Sphere, mem, "Balloon", new Vector3(-3f + b * 1.5f, 2f, 2.6f), Vector3.one * 0.5f, Color.HSVToRGB(b / 5f, 0.6f, 1f));

            BuildActors(Group(house, Actors));

            var camGo = new GameObject(Camera) { tag = "MainCamera" };
            camGo.transform.SetParent(house, false);
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 45f;
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 11f, -3f);
            camGo.transform.LookAt(Vector3.zero);
            return house.gameObject;
        }

        static void BuildActors(Transform t)
        {
            // ตัวละครมีข้อต่อ (TapeFigure) + FigureAnimator — เดิน/นั่ง/ถือของ/ท่าศพ
            var skin = new Color(0.78f, 0.6f, 0.45f);
            var phop = TapeFigure.Build(t, TapeLevels.Phop, new Color(0.88f, 0.88f, 0.86f), new Color(0.18f, 0.2f, 0.28f), skin, new Color(0.06f, 0.05f, 0.05f));
            BuildHeldItems(phop.transform.Find(TapeFigure.ShoulderR));
            var mint = TapeFigure.Build(t, TapeLevels.Mint, new Color(0.95f, 0.62f, 0.75f), new Color(0.9f, 0.85f, 0.9f), skin, new Color(0.06f, 0.05f, 0.05f), longHair: true);
            mint.transform.localScale = Vector3.one * 0.68f;
            TapeFigure.Build(t, TapeLevels.Mom, new Color(0.62f, 0.42f, 0.5f), new Color(0.3f, 0.25f, 0.32f), skin, new Color(0.12f, 0.08f, 0.06f), longHair: true);
            TapeFigure.Build(t, TapeLevels.Dad, new Color(0.35f, 0.45f, 0.4f), new Color(0.22f, 0.22f, 0.26f), new Color(0.72f, 0.55f, 0.42f), new Color(0.08f, 0.07f, 0.06f));
            var shadow = TapeFigure.Build(t, TapeLevels.Shadow, Dark, Dark, Dark, Dark, animate: false);
            foreach (var r in shadow.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = r.name.StartsWith("Eye")
                    ? Greybox.Glow(Color.white, 6f)   // ตาเรืองแสง — ฉากเงาหันมามองกล้อง
                    : Greybox.Mat(new Color(0.02f, 0.02f, 0.02f));
            shadow.AddComponent<ShadowAnimator>();

            var vac = new GameObject(TapeLevels.Vacuum).transform;
            vac.SetParent(t, false);
            Greybox.Shape(PrimitiveType.Cylinder, vac, "Body", new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.08f, 0.6f), Dark);
            Greybox.GlowBox(vac, "Status Light", new Vector3(0f, 0.17f, 0.2f), new Vector3(0.08f, 0.02f, 0.08f), new Color(1f, 0.05f, 0.05f), 3f);
            var brush = Greybox.Box(vac, "Brush", new Vector3(0.18f, 0.03f, 0.22f), new Vector3(0.22f, 0.02f, 0.03f), Dark, false);
            Greybox.Box(brush.transform, "Bristle", new Vector3(0f, 0f, 0f), new Vector3(0.12f, 1f, 4f), Dark, false);
            vac.gameObject.AddComponent<VacuumAnimator>();

            foreach (Transform actor in t) actor.gameObject.SetActive(false);
            phop.SetActive(false);
        }

        // ของในมือภพ (ลูกของไหล่ขวา — ตามแขนไป) ชื่อ "Held:<item>" TapeGame เปิดตามของที่ถืออยู่
        static void BuildHeldItems(Transform arm)
        {
            var phone = Greybox.Box(arm, "Held:phone", new Vector3(0f, -0.74f, 0.06f), new Vector3(0.07f, 0.14f, 0.015f), Dark, false);
            Greybox.GlowBox(phone.transform, "Screen", new Vector3(0f, 0f, 0.6f), new Vector3(0.85f, 0.85f, 0.2f), new Color(0.8f, 0.9f, 1f), 2f);
            // ไฟฉายโทรศัพท์ = ไฟดวงเดียวในบ้านตอนไฟดับ
            var torch = new GameObject("Flashlight").AddComponent<Light>();
            torch.transform.SetParent(phone.transform, false);
            torch.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            torch.type = LightType.Spot;
            torch.spotAngle = 55f;
            torch.innerSpotAngle = 25f;
            torch.range = 9f;
            torch.intensity = 18f;
            torch.color = new Color(0.95f, 0.97f, 1f);
            torch.shadows = LightShadows.Soft;

            var held = Extinguisher(arm, "Held:extinguisher", new Vector3(0f, -1.05f, 0.12f), 0.75f, wallMounted: false);
            Greybox.Box(arm, "Held:tablet", new Vector3(0f, -0.74f, 0.08f), new Vector3(0.22f, 0.3f, 0.015f), Dark, false);
            Key(arm, "Held:key", new Vector3(0f, -0.74f, 0.06f), 0.7f).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Strap(arm, "Held:strap", new Vector3(0f, -0.74f, 0.08f), 0.6f).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (Transform c in arm)
                if (c.name.StartsWith("Held:")) c.gameObject.SetActive(false);
        }

        // ถังดับเพลิง: ตัวถัง + แถบฉลาก + วาล์ว + ด้ามบีบ + สายฉีด (+ ขายึดผนัง) — รูปทรงต้องอ่านออกจากกล้องเพดาน
        static GameObject Extinguisher(Transform parent, string name, Vector3 position, float scale, bool wallMounted)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * scale;
            var body = new Color(0.78f, 0.64f, 0.2f);
            Greybox.Shape(PrimitiveType.Cylinder, root, "Tank", new Vector3(0f, 0.32f, 0f), new Vector3(0.26f, 0.3f, 0.26f), body);
            Greybox.Shape(PrimitiveType.Sphere, root, "Tank Top", new Vector3(0f, 0.62f, 0f), new Vector3(0.26f, 0.12f, 0.26f), body);
            Greybox.Shape(PrimitiveType.Cylinder, root, "Label", new Vector3(0f, 0.32f, 0f), new Vector3(0.265f, 0.07f, 0.265f), new Color(0.92f, 0.92f, 0.9f));
            Greybox.Box(root, "Valve", new Vector3(0f, 0.72f, 0f), new Vector3(0.07f, 0.1f, 0.07f), Dark, false);
            var lever = Greybox.Box(root, "Lever", new Vector3(0f, 0.8f, -0.06f), new Vector3(0.05f, 0.03f, 0.2f), Dark, false);
            lever.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            var hose = Greybox.Shape(PrimitiveType.Cylinder, root, "Hose", new Vector3(0.12f, 0.5f, 0.06f), new Vector3(0.035f, 0.22f, 0.035f), Dark);
            hose.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Greybox.Shape(PrimitiveType.Cylinder, root, "Nozzle", new Vector3(0.16f, 0.27f, 0.06f), new Vector3(0.06f, 0.05f, 0.06f), Dark);
            if (wallMounted) Greybox.Box(root, "Bracket", new Vector3(0f, 0.45f, 0.16f), new Vector3(0.3f, 0.08f, 0.05f), Dark, false);
            return root.gameObject;
        }

        // กุญแจ: ห่วง + ก้าน + ฟัน + พวงกุญแจ — ขยายใหญ่กว่าจริงนิดหน่อยให้อ่านออกจากเพดาน
        static GameObject Key(Transform parent, string name, Vector3 position, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * scale;
            var brass = new Color(0.92f, 0.8f, 0.35f);
            Greybox.Shape(PrimitiveType.Cylinder, root, "Bow", new Vector3(-0.1f, 0f, 0f), new Vector3(0.11f, 0.008f, 0.11f), brass);
            Greybox.Shape(PrimitiveType.Cylinder, root, "Bow Hole", new Vector3(-0.1f, 0.004f, 0f), new Vector3(0.045f, 0.008f, 0.045f), Dark);
            Greybox.Box(root, "Shaft", new Vector3(0.04f, 0f, 0f), new Vector3(0.18f, 0.014f, 0.03f), brass, false);
            for (int i = 0; i < 3; i++)
                Greybox.Box(root, "Tooth", new Vector3(0.06f + i * 0.04f, 0f, -0.024f), new Vector3(0.02f, 0.014f, 0.025f), brass, false);
            Greybox.Box(root, "Fob", new Vector3(-0.2f, 0f, 0.04f), new Vector3(0.08f, 0.012f, 0.05f), new Color(0.2f, 0.4f, 0.75f), false);
            return root.gameObject;
        }

        // สายรัดของ: ม้วนสาย + หัวล็อก
        static GameObject Strap(Transform parent, string name, Vector3 position, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * scale;
            Greybox.Shape(PrimitiveType.Cylinder, root, "Coil", Vector3.zero, new Vector3(0.24f, 0.03f, 0.24f), new Color(0.95f, 0.8f, 0.1f));
            Greybox.Shape(PrimitiveType.Cylinder, root, "Core", new Vector3(0f, 0.01f, 0f), new Vector3(0.12f, 0.03f, 0.12f), Dark);
            Greybox.Box(root, "Buckle", new Vector3(0.15f, 0.01f, 0f), new Vector3(0.08f, 0.03f, 0.06f), new Color(0.6f, 0.6f, 0.6f), false);
            return root.gameObject;
        }

        // ---- helpers ----
        static Transform NewRoom(Transform rooms, string cam, string label, int index)
        {
            var room = Group(rooms, cam);
            room.localPosition = new Vector3(index * RoomSpacing, 0f, 0f);
            Box(room, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 6f), Floor);
            Box(room, "Wall N", new Vector3(0f, 1.2f, 3.05f), new Vector3(8.2f, 2.4f, 0.1f), Wall);
            Box(room, "Wall S", new Vector3(0f, 0.15f, -3.05f), new Vector3(8.2f, 0.3f, 0.1f), Wall);   // ผนังหน้าเตี้ย ไม่บังกล้อง
            Box(room, "Wall W", new Vector3(-4.05f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 6.2f), Wall);
            Box(room, "Wall E", new Vector3(4.05f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 6.2f), Wall);

            // ไฟเพดาน (ดับเมื่อ flag lights ถูกล้าง) + ไฟสลัวสำรองให้ยังพอเห็นในความมืด
            var ceiling = new GameObject("flag:lights").AddComponent<Light>();
            ceiling.transform.SetParent(room, false);
            ceiling.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ceiling.type = LightType.Spot;
            ceiling.spotAngle = 110f;
            ceiling.range = 10f;
            ceiling.intensity = 40f;
            ceiling.color = new Color(1f, 0.95f, 0.88f);
            ceiling.shadows = LightShadows.Soft;
            var dim = new GameObject("!flag:lights").AddComponent<Light>();
            dim.transform.SetParent(room, false);
            dim.transform.localPosition = new Vector3(-1f, 3f, -2f);
            dim.type = LightType.Point;
            dim.range = 9f;
            dim.intensity = 4f;
            dim.color = new Color(0.6f, 0.7f, 1f);

            var tag = new GameObject("Label: " + label);
            tag.transform.SetParent(room, false);
            return room;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool solid = false)
            => Greybox.Box(parent, name, center, size, color, solid);

        static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 center, Vector3 scale, Color color)
            => Greybox.Shape(type, parent, name, center, scale, color);

        static void Glow(Transform parent, string name, Vector3 center, Vector3 size, Color color, float intensity, float lightIntensity)
        {
            var go = Greybox.GlowBox(parent, name, center, size, color, intensity);
            if (lightIntensity > 0f)
                Greybox.PointLight(go.transform, Vector3.zero, color, lightIntensity, 4f);
        }
    }
}
