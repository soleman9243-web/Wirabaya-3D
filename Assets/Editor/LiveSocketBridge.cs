#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Live Socket Bridge between Antigravity AI and Unity Editor.
/// Listens on http://127.0.0.1:7890/ and safely dispatches commands to Unity's main thread.
/// </summary>
[InitializeOnLoad]
public static class LiveSocketBridge
{
    private const int PORT = 7890;
    private static HttpListener listener;
    private static Thread listenerThread;
    private static bool isRunning = false;

    private class PendingRequest
    {
        public HttpListenerContext Context;
        public string Method;
        public string Path;
        public string Body;
        public Action<string, int> ResponseCallback;
    }

    private static readonly ConcurrentQueue<PendingRequest> requestQueue = new ConcurrentQueue<PendingRequest>();

    private static double nextRetryTime = 0d;

    static LiveSocketBridge()
    {
        StartServer();
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.quitting -= StopServer;
        EditorApplication.quitting += StopServer;
        AssemblyReloadEvents.beforeAssemblyReload -= StopServer;
        AssemblyReloadEvents.beforeAssemblyReload += StopServer;
    }

    public static void StartServer()
    {
        if (isRunning) return;

        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{PORT}/");
            listener.Start();
            isRunning = true;

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "LiveSocketBridge_Listener"
            };
            listenerThread.Start();
            Debug.Log($"<color=#4CAF50><b>[LiveSocketBridge]</b></color> Live Socket Server aktif di <b>http://127.0.0.1:{PORT}/</b>");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LiveSocketBridge] Gagal memulai listener di port {PORT}: {ex.Message}");
            isRunning = false;
        }
    }

    public static void StopServer()
    {
        isRunning = false;
        try
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
            }
        }
        catch { }
        listener = null;
    }

    private static void ListenLoop()
    {
        while (isRunning && listener != null && listener.IsListening)
        {
            try
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem((_) => ProcessIncomingContext(context));
            }
            catch (HttpListenerException)
            {
                // Listener stopped
                break;
            }
            catch (Exception ex)
            {
                if (isRunning) Debug.LogWarning($"[LiveSocketBridge] Error accept context: {ex.Message}");
            }
        }
    }

    private static void ProcessIncomingContext(HttpListenerContext context)
    {
        try
        {
            string body = "";
            if (context.Request.HasEntityBody)
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }
            }

            var path = context.Request.Url.AbsolutePath.Trim().ToLowerInvariant();
            var method = context.Request.HttpMethod.ToUpperInvariant();

            // Queue to Unity Main Thread
            var req = new PendingRequest
            {
                Context = context,
                Method = method,
                Path = path,
                Body = body
            };

            requestQueue.Enqueue(req);
        }
        catch (Exception ex)
        {
            SendDirectResponse(context, $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}", 500);
        }
    }

    private static void OnEditorUpdate()
    {
        if (!isRunning && EditorApplication.timeSinceStartup >= nextRetryTime)
        {
            nextRetryTime = EditorApplication.timeSinceStartup + 2.0;
            StartServer();
        }

        while (requestQueue.TryDequeue(out var req))
        {
            ExecuteRequestOnMainThread(req);
        }
    }

    private static void ExecuteRequestOnMainThread(PendingRequest req)
    {
        try
        {
            string jsonResponse = "";
            int statusCode = 200;

            if (req.Path == "/ping" || req.Path == "/")
            {
                var scene = SceneManager.GetActiveScene();
                jsonResponse = $"{{\"status\":\"connected\",\"unityVersion\":\"{Application.unityVersion}\",\"activeScene\":\"{scene.name}\",\"rootCount\":{scene.rootCount},\"platform\":\"{Application.platform}\"}}";
            }
            else if (req.Path == "/create_audio_source")
            {
                jsonResponse = HandleCreateAudioSource(req.Body);
            }
            else if (req.Path == "/get_hierarchy")
            {
                jsonResponse = HandleGetHierarchy();
            }
            else if (req.Path == "/setup_bgm")
            {
                jsonResponse = HandleSetupBgm();
            }
            else if (req.Path == "/get_audio_info")
            {
                jsonResponse = HandleGetAudioInfo();
            }
            else if (req.Path == "/setup_house_audio")
            {
                jsonResponse = HandleSetupHouseAudio();
            }
            else if (req.Path == "/cleanup_house_zone")
            {
                jsonResponse = HandleCleanupHouseZone();
            }
            else if (req.Path == "/reimport_ui")
            {
                AssetDatabase.ImportAsset("Assets/8-12-2026/UI/SETTING (1).png", ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                jsonResponse = "{\"status\":\"ok\",\"reimported\":\"Assets/8-12-2026/UI/SETTING (1).png\"}";
            }
            else if (req.Path == "/inspect_settings")
            {
                jsonResponse = HandleInspectSettings();
            }
            else if (req.Path == "/organize_settings")
            {
                jsonResponse = HandleOrganizeSettings();
            }
            else if (req.Path == "/setup_frosted_glass")
            {
                jsonResponse = Wirabaya.EditorTools.UIFrostedGlassSetup.ApplySetup();
            }
            else if (req.Path == "/execute")
            {
                jsonResponse = HandleGenericExecute(req.Body);
            }
            else
            {
                statusCode = 404;
                jsonResponse = $"{{\"error\":\"Endpoint '{req.Path}' tidak ditemukan\"}}";
            }

            SendDirectResponse(req.Context, jsonResponse, statusCode);
        }
        catch (Exception ex)
        {
            SendDirectResponse(req.Context, $"{{\"error\":\"{EscapeJson(ex.ToString())}\"}}", 500);
        }
    }

    private static string HandleCreateAudioSource(string jsonBody)
    {
        var data = JsonUtility.FromJson<AudioSourceParams>(jsonBody);
        if (data == null) data = new AudioSourceParams();

        string objName = string.IsNullOrEmpty(data.name) ? "Audio_LiveObject" : data.name;
        GameObject go = new GameObject(objName);

        // Position
        Vector3 pos = Vector3.zero;
        if (data.position != null && data.position.Length >= 3)
        {
            pos = new Vector3(data.position[0], data.position[1], data.position[2]);
        }
        go.transform.position = pos;

        // Parent
        if (!string.IsNullOrEmpty(data.parent))
        {
            GameObject parentObj = GameObject.Find(data.parent);
            if (parentObj != null)
            {
                go.transform.SetParent(parentObj.transform, true);
            }
        }

        // AudioSource
        AudioSource audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = data.playOnAwake;
        audio.loop = data.loop;
        audio.volume = Mathf.Clamp01(data.volume <= 0f ? 1f : data.volume);
        audio.spatialBlend = Mathf.Clamp01(data.spatialBlend);
        if (data.minDistance > 0) audio.minDistance = data.minDistance;
        if (data.maxDistance > 0) audio.maxDistance = data.maxDistance;

        // Optional Clip search by name
        if (!string.IsNullOrEmpty(data.clipName))
        {
            string[] guids = AssetDatabase.FindAssets($"{data.clipName} t:AudioClip");
            if (guids != null && guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    audio.clip = clip;
                }
            }
        }

        // Register Undo and Selection
        Undo.RegisterCreatedObjectUndo(go, $"Create Live Audio {objName}");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);

        // Ping Scene View if open
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log($"<color=#00bcd4><b>[LiveSocketBridge]</b></color> Berhasil membuat AudioSource LIVE: <b>{objName}</b> di posisi {pos}");

        return $"{{\"status\":\"success\",\"name\":\"{go.name}\",\"instanceId\":{go.GetInstanceID()},\"position\":[{pos.x},{pos.y},{pos.z}]}}";
    }

    private static string HandleGetHierarchy()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var list = new List<string>();
        foreach (var r in roots)
        {
            list.Add($"\"{EscapeJson(r.name)}\"");
        }
        return $"{{\"scene\":\"{scene.name}\",\"rootObjects\":[{string.Join(",", list)}]}}";
    }

    private static string HandleGenericExecute(string jsonBody)
    {
        var cmd = JsonUtility.FromJson<CommandPayload>(jsonBody);
        if (cmd == null || string.IsNullOrEmpty(cmd.command))
        {
            return "{\"error\":\"Parameter 'command' diperlukan\"}";
        }

        switch (cmd.command.ToLowerInvariant())
        {
            case "ping":
                return $"{{\"status\":\"ok\",\"time\":\"{DateTime.Now:HH:mm:ss}\"}}";

            case "select":
                if (!string.IsNullOrEmpty(cmd.targetName))
                {
                    var target = GameObject.Find(cmd.targetName);
                    if (target != null)
                    {
                        Selection.activeGameObject = target;
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.FrameSelected();
                        return $"{{\"status\":\"selected\",\"name\":\"{target.name}\"}}";
                    }
                    return $"{{\"error\":\"Target '{cmd.targetName}' tidak ditemukan\"}}";
                }
                break;

            case "set_active":
                if (!string.IsNullOrEmpty(cmd.targetName))
                {
                    var target = GameObject.Find(cmd.targetName);
                    if (target != null)
                    {
                        Undo.RecordObject(target, "Set Active Live");
                        target.SetActive(cmd.boolValue);
                        EditorSceneManager.MarkSceneDirty(target.scene);
                        return $"{{\"status\":\"ok\",\"name\":\"{target.name}\",\"active\":{target.activeSelf.ToString().ToLower()}}}";
                    }
                    return $"{{\"error\":\"Target '{cmd.targetName}' tidak ditemukan\"}}";
                }
                break;

            case "playmode_toggle":
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
                return $"{{\"status\":\"ok\",\"isPlaying\":{EditorApplication.isPlaying.ToString().ToLower()}}}";

            case "setup_bgm":
                return HandleSetupBgm();
        }

        return $"{{\"status\":\"unknown_command\",\"command\":\"{cmd.command}\"}}";
    }

    private static string HandleSetupBgm()
    {
        GameObject go = null;

        // 1. Cek Selection aktif (misal user sedang klik AudioManager di Hierarchy)
        if (Selection.activeGameObject != null && Selection.activeGameObject.name.IndexOf("AudioManager", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            go = Selection.activeGameObject;
        }

        // 2. Cek apakah sudah ada komponen AudioManager di scene
        if (go == null)
        {
            var allManagers = UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
            if (allManagers != null && allManagers.Length > 0)
            {
                go = allManagers[0].gameObject;
            }
        }

        // 3. Cek rekursif seluruh objek di scene aktif (termasuk yang di dalam parent seperti '4TH WALL ------------')
        if (go == null)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                go = FindObjectRecursive(r, "AudioManager");
                if (go != null) break;
            }
        }

        // 4. Jika benar-benar belum ada, buat baru
        if (go == null)
        {
            go = new GameObject("AudioManager");
            Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
        }

        // Pastikan komponen AudioManager terpasang
        var manager = go.GetComponent<AudioManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<AudioManager>(go);
        }

        Undo.RecordObject(manager, "Setup 3 BGM Slots");
        SerializedObject so = new SerializedObject(manager);

        // Pastikan Master Volume 1.0f (100%)
        SerializedProperty bgmMasterProp = so.FindProperty("bgmMasterVolume");
        if (bgmMasterProp != null) bgmMasterProp.floatValue = 1f;

        SerializedProperty sfxMasterProp = so.FindProperty("sfxMasterVolume");
        if (sfxMasterProp != null) sfxMasterProp.floatValue = 1f;

        // Setup 3 BGM Slots
        SerializedProperty bgmProp = so.FindProperty("bgmSounds");
        if (bgmProp != null && bgmProp.isArray)
        {
            int targetCount = Mathf.Max(3, bgmProp.arraySize);
            bgmProp.arraySize = targetCount;

            string[] defaultNames = { "BGM_1", "BGM_2", "BGM_3" };
            for (int i = 0; i < 3; i++)
            {
                SerializedProperty elem = bgmProp.GetArrayElementAtIndex(i);
                var nameProp = elem.FindPropertyRelative("name");
                var volProp = elem.FindPropertyRelative("volume");
                var pitchProp = elem.FindPropertyRelative("pitch");
                var loopProp = elem.FindPropertyRelative("loop");

                if (nameProp != null && string.IsNullOrEmpty(nameProp.stringValue))
                    nameProp.stringValue = defaultNames[i];
                if (volProp != null)
                    volProp.floatValue = 1f; // 100%!
                if (pitchProp != null && pitchProp.floatValue <= 0f)
                    pitchProp.floatValue = 1f;
                if (loopProp != null)
                    loopProp.boolValue = true;
            }
        }

        so.ApplyModifiedProperties();

        // Setup SceneBGMStarter agar BGM otomatis mulai di scene ini
        var starter = go.GetComponent<SceneBGMStarter>();
        if (starter == null)
        {
            starter = Undo.AddComponent<SceneBGMStarter>(go);
        }
        SerializedObject starterSo = new SerializedObject(starter);
        SerializedProperty bgmNameProp = starterSo.FindProperty("bgmName");
        if (bgmNameProp != null)
        {
            if (string.IsNullOrEmpty(bgmNameProp.stringValue))
            {
                bgmNameProp.stringValue = "BGM_1";
            }
            starterSo.ApplyModifiedProperties();
        }

        // Fokuskan dan ping di Inspector agar Mas Nauval langsung melihat slotnya
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);

        // Update juga ke Prefab
        try
        {
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/AudioManager.prefab");
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/AudioManager.prefab");
        }
        catch { }

        Debug.Log($"<color=#00bcd4><b>[LiveSocketBridge]</b></color> Berhasil setup 3 slot BGM (Volume 100%) pada objek: <b>{go.name}</b> di scene {go.scene.name}");

        return $"{{\"status\":\"ok\",\"object\":\"{go.name}\",\"bgmCount\":3,\"path\":\"{GetHierarchyPath(go.transform)}\"}}";
    }

    private static GameObject FindObjectRecursive(GameObject current, string targetName)
    {
        if (current.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            return current;

        foreach (Transform child in current.transform)
        {
            var found = FindObjectRecursive(child.gameObject, targetName);
            if (found != null) return found;
        }

        return null;
    }

    private static string HandleGetAudioInfo()
    {
        var manager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
        if (manager == null) return "{\"error\":\"AudioManager not found\"}";

        var starter = manager.GetComponent<SceneBGMStarter>();
        SerializedObject so = new SerializedObject(manager);
        var bgmProp = so.FindProperty("bgmSounds");
        var bgmMaster = so.FindProperty("bgmMasterVolume")?.floatValue ?? -1f;
        var sfxMaster = so.FindProperty("sfxMasterVolume")?.floatValue ?? -1f;

        List<string> slots = new List<string>();
        if (bgmProp != null && bgmProp.isArray)
        {
            for (int i = 0; i < bgmProp.arraySize; i++)
            {
                var el = bgmProp.GetArrayElementAtIndex(i);
                var n = el.FindPropertyRelative("name")?.stringValue;
                var v = el.FindPropertyRelative("volume")?.floatValue ?? 0f;
                var p = el.FindPropertyRelative("pitch")?.floatValue ?? 0f;
                var l = el.FindPropertyRelative("loop")?.boolValue ?? false;
                var c = el.FindPropertyRelative("clip")?.objectReferenceValue != null ? el.FindPropertyRelative("clip").objectReferenceValue.name : "None";
                slots.Add($"{{\"slot\":{i},\"name\":\"{n}\",\"volume\":{v},\"pitch\":{p},\"loop\":{l.ToString().ToLower()},\"clip\":\"{c}\"}}");
            }
        }

        string starterBgm = "";
        if (starter != null)
        {
            SerializedObject starterSo = new SerializedObject(starter);
            starterBgm = starterSo.FindProperty("bgmName")?.stringValue ?? "";
        }

        var components = manager.GetComponents<Component>();
        List<string> compNames = new List<string>();
        foreach (var c in components) if (c != null) compNames.Add($"\"{c.GetType().Name}\"");

        return $"{{\"objectName\":\"{manager.gameObject.name}\",\"components\":[{string.Join(",", compNames)}],\"bgmMasterVolume\":{bgmMaster},\"sfxMasterVolume\":{sfxMaster},\"starterBgmName\":\"{starterBgm}\",\"slots\":[{string.Join(",", slots)}]}}";
    }

    private static string HandleSetupHouseAudio()
    {
        var scene = SceneManager.GetActiveScene();
        GameObject houseRoot = null;
        foreach (var r in scene.GetRootGameObjects())
        {
            houseRoot = FindObjectRecursive(r, "LINGKUNGAN RUMAH");
            if (houseRoot != null) break;
        }

        // Hitung bounds dari semua Renderer di dalam LINGKUNGAN RUMAH
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(12f, 6f, 12f));
        bool hasBounds = false;
        if (houseRoot != null)
        {
            var renderers = houseRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                if (!hasBounds)
                {
                    bounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
        }

        // Cari atau buat HouseAudioZone di dalam LINGKUNGAN RUMAH (atau root)
        var existingZone = UnityEngine.Object.FindAnyObjectByType<HouseAudioZone>();
        GameObject zoneObj;
        if (existingZone != null)
        {
            zoneObj = existingZone.gameObject;
        }
        else
        {
            zoneObj = new GameObject("HouseAudioZone");
            Undo.RegisterCreatedObjectUndo(zoneObj, "Create HouseAudioZone");
            if (houseRoot != null)
            {
                zoneObj.transform.SetParent(houseRoot.transform, true);
            }
        }

        if (hasBounds)
        {
            zoneObj.transform.position = bounds.center;
        }

        var box = zoneObj.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(zoneObj);
        box.isTrigger = true;
        if (hasBounds)
        {
            // Sedikit diperkecil di pinggir agar benar-benar aktif saat masuk bangunan, tinggi cukup mencakup lantai-atap
            box.size = new Vector3(
                Mathf.Max(4f, bounds.size.x * 0.85f),
                Mathf.Max(5f, bounds.size.y),
                Mathf.Max(4f, bounds.size.z * 0.85f)
            );
        }

        var zone = zoneObj.GetComponent<HouseAudioZone>();
        if (zone == null) zone = Undo.AddComponent<HouseAudioZone>(zoneObj);

        Selection.activeGameObject = zoneObj;
        EditorGUIUtility.PingObject(zoneObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return $"{{\"status\":\"ok\",\"zone\":\"{zoneObj.name}\",\"parent\":\"{(houseRoot != null ? houseRoot.name : "Root")}\",\"center\":[{bounds.center.x},{bounds.center.y},{bounds.center.z}],\"size\":[{box.size.x},{box.size.y},{box.size.z}]}}";
    }

    private static string HandleCleanupHouseZone()
    {
        var scene = SceneManager.GetActiveScene();
        var zones = UnityEngine.Object.FindObjectsByType<HouseAudioZone>(FindObjectsSortMode.None);
        int removed = 0;
        foreach (var z in zones)
        {
            if (z != null)
            {
                Undo.DestroyObjectImmediate(z.gameObject);
                removed++;
            }
        }
        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return $"{{\"status\":\"ok\",\"removed\":{removed}}}";
    }

    private static string HandleInspectSettings()
    {
        var ctrl = UnityEngine.Object.FindAnyObjectByType<Wirabaya.UI.SettingsMenuController>(FindObjectsInactive.Include);
        if (ctrl == null)
        {
            return "{\"error\":\"SettingsMenuController tidak ditemukan di scene aktif.\"}";
        }

        SerializedObject so = new SerializedObject(ctrl);
        var mbProp = so.FindProperty("motionBlurDropdown")?.objectReferenceValue as TMPro.TMP_Dropdown;
        var smProp = so.FindProperty("screenModeDropdown")?.objectReferenceValue as TMPro.TMP_Dropdown;
        var sensInputProp = so.FindProperty("sensitivityInputField")?.objectReferenceValue as TMPro.TMP_InputField;
        var sensSliderProp = so.FindProperty("sensitivitySlider")?.objectReferenceValue as UnityEngine.UI.Slider;
        var selfCanvas = ctrl.GetComponent<Canvas>();
        var parentCanvas = ctrl.transform.parent != null ? ctrl.transform.parent.GetComponentInParent<Canvas>() : null;

        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"isPlaying\":{EditorApplication.isPlaying.ToString().ToLower()},");
        sb.Append($"\"ctrlEnabled\":{ctrl.enabled.ToString().ToLower()},");
        sb.Append($"\"rootActiveInHierarchy\":{ctrl.gameObject.activeInHierarchy.ToString().ToLower()},");
        sb.Append($"\"selfCanvas\":{(selfCanvas != null ? $"\"override:{selfCanvas.overrideSorting},order:{selfCanvas.sortingOrder}\"" : "\"none\"")},");
        sb.Append($"\"parentCanvas\":{(parentCanvas != null ? $"\"{EscapeJson(parentCanvas.name)}(override:{parentCanvas.overrideSorting},order:{parentCanvas.sortingOrder})\"" : "\"none\"")},");
        sb.Append($"\"mbDropdown\":{(mbProp != null ? $"\"{EscapeJson(mbProp.name)}(opts:{mbProp.options.Count},tmpl:{(mbProp.template != null ? mbProp.template.name : "null")})\"" : "\"null\"")},");
        sb.Append($"\"smDropdown\":{(smProp != null ? $"\"{EscapeJson(smProp.name)}(opts:{smProp.options.Count},tmpl:{(smProp.template != null ? smProp.template.name : "null")})\"" : "\"null\"")},");
        sb.Append($"\"sensInput\":{(sensInputProp != null ? $"\"{EscapeJson(sensInputProp.name)}\"" : "\"null\"")},");
        sb.Append($"\"sensSlider\":{(sensSliderProp != null ? $"\"{EscapeJson(sensSliderProp.name)}\"" : "\"null\"")},");
        sb.Append($"\"root\":\"{EscapeJson(ctrl.gameObject.name)}\",");
        sb.Append("\"tree\":[");
        var lines = new List<string>();
        AppendUiTree(ctrl.transform, 0, lines);
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"{EscapeJson(lines[i])}\"");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static void AppendUiTree(Transform tr, int depth, List<string> lines)
    {
        var rt = tr as RectTransform;
        string scaleStr = $"{tr.localScale.x:F2},{tr.localScale.y:F2},{tr.localScale.z:F2}";
        string posStr = rt != null ? $"{rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}" : $"{tr.localPosition.x:F1},{tr.localPosition.y:F1}";
        string sizeStr = rt != null ? $"{rt.sizeDelta.x:F1},{rt.sizeDelta.y:F1}" : "-";
        lines.Add($"{new string(' ', depth * 2)}- {tr.name} (active:{tr.gameObject.activeSelf}, scale:[{scaleStr}], pos:[{posStr}], size:[{sizeStr}])");
        for (int i = 0; i < tr.childCount; i++)
        {
            AppendUiTree(tr.GetChild(i), depth + 1, lines);
        }
    }

    private static string HandleOrganizeSettings()
    {
        if (EditorApplication.isPlaying)
        {
            return "{\"error\":\"Unity sedang dalam Play Mode. Keluar dari Play Mode terlebih dahulu agar perubahan hierarki tersimpan permanen.\"}";
        }

        var ctrl = UnityEngine.Object.FindAnyObjectByType<Wirabaya.UI.SettingsMenuController>(FindObjectsInactive.Include);
        if (ctrl == null)
        {
            return "{\"error\":\"SettingsMenuController tidak ditemukan di scene aktif.\"}";
        }

        SerializedObject so = new SerializedObject(ctrl);
        var btnControls = so.FindProperty("buttonControls")?.objectReferenceValue as UnityEngine.UI.Button;
        var controlsParent = so.FindProperty("controlsParent")?.objectReferenceValue as GameObject;
        var motionBlurDropdown = so.FindProperty("motionBlurDropdown")?.objectReferenceValue as TMPro.TMP_Dropdown;
        var screenModeDropdown = so.FindProperty("screenModeDropdown")?.objectReferenceValue as TMPro.TMP_Dropdown;
        var sensitivitySlider = so.FindProperty("sensitivitySlider")?.objectReferenceValue as UnityEngine.UI.Slider;
        var audioParent = so.FindProperty("audioParent")?.objectReferenceValue as GameObject;

        int movedCount = 0;
        var movedLogs = new List<string>();
        Transform root = ctrl.transform;

        void SafeReparent(Transform child, Transform newParent, string label)
        {
            if (child == null || newParent == null || child == newParent || child.parent == newParent) return;
            Undo.SetTransformParent(child, newParent, true, "Organize Settings Hierarchy");
            movedCount++;
            movedLogs.Add($"{label} -> {newParent.name}");
        }

        // 1. Rapikan Text (TMP) milik buttonControls jika terlepas di root sETTINGS
        if (btnControls != null)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform ch = root.GetChild(i);
                if (ch.name == "Text (TMP)")
                {
                    var tmp = ch.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null && tmp.text.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SafeReparent(ch, btnControls.transform, "controls/Text (TMP)");
                        Undo.RecordObject(tmp, "Disable RaycastTarget on Button Label");
                        tmp.raycastTarget = false;
                    }
                }
            }
        }

        // 2. Rapikan Dropdown Motion Blur (Label, Arrow, Template -> Dropdown -> MOTION BLUR)
        Transform motionBlurLabel = null;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).name == "MOTION BLUR")
            {
                motionBlurLabel = root.GetChild(i);
                break;
            }
        }
        if (motionBlurLabel == null && controlsParent != null)
        {
            motionBlurLabel = controlsParent.transform.Find("MOTION BLUR");
        }

        if (motionBlurDropdown != null)
        {
            if (motionBlurDropdown.captionText != null && motionBlurDropdown.captionText.transform.parent == root)
            {
                SafeReparent(motionBlurDropdown.captionText.transform, motionBlurDropdown.transform, "MotionBlurDropdown/Label");
            }
            Transform detachedArrow = root.Find("Arrow");
            if (detachedArrow != null)
            {
                SafeReparent(detachedArrow, motionBlurDropdown.transform, "MotionBlurDropdown/Arrow");
            }
            if (motionBlurDropdown.template != null && motionBlurDropdown.template.parent == root)
            {
                SafeReparent(motionBlurDropdown.template, motionBlurDropdown.transform, "MotionBlurDropdown/Template");
            }
            if (motionBlurLabel != null)
            {
                SafeReparent(motionBlurDropdown.transform, motionBlurLabel, "MOTION BLUR/Dropdown");
            }
        }

        // 3. Masukkan MOTION BLUR, Screen Mode, dan Sensitivity ke dalam controlsParent (cntrl)
        if (controlsParent != null)
        {
            if (motionBlurLabel != null)
            {
                SafeReparent(motionBlurLabel, controlsParent.transform, "cntrl/MOTION BLUR");
            }

            Transform screenModeRoot = screenModeDropdown != null ? FindDirectChildContaining(root, screenModeDropdown.transform) : root.Find("Screen Mode");
            if (screenModeRoot != null && screenModeRoot != controlsParent.transform)
            {
                SafeReparent(screenModeRoot, controlsParent.transform, $"cntrl/{screenModeRoot.name}");
            }

            Transform sensitivityRoot = sensitivitySlider != null ? FindDirectChildContaining(root, sensitivitySlider.transform) : root.Find("Sensitivity");
            if (sensitivityRoot != null && sensitivityRoot != controlsParent.transform)
            {
                SafeReparent(sensitivityRoot, controlsParent.transform, $"cntrl/{sensitivityRoot.name}");
            }

            // Rapikan urutan sibling di dalam cntrl agar dropdown atas tidak tertutup
            if (motionBlurLabel != null && motionBlurLabel.parent == controlsParent.transform) motionBlurLabel.SetSiblingIndex(0);
            if (screenModeRoot != null && screenModeRoot.parent == controlsParent.transform) screenModeRoot.SetSiblingIndex(1);
            if (sensitivityRoot != null && sensitivityRoot.parent == controlsParent.transform) sensitivityRoot.SetSiblingIndex(2);

            Undo.RecordObject(controlsParent, "Ensure cntrl active");
            controlsParent.SetActive(true);

            if (btnControls != null)
            {
                SafeReparent(controlsParent.transform, btnControls.transform, "controls/cntrl");
            }
        }

        // Pastikan Template dropdown tetap mati dan audioParent mati saat idle
        if (motionBlurDropdown != null && motionBlurDropdown.template != null)
            motionBlurDropdown.template.gameObject.SetActive(false);
        if (screenModeDropdown != null && screenModeDropdown.template != null)
            screenModeDropdown.template.gameObject.SetActive(false);
        if (audioParent != null)
            audioParent.SetActive(false);

        EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);

        var treeLines = new List<string>();
        AppendUiTree(ctrl.transform, 0, treeLines);

        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"status\":\"ok\",\"movedCount\":{movedCount},");
        sb.Append("\"moved\":[");
        for (int i = 0; i < movedLogs.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"{EscapeJson(movedLogs[i])}\"");
        }
        sb.Append("],\"tree\":[");
        for (int i = 0; i < treeLines.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"{EscapeJson(treeLines[i])}\"");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static Transform FindDirectChildContaining(Transform root, Transform descendant)
    {
        if (descendant == null || root == null) return null;
        Transform cur = descendant;
        while (cur != null && cur.parent != root)
        {
            cur = cur.parent;
        }
        return (cur != null && cur.parent == root) ? cur : null;
    }

    private static string GetHierarchyPath(Transform tr)
    {
        if (tr.parent == null) return tr.name;
        return GetHierarchyPath(tr.parent) + "/" + tr.name;
    }

    private static void SendDirectResponse(HttpListenerContext context, string responseString, int statusCode)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        catch { }
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    [Serializable]
    private class AudioSourceParams
    {
        public string name;
        public float[] position;
        public string clipName;
        public float volume = 1f;
        public bool loop = true;
        public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 20f;
        public bool playOnAwake = true;
        public string parent;
    }

    [Serializable]
    private class CommandPayload
    {
        public string command;
        public string targetName;
        public bool boolValue;
    }
}
#endif

