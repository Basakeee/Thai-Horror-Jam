using System.Collections.Generic;
using UnityEngine;

namespace TapeLoop
{
    // ข้อมูลตั้งต้นของ 7 ม้วน ตาม 0047-level-design.md
    // Bake (Tools/Tape Loop/Bake) แปลงเป็น Assets/_TapeLoop/Rolls/RollN.asset — หลังจากนั้นแก้ใน Inspector ได้เลย
    // ตำแหน่งตัวละคร = (x, z) เมตรจากกลางห้อง · ห้องกว้าง 8 x 6 (x: -4..4, z: -3..3, z+ = ขึ้นบนจอ)
    public static class TapeLevels
    {
        public const string Phop = "ภพ", Mint = "มิ้นท์", Mom = "แม่", Dad = "พ่อ", Vacuum = "หุ่นยนต์ดูดฝุ่น", Shadow = "เงา";

        public static List<TapeRoll> CreateAll() => new() { Roll1(), Roll2(), Roll3(), Roll4(), Roll5(), Roll6(), Roll7() };

        // ---------------------------------------------------------------- ม้วน 1
        static TapeRoll Roll1()
        {
            var r = NewRoll(1, "บันไดลอย", Phop,
                "เทปเล่นเองกลางดึก… ภาพกล้องเพดานของบ้านหลังนี้ — คืนนี้\nภพเดินลงบันไดตอนไฟดับ แล้วไม่ได้ลุกขึ้นมาอีก");
            r.startFlags = "lights, vacuumStairs";

            var a = Seg(r, "A", "ภพตื่น", "CAM 01", "23:40:00", 6);
            Track(a, Phop, (0, -2.5f, 1f), (2, -2.5f, 1f), (6, 2.5f, -2.5f));
            Say(a, 2, "ภพตื่น — ได้ยินเสียงเทปเล่นเองที่ชั้นล่าง");

            var b = Seg(r, "B", "ทางเดินชั้น 2 → บันได", "CAM 04", "23:40:20", 12);
            Track(b, Phop, (0, -3.5f, -1.5f), (2, -2f, 1.6f), (3, -2f, 1.6f), (6, 0.5f, 0f), (9, 2.4f, 0f), (10, 2.9f, 0f), (11, 3.5f, 0.6f));
            Ev(b, 3, EffectType.GiveItem, "phone", "หยิบโทรศัพท์ของมิ้นท์มาใช้เป็นไฟฉาย");
            Ev(b, 7, EffectType.ClearFlag, "lights", "ไฟทั้งบ้านดับ");
            Ev(b, 10, EffectType.Kill, Phop, "ไฟดับ เหยียบหุ่นยนต์ดูดฝุ่นบนขั้นบันได ตกช่องบันได");

            var c = Seg(r, "C", "ห้องนั่งเล่น", "CAM 08", "23:41:00", 8);
            Track(c, Shadow, (0, -1f, 2.1f), (1.9f, -1f, 2.1f));   // clue: เงานั่งหน้าเครื่องเล่นเทป
            Track(c, Phop, (2, 3f, -2f), (7, -0.6f, 1.2f));
            Ev(c, 0.5f, EffectType.ClearFlag, "lights", "ไฟยังดับทั้งบ้าน");
            Ev(c, 7, EffectType.Win, "", "ภพถือไฟฉายลงมาถึงเครื่องเล่นเทป").requireItems = "phone";
            Ev(c, 7.5f, EffectType.Say, "", "มืดสนิท — ภพมองไม่เห็นทาง ยืนนิ่งอยู่ตรงนั้น").forbidItems = "phone";

            r.hints = new[] { "ไฟจะดับ", "เอาไฟติดตัวไปด้วย", "ตัดหลังหยิบ ก่อนมืด" };
            r.carryOver = "หุ่นยนต์ดูดฝุ่นยังค้างอยู่บนบันได…\nมิ้นท์ตื่นมาในความมืด — โทรศัพท์ของเธอหายไป";
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 2
        static TapeRoll Roll2()
        {
            var r = NewRoll(2, "ห้องมิ้นท์", Mint,
                "ม้วนที่สองเล่นเอง — 00:15\nมิ้นท์หาโทรศัพท์ไม่เจอ ในห้องมีแค่สกู๊ตเตอร์ที่ชาร์จทิ้งไว้");
            r.startFlags = "lights";

            var a = Seg(r, "A", "มิ้นท์ตื่น", "CAM 03", "00:15:00", 10);
            a.happensOffscreen = true;
            Track(a, Mint, (0, -2.5f, 1.2f), (3, -2.5f, 1.2f), (5, 1.6f, -1f), (10, 1.2f, -0.6f));
            Say(a, 2, "มิ้นท์หาโทรศัพท์ไม่เจอ");
            Say(a, 5, "มิ้นท์เปิดไฟหน้าสกู๊ตเตอร์ที่ชาร์จอยู่แทนไฟฉาย");
            Ev(a, 8, EffectType.SetFlag, "smoke", "แบตสกู๊ตเตอร์บวม — มีควัน");
            Ev(a, 9.5f, EffectType.SetFlag, "fire", "ไฟลุก!");
            Ev(a, 9.8f, EffectType.SetFlag, "doorLocked", "smart lock ประตูค้าง — มิ้นท์ออกไม่ได้");

            var b = Seg(r, "B", "หน้าห้องมิ้นท์", "CAM 04", "00:15:30", 8);
            Track(b, Phop, (0, -3.5f, -1.5f), (4, -2f, 1.2f), (8, -2f, 1.2f));
            var unlock = Ev(b, 4, EffectType.ClearFlag, "doorLocked", "ใช้กุญแจฉุกเฉินปลดล็อกประตู");
            unlock.requireItems = "key";
            unlock.requireFlags = "doorLocked";
            var win = Ev(b, 5, EffectType.Win, "", "ฉีดถังดับเพลิง — ไฟดับ มิ้นท์รอด");
            win.requireFlags = "fire";
            win.forbidFlags = "doorLocked";
            win.requireItems = "extinguisher";
            Ev(b, 5.2f, EffectType.Say, "", "ประตูล็อก — ทุบเท่าไหร่ก็ไม่เข้า").requireFlags = "fire, doorLocked";
            var retreat = Ev(b, 5.4f, EffectType.Say, "", "ไฟลามออกทางเดิน — ภพต้องถอย");
            retreat.requireFlags = "fire";
            retreat.forbidFlags = "doorLocked";
            retreat.forbidItems = "extinguisher";
            Ev(b, 8, EffectType.Kill, Mint, "ไฟคลอก — ติดอยู่ในห้องที่ประตูล็อก").requireFlags = "fire";

            var c = Seg(r, "C", "ห้องเก็บของ", "CAM 02", "00:14:00", 6);
            Track(c, Phop, (0, -3.5f, 0f), (3, 0f, 1.6f), (6, 3.5f, 0f));
            Ev(c, 3, EffectType.GiveItem, "extinguisher", "หยิบถังดับเพลิงที่พ่อวางไว้");

            var d = Seg(r, "D", "ครัว", "CAM 09", "00:13:00", 7);
            Track(d, Phop, (0, -3.5f, 0f), (4, 1f, 1.6f), (5.5f, 1f, 1.6f), (7, 3.5f, -1f));
            Ev(d, 4, EffectType.GiveItem, "key", "เปิดลิ้นชัก — หยิบกุญแจฉุกเฉิน smart lock");
            Ev(d, 6, EffectType.TakeItem, "key", "ภพวางกุญแจคืนลิ้นชัก");

            r.hints = new[] { "ประตูมันล็อก", "กุญแจสำรองอยู่ในครัว", "ไปเอาของให้ครบก่อน ไฟจะลุก" };
            r.spliceCaption = "(…เสียงกรรไกรตัดเทปอีกเสียง ดังตามหลังเรามานิดเดียว)";
            r.carryOver = "ภพยื่นโทรศัพท์ของมิ้นท์ให้แม่ — \"แม่เก็บไว้ทีนะ\"\nแม่ตัวเปื้อนเขม่า ไปอาบน้ำ… เอาโทรศัพท์ไปชาร์จบนชั้นเหนืออ่าง";
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 3
        static TapeRoll Roll3()
        {
            var r = NewRoll(3, "ห้องน้ำ", Mom,
                "01:00 — แม่แช่น้ำในอ่าง\nปุ่ม ◄ กลับด้าน ใช้ได้แล้ว");
            r.allowReverse = true;
            r.startFlags = "lights";

            var a = Seg(r, "A", "แม่เข้าห้องน้ำ", "CAM 05", "01:00:00", 7);
            a.happensOffscreen = true;
            Track(a, Mom, (0, -2.5f, -2.2f), (3, 2.2f, 1.2f), (5, 0f, 0f), (7, 0f, 0f));
            Ev(a, 3, EffectType.SetFlag, "phoneOnShelf", "แม่เสียบชาร์จโทรศัพท์บนชั้นเหนืออ่าง");
            Say(a, 6, "แม่ลงแช่น้ำ");

            var b = Seg(r, "B", "ทางเดินชั้น 2", "CAM 04", "01:00:10", 6);
            b.happensOffscreen = true;
            Track(b, Vacuum, (0, -3.2f, -1.6f), (5, 2f, 1.6f), (6, 2f, 1.6f));
            Ev(b, 5, EffectType.SetFlag, "doorHit", "หุ่นยนต์ดูดฝุ่นวิ่งชนประตูห้องน้ำ — ปัง!");
            var reversedVoice = Ev(b, 3, EffectType.Say, "", "(ลำโพงหุ่นยนต์ — เสียงถอยหลัง)  \"…รอบที่สี่สิบเจ็ด\"");
            reversedVoice.onlyWhenReversed = true;

            var c = Seg(r, "C", "ในอ่าง", "CAM 05", "01:00:20", 8);
            Track(c, Mom, (0, 0f, 0f), (5, 0f, 0f), (8, -2.5f, -2.2f));
            Ev(c, 2, EffectType.Say, "", "ชั้นวางสั่น…").requireFlags = "doorHit";
            Ev(c, 3, EffectType.Kill, Mom, "โทรศัพท์กับสายชาร์จตกลงอ่าง — ไฟดูด").requireFlags = "doorHit, phoneOnShelf";
            Ev(c, 7.5f, EffectType.Win, "", "แม่อาบน้ำเสร็จ เดินออกจากห้องน้ำ").forbidFlags = "doorHit";

            r.hints = new[] { "มันไม่ได้เริ่มในห้องน้ำ", "ตัดไปมันก็ยังชน", "ให้มันถอยกลับ" };
            r.carryOver = "หุ่นยนต์ที่ถูกย้อน ถอยเลยแท่นชาร์จ…\nไปค้างอยู่หน้าประตูระเบียงกระจก ไฟสถานะกะพริบแดง";
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 4
        static TapeRoll Roll4()
        {
            var r = NewRoll(4, "ระเบียงกระจก", Dad,
                "01:40 — พ่อยังไม่นอน\nหุ่นยนต์ดูดฝุ่นยังเปิดอยู่ที่ระเบียง");
            r.allowReverse = true;
            r.startFlags = "lights, vacuumOn";

            var a = Seg(r, "A", "ระเบียง", "CAM 06b", "01:30:00", 6);
            a.happensOffscreen = true;
            Track(a, Vacuum, (0, -2.5f, 0f), (4, 1.6f, 0f), (6, 1.6f, 0f));
            Ev(a, 4, EffectType.SetFlag, "crack", "หุ่นยนต์ชนแผงกระจกระเบียง — ร้าว").requireFlags = "vacuumOn";

            var b = Seg(r, "B", "ห้องเก็บของ", "CAM 02", "01:35:00", 6);
            Track(b, Phop, (0, -3.5f, 0f), (3, 1.5f, 2f), (6, 3.5f, 0f));
            Ev(b, 3, EffectType.GiveItem, "tablet", "หยิบแท็บเล็ตคุมบ้านจากชั้น — กองกล่องเอียง");
            Ev(b, 3.1f, EffectType.SetFlag, "shelfTilted", "");

            var c = Seg(r, "C", "นั่งเล่นชั้น 2", "CAM 06", "01:40:00", 7);
            Track(c, Dad, (0, -2f, 0.5f), (4, 2.8f, 0f), (7, 2.8f, 0f));
            var safe = Ev(c, 3, EffectType.Win, "", "ประตูระเบียงล็อก — พ่อกลับมานั่ง");
            safe.requireFlags = "balconyLocked";
            safe.forbidFlags = "crack";
            Ev(c, 4, EffectType.SetFlag, "dadOnBalcony", "พ่อเปิดประตูออกไประเบียง").forbidFlags = "balconyLocked";
            var breakOut = Ev(c, 4.2f, EffectType.SetFlag, "dadOnBalcony", "พ่อเห็นรอยร้าว — ปลดล็อกประตูออกไปดู");
            breakOut.requireFlags = "balconyLocked, crack";

            var d = Seg(r, "D", "ระเบียง", "CAM 06b", "01:40:10", 6);
            d.happensOffscreen = true;
            Track(d, Dad, (0, -2.5f, 0f), (3, 1.2f, 0f), (6, 1.2f, 0f));
            Track(d, Vacuum, (0, -1f, -2f), (2, 1.6f, -0.5f), (6, 1.6f, -0.5f));
            Ev(d, 2, EffectType.SetFlag, "crack", "หุ่นยนต์ยังเปิดอยู่ — ชนกระจกซ้ำ").requireFlags = "vacuumOn";
            Ev(d, 4, EffectType.Kill, Dad, "พิงกระจกที่ร้าว — กระจกแตก ตกลงไปสองชั้น").requireFlags = "dadOnBalcony, crack";

            var e = Seg(r, "E", "นั่งเล่นชั้น 2", "CAM 06", "01:38:00", 5);
            Track(e, Phop, (0, -3.5f, 1f), (3, 0f, 1f), (5, 2f, 1f));
            Ev(e, 3, EffectType.ClearFlag, "vacuumOn", "สั่งปิดหุ่นยนต์ผ่านแท็บเล็ต").requireItems = "tablet";
            Ev(e, 3.2f, EffectType.SetFlag, "balconyLocked", "ล็อกประตูระเบียงผ่านแอป").requireItems = "tablet";

            r.hints = new[] { "กระจกร้าวก่อนพ่อมา", "ย้อนรอยร้าวแล้วมันจะชนอีก", "ปิดมันจากแท็บเล็ต" };
            r.spliceCaption = "(ภาพกระตุก — แวบหนึ่ง ภพตกบันได… จากอีกฝั่งหนึ่ง ไม่ใช่แบบที่เราเคยเห็น)";
            r.carryOver = "ห้องเก็บของ — กองกล่องบนชั้นยังเอียงอยู่\nตั้งแต่ภพดึงแท็บเล็ตออกมา";
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 5
        static TapeRoll Roll5()
        {
            var r = NewRoll(5, "ห้องเก็บของ", Phop,
                "02:30 — ความตายวนกลับมาหาภพ\nเทปม้วนนี้สั้นกว่าทุกม้วน: ยาวได้ 30 วิ และการต่อแต่ละครั้งกินเทป 2 วิ");
            r.allowReverse = true;
            r.maxLength = 30f;
            r.spliceCost = 2f;
            r.startFlags = "lights, shelfTilted";

            var a = Seg(r, "A", "ห้องเก็บของ", "CAM 02", "02:30:00", 8);
            Track(a, Phop, (0, -3.5f, 0f), (3, -1f, 1f), (8, 1f, 1.5f));
            Say(a, 2, "ภพตามหาที่มาของกล่องพัสดุ");

            var b = Seg(r, "B", "ความทรงจำ — วันเกิดมิ้นท์", "MEMORY", "", 12);
            b.memory = true;
            Track(b, Mint, (0, 0f, 0.6f), (12, 0f, 0.6f));
            Track(b, Mom, (0, -1.2f, 1f), (12, -1.2f, 1f));
            Track(b, Dad, (0, 1.2f, 1f), (12, 1.2f, 1f));
            Track(b, Phop, (0, 0.8f, -1f), (12, 0.8f, -1f));
            Say(b, 1, "\"แฮปปี้เบิร์ธเดย์ทู้ยู…\"");
            Say(b, 5, "(เฟรมกระตุก — รอยต่อเก่า… ตัวนับขึ้นว่า 0046)");
            Say(b, 8, "มิ้นท์เป่าเทียน ทุกคนหัวเราะ");

            var c = Seg(r, "C", "ดึงกล่อง", "CAM 02", "02:31:00", 9);
            Track(c, Phop, (0, 1f, 1.5f), (4, 2.2f, 2f), (9, 2.2f, 2f));
            Ev(c, 6, EffectType.Kill, Phop, "ดึงกล่องจากชั้นล่าง — ชั้นทั้งตู้ล้มทับ").forbidFlags = "shelfSecured";
            Ev(c, 7, EffectType.Win, "", "ได้กล่องพัสดุ — ชั้นที่ผูกไว้ไม่ขยับ").requireFlags = "shelfSecured";

            var d = Seg(r, "D", "ห้องนั่งเล่น", "CAM 08", "02:20:00", 7);
            Track(d, Phop, (0, -2f, -2.2f), (3, -1.2f, -2.4f), (7, 3f, -2f));
            Ev(d, 3, EffectType.GiveItem, "strap", "หยิบสายรัดของจากกล่องพัสดุ");
            Ev(d, 5, EffectType.TakeItem, "strap", "ภพเก็บสายรัดคืนกล่อง");

            var e = Seg(r, "E", "ห้องเก็บของ", "CAM 02", "02:28:00", 7);
            Track(e, Phop, (0, -3.5f, 0f), (4, 2f, 2f), (7, 0f, 0f));
            Ev(e, 4, EffectType.SetFlag, "shelfSecured", "ผูกชั้นกับราวผนังด้วยสายรัด").requireItems = "strap";

            r.hints = new[] { "เทปไม่พอ", "มีช่วงหนึ่งที่ยาวเกิน", "ขอโทษนะมิ้นท์" };
            r.carryOver = "ชิ้นความทรงจำถูกทิ้งลงกองข้างเครื่อง — มันสั่นอยู่\nทุกคนในบ้านตื่น เดินลงมารวมกันที่ห้องนั่งเล่น";
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 6 (จุดพีค)
        static TapeRoll Roll6()
        {
            var r = NewRoll(6, "ห้องนั่งเล่น", "ทุกคน",
                "03:00 — ทุกคนอยู่ด้วยกันในห้องนั่งเล่น\nแท็บเล็ตคุมบ้านยังอยู่ในมือภพ");
            r.allowReverse = true;
            r.maxLength = 34f;
            r.spliceCost = 2f;
            r.startFlags = "lights";
            r.startItems = "tablet";
            r.isPeak = true;

            var a = Seg(r, "A", "มิ้นท์เสียบชาร์จ", "CAM 08", "03:00:00", 8);
            a.happensOffscreen = true;
            Track(a, Mint, (0, -1f, 1.2f), (3, 1.6f, -1f), (8, 0f, 1.2f));
            Track(a, Mom, (0, 0.6f, 1.4f), (8, 0.6f, 1.4f));
            Ev(a, 3, EffectType.SetFlag, "scooterCharging", "มิ้นท์เสียบชาร์จสกู๊ตเตอร์ข้างโซฟา").forbidFlags = "powerCut";
            Ev(a, 8, EffectType.SetFlag, "fireLiving", "แบตสกู๊ตเตอร์ไฟลุก").requireFlags = "scooterCharging";

            var b = Seg(r, "B", "ครัว", "CAM 09", "03:00:05", 7);
            b.happensOffscreen = true;
            Track(b, Dad, (0, -3f, 0f), (3, 2f, -1.8f), (7, 0f, 1f));
            Ev(b, 3, EffectType.SetFlag, "stoveOn", "พ่อต้มน้ำ — ลืมปิดเตาไฟฟ้า").forbidFlags = "powerCut";
            Ev(b, 7, EffectType.SetFlag, "fireKitchen", "เตาร้อนจัด — ไฟลุกในครัว").requireFlags = "stoveOn";

            var c = Seg(r, "C", "ปลั๊กพ่วง", "CAM 08", "02:59:50", 6);
            Track(c, Phop, (0, -3.5f, -1f), (3, 0f, -2f), (6, -0.6f, 0.4f));
            Ev(c, 3, EffectType.SetFlag, "powerCut", "ตัดไฟปลั๊กทั้งบ้านผ่านแท็บเล็ต").requireItems = "tablet";

            var d = Seg(r, "D", "บนโซฟา", "CAM 08", "03:00:20", 6);
            Track(d, Mint, (0, 0f, 1.2f), (6, 0f, 1.2f));
            Track(d, Mom, (0, 0.6f, 1.4f), (6, 0.6f, 1.4f));
            Track(d, Dad, (0, -0.6f, 1.4f), (6, -0.6f, 1.4f));
            Track(d, Phop, (0, -0.6f, 0.4f), (6, -0.6f, 0.4f));
            Ev(d, 2, EffectType.Kill, Mint, "ไฟจากสกู๊ตเตอร์ลามถึงโซฟา").requireFlags = "fireLiving";
            Ev(d, 2.2f, EffectType.Kill, Dad, "ไฟจากครัวลามมา").requireFlags = "fireKitchen";
            var all = Ev(d, 4, EffectType.Win, "", "ทุกคนนั่งด้วยกันบนโซฟา… ทุกคนรอด");
            all.forbidFlags = "fireLiving, fireKitchen";

            r.hints = new[] { "ไฟมาจากสองที่", "แท็บเล็ตตัดไฟได้ทั้งบ้าน", "ตัดไฟก่อนทุกอย่างจะเริ่ม" };
            return r;
        }

        // ---------------------------------------------------------------- ม้วน 7 (ม้วนสุดท้าย)
        static TapeRoll Roll7()
        {
            var r = NewRoll(7, "ม้วนสุดท้าย", Phop,
                "03:33\nบนฉลากม้วนนี้ — ลายมือของภพ: \"ม้วนนี้ของเรา\"");
            r.allowReverse = true;
            r.startFlags = "";
            r.isFinal = true;

            var a = Seg(r, "A", "หน้าเครื่องเล่นเทป", "CAM 08", "03:33:00", 20);
            Track(a, Phop, (0, -1f, 1.6f), (20, -1f, 1.6f));
            Track(a, Mint, (0, 0f, 1.2f), (20, 0f, 1.2f));
            Track(a, Mom, (0, 0.6f, 1.4f), (20, 0.6f, 1.4f));
            Track(a, Dad, (0, -0.4f, 1.4f), (20, -0.4f, 1.4f));
            Say(a, 3, "ภพนั่งมองครอบครัวที่หลับอยู่บนโซฟา");
            Say(a, 9, "ไม่มีอะไรตก ไม่มีไฟลุก ไม่มีใครผิด… แค่ถึงเวลา");
            Ev(a, 12, EffectType.Say, "", "ภพหลับตา");
            Ev(a, 15, EffectType.SetFlag, "dawn", "แสงเช้าลอดม่านเข้ามา");
            Ev(a, 19.5f, EffectType.Win, "", "เทปเล่นจนจบ");

            r.hints = new[] { "ไม่ต้องแก้ทุกอย่างก็ได้", "เราช่วยทุกคนไม่ได้ ถ้ายังหนีตัวเอง", "แค่ปล่อยให้มันเล่น" };
            return r;
        }

        // ---------------------------------------------------------------- builders
        static TapeRoll NewRoll(int number, string title, string victim, string intro)
        {
            var r = ScriptableObject.CreateInstance<TapeRoll>();
            r.name = $"Roll{number}";
            r.number = number;
            r.title = title;
            r.victim = victim;
            r.intro = intro;
            return r;
        }

        static TapeSegment Seg(TapeRoll r, string id, string title, string room, string clock, float length)
        {
            var s = new TapeSegment { id = id, title = title, room = room, clock = clock, length = length };
            r.segments.Add(s);
            return s;
        }

        static void Track(TapeSegment s, string actor, params (float t, float x, float z)[] keys)
        {
            var track = new ActorTrack { actor = actor };
            foreach (var k in keys) track.keys.Add(new ActorKey(k.t, k.x, k.z));
            s.actors.Add(track);
        }

        static TapeEvent Ev(TapeSegment s, float t, EffectType effect, string value, string caption)
        {
            var e = new TapeEvent(t, effect, value, caption);
            s.events.Add(e);
            return e;
        }

        static void Say(TapeSegment s, float t, string caption) => Ev(s, t, EffectType.Say, "", caption);
    }
}
