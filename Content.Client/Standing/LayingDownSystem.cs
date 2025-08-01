using Content.Shared.Buckle;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Standing;

public sealed class LayingDownSystem : SharedLayingDownSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LayingDownComponent, MoveEvent>(OnMovementInput);
    }

    public override void Update(float frameTime)
    {
        // Update draw depth of laying down entities as necessary
        var query = EntityQueryEnumerator<LayingDownComponent, StandingStateComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var layingDown, out var standing, out var sprite))
        {
            // Do not modify the entities draw depth if it's modified externally
            if (sprite.DrawDepth != layingDown.NormalDrawDepth && sprite.DrawDepth != layingDown.CrawlingUnderDrawDepth)
                continue;

            sprite.DrawDepth = standing.CurrentState is StandingState.Lying && layingDown.IsCrawlingUnder
                ? layingDown.CrawlingUnderDrawDepth
                : layingDown.NormalDrawDepth;
        }

        query.Dispose();
    }

    private void OnMovementInput(EntityUid uid, LayingDownComponent component, MoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted
            || !_standing.IsDown(uid)
            || _buckle.IsBuckled(uid)
            || _animation.HasRunningAnimation(uid, "rotate")
            || !TryComp<TransformComponent>(uid, out var transform)
            || !TryComp<SpriteComponent>(uid, out var sprite)
            || !TryComp<RotationVisualsComponent>(uid, out var rotationVisuals))
            return;

        var rotation = transform.LocalRotation + (_eyeManager.CurrentEye.Rotation - (transform.LocalRotation - transform.WorldRotation));

        if (rotation.GetDir() is Direction.SouthEast or Direction.East or Direction.NorthEast or Direction.North)
        {
            rotationVisuals.HorizontalRotation = Angle.FromDegrees(270);
            sprite.Rotation = Angle.FromDegrees(270);
            return;
        }

        rotationVisuals.HorizontalRotation = Angle.FromDegrees(90);
        sprite.Rotation = Angle.FromDegrees(90);
    }
}
