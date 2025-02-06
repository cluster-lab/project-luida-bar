#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit.Trigger;
using ClusterVR.CreatorKit.Trigger.Implements;

public class TriggerSettings: MonoBehaviour
{
    public Vector3 vector = Vector3.zero;
    public float radius = 0f;
    public float triggerTime = 0f;
    public int basicColliderNumber = 0;
    public string coordSys = "World";
    public string objectFilePath = "";
    public Vector3 size = Vector3.one;
    public float value = 0f;
    public string compareOp = "MoreThan";
    public string actionType = "Start";
    public string key = "None";
    public string eventType = "Time";
    public string targetLink = "LeftArm";
    public string colliderID = "";
    public Vector3 triggerRotation = Vector3.zero;
    public Vector3 triggerPosition = Vector3.zero;

    // Future trigger-handling methods
    
    [SerializeField] [HideInInspector] private ItemTimer itemTimer;

    public void ReflectXmlSettingsToCCK()
    {
        ConstantTriggerParam actionTrigger = null;
        
        if (actionType == "Start" || actionType == "Stop")
        {
            ClusterVR.CreatorKit.Trigger.Implements.Value valueInstance = new ClusterVR.CreatorKit.Trigger.Implements.Value();
            FieldInfo boolField = typeof(ClusterVR.CreatorKit.Trigger.Implements.Value).GetField("boolValue", BindingFlags.Instance | BindingFlags.NonPublic);
            boolField.SetValue(valueInstance, actionType == "Start");
            
            actionTrigger = new ConstantTriggerParam(TriggerTarget.Item, GetComponent<Item>(), "active",
                ClusterVR.CreatorKit.ParameterType.Bool, valueInstance);
        }
        
        if (eventType == "Time")
        {
            var timerKey = "timer_" + System.Array.IndexOf(GetComponents<TriggerSettings>(), this);
            
            // Add trigger to OnCreateItemTrigger with key "timer_{number of this trigger settings}"
            ConstantTriggerParam trigger = new ConstantTriggerParam(TriggerTarget.Item, GetComponent<Item>(), timerKey,
                ClusterVR.CreatorKit.ParameterType.Signal, null);
            
            var onCreateItemTrigger = GetComponent<OnCreateItemTrigger>();
            FieldInfo triggersField = typeof(OnCreateItemTrigger).GetField("triggers", BindingFlags.Instance | BindingFlags.NonPublic);
            var currentTriggers = triggersField.GetValue(onCreateItemTrigger) as ConstantTriggerParam[] ?? new ConstantTriggerParam[0];
            var triggerList = currentTriggers.ToList();
            
            FieldInfo keyField = typeof(ConstantTriggerParam).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!triggerList.Any(trigger => {
                    var keyValue = keyField.GetValue(trigger) as string;
                    return keyValue != null && keyValue.Contains(timerKey);
                }))
            {
                triggerList.Add(trigger);
                triggersField.SetValue(onCreateItemTrigger, triggerList.ToArray());
            }
            
            
            if (!itemTimer)
            {
                // Add ItemTimer with key "timer_{number of this trigger settings}"
                itemTimer = gameObject.AddComponent<ItemTimer>();
                FieldInfo keyFieldInItemTimer = typeof(ItemTimer).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic);
                GimmickKey gimmickKey = keyFieldInItemTimer.GetValue(itemTimer) as GimmickKey;
                FieldInfo innerKeyField = typeof(GimmickKey).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic);
                innerKeyField.SetValue(gimmickKey, timerKey);
                
                // Set delayTimeSeconds for the ItemTimer
                FieldInfo delayField = typeof(ItemTimer).GetField("delayTimeSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
                delayField.SetValue(itemTimer, triggerTime);

                // Add `actionTrigger` as one of the triggers of the ItemTimer
                FieldInfo timerTriggersField = typeof(ItemTimer).GetField("triggers", BindingFlags.Instance | BindingFlags.NonPublic);
                timerTriggersField.SetValue(itemTimer, new[] { actionTrigger });

                EditorUtility.SetDirty(gameObject);
            }
        }
    }
}

#endif
