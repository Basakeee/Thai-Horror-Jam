using System.Collections.Generic;
using HorrorJam;
using UnityEngine;

namespace TapeLoop
{
    // ร่องรอยการตาย (เลือด / ควัน / ประกายไฟ / เศษกระจก / ชั้นทับ) — ชุดเดียวใช้ซ้ำ จัดตามสาเหตุ + ความคืบหน้า 0..1
    // ภาพเทปเป็นขาวดำ → เลือดออกมาเป็นสีดำมันวาว (ภาษาภาพหนังสยองขาวดำ)
    public class GoreRig : MonoBehaviour
    {
        // แดงล้วน (hue 0) — Color Curves ของ TapeGame เก็บสีนี้ไว้ ส่วนอื่นเป็นขาวดำ
        static readonly Color Blood = new(0.55f, 0.015f, 0.015f);

        Transform pool, shelf;
        readonly List<Transform> splatter = new(), smoke = new(), embers = new(), sparks = new(), shards = new(), boxes = new();
        Light sparkLight, fireLight;
        DeathStyle style;
        float progress;
        bool visible;

        public static GoreRig Create(Transform parent, string name)
        {
            var rig = new GameObject(name).AddComponent<GoreRig>();
            rig.transform.SetParent(parent, false);
            rig.Build();
            rig.Hide();
            return rig;
        }

        void Build()
        {
            var t = transform;
            pool = Greybox.Shape(PrimitiveType.Cylinder, t, "Blood Pool", Vector3.zero, Vector3.one, Blood).transform;
            pool.GetComponent<Renderer>().sharedMaterial = Greybox.Mat(Blood, 0.3f);
            for (int i = 0; i < 12; i++)
            {
                var d = Greybox.Shape(PrimitiveType.Sphere, t, "Splatter", Vector3.zero, Vector3.one, Blood).transform;
                d.GetComponent<Renderer>().sharedMaterial = Greybox.Mat(Blood, 0.3f);
                splatter.Add(d);
            }
            for (int i = 0; i < 6; i++) smoke.Add(Greybox.Shape(PrimitiveType.Sphere, t, "Smoke", Vector3.zero, Vector3.one, new Color(0.35f, 0.35f, 0.35f)).transform);
            for (int i = 0; i < 7; i++) embers.Add(Greybox.GlowBox(t, "Ember", Vector3.zero, Vector3.one * 0.06f, new Color(1f, 0.45f, 0.1f), 5f).transform);
            for (int i = 0; i < 6; i++) sparks.Add(Greybox.GlowBox(t, "Spark", Vector3.zero, Vector3.one * 0.05f, new Color(0.7f, 0.9f, 1f), 8f).transform);
            for (int i = 0; i < 14; i++) shards.Add(Greybox.Box(t, "Glass Shard", Vector3.zero, Vector3.one, new Color(0.85f, 0.92f, 0.95f), false).transform);
            shelf = Greybox.Box(t, "Fallen Shelf", Vector3.zero, new Vector3(2.4f, 0.55f, 0.55f), new Color(0.4f, 0.3f, 0.22f), false).transform;
            for (int i = 0; i < 4; i++) boxes.Add(Greybox.Box(t, "Box", Vector3.zero, new Vector3(0.5f, 0.4f, 0.45f), new Color(0.6f, 0.5f, 0.35f), false).transform);
            sparkLight = Greybox.PointLight(t, Vector3.zero, new Color(0.6f, 0.85f, 1f), 0f, 4f);
            fireLight = Greybox.PointLight(t, Vector3.zero, new Color(1f, 0.5f, 0.15f), 0f, 5f);
        }

        public void Hide()
        {
            if (!visible && pool != null && !pool.gameObject.activeSelf) return;
            visible = false;
            foreach (Transform c in transform) c.gameObject.SetActive(false);
        }

