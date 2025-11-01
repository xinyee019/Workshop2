using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CollectableItem : MonoBehaviourPunCallbacks
{
    [Header("Item Settings")]
    public string itemName = "Trash Item";
    public int itemValue = 1;

    private bool isCollected = false;
    public bool IsCollected => isCollected;

    private Collider itemCollider;
    private Renderer itemRenderer;

    void Awake()
    {
        EnsurePhotonComponents();
        itemCollider = GetComponent<Collider>();
        itemRenderer = GetComponent<Renderer>();
    }

    private void EnsurePhotonComponents()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
            pv.OwnershipTransfer = OwnershipOption.Takeover;
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
        }

        PhotonTransformViewClassic ptv = GetComponent<PhotonTransformViewClassic>();
        if (ptv == null)
        {
            ptv = gameObject.AddComponent<PhotonTransformViewClassic>();
        }

        if (pv.ObservedComponents == null || pv.ObservedComponents.Count == 0)
        {
            pv.ObservedComponents = new System.Collections.Generic.List<Component> { ptv };
        }
        else if (!pv.ObservedComponents.Contains(ptv))
        {
            pv.ObservedComponents.Add(ptv);
        }
    }

    void Start()
    {
        UpdateVisualState();
    }

    public void Collect()
    {
        if (IsCollected) return;

        if (PhotonNetwork.IsConnected)
        {
            // Request ownership then collect
            photonView.RequestOwnership();
            photonView.RPC("RPC_CollectItem", RpcTarget.AllBuffered);
        }
        else
        {
            LocalCollect();
        }
    }

    [PunRPC]
    void RPC_CollectItem()
    {
        if (IsCollected) return;

        isCollected = true;
        UpdateVisualState();

        Debug.Log($"{itemName} collected by player");

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCollectedItem(this);
            GameManager.Instance.AddScore(itemValue);
        }

        // Only master client destroys the object after a delay
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DestroyAfterDelay(2f));
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (photonView != null && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void LocalCollect()
    {
        isCollected = true;
        UpdateVisualState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCollectedItem(this);
            GameManager.Instance.AddScore(itemValue);
        }

        Destroy(gameObject, 0.1f);
    }

    private void UpdateVisualState()
    {
        if (itemCollider != null)
            itemCollider.enabled = !IsCollected;
        if (itemRenderer != null)
            itemRenderer.enabled = !IsCollected;
    }

    // Simplified trigger detection - let BoatCollector handle the ownership check
    private void OnTriggerEnter(Collider other)
    {
        if (IsCollected) return;

        BoatCollector boatCollector = other.GetComponent<BoatCollector>();
        if (boatCollector != null)
        {
            boatCollector.SetNearbyCollectable(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BoatCollector boatCollector = other.GetComponent<BoatCollector>();
        if (boatCollector != null)
        {
            boatCollector.ClearNearbyCollectable(this);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Photon Components")]
    private void SetupPhotonComponents()
    {
        EnsurePhotonComponents();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}