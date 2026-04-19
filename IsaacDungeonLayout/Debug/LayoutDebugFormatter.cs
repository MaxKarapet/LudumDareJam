namespace IsaacDungeonLayout;

public static class LayoutDebugFormatter
{
    public static string ToAsciiMap(DungeonLayout layout)
    {
        var rooms = layout.Rooms;
        int minX = rooms.Min(r => r.GridPosition.X);
        int maxX = rooms.Max(r => r.GridPosition.X);
        int minZ = rooms.Min(r => r.GridPosition.Z);
        int maxZ = rooms.Max(r => r.GridPosition.Z);
        var map = rooms.ToDictionary(r => r.GridPosition, r => r);

        var lines = new List<string>
        {
            "ASCII: X вправо, Z снизу вверх по строкам.",
        };
        for (int z = maxZ; z >= minZ; z--)
        {
            var row = new char[(maxX - minX + 1) * 3];
            Array.Fill(row, ' ');
            int col = 0;
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Int2(x, z);
                char c = map.TryGetValue(p, out var r)
                    ? r.RoomType switch
                    {
                        RoomType.Start => 'S',
                        RoomType.End => 'E',
                        RoomType.Mob => 'M',
                        RoomType.Base => r.FinalOutsDir.Count switch
                        {
                            2 => '2',
                            3 => '3',
                            4 => '4',
                            _ => '?',
                        },
                        _ => '?',
                    }
                    : '.';
                row[col++] = ' ';
                row[col++] = c;
                row[col++] = ' ';
            }

            lines.Add(new string(row));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
