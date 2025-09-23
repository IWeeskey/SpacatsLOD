### Общее устройство LOD-системы Spacats LOD
Система разделена на две подсистемы:
- `StaticLOD` — для объектов, чьи трансформы не меняются (или меняются редко). Делает расчёты по кэшированным данным, поддерживает вычисление дистанции до ориентированного параллелепипеда (OBB) через `Cuboid`.
- `DynamicLOD` — для движущихся объектов. Читает позиции/скейлы трансформов напрямую внутри джоба через `TransformAccessArray`.

Обе подсистемы:
- используют общий набор утилит (`LodUtils`, `LodDistances`),
- имеют контроллер, который управляет регистрацией юнитов (`SLodUnit`/`DLodUnit`), планированием джобов, сбором результатов и уведомлением о смене LOD,
- поддерживают как синхронный расчёт на основном потоке (Instant), так и асинхронный расчёт в фоновых потоках (Jobs + Burst),
- поддерживают групповые множители дистанции (`GroupMultipliers`), позволяющие масштабировать пороги LOD для целых групп объектов,
- отдают событие/колбэк смены уровня детализации в `LodUnitReciever` на каждом объекте.

Ключевые файлы:
- Контроллеры: `StaticLODController.cs`, `DynamicLODController.cs`
- Юниты: `SLodUnit.cs`, `DLodUnit.cs`
- Джобы: `SLodJob.cs`, `DLodJob.cs`
- Общие: `LodUtils.cs`, `LodDistances.cs`, `LodUnitReciever.cs`, `Cuboid.cs`
- Примеры получателей: `LODRMaterial.cs`, `LODRSpriteColor.cs`

---

### StaticLOD vs DynamicLOD: что и как отличается
#### StaticLOD
- Юнит: `SLodUnit` с данными `SLodUnitData`.
- Данные кэшируются внутри юнита: `Position`, `Rotation`, `Scale`, `Distances`, `GroupIndex`, флаг `CuboidCalculations` и `CuboidData` (если нужно OBB).
- Контроллер (`StaticLODController`) оперирует массивом `NativeArray<SLodUnitData>` и планирует `SLodJob : IJobParallelFor`.
- Дистанция может считаться:
  - до точки центра (`math.distance(TargetPosition, unit.Position)`), или
  - до ориентированного параллелепипеда (`LodUtils.DistanceToOBB(...)`), если включён `CuboidCalculations`.
- Нет чтения `Transform` внутри джоба — меньше contention, быстро для больших статичных наборов.

Фрагмент `SLodJob`:
```csharp
[BurstCompile]
public struct SLodJob : IJobParallelFor
{
    public float3 TargetPosition;
    public NativeArray<SLodUnitData> UnitsData;
    public NativeList<int2>.ParallelWriter ChangedLodsWriter;
    [ReadOnly] public NativeList<float> GroupMultipliers;

    public void Execute(int index)
    {
        SLodUnitData unit = UnitsData[index];
        float distance = unit.CuboidCalculations 
            ? LodUtils.DistanceToOBB(TargetPosition, unit.Position, unit.CuboidData, unit.Rotation)
            : math.distance(TargetPosition, unit.Position);

        float mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers);
        int lod = LodUtils.LevelForDistance(distance, in unit.Distances, unit.Scale, mult);
        if (lod != unit.CurrentLod)
        {
            unit.CurrentLod = lod;
            ChangedLodsWriter.AddNoResize(new int2(index, lod));
            UnitsData[index] = unit;
        }
        UnitsData[index] = unit;
    }
}
```

#### DynamicLOD
- Юнит: `DLodUnit` с данными `DLodUnitData` (минимально необходимые — индексы, текущий LOD, пороги и т. п.).
- Контроллер (`DynamicLODController`) держит `TransformAccessArray` параллельно с `NativeArray<DLodUnitData>` и планирует `DLodJob : IJobParallelForTransform`.
- Джоб читает актуальные `transform.position` и `transform.localScale.x` прямо внутри `Execute` — подходит для движущихся объектов.

Фрагмент `DLodJob`:
```csharp
[BurstCompile]
public struct DLodJob : IJobParallelForTransform
{
    public float3 TargetPosition;
    public NativeArray<DLodUnitData> UnitsData;
    public NativeList<int2>.ParallelWriter ChangedLodsWriter;
    [ReadOnly] public NativeList<float> GroupMultipliers;

    public void Execute(int index, TransformAccess transform)
    {
        DLodUnitData unit = UnitsData[index];
        float distance = math.distance(TargetPosition, transform.position);
        float mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers);
        int lod = LodUtils.LevelForDistance(distance, in unit.Distances, transform.localScale.x, mult);
        if (lod != unit.CurrentLod)
        {
            unit.CurrentLod = lod;
            ChangedLodsWriter.AddNoResize(new int2(index, lod));
            UnitsData[index] = unit;
        }
        UnitsData[index] = unit;
    }
}
```

