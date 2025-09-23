### Overview
This project implements a lightweight, jobified LOD (Level of Detail) system for Unity with two parallel pipelines:
- `DynamicLOD` — for moving objects that need per-frame transform reads.
- `StaticLOD` — for static props where transform doesn’t change often and a cached struct is enough.

Both pipelines share the core ideas:
- A singleton `...LODController` that owns data, schedules jobs, and applies LOD changes.
- Per-object `...LodUnit` components that describe LOD thresholds and receive LOD change callbacks.
- Highly parallel `IJobParallelFor(Transform)` jobs compiled with Burst that compute per-object LOD levels.
- A thread-safe change list so only objects whose level changed are updated on the main thread.

Below is the full story of how it all works, what’s different between Static and Dynamic modes, and how to use each one effectively.

---

### What a frame/update looks like
1) A `...LODController` keeps a `TargetPosition` (player/camera) and an update timer (`UpdateRateMS`).
2) When it’s time to update, it refreshes `TargetPosition` and either:
   - Runs `ProcessInstant()` — executes the LOD job immediately and completes it in the same frame.
   - Or runs `ProcessAsync()` — schedules the job and picks up results in subsequent frames via `TryCompleteJob()`.
3) Jobs compute distances and LOD levels in parallel, writing only “changed” results into a `NativeList<int2>` (`index`, `newLod`).
4) The controller completes the job (if async), pulls the changed indices, and calls `ChangeLOD(lod)` on the corresponding `...LodUnit`s. This invokes the unit’s `OnLodChanged` event and its `LodUnitReciever` implementation (if any) to apply visuals.

---

### DynamicLOD vs StaticLOD
#### Shared pieces
- `LodDistances` defines threshold distances for LOD0…LOD4. Anything farther becomes LOD5 (typically “culled” or minimal representation).
- `LodUtils.LevelForDistance(distance, distances, scale, groupMult)` applies thresholds and scaling.
- `GroupMultipliers` let you scale thresholds per logical group (e.g., “large trees” vs “small bushes”).
- Update cadence (`UpdateRateMS`), async/instant execution, performance measurement toggles, SceneView camera option in Editor are present in both.

#### DynamicLOD
- Controller: `DynamicLODController`
- Unit: `DLodUnit`
- Data: `DLodUnitData` (index, group index, current lod, distances)
- Job: `DLodJob : IJobParallelForTransform`
- Storage:
  - `TransformAccessArray UnitsTransform` to read live transforms inside the job.
  - `NativeArray<DLodUnitData> UnitsData` parallel with transforms by index.
  - `NativeList<int2> ChangedLods` for per-frame changes.
- Distance and scale source: `transform.position` and `transform.localScale.x` are read inside the job.
- Best for: NPCs, moving vehicles, birds — anything where transform changes frequently.

How it computes:
- In `DLodJob.Execute()`, it does:
  - `distance = math.distance(TargetPosition, transform.position)`
  - `mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers)`
  - `lod = LodUtils.LevelForDistance(distance, unit.Distances, transform.localScale.x, mult)`
  - If changed, writes `(index, lod)` to `ChangedLods` and updates `UnitsData[index].CurrentLod`.

#### StaticLOD
- Controller: `StaticLODController`
- Unit: `SLodUnit`
- Data: `SLodUnitData` (index, cached `Position`, `Rotation`, `Scale`, `GroupIndex`, `CurrentLod`, `Distances`, plus `CuboidCalculations` and `Cuboid CuboidData`)
- Job: `SLodJob : IJobParallelFor`
- Storage:
  - `NativeArray<SLodUnitData> UnitsData` which holds the cached transform info.
  - `NativeList<int2> ChangedLods` for per-frame changes.
- Distance and scale source:
  - Either a simple point distance to the cached `Position`.
  - Or a cuboid-based OBB distance (see “Cuboid” below) using cached `Position`, `Rotation`, and `CuboidData`.
- Best for: Trees, rocks, buildings — objects that don’t move or don’t need per-frame transform reads.

How it computes:
- In `SLodJob.Execute()`:
  - If `unit.CuboidCalculations` is true: `distance = LodUtils.DistanceToOBB(TargetPosition, unit.Position, unit.CuboidData, unit.Rotation)`.
  - Else: `distance = math.distance(TargetPosition, unit.Position)`.
  - `lod = LodUtils.LevelForDistance(distance, unit.Distances, unit.Scale, mult)`; on change, write `(index, lod)`.

Key difference: Dynamic reads live `Transform` inside the job (through `TransformAccessArray`), Static reads pre-cached `Position/Rotation/Scale` from `NativeArray` (and can use the Cuboid OBB distance). Static avoids Transform overhead and is slightly faster when objects don’t move.

