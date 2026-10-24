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

        [Header("ม้วนพิเศษ")]
        public bool isPeak;
        public bool isFinal;

        public TapeSegment Segment(string id) => segments.Find(s => s.id == id);
    }
}
