namespace IsaacDungeonLayout;

public static class ShuffleDungeonTests
{
    public static int Run()
    {
        Console.WriteLine("=== ShuffleDungeonTests ===");
        int failed = 0;

        void Fail(string msg)
        {
            Console.WriteLine($"[FAIL] {msg}");
            failed++;
        }

        var gen = new DungeonGenerator();
        var templates = DemoTemplates.BuildDefault();

        // 1) Линия из трёх клеток: старт в конце, финиш должен оказаться в противоположном конце (макс. BFS = 2)
        var line = new HashSet<Int2> { new(0, 0), new(1, 0), new(2, 0) };
        var slotsLine = new RoomSlotDescriptor[]
        {
            new() { RoomType = RoomType.Start },
            new() { RoomType = RoomType.Base },
            new() { RoomType = RoomType.End },
        };
        var inputLine = new ShuffleDungeonInput
        {
            OccupiedCells = line,
            StartPosition = new Int2(0, 0),
            Slots = slotsLine,
            Templates = templates,
            Seed = 7,
        };
        var oLine = gen.Shuffle(inputLine);
        if (!oLine.Success)
        {
            Fail($"линия 3: {oLine.Failure!.Value.Reason}");
        }
        else
        {
            var L = oLine.Result!;
            if (L.Source != DungeonLayoutSource.Shuffled)
                Fail("Source должен быть Shuffled");
            if (!L.EndPosition.Equals(new Int2(2, 0)))
                Fail($"линия: End ожидается (2,0), получено {L.EndPosition}");
            if (L.StartEndGraphDistance != 2)
                Fail($"линия: SE дистанция 2, получено {L.StartEndGraphDistance}");
            try
            {
                DungeonValidator.ValidateShuffledOrThrow(L, ShuffleTypeExpectation.FromSlots(slotsLine), line);
            }
            catch (DungeonLayoutValidationException ex)
            {
                Fail($"ValidateShuffled: {ex.Message}");
            }

            var errWrongGraph = DungeonValidator.ValidateShuffled(
                L,
                ShuffleTypeExpectation.FromSlots(slotsLine),
                new HashSet<Int2> { new(0, 0), new(1, 0) });
            if (errWrongGraph is null)
                Fail("ожидалась ошибка при несовпадении expectedOccupiedCells с layout");
        }

        // 2) Невалидный вход: несовпадение числа слотов и клеток
        try
        {
            new ShuffleDungeonInput
            {
                OccupiedCells = line,
                StartPosition = new Int2(0, 0),
                Slots = slotsLine[..2],
                Templates = templates,
            }.Validate();
            Fail("ожидался InvalidOperationException при Slots.Count != Occupied");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("[OK] Validate отклоняет неверное число слотов");
        }

        // 3) Старт в центре линии: shuffle должен вернуть Fail
        var slotsWrong = new RoomSlotDescriptor[]
        {
            new() { RoomType = RoomType.Start },
            new() { RoomType = RoomType.Base },
            new() { RoomType = RoomType.End },
        };
        var inputCenter = new ShuffleDungeonInput
        {
            OccupiedCells = line,
            StartPosition = new Int2(1, 0),
            Slots = slotsWrong,
            Templates = templates,
        };
        var oCenter = gen.Shuffle(inputCenter);
        if (oCenter.Success)
            Fail("старт в центре линии: ожидался провал shuffle");
        else
            Console.WriteLine("[OK] старт в центре: ожидаемый провал");

        // 4) T-граф + mob: два листа на одинаковой BFS-дистанции от старта
        var tee = new HashSet<Int2> { new(0, 0), new(1, 0), new(2, 0), new(1, 1) };
        var slotsTee = new RoomSlotDescriptor[]
        {
            new() { RoomType = RoomType.Start },
            new() { RoomType = RoomType.Base },
            new() { RoomType = RoomType.Mob },
            new() { RoomType = RoomType.End },
        };
        var inputT0 = new ShuffleDungeonInput
        {
            OccupiedCells = tee,
            StartPosition = new Int2(0, 0),
            Slots = slotsTee,
            Templates = templates,
            Seed = 0,
        };
        var inputT42 = new ShuffleDungeonInput
        {
            OccupiedCells = tee,
            StartPosition = new Int2(0, 0),
            Slots = slotsTee,
            Templates = templates,
            Seed = 42,
        };
        var oT0 = gen.Shuffle(inputT0);
        var oT42 = gen.Shuffle(inputT42);
        if (!oT0.Success || !oT42.Success)
            Fail($"T-граф: {oT0.Failure?.Reason ?? oT42.Failure?.Reason}");
        else
        {
            var ends = new HashSet<Int2> { new(2, 0), new(1, 1) };
            if (!ends.Contains(oT0.Result!.EndPosition) || oT0.Result.StartEndGraphDistance != 2)
                Fail($"T seed=0: End должен быть (2,0) или (1,1), SE=2; получено {oT0.Result.EndPosition}, SE={oT0.Result.StartEndGraphDistance}");
            if (!ends.Contains(oT42.Result!.EndPosition) || oT42.Result.StartEndGraphDistance != 2)
                Fail($"T seed=42: End должен быть (2,0) или (1,1), SE=2; получено {oT42.Result.EndPosition}");
            try
            {
                DungeonValidator.ValidateShuffledOrThrow(oT0.Result, ShuffleTypeExpectation.FromSlots(slotsTee), tee);
                DungeonValidator.ValidateShuffledOrThrow(oT42.Result, ShuffleTypeExpectation.FromSlots(slotsTee), tee);
            }
            catch (DungeonLayoutValidationException ex)
            {
                Fail($"T ValidateShuffled: {ex.Message}");
            }

            Console.WriteLine($"[OK] T-граф: seed0 End={oT0.Result.EndPosition}, seed42 End={oT42.Result.EndPosition}");
        }

        // 5) RequiredTemplateId: у середины линии требуем угол — нет совпадения
        var slotsBadCorner = new RoomSlotDescriptor[]
        {
            new() { RoomType = RoomType.Start },
            new() { RoomType = RoomType.Base, RequiredTemplateId = "base_corner" },
            new() { RoomType = RoomType.End },
        };
        var oBad = gen.Shuffle(new ShuffleDungeonInput
        {
            OccupiedCells = line,
            StartPosition = new Int2(0, 0),
            Slots = slotsBadCorner,
            Templates = templates,
        });
        if (oBad.Success)
            Fail("RequiredTemplateId base_corner на линии: ожидался провал");
        else
            Console.WriteLine("[OK] RequiredTemplateId несовместим с геометрией");

        // 6) Длинная линия (smoke)
        var line8 = Enumerable.Range(0, 8).Select(i => new Int2(i, 0)).ToHashSet();
        var slots8 = new List<RoomSlotDescriptor>
        {
            new() { RoomType = RoomType.Start },
            new() { RoomType = RoomType.End },
        };
        for (int i = 0; i < 6; i++)
            slots8.Add(new RoomSlotDescriptor { RoomType = RoomType.Base });
        var o8 = gen.Shuffle(new ShuffleDungeonInput
        {
            OccupiedCells = line8,
            StartPosition = new Int2(0, 0),
            Slots = slots8,
            Templates = templates,
            Seed = 1,
        });
        if (!o8.Success)
            Fail($"линия 8: {o8.Failure!.Value.Reason}");
        else if (!o8.Result!.EndPosition.Equals(new Int2(7, 0)) || o8.Result.StartEndGraphDistance != 7)
            Fail($"линия 8: End (7,0), SE=7; получено End={o8.Result.EndPosition}, SE={o8.Result.StartEndGraphDistance}");
        else
            Console.WriteLine("[OK] линия 8 клеток");

        Console.WriteLine(failed == 0 ? "=== ShuffleDungeonTests: OK ===" : $"=== ShuffleDungeonTests: failures={failed} ===");
        return failed > 0 ? 1 : 0;
    }
}
