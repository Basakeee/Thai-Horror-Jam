# ดวงถึงฆาต — Prototype

1. เปิด `Assets/Scenes/Prototype.unity`
2. เมนู **Tools > Horror Jam > Bake World To Scene** → ซอยทั้งหมดเป็น GameObject จริงใต้ `World`
3. กด Play

- จัดวาง/เปลี่ยนหน้าตาใน Editor ได้ — แต่ **ห้ามเปลี่ยนชื่อ** object (ดู `Names` ใน `AlleyBuilder.cs`)
- `World/Markers/*` = จุดที่ตาราง NPC ใช้ ลากย้ายได้ ตารางจะตามไปเอง
- ตอน Play: `World` ถูกซ่อนเป็น template แล้ว clone ใหม่ทุกลูป
- ลูป 17:56 → 18:00 (240 วิ, Tab ค้าง = x4), F5 = ล้างความจำ
- ตารางเหตุการณ์ทั้งหมดอยู่ใน `LoopGame.Schedule()`

## Walkthrough (สปอยล์ — ไว้เทสต์)
1. ศาลพระภูมิ: อ่านผ้า → ชื่อ "เอ๋" + วันตาย ๒๓ ต.ค. (= รหัส 2310)
2. ช่วง 1:32–2:20 คีม (ปลายซอย) → ลุงชม → ชื่อจริงหมอดู "สมพงษ์"  (หรืออ่านบิลบนโต๊ะ)
3. ช่วง 2:03–2:17 หมอดูเข้าห้องน้ำ → อ่านสมุด (วิธีเผา) + เปิดกล่อง 2310 → หุ่นฟาง
4. ศาล: เขียนชื่อ "สมพงษ์" บนหุ่น → จุดเทียน (ห้ามวิ่ง) → กลับไปเผาหุ่นต่อหน้าหมอดู = จุดพีค
5. ลูปถัดไป: ศาล → เผาด้ายดำ (กด E 2 ครั้ง) → รอ 18:00 = true ending

## ปรับแต่งใน Inspector
- **LoopGame** → `Timing` (เวลาทุกเหตุการณ์ + ความเร็วเดิน), `Senses` (สายตาหมอดู/ระยะได้ยิน/ระยะโดน), `Camera Settings`, `Mood` (แสง/หมอก/ความหม่นตามการตาย), `Dialogue` (บทพูดหลัก + สาเหตุตาย) — แก้ระหว่าง Play ได้ (ค่าเวลาเห็นผลลูปถัดไป)
- **HorrorPostFX.asset** = ค่าฐานของ post FX — LoopGame แค่บวกความหม่นเพิ่มตามจำนวนครั้งที่ตาย
- **World/Templates** (ปิดไว้) = ของที่ clone ตอนเล่น: `Corpse`, `Sai Sin Band`, `Flash Light`
- **World/Key Lights**, **Street Lights**, ทุก prop/NPC = object จริง ปรับตรง ๆ ได้
- ⚠️ ค่าที่แก้ระหว่าง Play จะหายตอนกด Stop (ปกติของ Unity) — คลิกขวาที่ component → Copy Component แล้ว Paste Component Values หลัง Stop
