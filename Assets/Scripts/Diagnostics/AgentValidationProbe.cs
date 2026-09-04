using UnityEngine;

public class AgentValidationProbe : MonoBehaviour
{
    [SerializeField]
    private string message = "Agent validation succeeded";

    public string GetMessage()
    {
        return message;
    }
}
