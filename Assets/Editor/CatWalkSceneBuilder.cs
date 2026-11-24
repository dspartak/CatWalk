using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class CatWalkSceneBuilder
{
    static CatWalkSceneBuilder()
    {
        EditorApplication.delayCall += AutoBuildIfFresh;
    }

    static void AutoBuildIfFresh()
    {
        if (GameObject.Find("Cat") == null)
        {
            BuildScene(false);
        }
    }

    [MenuItem("CatWalk/🐱 Собрать сцену (Build Scene)")]
    public static void ManualBuildScene()
    {
        BuildScene(true);
    }

    public static void BuildScene(bool interactive)
    {
        EnsureDirectories();
        EnsureNewInputSystem();
        AddTag("Ground");
        AddTag("Player");

        // 1. Shaders & Materials (URP Lit preferred, fallback Standard)
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material groundMat = GetOrCreateMaterial("Ground_Mat", new Color(0.35f, 0.68f, 0.35f), shader);
        
        // Shader & Material for Houdini Vertex Colors (Cd)
        Shader vcShader = Shader.Find("Universal Render Pipeline/Custom/VertexColorLit");
        if (vcShader == null) vcShader = shader;
        Material forestMat = GetOrCreateMaterial("Forest_VertexColor_Mat", Color.white, vcShader);
        if (forestMat.HasProperty("_GroundColor"))
        {
            forestMat.SetColor("_GroundColor", new Color(0.35f, 0.65f, 0.32f, 1f));
        }

        Material catOrangeMat = GetOrCreateMaterial("Cat_Orange_Mat", new Color(0.95f, 0.55f, 0.2f), shader);
        Material catWhiteMat = GetOrCreateMaterial("Cat_White_Mat", new Color(0.95f, 0.95f, 0.95f), shader);
        Material catPinkMat = GetOrCreateMaterial("Cat_Pink_Mat", new Color(0.96f, 0.65f, 0.7f), shader);
        Material catBlackMat = GetOrCreateMaterial("Cat_Black_Mat", new Color(0.1f, 0.1f, 0.1f), shader);
        Material markerMat = GetOrCreateMaterial("TapMarker_Mat", new Color(1f, 0.25f, 0.25f, 0.75f), shader);
        
        Material coinMat = GetOrCreateMaterial("Coin_Gold_Mat", new Color(1f, 0.85f, 0.15f), shader);
        if (coinMat.HasProperty("_Metallic")) coinMat.SetFloat("_Metallic", 0.9f);
        if (coinMat.HasProperty("_Smoothness")) coinMat.SetFloat("_Smoothness", 0.85f);

        // 2. Prefabs
        GameObject markerPrefab = CreateOrUpdateTapMarkerPrefab(markerMat);
        GameObject coinPrefab = CreateOrUpdateCoinPrefab(coinMat);

        // 3. Clear existing objects
        DestroyIfExists("Ground");
        DestroyIfExists("Cat");
        DestroyIfExists("Managers");
        DestroyIfExists("CoinSpawner");
        DestroyIfExists("ScoreManager");
        DestroyIfExists("UI_Canvas");

        // 4. Forest Environment & Ground
        DestroyIfExists("Forest");
        string fbxPath = "Assets/Models/forest.fbx";

        ModelImporter mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (mi != null)
        {
            bool needReimport = false;
            if (!mi.addCollider) { mi.addCollider = true; needReimport = true; }
            if (mi.useFileUnits) { mi.useFileUnits = false; needReimport = true; }
            if (mi.useFileScale) { mi.useFileScale = false; needReimport = true; }
            if (Mathf.Abs(mi.globalScale - 1f) > 0.01f) { mi.globalScale = 1f; needReimport = true; }
            if (needReimport)
            {
                mi.SaveAndReimport();
            }
        }

        GameObject forestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        if (forestPrefab != null)
        {
            GameObject forest = Object.Instantiate(forestPrefab);
            forest.name = "Forest";
            forest.transform.position = Vector3.zero;
            forest.transform.localScale = Vector3.one;
            forest.tag = "Ground";

            foreach (var mf in forest.GetComponentsInChildren<MeshFilter>())
            {
                mf.gameObject.tag = "Ground";
                MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = forestMat; // Colored trees, leaves, ground
                }
            }
        }

        // Safety base plane (ниже холмов, чтобы не пересекать 3D рельеф)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -2f, 0);
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        ground.tag = "Ground";
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // 5. Cat GameObject
        GameObject cat = new GameObject("Cat");
        cat.transform.position = Vector3.zero;
        cat.tag = "Player"; // Important for coin pickup!

        CharacterController cc = cat.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0.5f, 0);
        cc.height = 1.0f;
        cc.radius = 0.3f;

        CatWander wander = cat.AddComponent<CatWander>();

        // Общий визуальный контейнер котика (чтобы все части тела всегда были единым целым)
        GameObject visuals = new GameObject("Visuals");
        visuals.transform.SetParent(cat.transform, false);
        visuals.transform.localPosition = Vector3.zero;

        wander.body = visuals.transform; // Покачивается весь котик целиком, части не отрываются!

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(visuals.transform, false);
        body.transform.localPosition = new Vector3(0, 0.45f, 0);
        body.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        body.transform.localScale = new Vector3(0.35f, 0.5f, 0.6f);
        body.GetComponent<MeshRenderer>().sharedMaterial = catOrangeMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visuals.transform, false);
        head.transform.localPosition = new Vector3(0, 0.8f, 0.55f);
        head.transform.localScale = Vector3.one * 0.5f;
        head.GetComponent<MeshRenderer>().sharedMaterial = catOrangeMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        // Ears
        GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        earL.name = "EarL";
        earL.transform.SetParent(visuals.transform, false);
        earL.transform.localPosition = new Vector3(-0.15f, 1.1f, 0.5f);
        earL.transform.localRotation = Quaternion.Euler(15f, 0, -20f);
        earL.transform.localScale = new Vector3(0.1f, 0.15f, 0.05f);
        earL.GetComponent<MeshRenderer>().sharedMaterial = catOrangeMat;
        Object.DestroyImmediate(earL.GetComponent<Collider>());

        GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        earR.name = "EarR";
        earR.transform.SetParent(visuals.transform, false);
        earR.transform.localPosition = new Vector3(0.15f, 1.1f, 0.5f);
        earR.transform.localRotation = Quaternion.Euler(15f, 0, 20f);
        earR.transform.localScale = new Vector3(0.1f, 0.15f, 0.05f);
        earR.GetComponent<MeshRenderer>().sharedMaterial = catOrangeMat;
        Object.DestroyImmediate(earR.GetComponent<Collider>());

        // Nose
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "Nose";
        nose.transform.SetParent(visuals.transform, false);
        nose.transform.localPosition = new Vector3(0, 0.77f, 0.82f);
        nose.transform.localScale = new Vector3(0.08f, 0.06f, 0.08f);
        nose.GetComponent<MeshRenderer>().sharedMaterial = catPinkMat;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        // Eyes
        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.name = "EyeL";
        eyeL.transform.SetParent(visuals.transform, false);
        eyeL.transform.localPosition = new Vector3(-0.12f, 0.9f, 0.75f);
        eyeL.transform.localScale = new Vector3(0.08f, 0.08f, 0.04f);
        eyeL.GetComponent<MeshRenderer>().sharedMaterial = catBlackMat;
        Object.DestroyImmediate(eyeL.GetComponent<Collider>());

        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.name = "EyeR";
        eyeR.transform.SetParent(visuals.transform, false);
        eyeR.transform.localPosition = new Vector3(0.12f, 0.9f, 0.75f);
        eyeR.transform.localScale = new Vector3(0.08f, 0.08f, 0.04f);
        eyeR.GetComponent<MeshRenderer>().sharedMaterial = catBlackMat;
        Object.DestroyImmediate(eyeR.GetComponent<Collider>());

        // Tail
        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tail.name = "Tail";
        tail.transform.SetParent(visuals.transform, false);
        tail.transform.localPosition = new Vector3(0, 0.55f, -0.6f);
        tail.transform.localRotation = Quaternion.Euler(30f, 0, 0);
        tail.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
        tail.GetComponent<MeshRenderer>().sharedMaterial = catOrangeMat;
        Object.DestroyImmediate(tail.GetComponent<Collider>());

        // Legs (Paws)
        CreatePaw("LegFL", new Vector3(-0.15f, 0.22f, 0.3f), visuals.transform, catWhiteMat);
        CreatePaw("LegFR", new Vector3(0.15f, 0.22f, 0.3f), visuals.transform, catWhiteMat);
        CreatePaw("LegBL", new Vector3(-0.15f, 0.22f, -0.3f), visuals.transform, catWhiteMat);
        CreatePaw("LegBR", new Vector3(0.15f, 0.22f, -0.3f), visuals.transform, catWhiteMat);

        // 6. Camera Setup
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0, 6f, -8f);
        cam.transform.rotation = Quaternion.Euler(30f, 0, 0);

        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = cat.transform;
        follow.offset = new Vector3(0, 6f, -8f);
        follow.smooth = 4f;

        // 7. Managers GameObject
        GameObject managers = new GameObject("Managers");
        TapToMove tap = managers.AddComponent<TapToMove>();
        tap.cat = wander;
        tap.cam = cam;
        tap.groundMask = ~0;
        tap.tapMarkerPrefab = markerPrefab;
        managers.AddComponent<ScreenAwake>();

        // 8. Coin Spawner
        GameObject spawnerObj = new GameObject("CoinSpawner");
        CoinSpawner spawner = spawnerObj.AddComponent<CoinSpawner>();
        spawner.coinPrefab = coinPrefab;
        spawner.cat = cat.transform;
        spawner.maxCoins = 10;
        spawner.spawnRadius = 8f;
        spawner.minDistanceFromCat = 3f;

        // 9. UI Canvas with Score
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem if needed
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ScoreText (TextMeshPro) в правом верхнем углу
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI tmp = scoreTextObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Монетки: 0";
        tmp.fontSize = 42;
        tmp.color = new Color(1f, 0.95f, 0.2f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.TopRight;

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(1f, 1f); // Правый верхний угол
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(450f, 80f);

        // 10. ScoreManager
        GameObject scoreMgrObj = new GameObject("ScoreManager");
        ScoreManager sm = scoreMgrObj.AddComponent<ScoreManager>();
        sm.scoreText = tmp;

        // 10.1 HUD Pause / Menu Button (в левом верхнем углу)
        GameObject pauseBtnObj = new GameObject("PauseButton");
        pauseBtnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform prt = pauseBtnObj.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(40f, -40f);
        prt.sizeDelta = new Vector2(170f, 65f);

        Image pImg = pauseBtnObj.AddComponent<Image>();
        pImg.color = new Color(0.18f, 0.14f, 0.1f, 0.85f);
        Button pBtn = pauseBtnObj.AddComponent<Button>();

        GameObject pTextObj = new GameObject("Text");
        pTextObj.transform.SetParent(pauseBtnObj.transform, false);
        TextMeshProUGUI pText = pTextObj.AddComponent<TextMeshProUGUI>();
        pText.text = "МЕНЮ";
        pText.fontSize = 28;
        pText.color = new Color(1f, 0.9f, 0.7f);
        pText.fontStyle = FontStyles.Bold;
        pText.alignment = TextAlignmentOptions.Center;
        RectTransform pTextRt = pText.rectTransform;
        pTextRt.anchorMin = Vector2.zero;
        pTextRt.anchorMax = Vector2.one;
        pTextRt.offsetMin = Vector2.zero;
        pTextRt.offsetMax = Vector2.zero;

        // 10.2 MainMenu (Стимпанк-меню Studio Agata)
        string menuPath = "Assets/Textures/MainMenu_Agata.jpg";
        AssetDatabase.ImportAsset(menuPath, ImportAssetOptions.ForceUpdate);
        TextureImporter menuTi = AssetImporter.GetAtPath(menuPath) as TextureImporter;
        if (menuTi != null && menuTi.textureType != TextureImporterType.Sprite)
        {
            menuTi.textureType = TextureImporterType.Sprite;
            menuTi.spriteImportMode = SpriteImportMode.Single;
            menuTi.alphaIsTransparency = true;
            menuTi.SaveAndReimport();
        }

        Sprite menuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(menuPath);

        GameObject menuObj = new GameObject("MainMenu");
        menuObj.transform.SetParent(canvasObj.transform, false);

        RectTransform menuRt = menuObj.AddComponent<RectTransform>();
        menuRt.anchorMin = Vector2.zero;
        menuRt.anchorMax = Vector2.one;
        menuRt.offsetMin = Vector2.zero;
        menuRt.offsetMax = Vector2.zero;

        Image menuBg = menuObj.AddComponent<Image>();
        menuBg.color = new Color(0.06f, 0.04f, 0.03f, 0.75f);

        CanvasGroup menuCg = menuObj.AddComponent<CanvasGroup>();
        MainMenuController menuCtrl = menuObj.AddComponent<MainMenuController>();

        // Карточка меню по центру с артом
        GameObject cardObj = new GameObject("MenuCard");
        cardObj.transform.SetParent(menuObj.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(1024f, 983f);
        cardRt.localScale = new Vector3(0.88f, 0.88f, 1f);

        Image cardImg = cardObj.AddComponent<Image>();
        if (menuSprite != null)
        {
            cardImg.sprite = menuSprite;
            cardImg.preserveAspect = true;
        }

        // Кнопка "Новая игра" (поверх таблички на арте)
        GameObject newGameObj = new GameObject("Button_NewGame");
        newGameObj.transform.SetParent(cardObj.transform, false);
        RectTransform ngRt = newGameObj.AddComponent<RectTransform>();
        ngRt.anchorMin = new Vector2(0.5f, 0.5f);
        ngRt.anchorMax = new Vector2(0.5f, 0.5f);
        ngRt.pivot = new Vector2(0.5f, 0.5f);
        ngRt.anchoredPosition = new Vector2(0f, -25f);
        ngRt.sizeDelta = new Vector2(620f, 150f);

        Image ngImg = newGameObj.AddComponent<Image>();
        ngImg.color = new Color(1f, 1f, 1f, 0.002f);
        Button ngBtn = newGameObj.AddComponent<Button>();
        ColorBlock ngCb = ngBtn.colors;
        ngCb.highlightedColor = new Color(1f, 0.85f, 0.4f, 0.25f);
        ngCb.pressedColor = new Color(0.5f, 0.3f, 0.1f, 0.4f);
        ngBtn.colors = ngCb;

        // Кнопка "Выйти" (поверх таблички на арте)
        GameObject exitObj = new GameObject("Button_Exit");
        exitObj.transform.SetParent(cardObj.transform, false);
        RectTransform exitRt = exitObj.AddComponent<RectTransform>();
        exitRt.anchorMin = new Vector2(0.5f, 0.5f);
        exitRt.anchorMax = new Vector2(0.5f, 0.5f);
        exitRt.pivot = new Vector2(0.5f, 0.5f);
        exitRt.anchoredPosition = new Vector2(0f, -235f);
        exitRt.sizeDelta = new Vector2(460f, 105f);

        Image exitImg = exitObj.AddComponent<Image>();
        exitImg.color = new Color(1f, 1f, 1f, 0.002f);
        Button exitBtn = exitObj.AddComponent<Button>();
        ColorBlock exitCb = exitBtn.colors;
        exitCb.highlightedColor = new Color(1f, 0.85f, 0.4f, 0.25f);
        exitCb.pressedColor = new Color(0.5f, 0.3f, 0.1f, 0.4f);
        exitBtn.colors = exitCb;

        // Привязываем кнопки к контроллеру
        menuCtrl.newGameButton = ngBtn;
        menuCtrl.exitButton = exitBtn;
        menuCtrl.pauseButton = pBtn;

        // 10.5 SplashScreen (Studio Agata - 3 sec)
        string splashPath = "Assets/Textures/StudioAgata_Splash.jpg";
        TextureImporter ti = AssetImporter.GetAtPath(splashPath) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.Sprite)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        Sprite splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(splashPath);
        if (splashSprite != null)
        {
            GameObject splashObj = new GameObject("SplashScreen");
            splashObj.transform.SetParent(canvasObj.transform, false);

            RectTransform srt = splashObj.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            Image bgImg = splashObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.08f, 0.05f, 1f);

            GameObject artObj = new GameObject("SplashArt");
            artObj.transform.SetParent(splashObj.transform, false);
            RectTransform artRt = artObj.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;

            Image artImg = artObj.AddComponent<Image>();
            artImg.sprite = splashSprite;
            artImg.preserveAspect = true;

            splashObj.AddComponent<CanvasGroup>();
            SplashScreenController splashCtrl = splashObj.AddComponent<SplashScreenController>();
            splashCtrl.displayDuration = 3f;
            splashCtrl.fadeOutDuration = 0.6f;

            splashObj.transform.SetAsLastSibling();
        }

        // 11. Light
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        dirLight.color = new Color(1f, 0.96f, 0.88f);
        dirLight.intensity = 1.2f;

        SetupAppIcon();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("CatWalk: Сцена успешно собрана (котик + монетки + UI счёта)!");
        if (interactive)
        {
            EditorUtility.DisplayDialog("CatWalk", "Сцена котика с монетками и счётом успешно собрана!\nНажмите Play для проверки.", "OK");
        }
    }

    static void CreatePaw(string name, Vector3 pos, Transform parent, Material mat)
    {
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = name;
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = pos;
        leg.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
        leg.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(leg.GetComponent<Collider>());
    }

    static GameObject CreateOrUpdateTapMarkerPrefab(Material mat)
    {
        string path = "Assets/Prefabs/TapMarker.prefab";
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "TapMarker";
        go.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateOrUpdateCoinPrefab(Material mat)
    {
        string path = "Assets/Prefabs/Coin.prefab";
        
        // Root GameObject
        GameObject coinRoot = new GameObject("Coin");
        coinRoot.AddComponent<Coin>();

        SphereCollider sc = coinRoot.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.45f;
        sc.center = Vector3.zero;

        Rigidbody rb = coinRoot.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Внутренняя 3D-модель (повернута ребром вертикально)
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        model.name = "Model";
        model.transform.SetParent(coinRoot.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        model.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(coinRoot, path);
        Object.DestroyImmediate(coinRoot);
        return prefab;
    }

    static Material GetOrCreateMaterial(string name, Color color, Shader shader)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
            mat.color = color;
        }
        return mat;
    }

    static void DestroyIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void AddTag(string tag)
    {
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset != null && asset.Length > 0)
        {
            SerializedObject so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue.Equals(tag)) return;
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
            so.Update();
        }
    }

    static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Shaders")) AssetDatabase.CreateFolder("Assets", "Shaders");
        if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
        if (!AssetDatabase.IsValidFolder("Assets/Textures")) AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scripts")) AssetDatabase.CreateFolder("Assets", "Scripts");
    }

    [MenuItem("CatWalk/🐱 Установить иконку приложения (Set App Icon)")]
    public static void SetAppIconMenuItem()
    {
        SetupAppIcon();
        EditorUtility.DisplayDialog("CatWalk", "Иконка приложения успешно установлена для всех платформ (включая Android)!", "OK");
    }

    public static void SetupAppIcon()
    {
        string iconPath = "Assets/Textures/AppIcon.jpg";
        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (ti != null)
        {
            if (ti.textureType != TextureImporterType.Default || !ti.isReadable)
            {
                ti.textureType = TextureImporterType.Default;
                ti.isReadable = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
        {
            Debug.LogError("CatWalk: Не удалось загрузить текстуру иконки: " + iconPath);
            return;
        }

        // 1. Дефолтная иконка для всех платформ (Default Icon в PlayerSettings)
        int[] defaultSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown);
        Texture2D[] defaultIcons = new Texture2D[defaultSizes != null && defaultSizes.Length > 0 ? defaultSizes.Length : 1];
        for (int i = 0; i < defaultIcons.Length; i++) defaultIcons[i] = icon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, defaultIcons);

        // 2. Иконки для платформы Android
        int[] androidSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        if (androidSizes != null && androidSizes.Length > 0)
        {
            Texture2D[] androidIcons = new Texture2D[androidSizes.Length];
            for (int i = 0; i < androidIcons.Length; i++) androidIcons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, androidIcons);
        }

        // 3. Иконки для Standalone (Windows / macOS)
        int[] standaloneSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
        if (standaloneSizes != null && standaloneSizes.Length > 0)
        {
            Texture2D[] standaloneIcons = new Texture2D[standaloneSizes.Length];
            for (int i = 0; i < standaloneIcons.Length; i++) standaloneIcons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, standaloneIcons);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("CatWalk: Иконка приложения успешно установлена в PlayerSettings!");
    }

    [MenuItem("CatWalk/📱 Подготовить настройки для Android")]
    public static void ConfigureForAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        PlayerSettings.companyName = "CatWalkStudio";
        PlayerSettings.productName = "CatWalk";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.catwalk.game");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EnsureNewInputSystem();
        SetupAppIcon();
        
        AssetDatabase.SaveAssets();
        Debug.Log("CatWalk: Проект переключён на платформу Android и настроен!");
        EditorUtility.DisplayDialog("CatWalk", "Платформа переключена на Android!\nPackage: com.catwalk.game\nАрхитектура: ARM64\n\nТеперь откройте File -> Build Profiles (или Build Settings) и нажмите Build.", "OK");
    }
    static void EnsureNewInputSystem()
    {
        UnityEngine.Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projectSettings != null && projectSettings.Length > 0)
        {
            SerializedObject ps = new SerializedObject(projectSettings[0]);
            SerializedProperty aih = ps.FindProperty("activeInputHandler");
            if (aih != null && aih.intValue != 1)
            {
                aih.intValue = 1; // 1 = Input System Package (New)
                ps.ApplyModifiedProperties();
            }
        }
    }
}