#### Ключевые отличия
- Источник позиции/масштаба:
  - StaticLOD: берёт из кэшированных данных юнита, вы заполняете их при регистрации (`SLodUnit.RefreshSelfData()`), подходит для статичных объектов.
  - DynamicLOD: читает напрямую из `Transform` в джобе, подходит для постоянно движущихся объектов.
- Геометрия для дистанции:
  - StaticLOD: может использовать `Cuboid` (OBB) — точнее для больших объектов.
  - DynamicLOD: считает до точки (центра трансформа) для простоты и скорости.
- Массивы в джобе:
  - StaticLOD: `IJobParallelFor` + `NativeArray<SLodUnitData>`.
  - DynamicLOD: `IJobParallelForTransform` + `TransformAccessArray` + `NativeArray<DLodUnitData>`.

---

### Как используются Jobs и Burst
- Оба джоба помечены атрибутом `[BurstCompile]`, что включает компиляцию Burst — значительный прирост производительности и SIMD-оптимизации.
- Контроллеры планируют джобы асинхронно при `LodSettings.AsyncLogic = true` и снимают результаты по `JobHandle.IsCompleted`:
  - `ScheduleJob(_runtimeData)` ставит джоб в очередь, сохраняет `LodJobHandle` и помечает `JobScheduled`.
  - В `CSharedUpdate()` вызывается `TryCompleteJob()` — если джоб завершён, вызывается `Complete()`, далее:
    - `HandleJobResult()` — проходит по собранному списку `ChangedLods` и вызывает `unit.ChangeLOD(newLevel)` у соответствующих юнитов;
    - `ProcessRequests()` — обрабатывает очереди добавления/удаления юнитов в структурe данных контроллера.
- При `LodSettings.PerformMeasurements` включена простая телеметрия (`TimeTracker`) для времени джоба и общего времени обновления.
- Есть режим Instant (синхронный) — `ProcessInstant()` выполняет ту же логику без планирования, сразу на главном потоке.

---

### Что такое `LodUnitReciever`
`LodUnitReciever` — абстрактный компонент-получатель уведомлений об изменении LOD уровня:
```csharp
[ExecuteInEditMode]
public abstract class LodUnitReciever: MonoBehaviour
{
    public abstract void OnLodChanged(int newLevel);
}
```
Вы вешаете конкретную реализацию на объект вместе с `SLodUnit`/`DLodUnit`. Когда система определяет новый LOD, юнит вызывает `receiver.OnLodChanged(lod)`.

Пример (`LODRMaterial`) меняет материал/видимость рендерера:
```csharp
public class LODRMaterial : LodUnitReciever
{
    public MeshRenderer TargetMRenderer;
    public SkinnedMeshRenderer TargetSMRenderer;
    public List<Material> LODMaterials;
    public int MaxLODLevel = 5;
    public bool DisableOnMaxLOD = false;

    public override void OnLodChanged(int newLevel)
    {
        if (TargetMRenderer == null && TargetSMRenderer == null) return;
        if (LODMaterials == null) return;

        if (newLevel >= MaxLODLevel)
        {
            SetMaterial(LODMaterials.Count - 1);
            if (DisableOnMaxLOD)
            {
                if (TargetMRenderer != null) TargetMRenderer.enabled = false;
                if (TargetSMRenderer != null) TargetSMRenderer.enabled = false;
            }
            return;
        }

        if (TargetMRenderer != null) TargetMRenderer.enabled = true;
        if (TargetSMRenderer != null) TargetSMRenderer.enabled = true;
        SetMaterial(newLevel);
    }

    private void SetMaterial(int matIndex)
    {
        if (matIndex > LODMaterials.Count || matIndex < 0) return;
        if (TargetMRenderer != null) TargetMRenderer.material = LODMaterials[matIndex];
        if (TargetSMRenderer != null) TargetSMRenderer.material = LODMaterials[matIndex];
    }
}
```

---

