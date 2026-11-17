using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

// Déplace le joueur (XR Origin) en douceur vers une planète ou la vue générale,
// puis le fait suivre la planète pendant qu'elle tourne sur son orbite.
// Au démarrage : coupe la gravité (pas de sol dans l'espace) et active le vol libre.
public class TravelManager : MonoBehaviour
{
    public Transform xrOrigin;        // l'objet XR Origin (le joueur)
    public Transform overviewPoint;   // la vue générale
    public float duration = 3f;       // durée du voyage en secondes
    public float freeMoveSpeed = 10f; // vitesse du déplacement libre au joystick (m/s)

    Coroutine current;
    Transform follow;
    Vector3 lastFollowPos;

    void Start()
    {
        if (xrOrigin == null || overviewPoint == null)
        {
            Debug.LogWarning("TravelManager : XR Origin ou OverviewPoint manquant.");
            enabled = false;
            return;
        }
        DisableGravity();
        EnableFreeFlight();
        xrOrigin.SetPositionAndRotation(overviewPoint.position, overviewPoint.rotation);
        Follow(overviewPoint);
    }

    // Dans l'espace, pas de sol : on coupe la gravité du joueur VR,
    // sinon il tombe sans fin depuis la vue générale.
    void DisableGravity()
    {
        foreach (MonoBehaviour b in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b != null && b.GetType().Name == "GravityProvider")
            {
                b.enabled = false;
                Debug.Log("TravelManager : gravité du joueur désactivée.");
            }
        }
    }

    // Déplacement libre : le joystick gauche fait avancer dans la direction
    // du regard, y compris vers le haut et vers le bas.
    void EnableFreeFlight()
    {
        foreach (ContinuousMoveProvider move in xrOrigin.GetComponentsInChildren<ContinuousMoveProvider>(true))
        {
            move.enableFly = true;
            move.moveSpeed = freeMoveSpeed;
            Debug.Log("TravelManager : vol libre activé (vitesse " + freeMoveSpeed + " m/s).");
        }
    }

    void LateUpdate()
    {
        if (follow == null) return;
        xrOrigin.position += follow.position - lastFollowPos;
        lastFollowPos = follow.position;
    }

    void Follow(Transform t)
    {
        follow = t;
        if (t != null) lastFollowPos = t.position;
    }

    public void GoTo(Transform target)
    {
        if (xrOrigin == null || target == null) return;
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Fly(target));
    }

    IEnumerator Fly(Transform target)
    {
        Follow(null);
        Vector3 p0 = xrOrigin.position;
        Quaternion r0 = xrOrigin.rotation;
        float time = Mathf.Max(duration, 0.1f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / time)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            xrOrigin.SetPositionAndRotation(
                Vector3.Lerp(p0, target.position, k),
                Quaternion.Slerp(r0, target.rotation, k));
            yield return null;
        }
        xrOrigin.SetPositionAndRotation(target.position, target.rotation);
        Follow(target);
    }
}