---

### How jobs and Burst are used efficiently
- Burst compilation:
  - `DLodJob` and `SLodJob` are annotated with `[BurstCompile]` for SIMD-friendly, optimized code.
  - `LodUtils` is also `[BurstCompile]`, so helpers like `DistanceToOBB` and `LevelForDistance` are optimized.
- Parallel scheduling:
  - Dynamic: `IJobParallelForTransform` scheduled with `LodJob.ScheduleReadOnlyByRef(UnitsTransform, 64)`; batch size 64.
  - Static: `IJobParallelFor` scheduled with `LodJob.Schedule(Units.Count, 64)`; batch size 64.
- Reduced main-thread work:
  - Jobs only push indices whose LOD changed via `NativeList<int2>.ParallelWriter`, so the main thread only calls `ChangeLOD` on those few.
- Data-oriented containers:
  - `NativeArray<T>` for unit data, `NativeList<T>` for `ChangedLods` and `GroupMultipliers` to keep contiguous memory.
  - Dynamic uses `TransformAccessArray` to read transforms at scale without marshaling per element.
- Async model:
  - Async scheduling lets heavy LOD passes be split from main-thread application. `TryCompleteJob()` polls completion, then applies results and processes pending unit add/remove requests.
- Measurement hooks:
  - Optional timing with `TimeTracker` using `JobMeasureID` and `TotalMeasureID` helps to tune `UpdateRateMS`, async mode, and batch sizes.

---

### What is `LodUnitReciever`?
- An abstract component you can add to a `...LodUnit` GameObject to react visually to LOD changes.
- API:
  - `abstract void OnLodChanged(int newLevel);`
- Example implementations:
  - `LODRMaterial` — swaps materials on a `MeshRenderer`/`SkinnedMeshRenderer`, can disable renderer at max LOD.
  - `LODRSpriteColor` / `LODRSpriteColorSmooth` — change sprite color (with optional smoothing) per LOD.
- How it’s called:
  - A unit’s `ChangeLOD(int value)` calls `OnLodChanged?.Invoke(value)` and then `_receiver?.OnLodChanged(value)`.
  - You can also subscribe to `OnLodChanged` for custom logic.

---

### What is `Cuboid` and what does it mean here?
- `Cuboid` is a serializable struct containing `float3 Size` with a convenience `HalfSize` property.
- In Static LOD, if `SLodUnitData.CuboidCalculations` is true, distance is computed to an oriented bounding box (OBB) roughly representing the object’s extents, not just to its center.
- The distance function:
  - `LodUtils.DistanceToOBB(target, cuboidCenter, cuboid, rotation)` transforms the target point into the cuboid’s local space using the conjugate of `rotation`, clamps to the box extents (`HalfSize`), and returns the length of the remaining delta. If the point is inside, distance is 0.
- Why it matters:
  - Cuboid-based distances improve precision for large objects: the player can be “close” to the object’s surface even if they’re far from the center, leading to better LOD selection.
- Gizmo support:
  - `LodMeshGizmoData` holds a mesh and material for drawing OBB (used by the editor scripts) to visualize LOD shells.

---

### Controllers: lifecycle and requests
- Both controllers are `ExecuteInEditMode` singletons with `DefaultExecutionOrder(-10)` so they run early.
- Registration lifecycle:
  - `COnRegister()` sets the static instance and calls `Clear()`.
  - `Clear()` -> `Dispose()` existing data, then `Create()` new `...DisposeData` with containers sized by `MaxUnitCount`.
  - On `OnDisable` they `Dispose()` and stop jobs.
- Update loop:
  - `CSharedUpdate()` checks `PerformLogic`, calls `TryCompleteJob()`, and if time elapsed (`UpdateRateMS`) — refreshes target, then instant/async path.
- Target selection:
  - If `UseEditorCamera` and Editor SceneView exists (and not in Play mode), uses SceneView camera position.
  - Else uses `LodSettings.Target.position`. If none, logs and uses zero.
- Add/remove requests from units:
  - Stored in a dictionary so duplicates coalesce. `ProcessRequests()` runs after job completion to keep data consistent.
  - On Add: assigns `UnitIndex`, pushes unit references and their data to arrays (`Units`, `UnitsData`, and for Dynamic also `UnitsTransform`).
  - On Remove: swap-with-last to keep arrays packed; updates indices accordingly.

---

