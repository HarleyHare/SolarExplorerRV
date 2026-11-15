using UnityEngine;

// Fait tourner la planète sur elle-même, autour du Soleil, et dessine sa trajectoire.
public class PlanetMotion : MonoBehaviour
{
    public Transform sun;
    public Transform model;
    public float orbitSpeed = 2f;    // degrés par seconde autour du Soleil
    public float spinSpeed = 40f;    // degrés par seconde sur elle-même
    public Color orbitColor = new Color(1f, 1f, 1f, 0.5f);

    LineRenderer line;

    void Start()
    {
        // Position de départ aléatoire sur l'orbite
        transform.RotateAround(sun.position, Vector3.up, Random.Range(0f, 360f));
        DrawOrbit();
    }

    void Update()
    {
        transform.RotateAround(sun.position, Vector3.up, orbitSpeed * Time.deltaTime);
        model.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

    void DrawOrbit()
    {
        float radius = Vector3.Distance(transform.position, sun.position);
        int points = 128;
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = points;
        line.startWidth = 0.3f;
        line.endWidth = 0.3f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = orbitColor;
        line.endColor = orbitColor;
        for (int i = 0; i < points; i++)
        {
            float a = i * Mathf.PI * 2f / points;
            line.SetPosition(i, sun.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    public void SetOrbitVisible(bool visible)
    {
        if (line != null) line.enabled = visible;
    }
}
