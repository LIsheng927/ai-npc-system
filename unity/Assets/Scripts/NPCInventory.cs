using UnityEngine;

public class NPCInventory : MonoBehaviour
{
    [SerializeField] private int coins = 10;
    [SerializeField] private int foodCount = 0;
    [SerializeField] private int foodPrice = 3;
    [SerializeField] private Transform shop;
    [SerializeField] private float buyDistance = 1.5f;
    [SerializeField] private HomeStorage homeStorage;
    [SerializeField] private float deliveryDistance = 1.5f;

    public bool TryBuyFood()
    {
        if (shop == null)
        {
            Debug.LogWarning("购买失败：没有绑定商店");
            return false;
        }

        // 只比较地面上的距离，忽略高度差。
        Vector3 npcPosition = transform.position;
        Vector3 shopPosition = shop.position;

        npcPosition.y = 0f;
        shopPosition.y = 0f;

        float distance = Vector3.Distance(npcPosition, shopPosition);

        if (distance > buyDistance)
        {
            Debug.LogWarning("购买失败：距离商店太远");
            return false;
        }

        if (foodPrice <= 0)
        {
            Debug.LogWarning("购买失败：价格必须大于 0");
            return false;
        }

        if (coins < foodPrice)
        {
            Debug.LogWarning("购买失败：金币不足");
            return false;
        }

        coins -= foodPrice;
        foodCount += 1;

        Debug.Log(
            "购买成功！剩余金币：" + coins
            + "，食物数量：" + foodCount
        );

        return true;
    }

    // 开发时手动测试购买，不需要接入 Python。
    [ContextMenu("测试购买一份食物")]
    private void TestBuyFood()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play 模式");
            return;
        }

        TryBuyFood();
    }

    public bool TryDeliverFood()
    {
        if (homeStorage == null)
        {
            Debug.LogWarning("交付失败：没有绑定储物箱");
            return false;
        }

        Vector3 npcPosition = transform.position;
        Vector3 storagePosition = homeStorage.transform.position;

        npcPosition.y = 0f;
        storagePosition.y = 0f;

        float distance = Vector3.Distance(npcPosition, storagePosition);

        if (distance > deliveryDistance)
        {
            Debug.LogWarning("交付失败：距离储物箱太远");
            return false;
        }

        if (foodCount < 1)
        {
            Debug.LogWarning("交付失败：背包里没有食物");
            return false;
        }

        foodCount -= 1;
        homeStorage.ReceiveOneFood();

        Debug.Log("交付成功！NPC 剩余食物：" + foodCount);
        return true;
    }

    [ContextMenu("测试交付一份食物")]
    private void TestDeliverFood()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play 模式");
            return;
        }

        TryDeliverFood();
    }
}
