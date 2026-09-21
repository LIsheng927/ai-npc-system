using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    public float speed = 2f;

    private Vector3 target;
    private bool isMoving;
    public bool IsMoving => isMoving;

    public bool MoveTo(float x, float z)
    {
        if (x < -4f || x > 4f || z < -4f || z > 4f)
        {
            Debug.LogWarning(
                "拒绝移动：目标超出范围，x=" + x + "，z=" + z
            );
            return false;
        }

        target = new Vector3(x, transform.position.y, z);
        isMoving = true;

        Debug.Log("收到移动目标：" + target);
        return true;
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            isMoving = false;
            Debug.Log("NPC 已到达目的地！");
        }
    }
}
