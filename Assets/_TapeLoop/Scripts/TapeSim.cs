using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TapeLoop
{
    // ชิ้นเทปบน timeline = ช่วงเวลาหนึ่งของ segment (+ เล่นถอยหลังหรือไม่)
    public class Piece
    {
        public string segment;
        public float start, end;
        public bool reversed;

        public float Length => end - start;
        public Piece Clone() => new() { segment = segment, start = start, end = end, reversed = reversed };
        public override string ToString() => $"{segment}[{start:0.#}-{end:0.#}]{(reversed ? "◄" : "")}";
    }

    public class SimState
    {
        public readonly HashSet<string> flags = new();
        public readonly HashSet<string> items = new();

        public SimState Clone()
        {
            var c = new SimState();
            c.flags.UnionWith(flags);
            c.items.UnionWith(items);
            return c;
        }
    }

    public enum Outcome { Running, Won, Died, NoEnd }

    // เล่นเทปทีละเฟรม — ใช้ทั้งตอนเล่นจริง (มีภาพ) และตอน scrub (คำนวณ state เงียบ ๆ)
    public class TapePlayer
    {
        public readonly TapeRoll roll;
        public readonly List<Piece> pieces;
        public SimState state = new();

        public int pieceIndex;
        public float time;                // เวลาใน segment ของชิ้นปัจจุบัน
        public Outcome outcome = Outcome.Running;
        public string deadWho, deathCause;
        public TapeEvent deathEvent;
        public bool deathOffscreen;
        public string winCaption;

        // เหตุการณ์ที่เพิ่งเกิด (ให้ TapeGame ขึ้นคำบรรยาย)
        public readonly List<string> captions = new();
        public readonly List<TapeEvent> firedEvents = new();
        public readonly List<TapeEvent> history = new();   // ทุกเหตุการณ์ที่เกิดตั้งแต่เริ่มเล่น (โต๊ะตัดต่อใช้บอกว่าเคยเห็นอะไรแล้ว)
        public bool justCrossedSplice;

        const float Eps = 0.0001f;
        bool offscreenCaption;
        readonly List<TapeSegment> pendingOffscreen;
        readonly HashSet<int> firedInPiece = new();

        public TapePlayer(TapeRoll roll, List<Piece> pieces)
        {
            this.roll = roll;
            this.pieces = pieces.Select(p => p.Clone()).ToList();
            state.flags.UnionWith(Csv.Split(roll.startFlags));
            state.items.UnionWith(Csv.Split(roll.startItems));

            // ชิ้นที่ไม่อยู่ในเทปเลย แต่ "เกิดนอกจออยู่ดี" (ตัดทิ้ง ≠ ไม่เกิด)
            var used = new HashSet<string>(this.pieces.Select(p => p.segment));
            pendingOffscreen = roll.segments
                .Where(s => s.happensOffscreen && !used.Contains(s.id))
                .OrderBy(s => s.ClockSeconds)
                .ToList();
            EnterPiece(0);
        }

        public Piece Current => pieceIndex < pieces.Count ? pieces[pieceIndex] : null;
        public TapeSegment CurrentSegment => Current != null ? roll.Segment(Current.segment) : null;

        void EnterPiece(int index)
        {
            pieceIndex = index;
            firedInPiece.Clear();
            if (index >= pieces.Count)
            {
                // เทปจบ — เหตุการณ์นอกจอที่เหลือยังเกิดต่อ
                RunOffscreen(float.MaxValue);
                if (outcome == Outcome.Running) outcome = Outcome.NoEnd;
                return;
            }
            var piece = pieces[index];
            var seg = roll.Segment(piece.segment);
            RunOffscreen(seg.ClockSeconds + piece.start);
            if (outcome != Outcome.Running) return;
            time = piece.reversed ? piece.end : piece.start;
            if (!piece.reversed) FireRange(seg, piece, time - 0.0001f, time);
        }

        void RunOffscreen(float beforeClock)
        {
            while (pendingOffscreen.Count > 0 && pendingOffscreen[0].ClockSeconds < beforeClock && outcome == Outcome.Running)
            {
                var seg = pendingOffscreen[0];
                pendingOffscreen.RemoveAt(0);
                foreach (var e in seg.events.OrderBy(e => e.time))
                {
                    if (e.onlyWhenReversed || e.effect == EffectType.Win || e.effect == EffectType.Say) continue;
                    if (!Met(e, state)) continue;
                    // ป้าย "(นอกจอ)" — ผู้เล่นต้องรู้ว่าชิ้นที่ตัดทิ้งยังเกิดอยู่ (สอนกฎ ตัดทิ้ง ≠ ไม่เกิด)
                    offscreenCaption = true;
                    Apply(e, false);
                    offscreenCaption = false;
                    if (e.effect == EffectType.Kill)
                    {
                        deathOffscreen = true;
                        return;
                    }
                }
            }
        }

        // เดินเทปไป dt วินาที
        public void Advance(float dt)
        {
            captions.Clear();
            firedEvents.Clear();
            justCrossedSplice = false;
            while (dt > 0f && outcome == Outcome.Running && Current != null)
            {
                var piece = Current;
                var seg = CurrentSegment;
                float remaining = piece.reversed ? time - piece.start : piece.end - time;
                float step = Mathf.Min(dt, remaining);
                float from = time;
                time += piece.reversed ? -step : step;
                FireRange(seg, piece, Mathf.Min(from, time), Mathf.Max(from, time));
                dt -= step;
                if (outcome != Outcome.Running) return;
                if (remaining - step <= 0.0001f)
                {
                    bool more = pieceIndex + 1 < pieces.Count;
                    EnterPiece(pieceIndex + 1);
                    if (more) justCrossedSplice = true;
                }
            }
        }

        // เดินจนจบหรือจนมีผล (ใช้ใน harness / preview)
        public Outcome RunToEnd()
        {
            int guard = 0;
            while (outcome == Outcome.Running && guard++ < 10000) Advance(0.25f);
            return outcome;
        }

        void FireRange(TapeSegment seg, Piece piece, float lo, float hi)
        {
            var order = piece.reversed
                ? seg.events.Select((e, i) => (e, i)).OrderByDescending(x => x.e.time)
                : seg.events.Select((e, i) => (e, i)).OrderBy(x => x.e.time);
            foreach (var (e, i) in order)
            {
                if (firedInPiece.Contains(i)) continue;
                if (e.time < piece.start - Eps || e.time > piece.end + Eps) continue;
                // เดินหน้า: (lo, hi] · ถอยหลัง: [lo, hi] — กันยิงซ้ำด้วย firedInPiece
                bool inside = piece.reversed
                    ? e.time >= lo - Eps && e.time <= hi + Eps
                    : e.time > lo && e.time <= hi + Eps;
                if (!inside) continue;
                firedInPiece.Add(i);

                if (piece.reversed)
                {
                    if (e.onlyWhenReversed)
                    {
                        history.Add(e);
                        Caption(e);
                    }
                    else if (e.reversible) Apply(e, true);
                    continue;
                }
                if (e.onlyWhenReversed || !Met(e, state)) continue;
                Apply(e, false);
                if (outcome != Outcome.Running) return;
            }
        }

        public static bool Met(TapeEvent e, SimState s)
        {
            foreach (var f in Csv.Split(e.requireFlags)) if (!s.flags.Contains(f)) return false;
            foreach (var f in Csv.Split(e.forbidFlags)) if (s.flags.Contains(f)) return false;
            foreach (var it in Csv.Split(e.requireItems)) if (!s.items.Contains(it)) return false;
            foreach (var it in Csv.Split(e.forbidItems)) if (s.items.Contains(it)) return false;
            return true;
        }

        void Apply(TapeEvent e, bool inverse)
        {
            firedEvents.Add(e);
            history.Add(e);
            switch (e.effect)
            {
                case EffectType.SetFlag:
                    if (inverse) state.flags.Remove(e.value); else state.flags.Add(e.value);
                    break;
                case EffectType.ClearFlag:
                    if (inverse) state.flags.Add(e.value); else state.flags.Remove(e.value);
                    break;
                case EffectType.GiveItem:
                    if (inverse) state.items.Remove(e.value); else state.items.Add(e.value);
                    break;
                case EffectType.TakeItem:
                    if (inverse) state.items.Add(e.value); else state.items.Remove(e.value);
                    break;
                case EffectType.Kill:
                    if (inverse) return;
                    outcome = Outcome.Died;
                    deadWho = e.value;
                    deathEvent = e;
                    deathCause = e.caption;
                    return;
                case EffectType.Win:
                    if (inverse) return;
                    outcome = Outcome.Won;
                    winCaption = e.caption;
                    return;
            }
            if (!inverse) Caption(e);
        }

        void Caption(TapeEvent e)
        {
            if (!string.IsNullOrEmpty(e.caption)) captions.Add(offscreenCaption ? "(นอกจอ) " + e.caption : e.caption);
        }

        // ---- helpers สำหรับ editor ----
        public static float TapeLength(TapeRoll roll, List<Piece> pieces)
        {
            float total = pieces.Sum(p => p.Length);
            if (pieces.Count > 1) total += (pieces.Count - 1) * roll.spliceCost;
            return total;
        }

        // state ณ ตำแหน่ง scrub (ชิ้นที่ index, เวลา t) — คำนวณเงียบ ๆ ไม่มีภาพ
        public static TapePlayer Seek(TapeRoll roll, List<Piece> pieces, int index, float t)
        {
            var p = new TapePlayer(roll, pieces);
            int guard = 0;
            while (p.outcome == Outcome.Running && p.pieceIndex < index && guard++ < 10000) p.Advance(0.5f);
            if (p.outcome != Outcome.Running || p.pieceIndex != index) return p;
            var piece = p.Current;
            float distance = piece.reversed ? piece.end - t : t - piece.start;
            if (distance > 0f) p.Advance(Mathf.Min(distance, piece.Length - 0.001f));
            return p;
        }
    }
}
