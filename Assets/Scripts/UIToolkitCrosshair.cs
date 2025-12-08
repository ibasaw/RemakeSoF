using UnityEngine;
using UnityEngine.InputSystem;

public class UIToolkitCrosshair : MonoBehaviour
{
    [Header("References")]
    public GameObject playerController;   // Player
    public Transform handBone;            // Weapon_jnt Bone
    public float pickupDistance = 3f;     // Reichweite zum Aufheben
    public InputActionReference interactAction; // deine InputAction

    [Header("Combat")]
    public Camera playerCamera;           // deine FPS Kamera
    public float shootDistance = 100f;    // wie weit der Schuss gehen soll
    
    [Header("Detection")]
    public LayerMask enemyLayer = -1;     // Layer für Gegner
    public LayerMask itemLayer = -1;      // Layer für Items
    public LayerMask environmentLayer = -1; // Layer für Umgebung

    private GameObject heldWeapon = null;

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (playerController == null || handBone == null) return;

        // Prüfen, ob InputAction ausgelöst wurde
        if (interactAction != null && interactAction.action.triggered)
        {
            if (heldWeapon == null)
            {
                // Alle Waffen in der Nähe checken
                Collider[] nearby = Physics.OverlapSphere(playerController.transform.position, pickupDistance, LayerMask.GetMask("Weapon"));

                if (nearby.Length > 0)
                {
                    PickupWeapon(nearby[0].gameObject);
                }
            }
            else
            {
                DropWeapon();
            }
        }

        // Linke Maustaste -> Schuss / Treffercheck
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckHit();
        }
    }

    private void PickupWeapon(GameObject weapon)
    {
        heldWeapon = weapon;

        weapon.transform.SetParent(handBone);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = weapon.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("Weapon aufgenommen: " + weapon.name);
    }

    private void DropWeapon()
    {
        if (heldWeapon == null) return;

        GameObject weapon = heldWeapon;
        heldWeapon = null;

        weapon.transform.SetParent(null, true);

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(playerController.transform.forward * 2f, ForceMode.Impulse);
        }

        Collider col = weapon.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Debug.Log("Weapon fallen gelassen: " + weapon.name);
    }

    // Raycast ins Crosshair, erkennt was du ansiehst
    private void CheckHit()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            GameObject hitObject = hit.collider.gameObject;
            string objectType = GetObjectType(hitObject);
            
            Debug.Log($"Getroffen: {hitObject.name} (Typ: {objectType})");
            
            // Hier kannst du je nach Objekttyp verschiedene Aktionen ausführen
            HandleHitObject(hitObject, objectType, hit.point);
        }
    }

    // Bestimmt den Typ des getroffenen Objekts basierend auf Layer
    private string GetObjectType(GameObject obj)
    {
        int layer = obj.layer;
        
        if (IsInLayerMask(layer, enemyLayer))
            return "Enemy";
        else if (IsInLayerMask(layer, itemLayer))
            return "Item";
        else if (IsInLayerMask(layer, environmentLayer))
            return "Environment";
        else
            return "Unknown";
    }
    
    // Hilfsmethode um zu prüfen ob ein Layer in einem LayerMask enthalten ist
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }
    
    // Behandelt das getroffene Objekt je nach Typ
    private void HandleHitObject(GameObject hitObject, string objectType, Vector3 hitPoint)
    {
        switch (objectType)
        {
            case "Enemy":
                Debug.Log($"Gegner getroffen: {hitObject.name} an Position {hitPoint}");
                // Hier kannst du Schaden am Gegner verursachen
                break;
                
            case "Item":
                Debug.Log($"Item erkannt: {hitObject.name}");
                // Hier kannst du Item-Interaktionen handhaben
                break;
                
            case "Environment":
                Debug.Log($"Umgebung getroffen: {hitObject.name}");
                // Hier kannst du Umgebungseffekte auslösen
                break;
                
            default:
                Debug.Log($"Unbekanntes Objekt: {hitObject.name}");
                break;
        }
    }
}
