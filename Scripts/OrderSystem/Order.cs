using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class Order : MonoBehaviour
{
    public enum OrderStatus
    {
        pending,
        accepted,
    }

    public OrderStatus status;

    public List<ProductScriptableObject> orderedItems;
    public List<OrderItem> instantiatedOrderObjects;
    public Store orderedFrom;

    public UnityEvent OnOrderCompleted;
    public UnityEvent OnOrderAccepted;
    public UnityEvent OnOrderDeclined;
    public UnityEvent OnOrderPendingTimeExpired;
    public UnityEvent OnOrderDeliveryTimeExpired;

    public float orderPlacedOnTime;
    public float orderAcceptedOnTime;
    public float orderDeliveredOnTime;

    public float orderDeliveryExpireTime;
    public float orderPendingExpireTime;
    public float timeLeftForPendingExpiration;
    public float timeLeftForDeliveryExpiration;
    
    public GameObject orderDeliveryPositionObject;

    public int orderNumber;
    public float orderDeliveryDistance;

    private void Start()
    {
        status = OrderStatus.pending;
        timeLeftForPendingExpiration = orderPendingExpireTime;
        timeLeftForDeliveryExpiration = orderDeliveryExpireTime;

        OnOrderPendingTimeExpired.AddListener(() =>
        {
            OrderSystem.instance.RemoveOrderFromActiveOrdersList(this);
            Destroy(gameObject);
        });

        OnOrderDeliveryTimeExpired.AddListener(() =>
        {
            PlayerOrderHandler.instance.NotifyFailedToDeliverOrder(this);
            OrderSystem.instance.RemoveOrderFromActiveOrdersList(this);
            Destroy(gameObject);
        });

        OnOrderAccepted.AddListener(() => status = OrderStatus.accepted);
        OnOrderAccepted.AddListener(() => PlayerOrderHandler.instance.NotifyAcceptedOrders(this));
        OnOrderAccepted.AddListener(() => StartCoroutine(orderedFrom.PrepareOrder(this)));
        OnOrderAccepted.AddListener(() => orderAcceptedOnTime = Time.time);
        
        OnOrderAccepted.AddListener(() =>
        {
            orderDeliveryPositionObject.SetActive(true);
            TMP_Text orderDeliveryText = orderDeliveryPositionObject.GetComponentInChildren<TMP_Text>();
            orderDeliveryText.text = $"Drop Order {orderNumber} Here";
        });

        OnOrderDeclined.AddListener(() => PlayerOrderHandler.instance.NotifyDeclinedOrders(this));

        OnOrderDeclined.AddListener(() =>
        {
            OrderSystem.instance.RemoveOrderFromActiveOrdersList(this);
            Destroy(gameObject);
        });

        OnOrderCompleted.AddListener(() => PlayerOrderHandler.instance.NotifyDeliveredOrders(this));

        OnOrderCompleted.AddListener(() =>
        {
            OrderSystem.instance.RemoveOrderFromActiveOrdersList(this);
            Destroy(gameObject);
        });

        OnOrderCompleted.AddListener(() =>
        {          
            foreach(OrderItem go in instantiatedOrderObjects)
            {
                Destroy(go.gameObject);          
            }

            instantiatedOrderObjects.Clear();
        });

        OnOrderCompleted.AddListener(() => orderDeliveryPositionObject.SetActive(false));
        OnOrderCompleted.AddListener(() =>
        {
            PlayerMoneyManager.instance.EarnMoney(OrderSystem.instance.storeQualityPriceCurve.Evaluate(orderedFrom.storeSO.quality) 
                * orderDeliveryDistance / 10 * OrderSystem.instance.globalTipMultiplier);
        });
    }
    private void Update()
    {
        //pending order timer
        if(status == OrderStatus.pending)
        {
            timeLeftForPendingExpiration -= Time.deltaTime;
            timeLeftForPendingExpiration = Mathf.Clamp(timeLeftForPendingExpiration, 0, orderPendingExpireTime);

            if (timeLeftForPendingExpiration == 0)
            {
                OnOrderPendingTimeExpired.Invoke();
            }
        }
        //accepted order timer
        if(status == OrderStatus.accepted)
        {
            timeLeftForDeliveryExpiration -= Time.deltaTime;
            timeLeftForDeliveryExpiration = Mathf.Clamp(timeLeftForDeliveryExpiration, 0, orderDeliveryExpireTime);

            if (timeLeftForDeliveryExpiration == 0)
            {
                OnOrderDeliveryTimeExpired.Invoke();
            }
        }
    }
    [ContextMenu("AcceptOrder")]
    private void AcceptOrder()
    {
        OnOrderAccepted.Invoke();
    }
    [ContextMenu("DeclineOrder")]
    private void DeclineOrder()
    {
        OnOrderDeclined.Invoke();
    }
    public bool CheckIfAllItemsAreDelivered()
    {
        bool allDelivered = true;

        foreach (OrderItem item in instantiatedOrderObjects)
        {
            if (!item.isDelivered)
            {
                allDelivered = false;
                break; 
            }
        }

        return allDelivered;
    }


}
