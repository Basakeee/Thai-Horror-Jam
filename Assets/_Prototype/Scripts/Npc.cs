using UnityEngine;
using UnityEngine.UI;

namespace HorrorJam
{
    // NPC แบบ Majora: ไม่มี AI แค่เดินไปตามจุดที่ LoopGame สั่งตามเวลา -> ทุกลูปทำเหมือนเดิมเป๊ะ
    public class Npc : MonoBehaviour
    {
        public string displayName;
        public Text nameText;
        public Text lineText;
        [HideInInspector] public Transform listener;   // ผู้เล่น — ใช้เช็คว่าได้ยินไหม

        public string Line { get; private set; }
        public float HearRadius { get; private set; }
        float lineUntil;

        Vector3 target;
        float speed;
        bool moving;

        public bool Speaking => Line != null && Time.time < lineUntil;
        public bool Visible => gameObject.activeSelf;

        public bool PlayerHears
        {
            get
            {
                if (HearRadius <= 0f || listener == null) return true;
                Vector3 d = listener.position - transform.position;
                d.y = 0f;
                return d.magnitude < HearRadius;
            }
        }

        // hearRadius = 0 คือได้ยินทั้งซอย, > 0 คือต้องเข้าไปใกล้ถึงจะได้ยิน (แอบฟัง)
        public void Say(string text, float seconds = 3.5f, float hearRadius = 0f)
        {
            Line = text;
            lineUntil = Time.time + seconds;
            HearRadius = hearRadius;
        }

        Quaternion? arriveRotation;

        // arriveYaw = หันหน้าทางไหนเมื่อเดินถึง (เช่นหมอดูกลับมานั่งต้องหันออกซอย เพราะสายตาเขาคือกลไกเกม)
        public void WalkTo(Vector3 position, float walkSpeed = 1.6f, float? arriveYaw = null)
        {
            target = position;
            speed = walkSpeed;
            moving = true;
            arriveRotation = arriveYaw.HasValue ? Quaternion.Euler(0f, arriveYaw.Value, 0f) : null;
        }

        public void Place(Vector3 position)
        {
            transform.position = position;
            moving = false;
        }

        public void Show(bool visible) => gameObject.SetActive(visible);

        // ย้อนเวลา: ล้างคำพูด/การเดินค้าง (ตำแหน่ง/การเปิดปิด LoopGame คืนค่าให้เอง)
        public void ResetState()
        {
            Line = null;
            lineUntil = 0f;
            moving = false;
            arriveRotation = null;
            if (lineText != null) lineText.text = "";
        }

        void Update()
        {
            if (lineText != null)
                lineText.text = Speaking && PlayerHears ? Line : "";

            if (!moving) return;
            Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            Vector3 dir = next - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
            transform.position = next;
            if ((transform.position - target).sqrMagnitude < 0.0001f)
            {
                moving = false;
                if (arriveRotation.HasValue) transform.rotation = arriveRotation.Value;
            }
        }
    }
}
