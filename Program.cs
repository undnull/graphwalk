using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

internal static class Program
{
    private class Point
    {
        public readonly uint ID;
        public bool Collapsed { get; private set; }

        public Point(uint id)
        {
            ID = id;
            Collapsed = false;
        }

        public void Collapse()
        {
            if(!Collapsed) {
                Console.WriteLine("Collapsing point {0}", ID);
                Collapsed = true;
            }
        }
    }

    private class Rib
    {
        public uint A { get; private set; }
        public uint B { get; private set; }
        public bool Collapsed { get; private set; }

        public Rib(uint a, uint b)
        {
            A = a;
            B = b;
            Collapsed = false;
        }

        // Uncollapsed rib is in the state of
        // superposition - we are using Contains
        // to detect whether a rib has a point,
        // but after collapsing we only use point getters.
        // This is done because we don't know the direction
        // of the graph yet, only the connecting points.
        public void Collapse()
        {
            if(!Collapsed) {
                Console.WriteLine("Collapsing rib {0}:{1}", A, B);
                Collapsed = true;
            }
        }

        public void Flip()
        {
            uint t = A;
            A = B;
            B = t;
        }

        public bool Contains(uint x)
        {
            if(A == x && B != x)
                return true;
            if(B == x && A != x)
                return true;
            return false;
        }

        public bool Equals(Rib rib)
        {
            return (A == rib.A && B == rib.B);
        }
    }

    private static readonly Dictionary<uint, uint> Order = new Dictionary<uint, uint>();
    private static readonly Dictionary<uint, Point> Parents = new Dictionary<uint, Point>();
    private static readonly Dictionary<uint, Point> Points = new Dictionary<uint, Point>();
    private static Dictionary<uint, uint> L = new Dictionary<uint, uint>();
    private static readonly List<Rib> AllRibs = new List<Rib>();
    private static readonly List<Rib> BackRibs = new List<Rib>();
    private static readonly Queue<Point> CollapseLog = new Queue<Point>();

    private static void AllocPoint(uint id)
    {
        if(Points.ContainsKey(id))
            return;
        Points[id] = new Point(id);
    }

    private static void AllocRib(uint a, uint b)
    {
        if(AllRibs.Any(r => r.Contains(a) && r.Contains(b)))
            return;
        AllRibs.Add(new Rib(a, b));
    }

    private static uint RecurseCollapse(Point point, uint order)
    {
        if(point.Collapsed)
            return order;

        Console.WriteLine("POINT: {0}", point.ID);
        Order[point.ID] = order;
        order++;

        // Find the parent rib.
        // Parent ribs are collapsed (have a direction)
        // and have our point being the second one.
        Rib? parent = AllRibs.FirstOrDefault(r => r.Collapsed && r.B == point.ID);
        if(parent != null) {
            // Register the point as a parent
            Parents[point.ID] = Points[parent.A];
        }

        // Collapse all the output ribs.
        // All outputs will point away from the point.
        Rib[] outputs = AllRibs.Where(r => !r.Collapsed && r.Contains(point.ID)).OrderBy(r => r.A + r.B).ToArray();
        foreach(Rib rib in outputs) {
            if(rib.A != point.ID)
                rib.Flip();
            rib.Collapse();

            if(parent != null) {
                bool misbehaving = false;
                Point? grandparent = Parents.GetValueOrDefault(parent.A);
                while(grandparent != null) {
                    if(rib.B == grandparent.ID) {
                        misbehaving = true;
                        break;
                    }

                    grandparent = Parents.GetValueOrDefault(grandparent.ID);
                }

                if(misbehaving) {
                    //Console.WriteLine("{0} {1} is misbehaving (current = {2})", rib.A, rib.B, point.ID);
                    rib.Flip();
                    BackRibs.Add(rib);
                    continue;
                }
            }

            order = RecurseCollapse(Points[rib.B], order);
        }

        // The algorithm collapses points in
        // slightly different way than by hand
        // but I think it's okay as long as it
        // detects ribs that misbehave.
        point.Collapse();
        CollapseLog.Enqueue(point);
        //CalcRating(point);
        //Console.WriteLine("L[{0}] = {1}", point.ID, CalcRating(point));

        return order;
    }

    private static uint CalcRating(Point point)
    {
        if(!L.ContainsKey(point.ID)) {
            List<uint> sum = new List<uint>();

            // Add the order of the point
            sum.Add(Order[point.ID]);

            // Add all the previous ratings
            // HACK: we need to go forward first, then
            // we are allowed to go backwards in terms of point IDs.
            Point[] children = Parents.Where(p => p.Value.ID == point.ID).Select(p => Points[p.Key]).ToArray();
            foreach(Point child in children) {
                uint r = CalcRating(child);
                //Console.WriteLine("L[{0}] += rating {1}", point.ID, r);
                sum.Add(r);
            }

            // Add orders of points that are
            // connected with the current one
            // with a reversed rib
            Point? parent = Parents.GetValueOrDefault(point.ID);
            while(parent != null) {
                if(BackRibs.Any(r => (r.Contains(point.ID) && r.Contains(parent.ID))))
                    sum.Add(Order[parent.ID]);
                parent = Parents.GetValueOrDefault(parent.ID);
            }

            L[point.ID] = sum.Min();
        }

        return L[point.ID];
    }

    private static void Main()
    {
        while(true) {
            string input;
            input = (Console.ReadLine() ?? String.Empty).Trim().ToLowerInvariant();

            if(input.Equals("done")) {
                Console.WriteLine();
                break;
            }

            Console.Write("({0}) ", input);
            uint[] parts = Regex.Split(input, @"\s+").Select(p => {
                if(UInt32.TryParse(p.Trim(), out uint pv))
                    return pv;
                return UInt32.MaxValue;
            }).ToArray();

            if(parts.Length != 2) {
                Console.WriteLine("syntax!");
                continue;
            }

            if(parts[0] == parts[1]) {
                Console.WriteLine("loop!");
                continue;
            }

            if(parts[0] == UInt32.MaxValue || parts[1] == UInt32.MaxValue) {
                Console.WriteLine("value!");
                continue;
            }

            AllocPoint(parts[0]);
            AllocPoint(parts[1]);
            AllocRib(parts[0], parts[1]);
        }

        Console.WriteLine();

        // Selecting the first point to work with
        Point begin = Points[Points.Select(p => p.Key).Min()];
        Console.WriteLine("BEGIN: {0}", begin.ID);

        Console.WriteLine();
        RecurseCollapse(begin, 1);

        Console.WriteLine();
        foreach(Rib rib in BackRibs) {
            Console.WriteLine("back: {0} {1}", rib.A, rib.B);
        }

        while(L.Count < Points.Count) {
            if(!CollapseLog.TryDequeue(out Point? point))
                break;
            Console.WriteLine("L[{0}] = {1}", point.ID, CalcRating(point));
        }

        //CalcRating(Points.First().Value);

        Console.WriteLine();
        List<uint> joints = new List<uint>();
        foreach(KeyValuePair<uint, Point> it in Points) {
            if(Parents.TryGetValue(it.Key, out Point? parent)) {
                if(!joints.Contains(parent.ID) && parent.ID != begin.ID && L[it.Key] >= Order[parent.ID]) {
                    Console.WriteLine("JOINT: {0}", parent.ID);
                    joints.Add(parent.ID);
                    continue;
                }
            }
        }
    }
}
