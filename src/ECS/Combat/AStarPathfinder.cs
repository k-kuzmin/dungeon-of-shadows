using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Combat;

/// <summary>
/// A* pathfinding на тайловой сетке. 4-directional.
/// </summary>
public class AStarPathfinder
{
    private const int MaxNodes = 512;

    private static readonly (int dx, int dy)[] Neighbors =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0)
    };

    public bool TryFindPath(TileMap map, int fromX, int fromY, int toX, int toY,
        List<(int x, int y)> outPath)
    {
        outPath.Clear();

        if (fromX == toX && fromY == toY) return true;
        if (!map.IsWalkable(toX, toY)) return false;

        var openSet = new PriorityQueue<(int x, int y), float>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var gScore = new Dictionary<(int, int), float>();

        var start = (fromX, fromY);
        var goal = (toX, toY);

        gScore[start] = 0;
        openSet.Enqueue(start, Heuristic(fromX, fromY, toX, toY));

        int explored = 0;

        while (openSet.Count > 0 && explored < MaxNodes)
        {
            var current = openSet.Dequeue();
            explored++;

            if (current == goal)
            {
                ReconstructPath(cameFrom, current, outPath);
                return true;
            }

            float currentG = gScore[current];

            foreach (var (dx, dy) in Neighbors)
            {
                int nx = current.x + dx;
                int ny = current.y + dy;

                if (!map.IsWalkable(nx, ny)) continue;

                var neighbor = (nx, ny);
                float tentativeG = currentG + 1f;

                if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                {
                    gScore[neighbor] = tentativeG;
                    cameFrom[neighbor] = current;
                    float f = tentativeG + Heuristic(nx, ny, toX, toY);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        return false; // Путь не найден
    }

    private static float Heuristic(int x1, int y1, int x2, int y2) =>
        Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

    private static void ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom,
        (int x, int y) current, List<(int x, int y)> outPath)
    {
        outPath.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            outPath.Add(current);
        }
        outPath.Reverse();
        // Убираем стартовую позицию — враг уже там стоит
        if (outPath.Count > 0)
            outPath.RemoveAt(0);
    }
}
