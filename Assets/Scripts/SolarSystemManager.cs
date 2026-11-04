using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Chef d'orchestre : fiches d'info, boutons, orbites, comparaison de planètes.
public class SolarSystemManager : MonoBehaviour
{
    public static SolarSystemManager Instance;

    public TravelManager travel;
    public Transform playerCamera;      // la Main Camera (dans XR Origin)
    public GameObject infoPanel;        // l'objet InfoCanvas
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public PlanetMotion[] planets;      // les 8 planètes (pour les orbites)

    PlanetInfo selected, previous;
    bool orbitsVisible = true;
    readonly List<GameObject> clones = new List<GameObject>();

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        yield return null;   // on laisse le temps de placer le joueur
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
        titleText.text = "Solar Explorer VR";
        bodyText.text = "Pointe une planète avec la manette et appuie sur la gâchette.";
        PlacePanel();
    }

    public void SelectPlanet(PlanetInfo p)
    {
        if (p != selected) { previous = selected; selected = p; }
        titleText.text = p.planetName;
        bodyText.text = "Diamètre : " + p.diameterKm.ToString("N0") + " km\nDistance au Soleil : "
            + p.distanceMillionKm.ToString("N0") + " millions de km\nRévolution : "
            + p.revolution + "\n\n" + p.fact;
        PlacePanel();
    }

    // ----- Boutons -----
    public void VisitSelected()
    {
        if (selected != null) Travel(selected.viewPoint);
    }

    public void GoToOverview() { Travel(travel.overviewPoint); }

    public void ToggleOrbits()
    {
        orbitsVisible = !orbitsVisible;
        foreach (PlanetMotion m in planets) m.SetOrbitVisible(orbitsVisible);
    }

    public void ToggleCompare()
    {
        if (clones.Count > 0) { CloseComparison(); return; }
        if (selected == null || previous == null)
        {
            bodyText.text = "Sélectionne deux planètes, puis appuie sur Comparer.";
            return;
        }
        float sA = selected.diameterKm / 12742f;   // Terre = 1
        float sB = previous.diameterKm / 12742f;
        Vector3 mid = playerCamera.position + playerCamera.forward * 40f;
        Vector3 right = playerCamera.right;
        Spawn(previous, mid - right * (sB + 1f), sB);
        Spawn(selected, mid + right * (sA + 1f), sA);
    }

    // ----- Fonctions internes -----
    void Spawn(PlanetInfo p, Vector3 pos, float scale)
    {
        GameObject c = Instantiate(p.model.gameObject, pos, Quaternion.identity);
        c.transform.localScale = Vector3.one * scale;
        Destroy(c.GetComponent<Collider>());
        clones.Add(c);
    }

    void CloseComparison()
    {
        foreach (GameObject c in clones) Destroy(c);
        clones.Clear();
    }

    void Travel(Transform target)
    {
        CloseComparison();
        infoPanel.SetActive(false);
        travel.GoTo(target);
        StartCoroutine(ShowPanelAfterTravel());
    }

    IEnumerator ShowPanelAfterTravel()
    {
        yield return new WaitForSeconds(travel.duration + 0.2f);
        PlacePanel();
        infoPanel.SetActive(true);
    }

    // Place le panneau à 2 m devant le joueur, tourné vers lui
    void PlacePanel()
    {
        if (playerCamera == null) return;
        Vector3 pos = playerCamera.position + playerCamera.forward * 2f;
        infoPanel.transform.position = pos;
        infoPanel.transform.rotation = Quaternion.LookRotation(pos - playerCamera.position);
    }
}
