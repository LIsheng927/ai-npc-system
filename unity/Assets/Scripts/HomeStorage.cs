using UnityEngine;

public class HomeStorage : MonoBehaviour
{
    [SerializeField] private int foodCount = 0;

    public void ReceiveOneFood()
    {
        foodCount += 1;
        Debug.Log("储物箱收到一份食物，当前数量：" + foodCount);
    }
}
