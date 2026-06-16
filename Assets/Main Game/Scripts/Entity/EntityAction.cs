using UnityEngine;

public abstract class EntityAction : ScriptableObject
{
    public string actionName;
    public AudioClip eventVoice;
    
    public virtual void OnActionSelected() {}

    public abstract void ExecuteAction(GameObject target);
}
