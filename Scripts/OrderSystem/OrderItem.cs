using Measure;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class OrderItem : MonoBehaviour, IInteractable
{
    [HideInInspector] public ProductScriptableObject productSO;
    public Order belongsToOrder;
    public bool isDelivered; 
    private IInteractable.InteractableType type;
    private Bounds nonTriggerColliderBounds;
    public Rigidbody rb;

    private void Reset()
    {
        //add neceasary colliders and rigidbody if not avaiable, set the presets
    }
    private void Awake()
    {
        type = IInteractable.InteractableType.Product;

        GetComponents<Collider>().ForEach((collider) =>
        {
            if (!collider.isTrigger)
            {
                nonTriggerColliderBounds = (collider.bounds);
                //collider.enabled = false;
            }
        });

        rb = GetComponent<Rigidbody>();
    }
    public void OnInteract(InteractionState state)
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (belongsToOrder && other.gameObject == belongsToOrder.orderDeliveryPositionObject)
        {
            isDelivered = true;

            if (belongsToOrder.CheckIfAllItemsAreDelivered())
            {
                belongsToOrder.OnOrderCompleted.Invoke();
            }
        }
    }

    public IInteractable.InteractableType GetInteractableType()
    {
        return type;
    }
    public Bounds GetObjectsBounds()
    {
        return nonTriggerColliderBounds;
    }

    public string GetUIInteractionAction()
    {
        return "Pickup/Drop";
    }
}
