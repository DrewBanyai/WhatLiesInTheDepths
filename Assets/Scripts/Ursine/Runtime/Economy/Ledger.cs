// Ursine — one purse.
//
// Every menu that spends, spends out of this. Nothing keeps a private total: a second
// store of the same resource is how two screens end up disagreeing about what a player has.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ursine.Text;

namespace Ursine.Economy
{
    public class Ledger
    {
        public readonly List<Res> resources = new List<Res>();

        /// <summary>Raised whenever anything in the ledger moves.</summary>
        public event Action Changed;
        public void Dirty() => Changed?.Invoke();

        public Res Find(string id) => resources.FirstOrDefault(r => r.k == id);

        public double Held(string id) => Find(id)?.c ?? 0;
        public double Ceiling(string id) => Find(id)?.m ?? 0;

        public bool Short(string id, double n) => Held(id) < n;

        public bool AboveCeiling(string id, double n)
        {
            var r = Find(id);
            return r != null && r.m > 0 && n > r.m;
        }

        /// <summary>A cost above a ceiling outranks being short, because it is the one the
        /// player has to do something structural about.</summary>
        public Refusal Judge(IEnumerable<Amount> cost)
        {
            if (cost == null) return Refusal.None;
            var list = cost as IList<Amount> ?? cost.ToList();
            if (list.Any(a => AboveCeiling(a.k, a.n))) return Refusal.AboveCeiling;
            if (list.Any(a => Short(a.k, a.n))) return Refusal.Short;
            return Refusal.None;
        }

        /// <summary>The one sentence a refusal is allowed. When several costs fail it names
        /// only the largest shortfall — the number that will take longest — rather than
        /// itemizing a bill the player did not ask for.</summary>
        public string ReasonLine(IEnumerable<Amount> cost)
        {
            if (cost == null) return null;
            var list = cost as IList<Amount> ?? cost.ToList();

            var over = list.Where(a => AboveCeiling(a.k, a.n))
                           .OrderByDescending(a => a.n - Ceiling(a.k)).FirstOrDefault();
            if (over != null)
                return Loc.T("ursine.ledger.ceiling", Find(over.k)?.n ?? over.k, Fmt.Count(Ceiling(over.k)));

            var shortest = list.Where(a => Short(a.k, a.n))
                               .OrderByDescending(a => a.n - Held(a.k)).FirstOrDefault();
            if (shortest != null)
                return Loc.T("ursine.ledger.short", Fmt.Count(shortest.n - Held(shortest.k)), Find(shortest.k)?.n ?? shortest.k);

            return null;
        }

        /// <summary>Takes a cost, or takes nothing and returns false. Never partially.</summary>
        public bool Spend(IEnumerable<Amount> cost)
        {
            if (cost == null) return true;
            var list = cost as IList<Amount> ?? cost.ToList();
            if (Judge(list) != Refusal.None) return false;
            foreach (var a in list)
            {
                var r = Find(a.k);
                if (r != null) r.c = Math.Max(0, r.c - a.n);
            }
            Dirty();
            return true;
        }

        /// <summary>Grants a gain, clamped at the ceiling. Waste is not signaled: producing
        /// past a ceiling costs nothing but time, so a ledger reports rather than scolds.</summary>
        public void Grant(IEnumerable<Amount> gain)
        {
            if (gain == null) return;
            foreach (var a in gain)
            {
                var r = Find(a.k);
                if (r == null) continue;
                r.c = Math.Min(r.m, r.c + a.n);
            }
            Dirty();
        }

        /// <summary>One tick of accrual. Passive rates are applied and everything is clamped
        /// to its ceiling and to zero.</summary>
        public virtual void Tick()
        {
            foreach (var r in resources)
                if (r.passive != 0) r.c = Math.Max(0, Math.Min(r.m, r.c + r.passive));
        }
    }

    /// <summary>A pool of interchangeable workers bound to tasks. Binding one somewhere is
    /// unbinding it somewhere else, which is the whole point of a pool: it makes assignment
    /// a decision rather than an accumulation.</summary>
    public class WorkerPool
    {
        readonly Func<int> _total;
        readonly Func<int> _bound;

        public WorkerPool(Func<int> total, Func<int> bound)
        {
            _total = total;
            _bound = bound;
        }

        public int Total => _total != null ? _total() : 0;
        public int Bound => _bound != null ? _bound() : 0;
        public int Free => Mathf.Max(0, Total - Bound);
    }
}
