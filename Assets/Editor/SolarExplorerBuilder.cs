using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Ajoute le menu "Solar Explorer" dans Unity.
// "1 - Construire la scène" crée automatiquement tout le système solaire,
// l'interface et les scripts connectés. Relance-le quand tu veux : il reconstruit.
public static class SolarExplorerBuilder
{
    const string ModelsPath = "Assets/Models/";
    const float PlanetGlow = 0.3f;   // auto-illumination des planètes (0 = aucune, 1 = très lumineux)
    const float SunGlow = 2.5f;      // éclat du Soleil : au-dessus de 1, le Bloom lui ajoute un halo

    class PData
    {
        public string name, tex, rev, fact;
        public float diam, dist, x, scale, orbit, spin;
        public Color color;
    }

    static PData Mk(string name, string tex, float diam, float dist, string rev,
                    float x, float scale, float orbit, float spin, Color color, string fact)
    {
        return new PData
        {
            name = name, tex = tex, diam = diam, dist = dist, rev = rev,
            x = x, scale = scale, orbit = orbit, spin = spin, color = color, fact = fact
        };
    }

    static readonly PData[] Data = new PData[]
    {
        Mk("Mercure", "2k_mercury", 4879, 58, "88 jours", 24, 1.5f, 5f, 10f, new Color(0.6f, 0.58f, 0.55f),
           "La plus petite planète et la plus proche du Soleil. Sa surface est couverte de cratères."),
        Mk("Vénus", "2k_venus_surface", 12104, 108, "225 jours", 33, 2.7f, 3.5f, -5f, new Color(0.9f, 0.75f, 0.5f),
           "La planète la plus chaude du système solaire. Elle tourne à l'envers."),
        Mk("Terre", "2k_earth_daymap", 12742, 150, "365 jours", 42, 3.0f, 2.5f, 40f, new Color(0.2f, 0.4f, 0.8f),
           "La seule planète connue qui abrite la vie. Environ 71 % de sa surface est de l'eau."),
        Mk("Mars", "2k_mars", 6779, 228, "687 jours", 51, 2.0f, 2f, 38f, new Color(0.8f, 0.35f, 0.2f),
           "La planète rouge : une surface rougeâtre avec des cratères et le plus grand volcan connu."),
        Mk("Jupiter", "2k_jupiter", 139820, 778, "11,9 ans", 70, 9.0f, 1f, 80f, new Color(0.8f, 0.65f, 0.5f),
           "La plus grande planète : une géante gazeuse avec une tempête géante, la Grande Tache rouge."),
        Mk("Saturne", "2k_saturn", 116460, 1434, "29,5 ans", 98, 7.5f, 0.7f, 70f, new Color(0.9f, 0.8f, 0.55f),
           "Célèbre pour ses anneaux faits de glace et de roche. Elle est moins dense que l'eau."),
        Mk("Uranus", "2k_uranus", 50724, 2871, "84 ans", 124, 5.0f, 0.5f, -50f, new Color(0.6f, 0.85f, 0.9f),
           "Une géante de glace qui tourne couchée sur le côté."),
        Mk("Neptune", "2k_neptune", 49244, 4495, "165 ans", 140, 5.0f, 0.4f, 55f, new Color(0.25f, 0.35f, 0.85f),
           "La planète la plus lointaine, avec les vents les plus rapides du système solaire.")
    };

    static readonly string[] TextureNames =
    {
        "2k_sun", "2k_mercury", "2k_venus_surface", "2k_earth_daymap", "2k_mars", "2k_jupiter",
        "2k_saturn", "2k_saturn_ring_alpha", "2k_uranus", "2k_neptune", "2k_stars_milky_way"
    };

    // ------------------------------------------------------------------
    [MenuItem("Solar Explorer/1 - Construire la scène")]
    public static void Build()
    {
        if (TMP_Settings.instance == null)
        {
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            EditorUtility.DisplayDialog("Solar Explorer",
                "Unity doit d'abord importer TextMeshPro (la fenêtre d'import vient de s'ouvrir : clique Import).\n\n" +
                "Ensuite relance : Solar Explorer > 1 - Construire la scène.", "OK");
            return;
        }

        // Nettoyage d'une construction précédente
        foreach (string n in new[] { "SolarSystem", "Managers", "InfoCanvas", "OverviewPoint" })
        {
            GameObject old = GameObject.Find(n);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }

        // Plus de soleil "directionnel" : notre soleil est une lumière ponctuelle
        foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.gameObject.SetActive(false);

        // Le joueur VR
        Transform origin = FindOrigin();
        if (origin == null)
        {
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)");
            origin = FindOrigin();
        }
        Camera cam = origin != null ? origin.GetComponentInChildren<Camera>() : Camera.main;

