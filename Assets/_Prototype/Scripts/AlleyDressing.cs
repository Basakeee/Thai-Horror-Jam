using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HorrorJam
{
    // ของตกแต่งล้วน ๆ — ไม่มี logic ผูกอยู่ ลบ/ย้าย/เปลี่ยนเป็นโมเดลจริงได้ตามใจ (ไม่ต้องตามชื่อใน Names)
    public static class AlleyDressing
    {
        static readonly Color Black = new(0.03f, 0.03f, 0.03f);
        static readonly Color Shutter = new(0.38f, 0.4f, 0.42f);
        static readonly Color WarmWindow = new(1f, 0.72f, 0.4f);
        static readonly Color DarkWindow = new(0.05f, 0.07f, 0.1f);
        static readonly Color Neon = new(0.75f, 1f, 0.85f);
        static readonly Color[] Awnings = { new(0.2f, 0.32f, 0.55f), new(0.6f, 0.15f, 0.12f), new(0.22f, 0.45f, 0.3f), new(0.7f, 0.55f, 0.2f) };

        public static void Dress(Transform world)
        {
            var root = new GameObject("Dressing").transform;
            root.SetParent(world, false);

            Shopfronts(Group(root, "Shopfronts"));
            Overhead(Group(root, "Overhead Wires"));
            Clutter(Group(root, "Clutter"));
            Stall(Group(root, "Fortune Stall Dressing"));
            Shrine(Group(root, "Shrine Dressing"));
            Lamps(Group(root, "Lamps"));
            Ground(Group(root, "Ground Detail"));
        }

        // ---------------------------------------------------------------------
        static void Shopfronts(Transform t)
        {
            string[] leftSigns = { null, "ร้านทองเยาวราช (ปิด)", "ห้องน้ำ 3 บาท", "ซ่อมแอร์ ตู้เย็น", "โชห่วยเจ๊หมวย", null, "ร้านตัดผมชาย", null };
            string[] rightSigns = { "รับซื้อของเก่า", null, "ร้านยาจีน", "ข้าวแกงป้าศรี", null, "อู่ซ่อมรถ", null, "หอพักสุขใจ" };

            for (int side = -1; side <= 1; side += 2)
            {
                var signs = side < 0 ? leftSigns : rightSigns;
                for (int i = 0; i < 8; i++)
                {
                    float z = i * 4f + 1f;
                    float face = side * 3.97f;
                    var shop = Group(t, $"{(side < 0 ? "L" : "R")}{i}");
                    bool toilet = side < 0 && i == 2;
                    bool auntHouse = side > 0 && i == 3;
                    bool open = (i + (side > 0 ? 1 : 0)) % 3 == 0 && !toilet && !auntHouse;

                    // ชั้นล่าง: ประตูม้วนเหล็ก หรือร้านที่ยังเปิดไฟสลัว ๆ
                    if (!toilet && !auntHouse)
                    {
                        if (open)
                        {
                            Greybox.GlowBox(shop, "Interior", new Vector3(face + side * 0.01f, 1.3f, z), new Vector3(0.04f, 2.5f, 3.2f), new Color(0.55f, 0.4f, 0.25f), 0.6f);
                            for (int s = 0; s < 3; s++)
                                Greybox.Box(shop, "Shelf", new Vector3(face - side * 0.05f, 0.6f + s * 0.7f, z), new Vector3(0.12f, 0.06f, 2.8f), new Color(0.25f, 0.18f, 0.12f), false);
                        }
                        else
                        {
                            Greybox.Box(shop, "Shutter", new Vector3(face, 1.3f, z), new Vector3(0.05f, 2.6f, 3.4f), Shutter, false);
                            for (int s = 0; s < 7; s++)
                                Greybox.Box(shop, "Rib", new Vector3(face - side * 0.03f, 0.25f + s * 0.36f, z), new Vector3(0.02f, 0.04f, 3.4f), new Color(0.25f, 0.27f, 0.28f), false);
                        }
                    }

                    // กันสาด
                    var awning = Greybox.Box(shop, "Awning", new Vector3(side * 3.45f, 2.95f, z), new Vector3(1.1f, 0.06f, 3.7f), Awnings[(i + side + 2) % Awnings.Length], false);
                    awning.transform.localRotation = Quaternion.Euler(0f, 0f, side * -14f);
                    Greybox.ShadowOnly(awning);

                    // ชั้นบน: หน้าต่าง — บางบานเปิดไฟ (มีคนอยู่ แต่ไม่มีใครออกมาช่วย)
                    for (int w = -1; w <= 1; w += 2)
                    {
                        bool lit = (i * 3 + w + side * 5 + 11) % 4 == 0;
                        var winPos = new Vector3(face, 4.6f, z + w * 0.9f);
                        if (lit) Greybox.GlowBox(shop, "Window Lit", winPos, new Vector3(0.04f, 1f, 0.9f), WarmWindow, 1.6f);
                        else Greybox.Box(shop, "Window", winPos, new Vector3(0.04f, 1f, 0.9f), DarkWindow, false);
                        Greybox.Box(shop, "Grille", winPos - new Vector3(side * 0.03f, 0f, 0f), new Vector3(0.02f, 1.05f, 0.04f), Black, false);
                    }

                    if ((i + side) % 3 != 0)
                        Greybox.Box(shop, "AC Unit", new Vector3(side * 3.7f, 3.6f, z + 1.3f), new Vector3(0.5f, 0.45f, 0.7f), new Color(0.75f, 0.75f, 0.72f), false);

                    if (signs[i] != null)
                        Sign(shop, signs[i], new Vector3(side * 3.9f, 3.45f, z), side < 0 ? -90f : 90f, new Vector2(2.6f, 0.5f),
                            toilet ? new Color(0.15f, 0.3f, 0.6f) : new Color(0.75f, 0.68f, 0.5f), toilet ? Color.white : new Color(0.35f, 0.08f, 0.05f));
                }
            }
        }

        static void Overhead(Transform t)
        {
            // สายไฟพันกันเหนือหัว — เส้นที่ลุงชมซ่อมคือหนึ่งในนี้
            float[] zs = { 3f, 7.5f, 11f, 16f, 20f, 27f, 30f };
            for (int i = 0; i < zs.Length; i++)
            {
                var wire = Greybox.Box(t, "Wire", new Vector3(0f, 6.6f + (i % 3) * 0.3f, zs[i]), new Vector3(8f, 0.03f, 0.03f), Black, false);
                wire.transform.localRotation = Quaternion.Euler(0f, (i % 2 == 0 ? 6f : -8f), (i % 2 == 0 ? 2f : -3f));
                Greybox.ShadowOnly(wire);
            }
            for (int side = -1; side <= 1; side += 2)
                Greybox.ShadowOnly(Greybox.Box(t, "Wall Cable", new Vector3(side * 3.93f, 6.1f, 15.5f), new Vector3(0.06f, 0.06f, 32f), Black, false));
        }

        static void Clutter(Transform t)
        {
            // โอ่ง
            foreach (var p in new[] { new Vector3(3.4f, 0f, 17.4f), new Vector3(3.45f, 0f, 18.3f), new Vector3(-3.4f, 0f, 28.5f) })
            {
                Greybox.Shape(PrimitiveType.Sphere, t, "Jar", p + Vector3.up * 0.4f, new Vector3(0.7f, 0.8f, 0.7f), new Color(0.3f, 0.17f, 0.1f), true);
                Greybox.Shape(PrimitiveType.Cylinder, t, "Jar Lid", p + Vector3.up * 0.8f, new Vector3(0.5f, 0.02f, 0.5f), new Color(0.5f, 0.45f, 0.4f));
            }

            // เก้าอี้พลาสติกแดง + ถังขยะ
            foreach (var p in new[] { new Vector3(-3.3f, 0f, 12.2f), new Vector3(-3.35f, 0f, 13f) })
            {
                Greybox.Box(t, "Chair Seat", p + Vector3.up * 0.45f, new Vector3(0.45f, 0.05f, 0.45f), new Color(0.75f, 0.12f, 0.1f), false);
                Greybox.Box(t, "Chair Base", p + Vector3.up * 0.22f, new Vector3(0.38f, 0.44f, 0.38f), new Color(0.65f, 0.1f, 0.08f), true);
            }
            Greybox.Shape(PrimitiveType.Cylinder, t, "Bin", new Vector3(-3.4f, 0.45f, 15f), new Vector3(0.6f, 0.45f, 0.6f), new Color(0.15f, 0.35f, 0.2f), true);

            // มอเตอร์ไซค์จอดข้างกำแพง
            var bike = Group(t, "Motorbike");
            bike.localPosition = new Vector3(3.3f, 0f, 24.8f);
            Greybox.Box(bike, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.4f, 1.3f), new Color(0.6f, 0.1f, 0.1f), true);
            Greybox.Box(bike, "Seat", new Vector3(0f, 0.85f, -0.15f), new Vector3(0.3f, 0.1f, 0.7f), Black, false);
            foreach (float wz in new[] { -0.6f, 0.6f })
            {
                var wheel = Greybox.Shape(PrimitiveType.Cylinder, bike, "Wheel", new Vector3(0f, 0.3f, wz), new Vector3(0.6f, 0.06f, 0.6f), Black);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // ราวตากผ้า
            Greybox.ShadowOnly(Greybox.Box(t, "Laundry Line", new Vector3(0f, 3.4f, 18.5f), new Vector3(8f, 0.02f, 0.02f), new Color(0.6f, 0.6f, 0.6f), false));
            Color[] cloth = { new(0.8f, 0.8f, 0.75f), new(0.3f, 0.4f, 0.7f), new(0.7f, 0.3f, 0.35f), new(0.75f, 0.7f, 0.3f) };
            for (int i = 0; i < 6; i++)
                Greybox.ShadowOnly(Greybox.Box(t, "Cloth", new Vector3(-3f + i * 1.15f, 3.05f, 18.5f), new Vector3(0.55f, 0.65f, 0.02f), cloth[i % cloth.Length], false));

            // กระถางหน้าบ้านป้าศรี
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(3.55f, 0f, 15.2f + i * 0.5f);
                Greybox.Shape(PrimitiveType.Cylinder, t, "Pot", p + Vector3.up * 0.2f, new Vector3(0.35f, 0.2f, 0.35f), new Color(0.55f, 0.3f, 0.2f));
                Greybox.Shape(PrimitiveType.Sphere, t, "Leaves", p + Vector3.up * 0.55f, new Vector3(0.45f, 0.4f, 0.45f), new Color(0.15f, 0.35f, 0.15f));
            }
        }

        static void Stall(Transform t)
        {
            Greybox.Box(t, "Tablecloth", new Vector3(-2.9f, 0.79f, 4f), new Vector3(0.95f, 0.02f, 1.95f), new Color(0.65f, 0.55f, 0.2f), false);
            Greybox.Box(t, "Stool", new Vector3(-3.55f, 0.25f, 4f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.2f, 0.25f, 0.5f), true);
            for (int i = 0; i < 5; i++)
            {
                var card = Greybox.Box(t, "Card", new Vector3(-2.75f, 0.81f, 3.85f + i * 0.09f), new Vector3(0.12f, 0.01f, 0.08f), new Color(0.85f, 0.82f, 0.7f), false);
                card.transform.localRotation = Quaternion.Euler(0f, -20f + i * 10f, 0f);
            }
            Greybox.GlowBox(t, "Incense", new Vector3(-2.6f, 0.95f, 4.7f), new Vector3(0.02f, 0.25f, 0.02f), new Color(1f, 0.35f, 0.1f), 3f);
            Sign(t, "ดูดวง แม่นยำ ไม่แม่นไม่เอาตังค์", new Vector3(-2.42f, 0.55f, 4f), -90f, new Vector2(1.4f, 0.4f), new Color(0.85f, 0.75f, 0.3f), new Color(0.45f, 0.05f, 0.05f));
        }

        static void Shrine(Transform t)
        {
            var c = new Vector3(3.2f, 0f, 9f);
            Greybox.Box(t, "Shelf", c + new Vector3(-0.1f, 1.18f, 0f), new Vector3(0.55f, 0.04f, 0.8f), new Color(0.75f, 0.6f, 0.2f), false);
            // น้ำแดงถวายศาล
            for (int i = 0; i < 3; i++)
                Greybox.Shape(PrimitiveType.Cylinder, t, "Red Soda", c + new Vector3(-0.25f, 1.32f, -0.25f + i * 0.25f), new Vector3(0.07f, 0.12f, 0.07f), new Color(0.85f, 0.05f, 0.1f));
            for (int i = 0; i < 6; i++)
                Greybox.Shape(PrimitiveType.Sphere, t, "Garland", c + new Vector3(-0.38f, 1.6f - Mathf.Abs(i - 2.5f) * 0.06f, -0.3f + i * 0.12f), Vector3.one * 0.09f, i % 2 == 0 ? new Color(0.95f, 0.75f, 0.1f) : new Color(0.95f, 0.4f, 0.6f));
        }

        static void Lamps(Transform t)
        {
            // โคมติดผนังตรงตำแหน่ง Street Lights + หลอดนีออนเขียวซีด 2 ดวง (โทนสยอง)
            foreach (var p in new[] { new Vector3(-3.4f, 4.5f, 6f), new Vector3(3.4f, 4.5f, 12f), new Vector3(-3.4f, 4.5f, 19f), new Vector3(3.4f, 4.5f, 27f) })
            {
                float side = Mathf.Sign(p.x);
                Greybox.Box(t, "Bracket", new Vector3(side * 3.7f, p.y + 0.15f, p.z), new Vector3(0.6f, 0.05f, 0.05f), Black, false);
                Greybox.Shape(PrimitiveType.Sphere, t, "Bulb", p, Vector3.one * 0.22f, WarmWindow).GetComponent<Renderer>().sharedMaterial = Greybox.Glow(new Color(1f, 0.75f, 0.45f), 4f);
            }
            foreach (var p in new[] { new Vector3(-3.92f, 2.6f, 16.5f), new Vector3(3.92f, 2.6f, 21.5f) })
            {
                Greybox.GlowBox(t, "Neon Tube", p, new Vector3(0.05f, 0.05f, 1.2f), Neon, 3f);
                Greybox.PointLight(t, p - new Vector3(Mathf.Sign(p.x) * 0.4f, 0f, 0f), new Color(0.6f, 1f, 0.75f), 2.5f, 5f);
            }
            // ไฟถนนปากซอย
            Greybox.Box(t, "Road Lamp Post", new Vector3(4.6f, 3f, 34f), new Vector3(0.15f, 6f, 0.15f), new Color(0.4f, 0.4f, 0.4f), true);
            Greybox.PointLight(t, new Vector3(3.6f, 5.8f, 34f), new Color(1f, 0.6f, 0.3f), 8f, 12f);
        }

        static void Ground(Transform t)
        {
            for (int side = -1; side <= 1; side += 2)
                Greybox.Box(t, "Gutter", new Vector3(side * 3.8f, 0.005f, 15.5f), new Vector3(0.3f, 0.01f, 33f), new Color(0.07f, 0.07f, 0.07f), false);
            Greybox.Shape(PrimitiveType.Cylinder, t, "Manhole", new Vector3(0.8f, 0.006f, 11f), new Vector3(0.9f, 0.005f, 0.9f), new Color(0.12f, 0.12f, 0.12f));
            // แอ่งน้ำเล็ก ๆ กระจาย = พื้นเปียก สะท้อนไฟ
            foreach (var p in new[] { new Vector3(-1.5f, 0f, 7f), new Vector3(1.8f, 0f, 12.5f), new Vector3(-2.2f, 0f, 27f) })
            {
                var puddle = Greybox.Shape(PrimitiveType.Cylinder, t, "Wet Patch", p + Vector3.up * 0.006f, new Vector3(1.4f, 0.003f, 0.9f), Color.black);
                puddle.GetComponent<Renderer>().sharedMaterial = Greybox.Mat(new Color(0.17f, 0.19f, 0.22f), 0.65f);
            }
        }

        // ---------------------------------------------------------------------
        public static Volume PostFx(Transform world)
        {
            var go = new GameObject("Post FX");
            go.transform.SetParent(world, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "HorrorPostFX";
            volume.sharedProfile = profile;

            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.75f);
            var grade = profile.Add<ColorAdjustments>(true);
            grade.saturation.Override(-8f);
            grade.contrast.Override(18f);
            grade.postExposure.Override(1.05f);
            grade.colorFilter.Override(new Color(1f, 0.96f, 0.9f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.18f);
            profile.Add<ChromaticAberration>(true).intensity.Override(0.06f);
            return volume;
        }

        // ---------------------------------------------------------------------
        public static void Sign(Transform parent, string text, Vector3 position, float yaw, Vector2 size, Color board, Color ink)
        {
            var sign = Group(parent, "Sign");
            sign.localPosition = position;
            sign.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Greybox.Box(sign, "Board", Vector3.zero, new Vector3(size.x, size.y, 0.05f), board, false);

            var canvasGo = new GameObject("Text", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(sign, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.localPosition = new Vector3(0f, 0f, -0.03f);
            rt.sizeDelta = new Vector2(size.x * 100f, size.y * 100f);
            rt.localScale = Vector3.one * 0.01f;
            var label = AlleyBuilder.UiText(rt, "Label", Vector2.zero, Vector2.one, Mathf.RoundToInt(size.y * 55f), TextAnchor.MiddleCenter, ink);
            label.text = text;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = Mathf.RoundToInt(size.y * 60f);
            Object.DestroyImmediate(label.GetComponent<Shadow>());
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
