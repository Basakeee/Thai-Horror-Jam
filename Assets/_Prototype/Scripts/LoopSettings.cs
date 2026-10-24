using System;
using UnityEngine;

namespace HorrorJam
{
    // ค่าจูนทั้งหมดของ LoopGame — แก้ใน Inspector ได้เลย ไม่ต้องแตะโค้ด
    // (ค่าตั้งต้นในไฟล์นี้ = ค่าที่เทสต์ผ่านแล้ว 2026-10-24)

    [Serializable]
    public class LoopTimings
    {
        [Tooltip("ความยาวลูป (วินาที) — 240 = 17:56 → 18:00")]
        public float loopLength = 240f;
        [Tooltip("กด Tab ค้าง = เร่งเวลากี่เท่า")]
        public float fastForward = 4f;

        [Header("ป้าศรี")]
        public float auntWalkHome = 2f;
        public float auntGoesInside = 8f;
        public float auntOnRoof = 45f;
        [Tooltip("ช่วงที่ป้าพึมพำบนดาดฟ้า (แอบฟังชื่อเอ๋)")]
        public float murmurStart = 60f;
        public float murmurEnd = 86f;
        public float murmurLineSeconds = 6.5f;
        public float potOmen = 88f;
        public float potFall = 90f;

        [Header("ต้น")]
        public float childWanderEvery = 3f;
        public float childWanderUntil = 134f;
        public float childToKickSpot = 136f;
        public float childKick = 140f;

        [Header("ลุงชม + สายไฟ")]
        public float uncleArrive = 92f;
        public float ladderUp = 100f;
        public float wireOmen = 155f;
        public float wireDrop = 160f;

        [Header("หมอดู")]
        public float phoneStart = 100f;
        public float phoneEnd = 113f;
        public float phoneLineSeconds = 4.3f;
        public float fortuneLeaves = 120f;
        [Tooltip("หายเข้าห้องน้ำ = ช่วงเดียวที่แตะของบนโต๊ะได้")]
        public float fortuneAway = 123f;
        public float fortuneReturns = 137f;

        [Header("มอเตอร์ไซค์")]
        public float bikeOmen = 187f;
        public float bikeRide = 190f;
        public float bikeDuration = 3f;

        [Header("ความเร็วเดิน (ม./วิ)")]
        public float auntWalkSpeed = 1.2f;
        public float childWanderSpeed = 1.4f;
        public float childRunSpeed = 2.2f;
        public float childChaseBallSpeed = 4f;
        public float uncleWalkSpeed = 1.4f;
        public float fortuneWalkSpeed = 1.8f;
        public float ghostWalkSpeed = 1.6f;
    }

    [Serializable]
    public class SenseSettings
    {
        [Tooltip("ระยะที่หมอดูมองเห็น (ม.)")]
        public float fortuneSightRange = 4.5f;
        [Tooltip("ครึ่งมุมกรวยสายตาหมอดู (องศา) — 50 = มองกว้าง 100°")]
        public float fortuneSightHalfAngle = 50f;
        public float auntMurmurHearRadius = 3.5f;
        public float phoneHearRadius = 3.8f;
        public float uncleComplaintRadius = 5f;
        [Tooltip("ยืนห่างจุดกระถางตกน้อยกว่านี้ = ตาย")]
        public float potHitRadius = 1.4f;
        public float bikeHitHalfWidth = 0.85f;
        public float bikeHitHalfLength = 1f;
        public float corpseLabelRadius = 2.2f;
    }

    [Serializable]
    public class CameraSettings
    {
        [Tooltip("ตำแหน่งกล้องเทียบกับจุดโฟกัส — y สูงขึ้น/z ใกล้ 0 = มุมตั้งฉากขึ้น (Twelve Minutes)")]
        public Vector3 offset = new(0f, 14f, -3.8f);
        public float fieldOfView = 45f;
        [Tooltip("ยิ่งสูง กล้องยิ่งตามไว")]
        public float followSharpness = 4f;
        [Tooltip("ล็อกกล้องไว้กลางซอยแกน x — เห็นกำแพงสองฝั่งเสมอ")]
        public bool lockToAlleyCenter = true;
        public float minZ = 3.5f;
        public float maxZ = 30f;
        [Tooltip("ระยะถอยกล้องตอนจุดพีค (ทุก chain พร้อมกัน)")]
        public float peakWide = 1.6f;
        public float endingWide = 1.2f;
    }

    [Serializable]
    public class MoodSettings
    {
        [Tooltip("ตายกี่ครั้งถึงหม่นสุด")]
        public int deathsForFullDoom = 8;

        [Header("แสงรอบข้าง / หมอก (ตอนเริ่ม → ตอนหม่นสุด)")]
        public Color ambient = new(0.42f, 0.4f, 0.42f);
        public Color ambientDoom = new(0.3f, 0.2f, 0.2f);
        public Color fogColor = new(0.05f, 0.05f, 0.08f);
        public Color fogColorDoom = new(0.18f, 0.03f, 0.03f);
        public float fogDensity = 0.008f;
        public float fogDensityDoom = 0.03f;

