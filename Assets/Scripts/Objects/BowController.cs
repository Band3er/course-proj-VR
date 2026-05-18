using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BowController : MonoBehaviour
{
    [Header("References")]
    public Transform stringAnchor;      // punct fix pe arc unde se ancoreaza coarda
    public Transform nockPoint;         // locul unde sta sageata
    public GameObject arrowPrefab;

    [Header("Settings")]
    public float maxDrawDistance = 0.5f;
    public float maxLaunchForce = 30f;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor drawHand;  // mana care trage
    private GameObject currentArrow;
    private bool isDrawing = false;
    private float drawAmount = 0f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable bowGrab;

    void Awake()
    {
        bowGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        bowGrab.selectEntered.AddListener(OnBowGrabbed);
        bowGrab.selectExited.AddListener(OnBowReleased);
    }

    void Update()
    {
        if (!isDrawing || drawHand == null) return;

        // Calculeaza distanta dintre mana de tragere si stringAnchor
        float dist = Vector3.Distance(drawHand.transform.position, stringAnchor.position);
        drawAmount = Mathf.Clamp01(dist / maxDrawDistance);

        // Muta nockPoint spre mana
        if (currentArrow != null)
        {
            Vector3 drawDir = (drawHand.transform.position - stringAnchor.position).normalized;
            nockPoint.position = stringAnchor.position + drawDir * (drawAmount * maxDrawDistance);
            currentArrow.transform.position = nockPoint.position;
            currentArrow.transform.rotation = Quaternion.LookRotation(stringAnchor.position - nockPoint.position);
        }
    }

    // Mana opusa care apuca "coarda"
    public void StartDraw(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor hand)
    {
        if (isDrawing) return;
        drawHand = hand;
        isDrawing = true;

        // Spawn arrow la nock point
        currentArrow = Instantiate(arrowPrefab, nockPoint.position, nockPoint.rotation);
        currentArrow.GetComponent<Rigidbody>().isKinematic = true;
        currentArrow.GetComponent<ArrowController>().SetNocked(true);
    }

    public void Release()
    {
        if (!isDrawing || currentArrow == null) return;

        isDrawing = false;

        // Calculeaza directia si forta
        Vector3 launchDir = (stringAnchor.position - nockPoint.position).normalized;
        float force = drawAmount * maxLaunchForce;

        // Activeaza fizica si lanseaza
        Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = launchDir * force;

        currentArrow.GetComponent<ArrowController>().Launch(launchDir);
        currentArrow = null;
        drawHand = null;
        drawAmount = 0f;
    }

    void OnBowGrabbed(SelectEnterEventArgs args) { }
    void OnBowReleased(SelectExitEventArgs args)
    {
        Release();
    }
}