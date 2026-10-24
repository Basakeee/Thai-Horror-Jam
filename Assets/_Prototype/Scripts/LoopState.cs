using System.Collections.Generic;
using UnityEngine;

namespace HorrorJam
{
    // ความรู้ที่ผู้เล่นได้ (เหมือน Twelve Minutes: ของในมือหายทุกลูป แต่สิ่งที่ "รู้" ติดตัวไปด้วย)
    public static class Facts
    {
        // องก์ 2: สืบเรื่องหมอดู (ทุกข้อมี 2 แหล่ง)
        public const string SonName = "son_name";       // ผ้าศาล / แอบฟังป้าบนดาดฟ้า
        public const string DeathDate = "death_date";   // ผ้าศาล (เลขไทย) / ป้าศรีเล่า → รหัสกล่อง 2310
        public const string DeathList = "death_list";   // สมุดดวง
        public const string BurnMethod = "burn_method"; // สมุดดวงหน้าสุดท้าย / ป้าศรีเล่า
        public const string RealName = "real_name";     // ลุงชม / บิลค่าไฟ
        public const string DollInBox = "doll_in_box";  // แอบฟังหมอดูคุยโทรศัพท์
        // องก์ 3: จุดพีค
        public const string Truth = "truth";

        public static string Describe(string fact) => fact switch
        {
            SonName => "ลูกชายป้าศรีชื่อ 'เอ๋' (เสียไปแล้ว)",
            DeathDate => "เอ๋ตาย ๒๓ ต.ค. ปีที่แล้ว เวลา ๑๘.๐๐ — ตรงกับวันนี้พอดี",
            DeathList => "สมุดดวง: 1.ภพ (เธอ)  2.ต้น  3.ป้าศรี  4.ลุงชม — หน้าแรกมีชื่อ 'เอ๋' ขีดฆ่าไว้",
            BurnMethod => "หุ่นคุณไสยแพ้ไฟศาลพระภูมิ — ต้องเผาต่อหน้าเจ้าของหุ่น",
            RealName => "ชื่อจริงอาจารย์เพียร: นายสมพงษ์ แสงทอง",
            DollInBox => "หมอดูซ่อนหุ่นไว้ในกล่องใต้โต๊ะ รหัสคือ 'วันที่ได้ดวงแรก'",
            Truth => "ด้ายดำที่ข้อมือ = สิ่งที่ทำให้ภพหนีความตาย\n   ทุกครั้งที่หนี ดวงไหลไปหาหมอดู และคนอื่นต้องตายแทน",
            _ => fact,
        };
    }

    // ทุกอย่างที่ต้องรอดข้ามลูปอยู่ที่นี่ (static = ไม่โดนทำลายตอน LoopGame สร้างโลกใหม่)
    public static class LoopState
    {
        public struct Corpse
        {
            public Vector3 position;
            public string cause;
            public int loop;
        }

        public struct Death
        {
            public int loop;
            public string who;
            public string cause;
        }

        public static int Loop = 1;
        public static readonly List<Corpse> Corpses = new();
        public static readonly List<Death> Deaths = new();   // ใครตายเพราะอะไร ทุกลูป (รวม NPC ตอน 18:00)
        public static readonly List<string> Known = new();

        public static void RecordDeath(string who, string cause) =>
            Deaths.Add(new Death { loop = Loop, who = who, cause = cause });

        public static bool Knows(string fact) => Known.Contains(fact);

        public static bool Learn(string fact)
        {
            if (Known.Contains(fact)) return false;
            Known.Add(fact);
            return true;
        }

        public static void Clear()
        {
            Loop = 1;
            Corpses.Clear();
            Deaths.Clear();
            Known.Clear();
        }

        // ปิด Domain Reload ไว้ก็ยังเริ่ม Play แบบสดทุกครั้ง
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Clear();
    }
}
