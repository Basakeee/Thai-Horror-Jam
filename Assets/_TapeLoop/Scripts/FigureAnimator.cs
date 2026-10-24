using UnityEngine;

namespace TapeLoop
{
    // แอนิเมชันแบบ procedural (ไม่มี rig/clip) — ดูความเร็วที่ตัวละครถูกย้ายจริง แล้วแกว่งแขนขาเอง
    // TapeGame แค่วางตำแหน่ง + ตั้งท่า (pose) ตามชิ้นเทป · ตอนตาย TapeGame ปิดตัวนี้แล้วจัดท่าศพเอง
    public class FigureAnimator : MonoBehaviour
    {
        public enum Pose
        {
            Auto,   // เดิน/ยืน ตามความเร็ว
            Hold,   // มือขวายื่นถือของ (ไฟฉาย/ถังดับเพลิง/แท็บเล็ต)
            Sit,    // นั่งโซฟา
            Sleep,  // นั่งหลับ หัวพับ
            Bath,   // แช่อ่าง แขนพาดขอบ
            Still,
        }

        [Tooltip("ท่าปัจจุบัน — TapeGame ตั้งให้ตามชิ้นเทป")]
        public Pose pose = Pose.Auto;
        [Tooltip("0..1 เอื้อมมือขวา (หยิบ/วาง/กดปุ่ม) — TapeGame ตั้งตามเหตุการณ์ในเทป")]
        [Range(0f, 1f)] public float reach;
        [Tooltip("0..1 กำลังพูด (พยักหน้า + มือประกอบ)")]
        [Range(0f, 1f)] public float talk;

        [Header("เดิน")]
        public float strideDegrees = 30f;
        public float armSwingDegrees = 26f;
        [Tooltip("ก้าวต่อเมตร")]
        public float stepsPerMeter = 1.5f;
        public float bobHeight = 0.04f;

        [Header("ยืนนิ่ง")]
        public float breathHeight = 0.01f;
        public float breathSpeed = 1.6f;

        [Header("เปลี่ยนท่า")]
        public float blendSharpness = 12f;

        Transform rig, hipL, hipR, torso, shoulderL, shoulderR, neck;
        Vector3 lastPosition;
        float phase, speed, seed;

        void Awake()
        {
            rig = transform.Find(TapeFigure.Rig);
            hipL = transform.Find(TapeFigure.HipL);
            hipR = transform.Find(TapeFigure.HipR);
            torso = transform.Find(TapeFigure.Torso);
            shoulderL = transform.Find(TapeFigure.ShoulderL);
            shoulderR = transform.Find(TapeFigure.ShoulderR);
            neck = transform.Find(TapeFigure.Neck);
            seed = Random.value * 10f;
            lastPosition = transform.position;
        }

        void OnEnable() => Snap();

        // หลังย้ายแบบกระโดด (เปลี่ยนห้อง/ย้อนเวลา) — ไม่ให้นับเป็นความเร็ว
        public void Snap()
        {
            lastPosition = transform.position;
            speed = 0f;
        }

        void LateUpdate()
        {
            if (rig == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float t = Time.time + seed;

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;
            float measured = delta.magnitude / dt;
            if (measured > 12f) measured = 0f;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-10f * dt));
            float walk = Mathf.Clamp01(speed / 1.1f);
            phase += speed * stepsPerMeter * Mathf.PI * dt;
            float s = Mathf.Sin(phase);

            Vector3 legL = new(s * strideDegrees * walk, 0f, 0f);
            Vector3 legR = new(-s * strideDegrees * walk, 0f, 0f);
            Vector3 armL = new(-s * armSwingDegrees * walk, 0f, 5f);
            Vector3 armR = new(s * armSwingDegrees * walk, 0f, -5f);
            Vector3 head = new(Mathf.Sin(t * 0.37f) * 3f, Mathf.Sin(t * 0.23f) * 8f, 0f);   // มองนั่นนี่นิด ๆ
            Vector3 torsoTilt = new(walk * 4f, 0f, 0f);
            float rigY = Mathf.Abs(s) * bobHeight * walk + Mathf.Sin(t * breathSpeed) * breathHeight * (1f - walk);

            switch (pose)
            {
                case Pose.Hold:
                    armR = new Vector3(-70f, 0f, -4f);
                    break;
                case Pose.Sit:
                    legL = legR = new Vector3(-88f, 0f, 0f);
                    armL = new Vector3(-22f, 0f, 10f);
                    armR = new Vector3(-22f, 0f, -10f);
                    rigY = -0.42f + Mathf.Sin(t * breathSpeed) * breathHeight;
                    torsoTilt = new Vector3(-6f, 0f, 0f);
                    break;
                case Pose.Sleep:
                    legL = legR = new Vector3(-85f, 0f, 0f);
                    armL = new Vector3(-12f, 0f, 14f);
                    armR = new Vector3(-12f, 0f, -14f);
                    head = new Vector3(38f, 0f, 16f);
                    rigY = -0.44f + Mathf.Sin(t * 0.9f) * 0.012f;
                    torsoTilt = new Vector3(-14f, 0f, 4f);
                    break;
                case Pose.Bath:
                    legL = legR = new Vector3(-75f, 0f, 0f);
                    armL = new Vector3(0f, 0f, 55f);
                    armR = new Vector3(0f, 0f, -55f);
                    head = new Vector3(-18f, 0f, 0f);
                    rigY = -0.6f;
                    torsoTilt = new Vector3(-20f, 0f, 0f);
                    break;
                case Pose.Still:
                    legL = legR = armL = armR = head = torsoTilt = Vector3.zero;
                    rigY = 0f;
                    break;
            }

            // ท่าทางตามเหตุการณ์ ทับบนท่าหลัก
            if (reach > 0f)
            {
                armR = Vector3.Lerp(armR, new Vector3(-78f, 0f, -14f), reach);
                torsoTilt += new Vector3(14f * reach, 0f, 0f);
                head += new Vector3(18f * reach, 0f, 0f);
            }
            if (talk > 0f)
            {
                head += new Vector3(Mathf.Sin(t * 9f) * 7f * talk, Mathf.Sin(t * 2.3f) * 10f * talk, 0f);
                armL = Vector3.Lerp(armL, new Vector3(-38f + Mathf.Sin(t * 4.5f) * 14f, 0f, 16f), talk * 0.7f);
            }

            float blend = 1f - Mathf.Exp(-blendSharpness * dt);
            Apply(hipL, legL, blend);
            Apply(hipR, legR, blend);
            Apply(shoulderL, armL, blend);
            Apply(shoulderR, armR, blend);
            Apply(neck, head, blend);
            Apply(torso, torsoTilt, blend);
            rig.localPosition = Vector3.Lerp(rig.localPosition, new Vector3(0f, rigY, 0f), blend);
            rig.localRotation = Quaternion.Slerp(rig.localRotation, Quaternion.identity, blend);
        }

        static void Apply(Transform joint, Vector3 euler, float blend)
        {
            if (joint == null) return;
            joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(euler), blend);
        }
    }
}
