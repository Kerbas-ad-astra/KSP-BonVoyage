using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BonVoyage
{
    /// <summary>
    /// A* pathfinding
    /// https://blogs.msdn.microsoft.com/ericlippert/tag/astar/
    /// </summary>
    /// 
    class Path<Node> : IEnumerable<Node>
    {
        internal Node LastStep { get; private set; }
        internal Path<Node> PreviousSteps { get; private set; }
        internal double TotalCost { get; private set; }

        private Path(Node lastStep, Path<Node> previousSteps, double totalCost)
        {
            LastStep = lastStep;
            PreviousSteps = previousSteps;
            TotalCost = totalCost;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="start"></param>
        internal Path(Node start) : this(start, null, 0)
        {
        }


        internal Path<Node> AddStep(Node step, double stepCost)
        {
            return new Path<Node>(step, this, TotalCost + stepCost);
        }


        public IEnumerator<Node> GetEnumerator()
        {
            for (Path<Node> p = this; p != null; p = p.PreviousSteps)
                yield return p.LastStep;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }



        static internal Path<Node> FindPath<Node>(
            Node start,
            Node destination,
            Func<Node, Node, double> distance,
            Func<Node, double> estimate
        )
            where Node : IHasNeighbours<Node>
        {
            DateTime startedAt = DateTime.Now;
            var closed = new HashSet<Node>();
            var queue = new PriorityQueue<double, Path<Node>>();
            queue.Enqueue(0, new Path<Node>(start));
            while (!queue.IsEmpty)
            {
                var path = queue.Dequeue();
                if (closed.Contains(path.LastStep))
                    continue;
                if (path.LastStep.Equals(destination))
                    return path;
                closed.Add(path.LastStep);
                foreach (Node n in path.LastStep.Neighbours)
                {
                    double d = distance(path.LastStep, n);
                    var newPath = path.AddStep(n, d);
                    queue.Enqueue(newPath.TotalCost + estimate(n), newPath);
                }
                if (startedAt.AddSeconds(10) < DateTime.Now)
                    return null;
            }
            return null;
        }
    }


    class PriorityQueue<P, V>
    {
        private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();


        internal void Enqueue(P priority, V value)
        {
            Queue<V> q;
            if (!list.TryGetValue(priority, out q))
            {
                q = new Queue<V>();
                list.Add(priority, q);
            }
            q.Enqueue(value);
        }


        internal V Dequeue()
        {
            // will throw if there isn’t any first element!
            var pair = list.First();
            var v = pair.Value.Dequeue();
            if (pair.Value.Count == 0) // nothing left of the top priority.
                list.Remove(pair.Key);
            return v;
        }


        internal bool IsEmpty
        {
            get { return !list.Any(); }
        }
    }


    interface IHasNeighbours<N>
    {
        IEnumerable<N> Neighbours { get; }
    }

}