### Что такое `Cuboid`
`Cuboid` — сериализуемая структура с размерами параллелепипеда и удобным свойством половинного размера:
```csharp
[Serializable]
public struct Cuboid
{
    public float3 Size;        // полные размеры по осям
    public float3 HalfSize => Size * 0.5f;
    public Cuboid(float3 size) { Size = size; }
}
```
Используется в `StaticLOD` для вычисления расстояния до ориентированного параллелепипеда (OBB). Формула в `LodUtils.DistanceToOBB` переводит целевую точку в локальные координаты кубоида (с учётом вращения), вычисляет выход за половинные размеры по осям и берёт длину получившегося вектора — это и есть минимальная дистанция до OBB:
```csharp
public static float DistanceToOBB(float3 target, float3 cuboidCenter, in Cuboid cuboid, quaternion rotation)
{
    float3 localPoint = mul(conjugate(rotation), target - cuboidCenter);
    float3 halfSize = cuboid.HalfSize;
    float3 delta = max(abs(localPoint) - halfSize, 0);
    return length(delta);
}
```
Преимущество: для больших/продолговатых статичных объектов порог LOD будет срабатывать по реальной «оболочке», а не по расстоянию до центра, что повышает визуальную корректность.

---

### Как определяется уровень LOD
Базовая функция порогов — `LodUtils.LevelForDistance`:
```csharp
public static int LevelForDistance(float distance, in LodDistances distances, float scale, float mult)
{
    float totalMult = scale * mult; // учитывается масштаб объекта и групповой множитель
    if (distance <= distances.Lod0 * totalMult) return 0;
    else if (distance <= distances.Lod1 * totalMult) return 1;
    else if (distance <= distances.Lod2 * totalMult) return 2;
    else if (distance <= distances.Lod3 * totalMult) return 3;
    else if (distance <= distances.Lod4 * totalMult) return 4;
    else return 5; // «за пределами» настроенных порогов
}
```
`LodDistances` — сериализуемая пятёрка порогов (`Lod0..Lod4`). Уровень `5` часто трактуется как «самый дальний/выключение».

---

### Жизненный цикл: регистрация, апдейт, снятие
- Юнит на объекте (`SLodUnit` или `DLodUnit`) при включении добавляет себя в контроллер (для `Static` можно вручную через флаг `RegisterOnEnable` или метод):
  - `DLodUnit.OnEnable()` вызывает `RequestAdd()` → `DynamicLODController.AddRequest(this, Add)`.
  - `SLodUnit` может вызвать `RequestAddLOD()` после `RefreshSelfData()`.
- Контроллер хранит компактные структуры (`List` юнитов, `NativeArray` данных, `TransformAccessArray` для Dynamic) и «ринг-буфер» запросов на добавление/удаление, чтобы не ломать индексацию во время работы джоба. Удаление выполнено swap-back’ом для O(1).
- Апдейт (`CSharedUpdate`) раз в `LodSettings.UpdateRateMS` миллисекунд:
  - обновляет целевую позицию (`TargetPosition`) от `LodSettings.Target` или из активной `SceneView` камеры в редакторе,
  - запускает синхронный или асинхронный проход по всем юнитам,
  - применяет изменения (`unit.ChangeLOD(level)`) и вызывает `LodUnitReciever.OnLodChanged(level)`.

---

### Как пользоваться: пошаговые примеры
#### 1) DynamicLOD для движущихся объектов
- На сцену добавьте пустой объект `DynamicLODController` и задайте цель `LodSettings.Target` (например, камеру игрока). Включите `AsyncLogic`, при необходимости настройте `UpdateRateMS`, `GroupMultipliers`.
- На каждый движущийся объект повесьте:
  - `DLodUnit` и укажите `LODData.Distances`, `LODData.GroupIndex`;
  - реализацию `LodUnitReciever` для реакции на уровни LOD (например, `LODRMaterial`).

Код/настройки в инспекторе несложны, но можно и программно:
```csharp
public class SetupDynamic : MonoBehaviour
{
    public Transform playerCam;
    public DLodUnit unit;

    void Start()
    {
        // настроить контроллер
        var ctrl = FindObjectOfType<DynamicLODController>();
        ctrl.LodSettings.Target = playerCam;
        ctrl.LodSettings.AsyncLogic = true;
        ctrl.LodSettings.UpdateRateMS = 50; // 20 Гц
        ctrl.SetGroupMultipliers(new List<float> { 1f, 1.5f, 2f });

        // настроить юнит
        unit.LODData.Distances = new LodDistances { Lod0 = 10, Lod1 = 20, Lod2 = 30, Lod3 = 45, Lod4 = 60 };
        unit.LODData.GroupIndex = 0;
        // receiver на объект уже повешен (например, LODRMaterial)
        // регистрация произойдёт в OnEnable() автоматически
    }
}
```

