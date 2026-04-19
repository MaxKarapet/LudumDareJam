# Интеграция IsaacDungeonLayout в Godot

Этот документ описывает, как правильно интегрировать C# модуль генерации подземелий в ваш проект на Godot (версия с поддержкой C# / Mono).

## 1. Добавление файлов в проект

Просто скопируйте папку `IsaacDungeonLayout` (без папок `bin`, `obj` и `Tests`, если тесты вам не нужны) в корневую директорию вашего Godot проекта (например, в папку `Scripts/Generation`). 
Godot автоматически подхватит C# файлы и включит их в проектную сборку `.csproj`.

## 2. Создание узлов-шаблонов (Комнат)

Вам нужно создать сцены для каждого типа комнат. 
Для того чтобы алгоритм понимал, какие выходы есть у сцены, вам понадобится маппинг. 

Лучший способ — добавить к корневому узлу каждой сцены комнаты (например, `Node3D`) скрипт, содержащий метаданные:

```csharp
using Godot;
using Godot.Collections;

public partial class RoomScene : Node3D
{
	// Тип комнаты: "base", "start", "end", "mob"
	[Export] public string RoomType = "base"; 
	
	// Вектора выходов. ВАЖНО: используйте (1,0), (-1,0), (0,1), (0,-1)
	[Export] public Array<Vector2I> OutsDir; 
}
```

## 3. Настройка Генератора

Создайте скрипт, который будет управлять процессом генерации на сцене.

```csharp
using Godot;
using System.Linq;
using System.Collections.Generic;
using IsaacDungeonLayout; // Не забудьте namespace

public partial class LevelGenerator : Node3D
{
	[Export] public PackedScene[] RoomScenes;
	[Export] public int BaseRooms = 15;
	[Export] public int MobRooms = 4;
	[Export] public int Seed = 42;
	[Export] public float CellSize = 20.0f; // Физический размер одной комнаты

	// Словарь для быстрого инстанцирования
	private Dictionary<string, PackedScene> _sceneCache = new();

	public override void _Ready()
	{
		GenerateLevel();
	}

	private void GenerateLevel()
	{
		// 1. Собираем шаблоны для алгоритма из экспортированных сцен
		var templates = new List<RoomTemplate>();
		foreach (var scene in RoomScenes)
		{
			var instance = scene.Instantiate<RoomScene>();
			_sceneCache[instance.Name] = scene;

			var roomTypeVal = instance.RoomType switch {
				"start" => RoomType.Start,
				"end" => RoomType.End,
				"mob" => RoomType.Mob,
				_ => IsaacDungeonLayout.RoomType.Base
			};

			// Преобразуем Godot Vector2I в алгоритмический Int2
			var outs = instance.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();

			templates.Add(new RoomTemplate
			{
				Id = instance.Name,
				Type = roomTypeVal,
				OutsNum = outs.Length,
				OutsDir = outs
			});
			
			instance.QueueFree(); // Чистим временный инстанс
		}

		// 2. Конфигурируем генератор
		var config = new DungeonGenerationConfig
		{
			Templates = templates,
			BaseRoomCount = BaseRooms,
			MobRoomCount = MobRooms,
			Seed = Seed,
			MaxAttempts = 100,
			DiagnosticLog = GD.Print
		};

		// 3. Запускаем генерацию
		var generator = new DungeonGenerator();
		var outcome = generator.Generate(config);

		if (!outcome.Success)
		{
			GD.PrintErr("Failed to generate level: " + outcome.Failure!.Value.Reason);
			return;
		}

		// 4. Расставляем комнаты
		SpawnRooms(outcome.Result);
	}

	private void SpawnRooms(DungeonLayout layout)
	{
		foreach (var room in layout.Rooms)
		{
			var scene = _sceneCache[room.TemplateId];
			var instance = scene.Instantiate<Node3D>();
			
			// Расчет позиции (Godot X/Z)
			instance.Position = new Vector3(
				room.GridPosition.X * CellSize, 
				0, 
				room.GridPosition.Z * CellSize
			);

			// Поворот вокруг оси Y (В Godot поворот против часовой, так что умножаем на шаг 90 градусов)
			// PI / 2 = 90 градусов.
			float rotationY = room.RotationSteps90 * (Mathf.Pi / 2f);
			
			// Применяем вращение
			instance.Rotation = new Vector3(0, rotationY, 0);

			AddChild(instance);
		}
	}
}
```

## Важные замечания про оси координат

В сгенерированном алгоритме:
- `+X` = Восток
- `-X` = Запад
- `+Z` = Север
- `-Z` = Юг
Математический поворот против часовой стрелки.

В Godot 3D:
- `+X` = Вправо
- `-X` = Влево
- `+Z` = Назад (в экран)
- `-Z` = Вперед (вглубь экрана)

Обязательно проверьте, как соотносятся выходы ваших конкретных 3D моделей с координатной сеткой в Godot и при необходимости инвертируйте ротацию (-rotationY), если комнаты пристыковываются задом наперед.
