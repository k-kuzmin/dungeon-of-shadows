using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Рендер боевых эффектов: дуга атаки, HP-бары врагов, числа урона. World-space.
/// </summary>
public class CombatRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _attackBuffer = new();
    private readonly List<int> _enemyBuffer = new();
    private readonly List<int> _dmgNumBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    private static readonly Color SlashColor = new(255, 255, 255, 180);
    private static readonly Color HpBarBg = new(0, 0, 0, 180);
    private static readonly Color HpBarFg = new(220, 40, 40, 255);
    private static readonly Color DmgColorNormal = new(255, 255, 100, 255);
    private static readonly Color DmgColorCrit = new(255, 80, 40, 255);

    public CombatRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        DrawSlashArcs();
        DrawEnemyHealthBars();
        DrawDamageNumbers();
    }

    private void DrawSlashArcs()
    {
        var world = _ctx.World;
        int ts = _ctx.Config.ScaledTileSize;

        world.QueryInto<MeleeAttack, Position>(_attackBuffer);
        foreach (int id in _attackBuffer)
        {
            ref var attack = ref world.Get<MeleeAttack>(id);
            ref var pos = ref world.Get<Position>(id);

            float cx = pos.X + ts / 2f;
            float cy = pos.Y + ts / 2f;
            float alpha = attack.TimeRemaining / attack.Duration;

            // Угол направления атаки
            float baseAngle = MathF.Atan2(attack.DirY, attack.DirX) * (180f / MathF.PI);
            float halfArc = attack.ArcRadians * (180f / MathF.PI) / 2f;

            var color = new Color(SlashColor.R, SlashColor.G, SlashColor.B,
                (byte)(SlashColor.A * alpha));

            // Рисуем дугу как набор линий
            int segments = 8;
            float startAngle = baseAngle - halfArc;
            float endAngle = baseAngle + halfArc;

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + (endAngle - startAngle) * i / segments;
                float rad = angle * MathF.PI / 180f;
                float ex = cx + MathF.Cos(rad) * attack.Radius;
                float ey = cy + MathF.Sin(rad) * attack.Radius;

                Raylib.DrawLineEx(
                    new System.Numerics.Vector2(cx, cy),
                    new System.Numerics.Vector2(ex, ey),
                    2f, color);

                // Линия по дуге
                if (i > 0)
                {
                    float prevAngle = startAngle + (endAngle - startAngle) * (i - 1) / segments;
                    float prevRad = prevAngle * MathF.PI / 180f;
                    float px = cx + MathF.Cos(prevRad) * attack.Radius;
                    float py = cy + MathF.Sin(prevRad) * attack.Radius;

                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(px, py),
                        new System.Numerics.Vector2(ex, ey),
                        2f, color);
                }
            }
        }
    }

    private void DrawEnemyHealthBars()
    {
        var world = _ctx.World;
        int ts = _ctx.Config.ScaledTileSize;
        var map = _ctx.Map;

        world.QueryInto<EnemyTag, Health>(_enemyBuffer);
        foreach (int id in _enemyBuffer)
        {
            if (!world.Has<Position>(id)) continue;
            ref var pos = ref world.Get<Position>(id);
            ref var health = ref world.Get<Health>(id);

            // Только на видимых тайлах
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            int barW = (int)(ts * 0.8f);
            int barH = 4;
            int barX = (int)(pos.X + ts * 0.1f);
            int barY = (int)(pos.Y - 6);

            float fraction = (float)health.HP / health.MaxHP;

            Raylib.DrawRectangle(barX, barY, barW, barH, HpBarBg);
            Raylib.DrawRectangle(barX, barY, (int)(barW * fraction), barH, HpBarFg);
        }
    }

    private void DrawDamageNumbers()
    {
        var world = _ctx.World;

        world.QueryInto<DamageNumber>(_dmgNumBuffer);
        foreach (int id in _dmgNumBuffer)
        {
            ref var num = ref world.Get<DamageNumber>(id);

            float alpha = Math.Clamp(num.TimeRemaining / num.Lifetime, 0f, 1f);
            var baseColor = num.IsCrit ? DmgColorCrit : DmgColorNormal;
            var color = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)(255 * alpha));

            int fontSize = num.IsCrit ? 20 : 16;
            string text = num.Value.ToString();

            Raylib.DrawText(text, (int)num.WorldX, (int)num.WorldY, fontSize, color);
        }
    }
}