#### 2) StaticLOD для статичных пропов с простой (сферой) дистанцией
- Добавьте `StaticLODController` на сцену и укажите `LodSettings.Target`.
- На каждый статичный объект:
  - повесьте `SLodUnit`;
  - заполните пороги `LODData.Distances`, при необходимости `GroupIndex`;
  - включите `RegisterOnEnable = true` или вызовите `RequestAddLOD()` вручную после `RefreshSelfData()`.

```csharp
public class SetupStaticSimple : MonoBehaviour
{
    public Transform playerCam;
    public SLodUnit unit;

    void Start()
    {
        var ctrl = FindObjectOfType<StaticLODController>();
        ctrl.LodSettings.Target = playerCam;
        ctrl.LodSettings.AsyncLogic = true;
        ctrl.LodSettings.UpdateRateMS = 100;

        unit.LODData.Distances = new LodDistances { Lod0 = 5, Lod1 = 12, Lod2 = 20, Lod3 = 30, Lod4 = 45 };
        unit.LODData.GroupIndex = 1;
        unit.RegisterOnEnable = true; // зарегистрируется сам при включении
    }
}
```

#### 3) StaticLOD с `Cuboid` (точное расстояние до OBB)
- В `SLodUnit` включите `LODData.CuboidCalculations = true` и задайте `LODData.CuboidData.Size` приблизительно равный реальным габаритам объекта. Система будет считать расстояние до поверхности этого OBB.

```csharp
public class SetupStaticCuboid : MonoBehaviour
{
    public Transform playerCam;
    public SLodUnit unit;

    void Start()
    {
        var ctrl = FindObjectOfType<StaticLODController>();
        ctrl.LodSettings.Target = playerCam;
        ctrl.LodSettings.AsyncLogic = true;

        unit.LODData.Distances = new LodDistances { Lod0 = 8, Lod1 = 16, Lod2 = 28, Lod3 = 40, Lod4 = 60 };
        unit.LODData.GroupIndex = 0;

        unit.LODData.CuboidCalculations = true;
        unit.LODData.CuboidData = new Cuboid(new float3(2f, 3f, 5f)); // ширина, высота, глубина в мировых единицах

        // Важно: для Static нужно актуализировать кэшированную позу перед регистрацией
        unit.SendMessage("RefreshSelfData", SendMessageOptions.DontRequireReceiver);
        unit.RequestAddLOD();
    }
}
```

Советы по `Cuboid`:
- Старайтесь задавать `Size` по реальным границам визуального меша. Слишком большой размер даст «раннее» повышение LOD; слишком маленький — «позднее».
- Для высокого количества объектов оставляйте `AsyncLogic = true`.

---

### Настройки контроллеров и полезные детали
- `LodSettings.Target`: трансформ, относительно которого считаются дистанции (обычно камера). В редакторе можно включить `UseEditorCamera`, тогда цель будет браться из активной `SceneView`.
- `UpdateRateMS`: период апдейта. Большие значения = реже перерасчёт = дешевле.
- `AsyncLogic`: включает расчёт через Jobs + Burst. При отключении используется синхронный `ProcessInstant()`.
- `MaxUnitCount`: максимальное число юнитов, под которое выделяются `NativeArray` и прочие буферы.
- `GroupMultipliers`: список множителей для групп; получить/обновить можно через `SetGroupMultipliers()`.
- Регистрация/удаление: делаются запросами и обрабатываются пакетом после завершения джоба; массивы поддерживаются компактными, удаление — через swap-back.
- Применение результатов: массив `ChangedLods` заполняется внутри джоба (`ParallelWriter`), дальше контроллер вызывает `unit.ChangeLOD(level)`.

---

### Мини-FAQ
- Почему у меня не меняется LOD? Проверьте, что:
  - `LodSettings.Target` задан и реально обновляется;
  - включён `LodSettings.PerformLogic` и интервал `UpdateRateMS` подходит;
  - пороги `LodDistances` адекватны масштабу сцены;
  - для Static вы обновили кэш позы (`RefreshSelfData`) перед `RequestAddLOD()`;
  - на объекте действительно есть получатель (`LodUnitReciever`).
- Когда выбирать Static vs Dynamic?
  - Static: объекты почти не двигаются или их позы можно обновлять вручную по событиям. Даёт лучший throughput и точный `Cuboid`.
  - Dynamic: объекты двигаются каждый кадр — проще и надёжнее, но дороже из-за `TransformAccessArray`.

Если нужны примеры сцен — в проекте есть примеры в `Assets\SpacatsLOD\Examples` (включая «StaticLOD Cuboid»).
