using UnityEngine;

namespace HorrorJam
{
    // ป้ายชื่อ/คำพูดลอยเหนือหัว หันเข้ากล้องเสมอ
    // กล้องมองเกือบตั้งฉาก → ป้ายที่อยู่ "เหนือหัว" ตามแกน y จะทับตัวละครเอง
    // เลยวางป้ายด้วย world offset (+z = ขึ้นบนจอ) โดยไม่สนการหมุน/สเกลของตัวละคร
    public class FaceCamera : MonoBehaviour
    {
        public bool useWorldOffset;
        public Vector3 worldOffset;

        void LateUpdate()
        {
            if (useWorldOffset && transform.parent != null)
                transform.position = transform.parent.position + worldOffset;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
