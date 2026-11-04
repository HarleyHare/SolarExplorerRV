using UnityEngine;

// Données d'une planète (nom, taille, distance...) + réaction quand on la sélectionne.
public class PlanetInfo : MonoBehaviour
{
    public string planetName = "Mars";
    public float diameterKm = 6779;
    public float distanceMillionKm = 228;
    public string revolution = "687 jours";
    [TextArea(2, 5)] public string fact = "Une planète rouge couverte de cratères.";

    public Transform model;      // l'enfant "Model" (la sphère 3D)
    public Transform viewPoint;  // l'enfant "ViewPoint" (d'où on observe la planète)

    // Appelée quand le joueur sélectionne la planète avec la manette
    public void OnSelected()
    {
        if (SolarSystemManager.Instance != null)
            SolarSystemManager.Instance.SelectPlanet(this);
    }
}
