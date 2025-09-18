![Screenshot](Arts/SpaCats%20LogoSmall.png)

Spacats LOD.
Spacats LOD is a custom Level of Detail system designed to determine LOD levels based on the distance to a target Camera. Unlike Unity’s built-in LODGroup, it gives you direct access to the current LOD index, optimized with Jobs + Burst, and supports both static and dynamic use cases.

✨ FEATURES
* Works in the Editor – no need to hit Play just to test setups.
* Mobile-ready – tested and running smoothly on mobile devices.
* Jobified & Bursted – all LOD checks are done using Unity Jobs and Burst for high performance.
* Sync or Async – choose between synchronous updates or async jobs depending on your use case.
* Custom update interval – configure check frequency in milliseconds (every frame, or every few seconds).
* Two controller modules:
* DynamicLOD – for moving transforms (NPCs, birds, vehicles, etc.). Example scene: DynamicLOD.
* StaticLOD – for static props like trees, rocks, or buildings. Slightly faster since transform data doesn’t need to be rebuilt every frame. Example scene: StaticLOD.
* LOD change events – easily subscribe to LOD index changes on any LOD unit.
* Cuboid distance checks for static objects – instead of checking distance only to object center, you can use a custom cuboid. Perfect for elongated meshes (e.g. fences, bridges). Example scene: StaticLOD Cuboid.
* Smooth transitions – sample scenes show how to fade objects in/out using the lightweight Spacats.MonoTween. Examples: DynamicLOD Smooth and StaticLOD Smooth.

❓ WHY SPACATS LOD?
Unity’s built-in LOD system is great, but it doesn’t expose the current LOD level. Workarounds usually mean brute-forcing all objects every frame, checking LODGroups manually – which is heavy on the main thread.
Spacats LOD solves this by handling LOD logic entirely in Jobs, only passing changed results back to the main thread.

⚙️ HOW IT WORKS
1) Job step – all LOD units are processed in a Burst job, with results stored as changed indices.
2) Main thread step – the main thread only applies changes to affected GameObjects.

📊 Performance
(Coming soon – benchmarks will be added once profiling is complete.)

📦 DEPENDENCIES
1) Spacats.Utils (my helper package) (https://github.com/IWeeskey/SpacatsUtils.git)
2) "com.unity.burst": "1.8.24"
3) "com.unity.mathematics": "1.3.2"
4) "com.unity.collections": "2.5.7"

INSTALLATION
1) Ensure all required dependencies are installed
2) Open Unity and go to the top menu:
3) Window → Package Manager.
4) In the Package Manager window, click the "+" button in the top-left corner.
5) Select "Add package from git URL..." from the dropdown.
6) Paste the GitHub repository link:
https://github.com/IWeeskey/SpacatsLOD.git
6) Click "Add" and wait for Unity to download and install the package.
7) Once installed, you can find the scripts and assets inside your Packages folder in Unity. Also you can check out the examples.

Tested on Unity Versions:
2022.3.39f1
2023.2.20f1
