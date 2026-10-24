using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeLoop
{
    // ข้อมูลม้วนเทป — ทุกอย่างแก้ได้ใน Inspector (Assets/_TapeLoop/Rolls/RollN.asset)
    // ชิ้นเทปไม่ใช่วิดีโอตายตัว: เป็นลำดับ "เหตุการณ์ที่มีเงื่อนไข" เล่นซ้ำจาก state ปัจจุบัน
    // → ตัดต่อแล้วผลเปลี่ยน เพราะ state (ของในมือ / flag ของบ้าน) ที่พกข้ามรอยต่อเปลี่ยน

    public enum EffectType
    {
        Say,        // แค่ขึ้นคำบรรยาย
        SetFlag,    // ตั้ง flag บ้าน (เช่น fire, doorLocked)
        ClearFlag,
        GiveItem,   // ภพได้ของ
        TakeItem,   // ภพเสียของ
        Kill,       // value = ใครตาย
        Win,        // ถึง END — เหยื่อรอด
    }

    // ท่าศพ + ร่องรอยที่เหลือ (GoreRig) — Auto = เดาจากคำบรรยาย
    public enum DeathStyle { Auto, Fall, Burn, Electric, Crush, Glass, Peaceful }

    public static class DeathStyles
    {
        public static DeathStyle Resolve(TapeEvent e)
        {
            if (e == null) return DeathStyle.Fall;
            if (e.deathStyle != DeathStyle.Auto) return e.deathStyle;
            string c = e.caption ?? "";
            if (c.Contains("กระจก")) return DeathStyle.Glass;
            if (c.Contains("ทับ")) return DeathStyle.Crush;
            if (c.Contains("ดูด") || c.Contains("แรงสูง")) return DeathStyle.Electric;
            if (c.Contains("ตก")) return DeathStyle.Fall;
            if (c.Contains("ไฟ")) return DeathStyle.Burn;
            return DeathStyle.Fall;
        }
    }

    [Serializable]
    public class TapeEvent
    {
        [Tooltip("วินาทีในชิ้นเทป")]
        public float time;
        public EffectType effect;
        [Tooltip("ชื่อ flag / ของ / คนตาย (ตาม effect)")]
        public string value;
        [Tooltip("คำบรรยายที่ขึ้นตอนเหตุการณ์เกิด")]
        public string caption;

        [Header("เงื่อนไข (คั่นด้วย , )")]
        public string requireFlags;
        public string forbidFlags;
        public string requireItems;
        public string forbidItems;

        [Tooltip("Reverse ชิ้นนี้แล้วผลถูกยกเลิก (flag/ของ)")]
        public bool reversible = true;
        [Tooltip("เกิดเฉพาะตอนเล่นถอยหลัง (เช่น เสียงพูดกลับด้าน)")]
        public bool onlyWhenReversed;
        [Tooltip("Kill: ท่าศพ + ร่องรอย (Auto = เดาจากคำบรรยาย)")]
        public DeathStyle deathStyle;
        [Tooltip("Kill: flag ที่เปลี่ยนตอนตาย ให้ห้องตรงกับคำบรรยาย (เช่น shelfFallen, -phoneOnShelf) — ขึ้นต้น - = ล้าง")]
        public string deathFlags;
        [Tooltip("Kill: ตกลงไปห้องอื่น → กล้องตัดไปห้องนี้ เห็นศพที่จุดตก")]
        public string landRoom;
        public Vector2 landPosition;
        [Tooltip("Kill: ร่วงหลุดเฟรม ไม่เห็นศพ (เหลือแต่ร่องรอย)")]
        public bool fallsOut;

        public TapeEvent() { }

        public TapeEvent(float time, EffectType effect, string value, string caption)
        {
            this.time = time;
            this.effect = effect;
            this.value = value;
            this.caption = caption;
        }
    }

    [Serializable]
    public struct ActorKey
    {
        public float time;
        [Tooltip("ตำแหน่งในห้อง (x, z) เมตร จากกลางห้อง")]
        public Vector2 position;

        public ActorKey(float time, float x, float z)
        {
            this.time = time;
            position = new Vector2(x, z);
        }
    }

    [Serializable]
    public class ActorTrack
    {
        [Tooltip("ชื่อ object ใน House/Actors")]
        public string actor;
        [Tooltip("ท่าระหว่างชิ้นนี้ (Auto = เดาจากตำแหน่ง/ของในมือ)")]
        public FigureAnimator.Pose pose;
        public List<ActorKey> keys = new();
    }

    [Serializable]
    public class TapeSegment
    {
        [Tooltip("ตัวอักษรชิ้น (A, B, C…)")]
        public string id;
        public string title;
        [Tooltip("ชื่อห้องใน House/Rooms (เช่น CAM 04)")]
        public string room;
        [Tooltip("เวลาบนจอ เช่น 23:40:20 — ว่าง = ไม่ใช่ภาพกล้อง (ความทรงจำ)")]
        public string clock;
        public float length = 8f;
        [Tooltip("ถ้าไม่อยู่ในเทป ก็ยังเกิด 'นอกจอ' ตามเวลา (ตัดทิ้ง ≠ ไม่เกิด)")]
        public bool happensOffscreen;
        [Tooltip("ภาพสี (ความทรงจำ)")]
        public bool memory;
        public List<ActorTrack> actors = new();
        public List<TapeEvent> events = new();

        // วินาทีนับจากเที่ยงคืน (00:15 = 900) — ใช้เรียงเหตุการณ์นอกจอ
        public float ClockSeconds
        {
            get
            {
                if (string.IsNullOrEmpty(clock)) return -1f;
                var p = clock.Split(':');
                float h = float.Parse(p[0]), m = float.Parse(p[1]), s = p.Length > 2 ? float.Parse(p[2]) : 0f;
                if (h < 12f) h += 24f;   // หลังเที่ยงคืน ต่อจาก 23:xx
                return h * 3600f + m * 60f + s;
            }
        }

        public string ClockAt(float t)
        {
            if (string.IsNullOrEmpty(clock)) return "";
            float total = ClockSeconds + t;
            int h = (int)(total / 3600f) % 24, m = (int)(total / 60f) % 60, s = (int)total % 60;
            return $"{h:00}:{m:00}:{s:00}";
        }
    }

    [Serializable]
    public class PieceSpec
    {
        public string segment;
        public float start;
        [Tooltip("-1 = ถึงท้ายชิ้น")]
        public float end = -1f;
        public bool reversed;
    }

    // helper แยก "a, b ,c" → ["a","b","c"]
    public static class Csv
    {
        public static IEnumerable<string> Split(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            foreach (var part in text.Split(','))
            {
                var p = part.Trim();
                if (p.Length > 0) yield return p;
            }
        }
    }
}
