namespace DungeonOfShadows.Dungeon.Generation;

internal sealed class BspNode
{
    public int X, Y, Width, Height;
    public BspNode? Left, Right;

    public bool IsLeaf => Left is null && Right is null;

    public BspNode(int x, int y, int w, int h)
    {
        X = x; Y = y; Width = w; Height = h;
    }
}

internal static class BspTree
{
    /// <summary>
    /// Строит BSP-дерево рекурсивным делением прямоугольника.
    /// </summary>
    internal static BspNode Build(int x, int y, int w, int h, int minLeaf, Random rng)
    {
        var node = new BspNode(x, y, w, h);

        // Если узел слишком мал — это лист
        if (w < minLeaf * 2 && h < minLeaf * 2)
            return node;

        // Выбираем ось разделения
        bool splitH;
        if (w < minLeaf * 2) splitH = true;       // только горизонтально
        else if (h < minLeaf * 2) splitH = false;  // только вертикально
        else if (w > h * 1.25f) splitH = false;     // слишком широкий — вертикально
        else if (h > w * 1.25f) splitH = true;      // слишком высокий — горизонтально
        else splitH = rng.Next(2) == 0;

        if (splitH)
        {
            int splitY = rng.Next(minLeaf, h - minLeaf + 1);
            node.Left = Build(x, y, w, splitY, minLeaf, rng);
            node.Right = Build(x, y + splitY, w, h - splitY, minLeaf, rng);
        }
        else
        {
            int splitX = rng.Next(minLeaf, w - minLeaf + 1);
            node.Left = Build(x, y, splitX, h, minLeaf, rng);
            node.Right = Build(x + splitX, y, w - splitX, h, minLeaf, rng);
        }

        return node;
    }

    /// <summary>
    /// Собирает все листья дерева.
    /// </summary>
    internal static void CollectLeaves(BspNode node, List<BspNode> leaves)
    {
        if (node.IsLeaf)
        {
            leaves.Add(node);
            return;
        }
        if (node.Left != null) CollectLeaves(node.Left, leaves);
        if (node.Right != null) CollectLeaves(node.Right, leaves);
    }

    /// <summary>
    /// Обход дерева для получения пар смежных комнат (для коридоров).
    /// Учитывает только листья с комнатами, чтобы избежать разрывов.
    /// </summary>
    internal static void CollectSiblingPairs(
        BspNode node,
        HashSet<BspNode> leavesWithRooms,
        List<(BspNode A, BspNode B)> pairs)
    {
        if (node.IsLeaf) return;
        if (node.Left != null && node.Right != null)
        {
            var leftLeaf = FindClosestLeaf(node.Left, node.Right, leavesWithRooms);
            var rightLeaf = FindClosestLeaf(node.Right, node.Left, leavesWithRooms);
            if (leftLeaf != null && rightLeaf != null)
                pairs.Add((leftLeaf, rightLeaf));
        }
        if (node.Left != null) CollectSiblingPairs(node.Left, leavesWithRooms, pairs);
        if (node.Right != null) CollectSiblingPairs(node.Right, leavesWithRooms, pairs);
    }

    /// <summary>
    /// Находит лист с комнатой в поддереве source, ближайший к центру target.
    /// </summary>
    private static BspNode? FindClosestLeaf(BspNode source, BspNode target, HashSet<BspNode> leavesWithRooms)
    {
        var leaves = new List<BspNode>();
        CollectLeaves(source, leaves);

        int tcx = target.X + target.Width / 2;
        int tcy = target.Y + target.Height / 2;

        BspNode? best = null;
        int bestDist = int.MaxValue;
        foreach (var leaf in leaves)
        {
            if (!leavesWithRooms.Contains(leaf)) continue;
            int cx = leaf.X + leaf.Width / 2;
            int cy = leaf.Y + leaf.Height / 2;
            int dist = Math.Abs(cx - tcx) + Math.Abs(cy - tcy);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = leaf;
            }
        }
        return best;
    }
}
