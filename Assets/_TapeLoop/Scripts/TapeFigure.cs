using HorrorJam;
using UnityEngine;

namespace TapeLoop
{
    // คนจาก primitive แบบมีข้อต่อ (ไม่มี bone) — FigureAnimator หมุนข้อต่อพวกนี้
    //   root (TapeGame วางตำแหน่ง)
    //    └ Rig (ยก/กดทั้งตัว ตอนนั่ง/นอน)
    //        ├ Hip L / Hip R → Leg → Foot
    //        └ Torso
    //            ├ Shoulder L / Shoulder R → Arm → Hand
    //            └ Neck → Head, Hair, Eyes
    public static class TapeFigure
    {
        public const string Rig = "Rig";
        public const string HipL = "Rig/Hip L", HipR = "Rig/Hip R";
        public const string Torso = "Rig/Torso";
        public const string ShoulderL = "Rig/Torso/Shoulder L", ShoulderR = "Rig/Torso/Shoulder R";
        public const string Neck = "Rig/Torso/Neck";

        public static GameObject Build(Transform parent, string name, Color shirt, Color pants, Color skin, Color hair, bool longHair = false, bool animate = true)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var rig = Group(root, "Rig");
            var shoes = new Color(0.12f, 0.11f, 0.1f);

            foreach (float side in new[] { -1f, 1f })
            {
                var hip = Group(rig, side < 0 ? "Hip L" : "Hip R");
                hip.localPosition = new Vector3(side * 0.12f, -0.08f, 0f);
                Greybox.Shape(PrimitiveType.Capsule, hip, "Thigh", new Vector3(0f, -0.25f, 0f), new Vector3(0.2f, 0.25f, 0.2f), pants);
                Greybox.Shape(PrimitiveType.Capsule, hip, "Shin", new Vector3(0f, -0.62f, 0f), new Vector3(0.17f, 0.22f, 0.17f), pants);
                Greybox.Box(hip, "Foot", new Vector3(0f, -0.86f, 0.06f), new Vector3(0.16f, 0.08f, 0.28f), shoes, false);
            }

            var torso = Group(rig, "Torso");
            Greybox.Shape(PrimitiveType.Capsule, torso, "Chest", new Vector3(0f, 0.2f, 0f), new Vector3(0.52f, 0.3f, 0.32f), shirt);
            Greybox.Shape(PrimitiveType.Capsule, torso, "Belly", new Vector3(0f, -0.04f, 0f), new Vector3(0.46f, 0.2f, 0.3f), shirt);
            Greybox.Box(torso, "Belt", new Vector3(0f, -0.14f, 0f), new Vector3(0.44f, 0.06f, 0.29f), pants, false);

            foreach (float side in new[] { -1f, 1f })
            {
                var shoulder = Group(torso, side < 0 ? "Shoulder L" : "Shoulder R");
                shoulder.localPosition = new Vector3(side * 0.31f, 0.4f, 0f);
                Greybox.Shape(PrimitiveType.Capsule, shoulder, "Upper Arm", new Vector3(0f, -0.17f, 0f), new Vector3(0.14f, 0.18f, 0.14f), shirt);
                Greybox.Shape(PrimitiveType.Capsule, shoulder, "Forearm", new Vector3(0f, -0.47f, 0f), new Vector3(0.12f, 0.16f, 0.12f), skin);
                Greybox.Shape(PrimitiveType.Sphere, shoulder, "Hand", new Vector3(0f, -0.66f, 0.01f), new Vector3(0.11f, 0.12f, 0.08f), skin);
            }

            var neck = Group(torso, "Neck");
            neck.localPosition = new Vector3(0f, 0.48f, 0f);
            Greybox.Shape(PrimitiveType.Cylinder, neck, "Throat", new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.06f, 0.12f), skin);
            Greybox.Shape(PrimitiveType.Sphere, neck, "Head", new Vector3(0f, 0.22f, 0.01f), new Vector3(0.3f, 0.34f, 0.31f), skin);
            Greybox.Shape(PrimitiveType.Sphere, neck, "Hair", new Vector3(0f, 0.3f, -0.03f), new Vector3(0.33f, 0.26f, 0.34f), hair);
            if (longHair)
                Greybox.Shape(PrimitiveType.Capsule, neck, "Long Hair", new Vector3(0f, 0.12f, -0.12f), new Vector3(0.3f, 0.2f, 0.12f), hair);
            Greybox.Box(neck, "Eye L", new Vector3(-0.06f, 0.24f, 0.15f), new Vector3(0.04f, 0.035f, 0.02f), new Color(0.03f, 0.03f, 0.03f), false);
            Greybox.Box(neck, "Eye R", new Vector3(0.06f, 0.24f, 0.15f), new Vector3(0.04f, 0.035f, 0.02f), new Color(0.03f, 0.03f, 0.03f), false);
            Greybox.Box(neck, "Nose", new Vector3(0f, 0.19f, 0.165f), new Vector3(0.03f, 0.05f, 0.03f), skin * 0.92f, false);

            if (animate) root.gameObject.AddComponent<FigureAnimator>();
            return root.gameObject;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }
    }
}