### Basic usage: DynamicLOD
1) Scene setup
   - Add a `[SpaCats] DynamicLODController` to the scene.
   - In the controller’s `LodSettings`:
     - Set `Target` to your camera/player transform.
     - Optionally enable `UseEditorCamera` for SceneView testing.
     - Choose `PerformLogic = true`.
     - Choose `AsyncLogic` on/off (try async for heavy scenes).
     - Set `UpdateRateMS` (e.g., `16` for ~60 Hz; increase to reduce cost).
     - Configure `GroupMultipliers` list (e.g., `[1.0, 1.5, 2.0]`).

2) Create LODded object
   - Add `DLodUnit` to your GameObject.
   - Fill its `LODData.Distances` (LOD0…LOD4 thresholds).
   - Optionally set `LODData.GroupIndex` to pick a `GroupMultipliers` entry.
   - Add a `LodUnitReciever` implementation, e.g. `LODRMaterial`, and assign the renderers and materials.

3) Play
   - The controller registers units on enable (`DLodUnit.OnEnable()` calls `RequestAdd()`), schedules jobs and updates lods.

Example receiving component:
```csharp
public class MyLodHider : LodUnitReciever
{
    public GameObject HighDetail;
    public GameObject MidDetail;
    public GameObject LowDetail;

    public override void OnLodChanged(int newLevel)
    {
        HighDetail.SetActive(newLevel <= 1);
        MidDetail.SetActive(newLevel == 2 || newLevel == 3);
        LowDetail.SetActive(newLevel >= 4);
    }
}
```

---

### Basic usage: StaticLOD
1) Scene setup
   - Add a `[SpaCats] StaticLODController` to the scene.
   - Configure `LodSettings` same as Dynamic (target, update rate, async, groups, etc.).

2) Create a static LOD object
   - Add `SLodUnit` to your GameObject.
   - Toggle `RegisterOnEnable` if you want it to auto-register.
   - Fill `LODData.Distances` and optionally:
     - Enable `LODData.CuboidCalculations` and set `LODData.CuboidData.Size` to approximate the object’s bounds.
     - Set `LODData.GroupIndex`.
   - Add a `LodUnitReciever` (e.g., `LODRMaterial`).

3) Registering
   - `SLodUnit.RequestAddLOD()` caches `Position/Rotation/Scale` into `LODData` and queues an Add request.
   - If your static object moves at runtime, call `SLodUnit.Refresh()` to recache transform and re-register.

---

### Tips for performance and quality
- Prefer StaticLOD for non-moving props: it avoids Transform reads and supports Cuboid distance — faster and more accurate.
- Use `AsyncLogic` in heavy scenes: it decouples computation from application. Ensure your content tolerates 1+ frame of latency for LOD switches.
- Increase `UpdateRateMS` to reduce CPU cost, especially on large crowds/forests.
- Keep `MaxUnitCount` realistic: it sizes native arrays. The default is high (1,000,000) to be safe; lower it in your scene to save memory.
- Group multipliers: use `GroupIndex` to bias LODs per category (e.g., make buildings pop to lower detail earlier than characters).
- Cuboid sizes: set `CuboidData.Size` to your actual object bounds; too large and the object will stay in high LOD too long, too small and it will drop too early.
- Smooth transitions: try `LODRSpriteColorSmooth` or add your own receiver to fade meshes/materials when LOD changes.

---

### Minimal code examples
- Changing the controller target at runtime:
```csharp
// e.g., when your camera spawns or switches
DynamicLODController.Instance.SetTarget(mainCamera.transform);
StaticLODController.Instance.SetTarget(mainCamera.transform);
```

- Manually registering a static unit after moving it in editor:
```csharp
var sUnit = GetComponent<SLodUnit>();
sUnit.Refresh(); // Re-caches Position/Rotation/Scale and re-queues add
```

- Subscribing to LOD changes without a receiver:
```csharp
void Awake()
{
    var dUnit = GetComponent<DLodUnit>();
    dUnit.OnLodChanged += level => Debug.Log($"LOD changed to {level}");
}
```

---

### File map (for deeper reading)
- Dynamic pipeline:
  - `DynamicLODController.cs`, `DLodUnit.cs`, `DLodDisposeData.cs`, `DLodJob.cs`, `DLodUnitData.cs`, `DLodSettings.cs`
- Static pipeline:
  - `StaticLODController.cs`, `SLodUnit.cs`, `SLodDisposeData.cs`, `SLodJob.cs`, `SLodUnitData.cs`, `SLodSettings.cs`
- Shared:
  - `LodUtils.cs`, `LodDistances.cs`, `LodUnitReciever.cs`, receivers like `LODRMaterial.cs`, sprites, and `Cuboid.cs`