        [Header("Post FX — บวกเพิ่มจากค่าใน HorrorPostFX.asset ตามความหม่น")]
        public float vignetteDoomAdd = 0.2f;
        public Color vignetteDoomColor = new(0.35f, 0f, 0f);
        public float saturationDoomAdd = -27f;
        public float grainDoomAdd = 0.22f;

        [Header("หลังเผาด้ายดำ (ยอมรับ)")]
        public Color acceptAmbient = new(0.32f, 0.3f, 0.3f);
        public float acceptFogDensity = 0.004f;
        public float acceptVignette = 0.15f;
        public float acceptSaturation = 12f;
        public float acceptExposureAdd = 0.25f;
        public float acceptFadeSeconds = 3f;
    }

    [Serializable]
    public class DialogueSettings
    {
        [TextArea] public string intro = "อาจารย์เพียรผูกด้ายดำที่ข้อมือภพ\n<size=32>\"ผูกไว้เสริมดวง... เพราะดวงมึงถึงฆาต ไม่พ้นหกโมงเย็นวันนี้\"</size>";

        [Tooltip("ป้าพึมพำบนดาดฟ้า — ประโยคที่ 3 (index 2) คือประโยคที่มีชื่อ เมื่อได้ยิน = จำชื่อเอ๋ได้")]
        public string[] auntMurmurs =
        {
            "ฮือ... ต้นไม้ของลูกแม่ใกล้ตายแล้ว",
            "แม่ย้ายกระถางมาไว้ริมขอบ ให้โดนแดดนะลูก",
            "เอ๋เอ๊ย... เอ๋ลูกแม่...",
            "ถ้าเอ๋เรียกแม่สักคำ แม่จะวิ่งลงไปหาเลย",
        };
        [Tooltip("หมอดูคุยโทรศัพท์ — ได้ยินประโยคสุดท้าย = รู้ว่าหุ่นอยู่ในกล่อง")]
        public string[] phoneLines =
        {
            "หมอดู: ฮัลโหล... อีกสองนาทีกว่าก็ครบแล้ว",
            "หมอดู: ดวงเด็กคนนี้แรงดี ต่ออายุกูได้อีกยี่สิบปี",
            "หมอดู: หุ่นอยู่ในกล่องใต้โต๊ะ รหัสก็วันเดิม... วันที่กูได้ดวงแรก",
        };
        [Tooltip("คุยกับป้าหลังเรียกลงมา — ครั้งที่ 1 = วันตายเอ๋, ครั้งที่ 2 = วิธีเผา")]
        public string[] auntStory =
        {
            "ป้าศรี: เอ๋ก็ไปให้ไอ้เพียรมันดูดวง... แล้วก็ตาย ๒๓ ตุลา ปีที่แล้ว หกโมงเย็นเป๊ะ",
            "ป้าศรี: พระที่วัดบอกป้าว่า ของพวกนี้แพ้ไฟศาลพระภูมิ ต้องเผาต่อหน้ามัน",
            "ป้าศรี: ...แต่บางทีคนเราก็หนีดวงตัวเองไม่พ้นหรอกนะลูก",
            "ป้าศรี: ระวังตัวด้วยนะลูก",
        };
        [Tooltip("จุดพีค — บรรทัดที่ 2 กล้องตัดไปที่ด้ายดำ")]
        public string[] peakReveal =
        {
            "หมอดู: หุ่นนั่นกูทำไว้ล่อ... จะเขียนชื่อใครก็เผาไปเถอะ",
            "หมอดู: ของจริงน่ะ อยู่ที่ข้อมือมึง",
            "หมอดู: ทุกครั้งที่มึงหนีความตาย ดวงมึงไหลมาหากู... แล้วคนอื่นก็ตายแทนมึง",
            "หมอดู: มึงคิดว่ามึงช่วยใครได้เหรอ?",
        };
        public float peakLineSeconds = 3.4f;

        [Header("สาเหตุตายตอน 18:00")]
        public string childDeath = "วิ่งตามลูกบอลออกปากซอย โดนรถกระบะชน";
        public string auntDeath = "ตกจากดาดฟ้าบ้านตัวเอง";
        public string uncleDeath = "ปีนบันไดใหม่ขึ้นไปต่อสาย โดนไฟแรงสูงดูด";

        [TextArea] public string peakTitle = "ช่วยใครไม่ได้เลย...\n<size=30>ทุกครั้งที่ภพหนีความตาย คนอื่นต้องจ่ายแทน</size>";
        [TextArea] public string endingTitle = "ภพยอมรับดวงของตัวเอง\n<size=30>คนในซอยรอดทุกคน — ดวงที่ถูกขโมยไปกลับคืนเจ้าของ</size>\n\nดวงถึงฆาต";
    }
}
