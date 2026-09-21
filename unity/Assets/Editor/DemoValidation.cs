using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Rebuild missing scene bindings and check gameplay rules without network calls.
public static class DemoValidation
{
    [MenuItem("NPC Demo/Prepare and Validate Scene")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        NPCInventory inventory = UnityEngine.Object.FindFirstObjectByType<NPCInventory>();
        NPCMovement movement = UnityEngine.Object.FindFirstObjectByType<NPCMovement>();
        BackendConnection connection = UnityEngine.Object.FindFirstObjectByType<BackendConnection>();
        Require(inventory != null && movement != null && connection != null, "Missing NPC components");

        GameObject storageObject = GameObject.Find("HomeStorage");
        if (storageObject == null)
        {
            storageObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storageObject.name = "HomeStorage";
        }
        storageObject.transform.position = new Vector3(0, 0.5f, -1);
        HomeStorage storage = storageObject.GetComponent<HomeStorage>();
        if (storage == null) storage = storageObject.AddComponent<HomeStorage>();

        SerializedObject values = new SerializedObject(inventory);
        values.FindProperty("homeStorage").objectReferenceValue = storage;
        values.FindProperty("coins").intValue = 10;
        values.FindProperty("foodCount").intValue = 0;
        values.ApplyModifiedPropertiesWithoutUndo();
        connection.npc = movement;
        connection.inventory = inventory;
        connection.enabled = true;

        inventory.transform.position = new Vector3(0, 1, 0);
        Require(!inventory.TryBuyFood(), "Cannot buy away from shop");
        Require(!inventory.TryDeliverFood(), "Cannot deliver empty inventory");
        Require(!movement.MoveTo(20, 2), "Reject out-of-bounds movement");
        inventory.transform.position = new Vector3(3, 1, 0.8f);
        Require(inventory.TryBuyFood(), "Buy at shop");
        Require(GetInt(inventory, "coins") == 7, "Purchase deducts exact price");
        Require(GetInt(inventory, "foodCount") == 1, "Purchase adds one food");
        Require(!inventory.TryDeliverFood(), "Cannot deliver away from home");
        inventory.transform.position = new Vector3(0, 1, 0);
        Require(inventory.TryDeliverFood(), "Deliver at home");
        Require(GetInt(inventory, "foodCount") == 0, "Delivery removes food");
        Require(GetInt(storage, "foodCount") == 1, "Storage receives food");
        Require(!inventory.TryDeliverFood(), "Repeated delivery cannot create food");
        SetInt(inventory, "coins", 2);
        inventory.transform.position = new Vector3(3, 1, 0.8f);
        Require(!inventory.TryBuyFood(), "Reject purchase with insufficient money");
        Require(GetInt(inventory, "coins") == 2 && GetInt(inventory, "foodCount") == 0,
            "Rejected purchase leaves inventory unchanged");

        // Persist a clean initial state, never the mutated test state.
        SetInt(inventory, "coins", 10);
        SetInt(inventory, "foodCount", 0);
        SetInt(storage, "foodCount", 0);
        inventory.transform.position = new Vector3(0, 1, 0);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("NPC_DEMO_VALIDATION_PASSED");
    }

    private static int GetInt(UnityEngine.Object target, string field)
        => new SerializedObject(target).FindProperty(field).intValue;

    private static void SetInt(UnityEngine.Object target, string field, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
