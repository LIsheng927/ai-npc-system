using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class NPCCommand
{
    public int id;
    public string action;
    public float x;
    public float z;
}

[Serializable]
public class NPCResult
{
    public int id;
    public string status;
}

public class BackendConnection : MonoBehaviour
{
    public NPCMovement npc;
    public NPCInventory inventory;
    [SerializeField] private string baseUrl = "http://127.0.0.1:8001";
    private bool reportSucceeded;

    private IEnumerator Start()
    {
        if (npc == null || inventory == null)
        {
            Debug.LogError("请在 Inspector 中绑定 NPC 和 Inventory！");
            yield break;
        }

        while (true)
        {
            NPCCommand command;

            // 1. 获取当前指令。
            using (UnityWebRequest request = UnityWebRequest.Get(
                baseUrl.TrimEnd('/') + "/command"))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("获取指令失败：" + request.error);
                    yield break;
                }

                command = JsonUtility.FromJson<NPCCommand>(
                    request.downloadHandler.text
                );
            }

            if (command == null)
            {
                Debug.LogError("指令为空，停止执行");
                yield break;
            }

            // 2. 没有剩余任务，结束循环。
            if (command.action == "done")
            {
                Debug.Log("所有任务已完成！");
                yield break;
            }

            Debug.Log(
                "准备执行指令 #" + command.id
                + "，动作：" + command.action
            );

            bool accepted;

            if (command.action == "move")
            {
                accepted = npc.MoveTo(command.x, command.z);
            }
            else if (command.action == "buy_food")
            {
                accepted = inventory.TryBuyFood();
            }
            else if (command.action == "deliver_food")
            {
                accepted = inventory.TryDeliverFood();
            }
            else
            {
                Debug.LogError("无法识别的动作：" + command.action);
                yield break;
            }

            yield return SendResult(
                command.id,
                accepted ? "accepted" : "rejected"
            );

            if (!reportSucceeded || !accepted)
            {
                Debug.LogWarning("回报失败或指令被拒绝，停止任务流程");
                yield break;
            }

            if (command.action == "move")
            {
                while (npc.IsMoving)
                {
                    yield return null;
                }
            }

            Debug.Log("指令 #" + command.id + " 已完成");

            yield return SendResult(command.id, "completed");

            if (!reportSucceeded)
            {
                Debug.LogError("完成回报失败，停止任务流程");
                yield break;
            }

            // 到达循环末尾，回到顶部获取下一条指令。
        }
    }

    private IEnumerator SendResult(int commandId, string status)
    {
        reportSucceeded = false;
        string json = JsonUtility.ToJson(new NPCResult
        {
            id = commandId,
            status = status
        });

        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(
            baseUrl.TrimEnd('/') + "/result", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                reportSucceeded = true;
                Debug.Log("回报发送成功：" + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("回报发送失败：" + request.error + " " + request.downloadHandler.text);
            }
        }
    }
}
