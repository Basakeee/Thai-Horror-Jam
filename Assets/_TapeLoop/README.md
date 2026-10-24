# 0047 / Tape Loop — Prototype

เปิด `Assets/Scenes/TapeLoop.unity` แล้วกด Play (เอกสาร: `0047-tape-loop-gdd.md`, `0047-level-design.md`)

## ปุ่ม (โต๊ะตัดต่อ)
| ปุ่ม | ทำอะไร |
|---|---|
| A / D | เลือกชิ้นเทป |
| ← / → (Shift เร็ว) | เลื่อนหัวอ่าน (scrub) — ภาพข้างบนโชว์เหตุการณ์ ณ จุดนั้น |
| C | ตัดที่หัวอ่าน |
| Q / E | ย้ายชิ้นซ้าย/ขวา |
| R | กลับด้าน (ม้วน 3 ขึ้นไป) |
| X | ทิ้งลงกองข้างเครื่อง (คลิกในกองเพื่อเอากลับ) |
| Space | เล่นเทป |

## ปรับแต่ง
- **ม้วนเทป** = `Assets/_TapeLoop/Rolls/Roll1–7.asset` — ชิ้นเทป, เหตุการณ์ (เวลา/เงื่อนไข/ผล), ตำแหน่งตัวละคร, งบเทป, ลายมือใบ้ แก้ใน Inspector ได้หมด
  - เมนู `Tools/Tape Loop/Reset Rolls To Defaults` = เขียนทับด้วยค่าจาก `TapeLevels.cs`
- **บ้าน** = `House` ใน scene — prop ที่ชื่อ `flag:X` / `!flag:X` / `item:X` / `!item:X` โชว์ตามสถานะ (เพิ่มของเองได้)
- **TapeGame** (Inspector) = กล้อง, ความเร็วเล่น, ภาพ VHS ขาวดำ, แพ้กี่ครั้งได้ใบ้, เริ่มม้วนไหน (ทดสอบ)
- `Tools/Tape Loop/Bake Scene` = สร้างบ้านใหม่จากโค้ด (บ้านเดิมเก็บเป็น backup)

## กฎของเทป
- ชิ้นเทป = ลำดับ **เหตุการณ์ที่มีเงื่อนไข** — ของในมือ + flag ของบ้านข้ามรอยต่อได้ ตำแหน่งข้ามไม่ได้
- ชิ้นที่ติ๊ก `happensOffscreen` ถ้าไม่อยู่ในเทป **ยังเกิดนอกจอ** ตามเวลา (ตัดทิ้ง ≠ ไม่เกิด)
- Reverse = ยกเลิกผลของเหตุการณ์ในชิ้นนั้น (flag / ของ)

## ทดสอบ
- ตอนเปิดใน Editor: สร้าง `Library/TapeLoopTest.request` → ตรวจทุกม้วน + เล่นทั้งเกมใน Play mode + ภาพหน้าจอ → `Library/TapeLoopTest/`
- ตอนโปรเจกต์ปิดอยู่: `Unity.exe -batchmode -projectPath <proj> -executeMethod TapeLoop.EditorTools.TapeLoopHarness.BatchRun [-noBake]`