        // actor = ศพ (ใช้ตำแหน่งหัวเป็นจุดเลือด) · g = ความคืบหน้าของร่องรอย 0..1
        public void Show(DeathStyle deathStyle, Transform actor, float g, int seed)
        {
            Hide();
            if (deathStyle == DeathStyle.Peaceful) return;
            visible = true;
            style = deathStyle;
            progress = Mathf.Clamp01(g);
            var rng = new System.Random(seed);
            float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

            var head = actor.Find(TapeFigure.Neck);
            Vector3 body = actor.position;
            Vector3 headPos = head != null ? head.position : body;
            Vector3 floorHead = new(headPos.x, 0.012f, headPos.z);
            Vector3 floorBody = new(body.x, 0.012f, body.z);

            float poolSize = style switch
            {
                DeathStyle.Crush => 2.3f,
                DeathStyle.Fall => 1.5f,
                DeathStyle.Glass => 1.7f,
                DeathStyle.Electric => 0f,
                DeathStyle.Burn => 0f,
                _ => 0.9f,
            };
            if (poolSize > 0f)
            {
                // แอ่งเลือดขยายช้า ๆ (easeOut) — ส่วนที่น่ากลัวคือมันยังไหลอยู่
                float grow = 1f - Mathf.Pow(1f - progress, 2f);
                pool.gameObject.SetActive(true);
                pool.position = style == DeathStyle.Crush ? floorBody : floorHead;
                pool.localScale = new Vector3(poolSize * grow, 0.004f, poolSize * 0.8f * grow);
                pool.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);

                int drops = style == DeathStyle.Crush ? 12 : 8;
                for (int i = 0; i < drops; i++)
                {
                    var d = splatter[i];
                    float a = R(0f, Mathf.PI * 2f), dist = R(0.3f, poolSize * 0.9f);
                    d.gameObject.SetActive(progress > 0.05f);
                    d.position = floorHead + new Vector3(Mathf.Cos(a) * dist, 0.002f, Mathf.Sin(a) * dist);
                    float size = R(0.05f, 0.18f) * Mathf.Clamp01(progress * 3f);
                    d.localScale = new Vector3(size, 0.004f, size * R(0.6f, 1.8f));
                    d.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                }
            }

            if (style == DeathStyle.Burn || style == DeathStyle.Electric)
            {
                for (int i = 0; i < smoke.Count; i++)
                {
                    var s = smoke[i];
                    s.gameObject.SetActive(true);
                    float rise = progress * R(0.8f, 1.8f);
                    s.gameObject.SetActive(i < 4);
                    s.position = body + new Vector3(R(-0.7f, 0.7f), 0.9f + rise, R(-0.7f, 0.7f));
                    s.localScale = Vector3.one * R(0.12f, 0.24f) * (0.5f + progress);
                }
            }
            if (style == DeathStyle.Burn)
            {
                foreach (var e in embers)
                {
                    e.gameObject.SetActive(true);
                    e.position = body + new Vector3(R(-0.6f, 0.6f), R(0.05f, 0.3f), R(-0.6f, 0.6f));
                }
                fireLight.gameObject.SetActive(true);
                fireLight.transform.position = body + Vector3.up * 0.5f;
            }
            if (style == DeathStyle.Electric)
            {
                foreach (var sp in sparks)
                {
                    sp.gameObject.SetActive(progress < 0.6f);
                    sp.position = body + new Vector3(R(-0.5f, 0.5f), R(0.1f, 0.8f), R(-0.5f, 0.5f));
                }
                sparkLight.gameObject.SetActive(progress < 0.6f);
                sparkLight.transform.position = body + Vector3.up * 0.6f;
            }
            if (style == DeathStyle.Glass)
            {
                foreach (var sh in shards)
                {
                    sh.gameObject.SetActive(true);
                    sh.position = floorBody + new Vector3(R(-1.2f, 1.2f), 0.02f, R(-1.2f, 1.2f));
                    sh.localScale = new Vector3(R(0.06f, 0.25f), 0.01f, R(0.03f, 0.12f));
                    sh.rotation = Quaternion.Euler(0f, R(0f, 360f), R(-8f, 8f));
                }
            }
            if (style == DeathStyle.Crush)
            {
                // ชั้นล้มลงมาในช่วงแรก แล้วทับนิ่ง
                float drop = Mathf.Clamp01(progress / 0.2f);
                shelf.gameObject.SetActive(true);
                shelf.position = floorBody + new Vector3(0f, Mathf.Lerp(2.2f, 0.3f, drop * drop), 0f);
                shelf.rotation = Quaternion.Euler(0f, actor.eulerAngles.y + 90f, Mathf.Lerp(80f, 4f, drop));
                for (int i = 0; i < boxes.Count; i++)
                {
                    var b = boxes[i];
                    b.gameObject.SetActive(drop >= 1f);
                    b.position = floorBody + new Vector3(R(-1.4f, 1.4f), 0.2f, R(-1.2f, 1.2f));
                    b.rotation = Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-15f, 15f));
                }
            }
        }

        void Update()
        {
            if (!visible) return;
            float t = Time.time;
            if (style == DeathStyle.Burn)
            {
                fireLight.intensity = Mathf.Lerp(1.5f, 5f, Mathf.PerlinNoise(t * 6f, 0.3f)) * (1.2f - progress * 0.6f);
                foreach (var s in smoke) s.position += Vector3.up * 0.15f * Time.deltaTime;
            }
            if (style == DeathStyle.Electric && sparkLight.gameObject.activeSelf)
                sparkLight.intensity = Random.value > 0.5f ? Random.Range(4f, 14f) : 0f;
        }
    }
}
