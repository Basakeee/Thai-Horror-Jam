using UnityEngine;

namespace TapeLoop
{
    // หุ่นยนต์ดูดฝุ่น: แปรงหมุน (เร็วตอนวิ่ง) + ไฟสถานะกะพริบ
    public class VacuumAnimator : MonoBehaviour
    {
        public float spinDegreesPerSecond = 540f;
        public float blinkPerSecond = 1.5f;

        Transform brush;
        Renderer led;
        Vector3 last;

        void Awake()
        {
            brush = transform.Find("Brush");
            led = transform.Find("Status Light")?.GetComponent<Renderer>();
            last = transform.position;
        }

        void LateUpdate()
        {
            bool moving = (transform.position - last).sqrMagnitude > 0.000001f;
            last = transform.position;
            if (brush != null) brush.Rotate(0f, spinDegreesPerSecond * (moving ? 1f : 0.15f) * Time.deltaTime, 0f, Space.Self);
            if (led != null) led.enabled = Mathf.Repeat(Time.time * blinkPerSecond, 1f) < 0.55f;
        }
    }

    // เงา: หายใจช้า ๆ + หัวกระตุกเป็นจังหวะ (ไม่เหมือนคน)
    public class ShadowAnimator : MonoBehaviour
    {
        public float breathe = 0.025f;
        [Tooltip("กระตุกหัวทุก ๆ กี่วินาที (สุ่มรอบค่านี้)")]
        public float twitchEvery = 2.5f;

        Transform torso, neck;
        float nextTwitch;
        Quaternion twitch = Quaternion.identity;

        void Awake()
        {
            torso = transform.Find(TapeFigure.Torso);
            neck = transform.Find(TapeFigure.Neck);
        }

        void LateUpdate()
        {
            float t = Time.time;
            if (torso != null) torso.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.1f) * breathe, 1f);
            if (neck == null) return;
            if (t > nextTwitch)
            {
                nextTwitch = t + twitchEvery * Random.Range(0.5f, 1.5f);
                twitch = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(-40f, 40f), Random.Range(-30f, 30f));
            }
            // กระตุกเร็ว แล้วค้าง
            neck.localRotation = Quaternion.Slerp(neck.localRotation, twitch, 1f - Mathf.Exp(-25f * Time.deltaTime));
        }
    }
}
