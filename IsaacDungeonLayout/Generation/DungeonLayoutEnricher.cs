namespace IsaacDungeonLayout;

/// <summary>Добавляет к <see cref="PlacedRoom"/> игровые метаданные (BFS от старта/финиша/мобов, соседи по типам).</summary>
public static class DungeonLayoutEnricher
{
    public static DungeonLayout Enrich(DungeonLayout layout)
    {
        var occupied = new HashSet<Int2>(layout.Rooms.Select(r => r.GridPosition));
        var byPos = layout.Rooms.ToDictionary(r => r.GridPosition);
        var distS = GridBfs.DistancesFrom(occupied, layout.StartPosition);
        var distE = GridBfs.DistancesFrom(occupied, layout.EndPosition);
        int se = layout.StartEndGraphDistance;

        var mobSet = layout.MobPositions.ToHashSet();
        Dictionary<Int2, int> distMob = mobSet.Count == 0
            ? new Dictionary<Int2, int>()
            : GridBfs.MultiSourceMinDistance(occupied, mobSet);

        var newRooms = new List<PlacedRoom>();
        foreach (var r in layout.Rooms.OrderBy(x => x.GridPosition.X).ThenBy(x => x.GridPosition.Z))
        {
            var p = r.GridPosition;
            int ds = distS[p];
            int de = distE[p];
            bool onPath = ds + de == se;

            var neighborCounts = new Dictionary<RoomType, int>();
            foreach (var npos in r.ConnectedNeighborPositions)
            {
                var t = byPos[npos].RoomType;
                neighborCounts[t] = neighborCounts.GetValueOrDefault(t) + 1;
            }

            int distMobP = mobSet.Count == 0 ? -1 : distMob[p];

            var meta = new RoomGameplayMetadata
            {
                DistanceFromStartEdges = ds,
                DistanceToEndEdges = de,
                OnShortestPathStartToEnd = onPath,
                DistanceToNearestMobEdges = distMobP,
                NeighborCountByType = neighborCounts,
            };

            newRooms.Add(new PlacedRoom
            {
                TemplateId = r.TemplateId,
                RoomType = r.RoomType,
                GridPosition = r.GridPosition,
                RotationSteps90 = r.RotationSteps90,
                FinalOutsDir = r.FinalOutsDir,
                ConnectedNeighborPositions = r.ConnectedNeighborPositions,
                GameplayMetadata = meta,
            });
        }

        return new DungeonLayout
        {
            Rooms = newRooms,
            StartPosition = layout.StartPosition,
            EndPosition = layout.EndPosition,
            MobPositions = layout.MobPositions,
            StartEndGraphDistance = layout.StartEndGraphDistance,
            Topology = layout.Topology,
        };
    }
}
