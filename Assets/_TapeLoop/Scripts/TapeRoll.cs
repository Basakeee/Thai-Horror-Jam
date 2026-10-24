using System.Collections.Generic;
using UnityEngine;

namespace TapeLoop
{
    // ข้อมูลม้วนเทป 1 ม้วน — ScriptableObject ต้องอยู่ในไฟล์ชื่อเดียวกับคลาส ไม่งั้น asset หาสคริปต์ไม่เจอ (m_Script = 0)
    [CreateAssetMenu(menuName = "Tape Loop/Roll")]
    public class TapeRoll : ScriptableObject
    {
        public int number;
        public string title;
        [Tooltip("ใครตายในม้วนนี้ (ภพ / มิ้นท์ / แม่ / พ่อ / ทุกคน)")]
        public string victim;
        [TextArea] public string intro;

        [Header("เครื่องมือ / งบเทป")]
        public bool allowReverse;
        [Tooltip("0 = ไม่จำกัด")]
        public float maxLength;
        [Tooltip("วินาทีที่เสียต่อการต่อ 1 ครั้ง (Faust)")]
        public float spliceCost;

        [Header("สถานะเริ่มม้วน")]
        public string startFlags;
        public string startItems;

        [Header("ชิ้นเทป (ลำดับในเทปตอนเริ่ม = ลำดับในลิสต์)")]
        public List<TapeSegment> segments = new();

        [Header("ลายมือภพรอบก่อนบนฉลาก (ใบ้ทุก ๆ 3 ครั้งที่แพ้)")]
        public string[] hints = new string[0];
        [TextArea] public string carryOver;
        [Tooltip("ข้อความแทรกครั้งแรกที่เทปผ่านรอยต่อ (clue ปริศนาเทป)")]
        public string spliceCaption;

        [Header("ฉากจบเมื่อผ่านม้วน (ภาพจากกล้องหลายห้อง ต่อกัน)")]
        [Tooltip("ช็อตแรก = คนที่รอด · ช็อตต่อ ๆ ไป = สิ่งที่ทิ้งไว้ ซึ่งจะกลายเป็นความตายม้วนหน้า · ว่าง = ขึ้นแค่ข้อความ carryOver")]
        public List<EndShot> endShots = new();

        [Header("ม้วนพิเศษ")]
        public bool isPeak;
        public bool isFinal;

        public TapeSegment Segment(string id) => segments.Find(s => s.id == id);
    }

    [System.Serializable]
    public class EndShot
    {
        [Tooltip("ห้อง (ชื่อใน House/Rooms)")]
        public string room = "CAM 08";
        [Tooltip("flag ของบ้านในช็อตนี้ (คั่นด้วย , ) — ใส่ lights ถ้าไฟติด")]
        public string flags = "lights";
        [Tooltip("ของในมือภพ (คั่นด้วย , )")]
        public string items;
        public List<ShotActor> actors = new();
        [TextArea] public string caption;
        public float seconds = 3.5f;
        [Tooltip("ภาพกระตุกตอนตัดเข้าช็อตนี้")]
        public bool glitch;
    }

    [System.Serializable]
    public class ShotActor
    {
        public string actor;
        [Tooltip("(x, z) เมตรจากกลางห้อง")]
        public Vector2 position;
        [Tooltip("หันหน้า (องศา) — 0 = ขึ้นบนจอ, 180 = หันเข้ากล้อง")]
        public float yaw;
        public FigureAnimator.Pose pose;

        public ShotActor() { }

        public ShotActor(string actor, float x, float z, float yaw, FigureAnimator.Pose pose = FigureAnimator.Pose.Auto)
        {
            this.actor = actor;
            position = new Vector2(x, z);
            this.yaw = yaw;
            this.pose = pose;
        }
    }
}
