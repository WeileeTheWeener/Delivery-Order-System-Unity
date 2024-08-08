using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OrderSystem : SerializedMonoBehaviour
{
    public static OrderSystem instance;

    [SerializeField] AnimationCurve orderPlacementChanceCurve;
    [SerializeField] public AnimationCurve storeQualityPriceCurve;
    [SerializeField][Range(0, 5)] public float globalTipMultiplier;
    [SerializeField] GameObject orderPrefab;
    [SerializeField] GameObject orderUIPrefab;
    [SerializeField] PlayerPhone playersPhone;

    [ReadOnly] public int nextOrderNumber;
    [SerializeField][ReadOnly] float orderPlacementChance;
    [SerializeField][ReadOnly] float timeLeftForNextOrder;

    [HorizontalGroup("row1", Title = "Random Order Settings",LabelWidth = 0)]
    [HorizontalGroup("row1",Width = 1)]
    [SerializeField] float minTimeLeftForNextOrderSeconds,maxTimeLeftForNextOrderSeconds;
    [HorizontalGroup("row3", Width = 0)]
    [SerializeField] float randomOrderPendingExpireMinTimeSeconds, randomOrderPendingExpireMaxTimeSeconds;
    [HorizontalGroup("row4", Width = 0)]
    [SerializeField] float randomOrderDeliveryExpireMinTimeSeconds, randomOrderDeliveryExpireMaxTimeSeconds;

    [HideInInspector] public UnityEvent<Order> onOrderPlacedByCustomer;
    [HideInInspector] public UnityEvent onOrderCanceledByCustomer;

    [Tooltip("Accepted And Pending Orders")]
    public List<Order> currentActiveOrders; //accepted And pending orders
    public List<GameObject> orderDeliveryPositions;
    public List<Store> stores;

    float nextOrderTime;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        currentActiveOrders = new List<Order>();
        nextOrderNumber = 1;
        nextOrderTime = Time.time + Random.Range(minTimeLeftForNextOrderSeconds,maxTimeLeftForNextOrderSeconds);
    }
    private void Update()
    {
        // Update the time left for the next order
        timeLeftForNextOrder = nextOrderTime - Time.time;

        // Evaluate the order placement chance based on the time of day
        orderPlacementChance = orderPlacementChanceCurve.Evaluate(DayAndNightSystem.instance.timeOfDayNormalized);

        // Check if it's time to place the next order
        if (Time.time > nextOrderTime)
        {
            // Check if a random value is less than the order placement chance
            if (Random.value * 100 < orderPlacementChance)
            {
                PlaceRandomCustomerOrder();
            }

            // Schedule the next order time
            nextOrderTime = Time.time + Random.Range(minTimeLeftForNextOrderSeconds, maxTimeLeftForNextOrderSeconds);
        }
    }
    public void PlaceNewCustomerOrder(Order newOrder)
    {
        AddOrderToActiveOrdersList(newOrder);
        InstantiateNewOrderUI(newOrder, playersPhone.orderContentHolder.gameObject);   
        onOrderPlacedByCustomer.Invoke(newOrder);
    }
    private void AddOrderToActiveOrdersList(Order order)
    {
        if(!currentActiveOrders.Contains(order))
        {
            currentActiveOrders.Add(order);
        }
    }
    public void RemoveOrderFromActiveOrdersList(Order order)
    {
        if (currentActiveOrders.Contains(order))
        {
            currentActiveOrders.Remove(order);
        }
    }
    private Order GenerateRandomOrder()
    {
        Order order = Instantiate(orderPrefab).GetComponent<Order>();
        order.orderedItems = new List<ProductScriptableObject>();
        int randomOrderPosIndex = Random.Range(0, orderDeliveryPositions.Count - 1);
        int randomStoreIndex = Random.Range(0, stores.Count - 1);
        order.orderPlacedOnTime = Time.time;
        order.orderPendingExpireTime = Random.Range(randomOrderPendingExpireMinTimeSeconds, randomOrderPendingExpireMaxTimeSeconds);
        order.orderDeliveryExpireTime = Random.Range(randomOrderDeliveryExpireMinTimeSeconds, randomOrderDeliveryExpireMaxTimeSeconds);
        order.orderDeliveryPositionObject = orderDeliveryPositions[randomOrderPosIndex];
        order.orderedFrom = stores[randomStoreIndex];
        order.orderDeliveryDistance = Vector3.Distance(order.orderDeliveryPositionObject.transform.position, order.orderedFrom.transform.position);
        order.orderedItems.Add(order.orderedFrom.ReturnRandomAvailableProduct());
        order.orderNumber = nextOrderNumber;
        
        nextOrderNumber++;

        return order;      
    }
    [ContextMenu("PlaceRandomCustomerOrder")]
    public void PlaceRandomCustomerOrder()
    {
        PlaceNewCustomerOrder(GenerateRandomOrder());
    }
    [ContextMenu("ClearActiveOrderList")]
    public void ClearActiveOrderList()
    {
        currentActiveOrders.Clear();
    }
    [ContextMenu("ListActiveOrdersCount")]
    public void ListActiveOrdersCount()
    {
        Debug.Log("Active orders: " + currentActiveOrders.Count);
    }
    [ContextMenu("ListActiveOrders")]
    public void ListActiveOrders()
    {
        foreach (var order in currentActiveOrders)
        {
            string orderedItemsInfo = string.Join(", ", order.orderedItems.Select(item => $"Item Name: {item.productName}, Price: {item.price}"));
            Debug.Log($"Order: {currentActiveOrders.IndexOf(order)} -Ordered Items: {orderedItemsInfo}");
        }
    }
    public void InstantiateNewOrderUI(Order order, GameObject orderContentHolder)
    {
        GameObject orderUI = Instantiate(orderUIPrefab, orderContentHolder.transform);
        OrderUI orderUIsComponent = orderUI.GetComponent<OrderUI>();

        orderUIsComponent.order = order;
        orderUIsComponent.storeImage.sprite = order.orderedFrom.storeSO.icon;
        orderUIsComponent.storeNameText.text = order.orderedFrom.storeSO.storeName;
        orderUIsComponent.pendingExpireSlider.maxValue = order.orderPendingExpireTime;

        order.OnOrderAccepted.AddListener(() => orderUIsComponent.OnAcceptOrder());
        order.OnOrderDeclined.AddListener(() => orderUIsComponent.DestroyGameObject());
        order.OnOrderPendingTimeExpired.AddListener(() => orderUIsComponent.DestroyGameObject());
        order.OnOrderCompleted.AddListener(() => orderUIsComponent.DestroyGameObject());
    }
}
