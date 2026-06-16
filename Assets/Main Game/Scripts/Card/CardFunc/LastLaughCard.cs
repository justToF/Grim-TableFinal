using UnityEngine;

[CreateAssetMenu(fileName = "New Last Laugh Card", menuName = "Cards/Last Laugh")]
public class LastLaughCard : CardData
{

    public override void Execute(GameObject user, GameObject target)
    {

        Debug.Log("Last Laugh nu se folosește manual.");
    }
}