        // On est dans l'espace : on masque la salle de démonstration du modèle VR
        // (sol quadrillé, flèches, objets à attraper). Réversible : cocher l'objet dans la Hierarchy.
        foreach (string n in new[] { "Environment", "Interactables" })
        {
            GameObject g = GameObject.Find(n);
            if (g != null) g.SetActive(false);
        }

        // Halo lumineux autour du Soleil (effet Bloom)
        AddSunHalo(cam);

        // Ciel étoilé
        Texture2D stars = LoadTex("2k_stars_milky_way");
        if (stars != null)
        {
            Material sky = new Material(Shader.Find("Skybox/Panoramic"));
            sky.SetTexture("_MainTex", stars);
            Save(sky, "M_Ciel");
            RenderSettings.skybox = sky;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            RenderSettings.skybox = null;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; }
        }
        RenderSettings.fog = false;   // le modèle VR active un brouillard gris : il cachait les planètes
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);   // plus clair : les planètes lointaines restent visibles

        // Le Soleil
        GameObject root = new GameObject("SolarSystem");
        GameObject sunRoot = new GameObject("Soleil");
        sunRoot.transform.SetParent(root.transform, false);
        Material sunMat = MakeMat("Soleil", new Color(1f, 0.8f, 0.2f), "2k_sun", true);
        Transform sunModel = MakeModel(sunRoot.transform, 15f, sunMat, null, false);
        UnityEngine.Object.DestroyImmediate(sunModel.GetComponent<Collider>());

        GameObject lg = new GameObject("SunLight");
        lg.transform.SetParent(sunRoot.transform, false);
        Light li = lg.AddComponent<Light>();
        li.type = LightType.Point;
        li.range = 500f;
        li.intensity = IsURP() ? 1000f : 2f;   // à ajuster à l'oeil
        li.shadows = LightShadows.None;
        li.color = new Color(1f, 0.96f, 0.9f);

        // Les planètes
        List<PlanetMotion> motions = new List<PlanetMotion>();
        foreach (PData d in Data)
        {
            GameObject pr = new GameObject(d.name);
            pr.transform.SetParent(root.transform, false);
            pr.transform.position = new Vector3(d.x, 0f, 0f);

            bool saturn = d.name == "Saturne";
            Material pm = MakeMat(d.name, d.color, d.tex, false);
            Material rm = saturn ? MakeRingMat() : null;
            Transform model = MakeModel(pr.transform, d.scale, pm, rm, saturn);
            if (saturn) model.localRotation = Quaternion.Euler(0f, 0f, 20f);
            if (d.name == "Terre") AddClouds(model);

            GameObject vp = new GameObject("ViewPoint");
            vp.transform.SetParent(pr.transform, false);
            vp.transform.localPosition = new Vector3(-(3f * d.scale + 2f), 0f, 0f);
            vp.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            PlanetInfo info = pr.AddComponent<PlanetInfo>();
            info.planetName = d.name;
            info.diameterKm = d.diam;
            info.distanceMillionKm = d.dist;
            info.revolution = d.rev;
            info.fact = d.fact;
            info.model = model;
            info.viewPoint = vp.transform;

            PlanetMotion mo = pr.AddComponent<PlanetMotion>();
            mo.sun = sunRoot.transform;
            mo.model = model;
            mo.orbitSpeed = d.orbit;
            mo.spinSpeed = d.spin;
            motions.Add(mo);

            AddInteractable(model.gameObject, info);
        }

        // Vue générale
        GameObject ov = new GameObject("OverviewPoint");
        ov.transform.position = new Vector3(0f, 35f, -110f);   // proche : en VR on tourne la tête pour voir tout le système

        // Managers
        GameObject mg = new GameObject("Managers");
        // Son d'ambiance : joue en boucle Assets/Audio/ambiance.mp3 s'il existe
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ambiance.mp3");
        if (clip != null)
        {
            AudioSource a = mg.AddComponent<AudioSource>();
            a.clip = clip; a.loop = true; a.playOnAwake = true; a.volume = 0.4f;
            a.spatialBlend = 0f;   // son 2D : entendu partout, pas de direction
        }
        TravelManager travel = mg.AddComponent<TravelManager>();
        SolarSystemManager mgr = mg.AddComponent<SolarSystemManager>();

        // Interface
        GameObject cg = new GameObject("InfoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = cg.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        RectTransform crt = cg.GetComponent<RectTransform>();
        // A slightly taller panel gives the planet facts more room while keeping
        // all four controls on a single row at the bottom.
        crt.sizeDelta = new Vector2(650f, 440f);
        crt.localScale = Vector3.one * 0.002f;
        crt.position = ov.transform.position + new Vector3(0f, 0f, 2f);
        Type tdr = FindXRType("TrackedDeviceGraphicRaycaster");
        if (tdr != null) cg.AddComponent(tdr); else cg.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(cg.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.8f);

        TMP_Text title = MakeText(cg.transform, "TitleText", 48, TextAlignmentOptions.Center);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(-40f, 70f);
        trt.anchoredPosition = new Vector2(0f, -12f);

        TMP_Text body = MakeText(cg.transform, "BodyText", 28, TextAlignmentOptions.TopLeft);
        RectTransform brt = body.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(25f, 92f);
        brt.offsetMax = new Vector2(-25f, -92f);

        MakeButton(cg.transform, "Visiter", 0, mgr.VisitSelected);
        MakeButton(cg.transform, "Vue générale", 1, mgr.GoToOverview);
        MakeButton(cg.transform, "Orbites", 2, mgr.ToggleOrbits);
        MakeButton(cg.transform, "Comparer", 3, mgr.ToggleCompare);

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
            Type xrIn = FindXRType("XRUIInputModule");
            if (xrIn != null) es.AddComponent(xrIn); else es.AddComponent<StandaloneInputModule>();
        }

        // Connexions
        travel.xrOrigin = origin;
        travel.overviewPoint = ov.transform;
        mgr.travel = travel;
        mgr.playerCamera = cam != null ? cam.transform : null;
        mgr.infoPanel = cg;
        mgr.titleText = title;
        mgr.bodyText = body;
        mgr.planets = motions.ToArray();

        // Sauvegarde
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SolarSystem.unity");
        AssetDatabase.SaveAssets();

        string warn = origin == null
            ? "\n\nATTENTION : aucun XR Origin trouvé. Ajoute-le (GameObject > XR > XR Origin (VR)), puis glisse-le dans Managers > TravelManager > Xr Origin."
            : "";
        EditorUtility.DisplayDialog("Solar Explorer",
            "Scène construite et sauvegardée : Assets/Scenes/SolarSystem.unity\n\nClique sur Play pour tester." + warn, "OK");
    }

    // ------------------------------------------------------------------
    [MenuItem("Solar Explorer/2 - Vérifier le projet")]
    public static void Check()
    {
        string r = "";
        r += (FindXRType("XRSimpleInteractable") != null ? "OK       " : "MANQUE   ") + "XR Interaction Toolkit\n";
        r += (FindOrigin() != null ? "OK       " : "MANQUE   ") + "XR Origin dans la scène\n";
        r += (TMP_Settings.instance != null ? "OK       " : "MANQUE   ") + "TextMeshPro Essentials\n";
        r += (AssetDatabase.LoadAssetAtPath<GameObject>(ModelsPath + "Planete.fbx") != null ? "OK       " : "absent   ") + "Models/Planete.fbx (sinon sphère Unity)\n";
        r += (AssetDatabase.LoadAssetAtPath<GameObject>(ModelsPath + "Saturne.fbx") != null ? "OK       " : "absent   ") + "Models/Saturne.fbx (sinon sphère + anneau générés)\n\n";
        foreach (string t in TextureNames)
            r += (LoadTex(t) != null ? "OK       " : "absente  ") + "Texture " + t + "\n";
        r += "\n'absent(e)' n'est pas bloquant : une couleur de secours est utilisée.";
        Debug.Log(r);
        EditorUtility.DisplayDialog("Solar Explorer - vérification", r, "OK");
    }

    // ------------------------------------------------------------------
    static bool IsURP()
    {
        return GraphicsSettings.currentRenderPipeline != null && Shader.Find("Universal Render Pipeline/Lit") != null;
    }

    static Transform FindOrigin()
    {
        foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.parent == null && (t.name.StartsWith("XR Origin") || t.name.StartsWith("XR Rig"))) return t;
        return null;
    }

    // ---------- Nuages de la Terre ----------
    // Une sphère transparente 2 % plus grande que la Terre, avec la texture des nuages.
    // Pas de collider : pointer la Terre la sélectionne toujours.
    static void AddClouds(Transform earthModel)
    {
        if (!IsURP()) return;
        Texture2D tex = PrepareCloudTexture();
        if (tex == null) return;

        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        c.name = "Nuages";
        c.transform.SetParent(earthModel, false);
        c.transform.localScale = Vector3.one * 2f * 1.02f;   // sphère Unity de diamètre 1 -> rayon 1,02
        UnityEngine.Object.DestroyImmediate(c.GetComponent<Collider>());
        Renderer r = c.GetComponent<Renderer>();
        r.sharedMaterial = MakeCloudMat(tex);
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    // L'image des nuages est en noir et blanc : on lui fait utiliser le gris comme transparence
    // (blanc = nuage opaque, noir = transparent).
    static Texture2D PrepareCloudTexture()
    {
        Texture2D tex = LoadTex("2k_earth_clouds");
        if (tex == null) return null;
        string p = AssetDatabase.GetAssetPath(tex);
        TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti != null && (ti.alphaSource != TextureImporterAlphaSource.FromGrayScale || !ti.alphaIsTransparency))
        {
            ti.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return tex;
    }

    static Material MakeCloudMat(Texture2D tex)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", tex);
        m.SetTexture("_MainTex", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f);
        // Matériau transparent (mélange alpha classique)
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return Save(m, "M_Nuages");
    }

    // ---------- Halo du Soleil ----------
    // Un volume global avec un effet Bloom : ce qui dépasse une luminosité de 1 (le Soleil) rayonne.
    // Pour le désactiver : décocher l'objet "Halo" dans la Hierarchy.
    static void AddSunHalo(Camera cam)
    {
        if (!IsURP()) return;
        GameObject old = GameObject.Find("Halo");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        Directory.CreateDirectory("Assets/Generated");
        string p = "Assets/Generated/HaloProfile.asset";
        AssetDatabase.DeleteAsset(p);
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, p);
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.5f);
        bloom.scatter.Override(0.7f);
        bloom.name = "Bloom";
        AssetDatabase.AddObjectToAsset(bloom, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject h = new GameObject("Halo");
        Volume v = h.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 10f;
        v.sharedProfile = profile;

        // Le post-traitement doit être activé sur la caméra du joueur pour que le Bloom s'affiche
        if (cam != null)
        {
            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }
    }

    static Texture2D LoadTex(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }

    static Material Save(Material m, string name)
    {
        Directory.CreateDirectory("Assets/Materials");
        string p = "Assets/Materials/" + name + ".mat";
        AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Material MakeMat(string name, Color color, string texName, bool emissive)
    {
        bool urp = IsURP();
        Shader sh = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        Material m = new Material(sh);
        Texture2D tex = LoadTex(texName);
        m.SetColor(urp ? "_BaseColor" : "_Color", tex != null ? Color.white : color);
        if (tex != null) m.SetTexture(urp ? "_BaseMap" : "_MainTex", tex);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // sinon URP retire l'émission
            m.SetColor("_EmissionColor", tex != null ? Color.white * SunGlow : color * 1.5f);
            if (tex != null) m.SetTexture("_EmissionMap", tex);
        }
        else if (tex != null)
        {
            // Légère auto-illumination : la texture reste lisible de loin et du côté nuit
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", new Color(PlanetGlow, PlanetGlow, PlanetGlow));
            m.SetTexture("_EmissionMap", tex);
        }
        return Save(m, "M_" + name);
    }

    static Material MakeRingMat()
    {
        Material m = new Material(Shader.Find("Sprites/Default"));   // transparent, double face
        Texture2D t = LoadTex("2k_saturn_ring_alpha");
        if (t != null) m.mainTexture = t; else m.color = new Color(0.8f, 0.7f, 0.5f, 0.6f);
        return Save(m, "M_Anneaux");
    }

    static Transform MakeModel(Transform parent, float radius, Material planetMat, Material ringMat, bool saturn)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsPath + (saturn ? "Saturne.fbx" : "Planete.fbx"));
        GameObject model;
        if (asset != null)
        {
            // Modèle créé dans Blender (rayon 1)
            model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.transform.SetParent(parent, false);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = (r.name.ToLower().Contains("ring") && ringMat != null) ? ringMat : planetMat;
        }
        else
        {
            // Solution de secours : sphère Unity (diamètre 1 -> x2 pour avoir un rayon de 1)
            model = new GameObject("Model");
            model.transform.SetParent(parent, false);
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "Sphere";
            s.transform.SetParent(model.transform, false);
            s.transform.localScale = Vector3.one * 2f;
            UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>());
            s.GetComponent<Renderer>().sharedMaterial = planetMat;
            if (saturn)
            {
                GameObject ring = new GameObject("Anneaux");
                ring.transform.SetParent(model.transform, false);
                ring.AddComponent<MeshFilter>().sharedMesh = RingMesh();
                ring.AddComponent<MeshRenderer>().sharedMaterial = ringMat;
            }
        }
        model.name = "Model";
        model.transform.localScale = Vector3.one * radius;
        SphereCollider col = model.AddComponent<SphereCollider>();
        col.radius = 1f;
        return model.transform;
    }

    static Mesh RingMesh()
    {
        const float inner = 1.3f, outer = 2.3f;
        const int seg = 128;
        Vector3[] v = new Vector3[(seg + 1) * 2];
        Vector2[] uv = new Vector2[v.Length];
        int[] tris = new int[seg * 12];
        for (int i = 0; i <= seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            v[2 * i] = new Vector3(c * inner, 0f, s * inner);
            v[2 * i + 1] = new Vector3(c * outer, 0f, s * outer);
            uv[2 * i] = new Vector2(0f, (float)i / seg);
            uv[2 * i + 1] = new Vector2(1f, (float)i / seg);
        }
        for (int i = 0; i < seg; i++)
        {
            int a0 = 2 * i, b0 = 2 * i + 1, a1 = 2 * i + 2, b1 = 2 * i + 3;
            int k = i * 12;
            tris[k] = a0; tris[k + 1] = b0; tris[k + 2] = a1;
            tris[k + 3] = b0; tris[k + 4] = b1; tris[k + 5] = a1;
            tris[k + 6] = a0; tris[k + 7] = a1; tris[k + 8] = b0;
            tris[k + 9] = b0; tris[k + 10] = a1; tris[k + 11] = b1;
        }
        Mesh m = new Mesh();
        m.name = "SaturnRing";
        m.vertices = v;
        m.uv = uv;
        m.triangles = tris;
        m.RecalculateNormals();
        Directory.CreateDirectory("Assets/Generated");
        AssetDatabase.DeleteAsset("Assets/Generated/SaturnRing.asset");
        AssetDatabase.CreateAsset(m, "Assets/Generated/SaturnRing.asset");
        return m;
    }

    // Rend la planète "sélectionnable" à la manette (XR Simple Interactable)
    static void AddInteractable(GameObject model, PlanetInfo info)
    {
        Type t = FindXRType("XRSimpleInteractable");
        if (t == null)
        {
            Debug.LogWarning("XR Interaction Toolkit introuvable : installe-le dans le Package Manager.");
            return;
        }
        Component comp = model.AddComponent(t);
        PropertyInfo prop = t.GetProperty("selectEntered");
        UnityEventBase ev = prop != null ? prop.GetValue(comp, null) as UnityEventBase : null;
        if (ev != null) UnityEventTools.AddVoidPersistentListener(ev, info.OnSelected);
        else Debug.LogWarning("Impossible de relier l'événement Select Entered sur " + model.name + " : fais-le à la main.");
    }

    static Type FindXRType(string name)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            foreach (Type t in types)
                if (t != null && t.Name == name && t.Namespace != null &&
                    t.Namespace.StartsWith("UnityEngine.XR.Interaction.Toolkit")) return t;
        }
        return null;
    }

    // ----- Petits outils d'interface -----
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(Transform parent, string name, int size, TextAlignmentOptions align)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    static void MakeButton(Transform parent, string label, int index, UnityAction action)
    {
        GameObject g = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        g.transform.SetParent(parent, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(130f, 60f);
        rt.anchoredPosition = new Vector2(25f + index * 140f, 15f);
        Image img = g.GetComponent<Image>();
        img.color = new Color(0.15f, 0.35f, 0.7f, 1f);
        Button b = g.GetComponent<Button>();
        b.targetGraphic = img;
        TMP_Text t = MakeText(g.transform, "Label", 22, TextAlignmentOptions.Center);
        t.text = label;
        Stretch(t.rectTransform);
        UnityEventTools.AddPersistentListener(b.onClick, action);
    }
}
