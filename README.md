<!-- AI-GEN AI-015: README draft written by Claude, for Christopher to rewrite in his own words. -->
# Physics

A 2D physics library in C# (.NET 8), written from scratch as the engine for my RPG platformer
(built in Godot 4). It's plain C# with no Godot dependency. It's general-purpose, with some
parts tailored to what the game needs.

## What it does

- **Shapes:** circles, line segments, arcs and quadratic curves. Edges join into open or closed
  edge lists, and each edge is solid from one side or both.
- **Bodies:** objects made of collider sets, with mass, center of mass and rotational inertia
  calculated from the shapes. Static, anchored and free objects. Collision groups choose how
  each pair responds (ignore, solid, overlap, soft).
- **Fixed time step** with semi-implicit Euler integration, time rate and pause.
- **Continuous collision detection:** finds the exact time of first contact for moving shapes,
  so fast objects can't tunnel through walls. Uses an exact cubic solver for the curve cases.
- **Pipeline:** swept bounding boxes (broad phase), then a slab test on relative motion
  (mid phase), then exact checks (narrow phase).
- Overlap tests, distance functions, and transforms (rotate, mirror, move).

## Status

Collision detection works end to end. Next is collision response: a sequential-impulse solver
with speculative contacts, then rotation, friction and bounce, and then constraints.
Arc vs. curve and curve vs. curve checks aren't done yet.

## Layout

- `Physics/`: the library (all code by me).
- `Physics.Tests/`: 641 xUnit tests. **The tests were written by Claude (AI)** to check my
  code, and each test file is marked as such.

Run the tests with `dotnet test` in `Physics.Tests/`.

## License

No license for now: all rights reserved.
