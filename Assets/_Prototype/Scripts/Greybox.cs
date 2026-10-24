using System.Collections.Generic;
using UnityEngine;

namespace HorrorJam
{
    // ตัวช่วยสร้าง primitive สี ๆ สำหรับ greybox — เปลี่ยนเป็นโมเดลจริงทีหลังได้โดยไม่แตะ logic
    public static class Greybox
    {
        static readonly Dictionary<string, Material> materials = new();

        static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // smoothness สูง = ผิวเปียกสะท้อนไฟ (พื้นซอยหลังฝน, น้ำขัง)
        public static Material Mat(Color color, float smoothness = 0.25f)
        {
            string key = "Grey_" + ColorUtility.ToHtmlStringRGB(color) + (Mathf.Approximately(smoothness, 0.25f) ? "" : $"_s{Mathf.RoundToInt(smoothness * 100)}");
            if (materials.TryGetValue(key, out var mat) && mat != null) return mat;
            mat = new Material(LitShader) { name = key };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.color = color;
            materials[key] = mat;
            return mat;
        }

        // วัตถุเรืองแสง (หน้าต่าง, หลอดไฟ, เทียน) — ค่า > 1 ให้ Bloom จับ
        public static Material Glow(Color color, float intensity = 2f)
        {
            string key = "Glow_" + ColorUtility.ToHtmlStringRGB(color) + $"_{Mathf.RoundToInt(intensity * 10)}";
            if (materials.TryGetValue(key, out var mat) && mat != null) return mat;
            mat = new Material(LitShader) { name = key };
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            materials[key] = mat;
            return mat;
        }

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool solid = true)
        {
            return Shape(PrimitiveType.Cube, parent, name, center, size, color, solid);
        }

        public static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 center, Vector3 scale, Color color, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (!solid) Remove(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject GlowBox(Transform parent, string name, Vector3 center, Vector3 size, Color color, float intensity = 2f)
        {
            var go = Box(parent, name, center, size, color, false);
            go.GetComponent<Renderer>().sharedMaterial = Glow(color, intensity);
            return go;
        }

        // ของเหนือหัว (ร่ม, กันสาด, ราวผ้า): กล้องมองไม่เห็น แต่ยังทอดเงาลงพื้น — แบบที่ Twelve Minutes ซ่อนเพดาน
        public static GameObject ShadowOnly(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            return go;
        }

        // ไฟวงส่องลงจากด้านบน = บอกผู้เล่นว่า "ตรงนี้สำคัญ" โดยไม่ต้องมี UI
        public static Light KeyLight(Transform parent, string name, Vector3 floorPoint, float height, Color color, float intensity, float angle, bool castShadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = floorPoint + Vector3.up * height;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = height * 2f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.55f;
            // shadow atlas 2048 ใส่เงาความละเอียดสูงได้ 4 ดวง — เกินนั้น URP ลดความละเอียดเองทุกเฟรม (warning รัว)
            light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        public static Light PointLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        // Bake ใน Editor ใช้ Destroy ไม่ได้ ต้อง DestroyImmediate
        static void Remove(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
