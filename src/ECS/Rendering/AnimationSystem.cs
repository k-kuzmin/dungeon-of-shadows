using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Продвигает таймеры анимаций и переключает кадры.
/// Для сущностей с Animator — детектит смену клипа и сбрасывает Animation.
/// Для сущностей с только Animation (без Animator) — просто проигрывает текущий клип.
/// </summary>
public class AnimationSystem : ITickable
{
    private readonly World _world;
    private readonly List<int> _buffer = new();

    public AnimationSystem(World world)
    {
        _world = world;
    }

    public void Tick(float dt)
    {
        var world = _world;

        // Обновляем все сущности с Animation
        world.QueryInto<Animation>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            ref var anim = ref world.Get<Animation>(id);

            // Если есть Animator — проверяем смену клипа
            if (world.Has<Animator>(id))
            {
                ref var animator = ref world.Get<Animator>(id);
                var targetClip = animator.Clips[animator.CurrentClip];

                if (!ReferenceEquals(anim.Clip, targetClip))
                {
                    anim.Clip = targetClip;
                    anim.FrameIndex = 0;
                    anim.Timer = 0f;
                    anim.Finished = false;
                }
            }

            if (anim.Clip == null || anim.Finished) continue;

            // Продвигаем таймер
            anim.Timer += dt;

            while (anim.Timer >= anim.Clip.FrameDuration)
            {
                anim.Timer -= anim.Clip.FrameDuration;
                anim.FrameIndex++;

                if (anim.FrameIndex >= anim.Clip.FrameCount)
                {
                    if (anim.Clip.Loop)
                    {
                        anim.FrameIndex = 0;
                    }
                    else
                    {
                        anim.FrameIndex = anim.Clip.FrameCount - 1;
                        anim.Finished = true;
                        break;
                    }
                }
            }
        }
    }
}
