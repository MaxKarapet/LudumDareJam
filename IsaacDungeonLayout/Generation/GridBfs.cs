namespace IsaacDungeonLayout;

/// <summary>Обход сетки 4-связности: одна реализация для генератора, планировщика и валидатора.</summary>
public static class GridBfs
{
    /// <summary>Количество рёбер кратчайшего пути; -1 если <paramref name="to"/> недостижима.</summary>
    public static int ShortestPathEdgeCount(HashSet<Int2> vertices, Int2 from, Int2 to)
    {
        if (from.Equals(to))
            return 0;
        if (!vertices.Contains(from) || !vertices.Contains(to))
            return -1;

        var queue = new Queue<Int2>();
        var dist = new Dictionary<Int2, int> { [from] = 0 };
        queue.Enqueue(from);
        int dequeues = 0;
        int maxDequeues = Math.Max(1, vertices.Count) * GridSteps.CardinalCount + 1;
        while (queue.Count > 0)
        {
            if (++dequeues > maxDequeues)
                return -1;
            var u = queue.Dequeue();
            int du = dist[u];
            foreach (var d in GridSteps.Cardinal)
            {
                var v = u + d;
                if (!vertices.Contains(v) || dist.ContainsKey(v))
                    continue;
                dist[v] = du + 1;
                if (v.Equals(to))
                    return du + 1;
                queue.Enqueue(v);
            }
        }

        return -1;
    }

    public static bool IsConnected(HashSet<Int2> cells)
    {
        if (cells.Count == 0)
            return false;
        var start = cells.First();
        var seen = new HashSet<Int2> { start };
        var q = new Queue<Int2>();
        q.Enqueue(start);
        int steps = 0;
        int maxSteps = Math.Max(1, cells.Count) * GridSteps.CardinalCount + 1;
        while (q.Count > 0)
        {
            if (++steps > maxSteps)
                return false;
            var u = q.Dequeue();
            foreach (var d in GridSteps.Cardinal)
            {
                var v = u + d;
                if (!cells.Contains(v) || seen.Contains(v))
                    continue;
                seen.Add(v);
                q.Enqueue(v);
            }
        }

        return seen.Count == cells.Count;
    }

    /// <summary>Кратчайшие расстояния от <paramref name="source"/> до всех достижимых вершин (в рёбрах).</summary>
    public static Dictionary<Int2, int> DistancesFrom(HashSet<Int2> vertices, Int2 source)
    {
        var dist = new Dictionary<Int2, int>();
        if (!vertices.Contains(source))
            return dist;
        dist[source] = 0;
        var queue = new Queue<Int2>();
        queue.Enqueue(source);
        int dequeues = 0;
        int maxDequeues = Math.Max(1, vertices.Count) * GridSteps.CardinalCount + 1;
        while (queue.Count > 0)
        {
            if (++dequeues > maxDequeues)
                break;
            var u = queue.Dequeue();
            int du = dist[u];
            foreach (var d in GridSteps.Cardinal)
            {
                var v = u + d;
                if (!vertices.Contains(v) || dist.ContainsKey(v))
                    continue;
                dist[v] = du + 1;
                queue.Enqueue(v);
            }
        }

        return dist;
    }

    /// <summary>Минимальное расстояние от каждой вершины до ближайшего из <paramref name="sources"/> (мульти-источниковый BFS).</summary>
    public static Dictionary<Int2, int> MultiSourceMinDistance(HashSet<Int2> vertices, IEnumerable<Int2> sources)
    {
        var dist = new Dictionary<Int2, int>();
        var queue = new Queue<Int2>();
        foreach (var s in sources)
        {
            if (!vertices.Contains(s) || dist.ContainsKey(s))
                continue;
            dist[s] = 0;
            queue.Enqueue(s);
        }

        int dequeues = 0;
        int maxDequeues = Math.Max(1, vertices.Count) * GridSteps.CardinalCount + 1;
        while (queue.Count > 0)
        {
            if (++dequeues > maxDequeues)
                break;
            var u = queue.Dequeue();
            int du = dist[u];
            foreach (var d in GridSteps.Cardinal)
            {
                var v = u + d;
                if (!vertices.Contains(v) || dist.ContainsKey(v))
                    continue;
                dist[v] = du + 1;
                queue.Enqueue(v);
            }
        }

        return dist;
    }
}
