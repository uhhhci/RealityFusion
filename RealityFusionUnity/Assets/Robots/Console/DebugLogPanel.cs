using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugLogPanel : MonoBehaviour
{

    [Tooltip("Maximum number of messages before deleting the older messages.")]
    [SerializeField]
    private int maxNumberOfMessages=10;

    [Tooltip("Check this if you want the stack trace printed after the message.")]
    [SerializeField]
    private bool includeStackTrace=false;

    private bool newMessageArrived = false;

    private TextMeshProUGUI debugText;

    // The queue with the messages:
    private Queue<string> messageQueue;



    void OnEnable()
    {
        messageQueue = new Queue<string>();       
        debugText = GetComponent<TextMeshProUGUI>();
        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
    }
   
    void SetTextColor(LogType type)
    {
        if (type == LogType.Error)
        {
            debugText.color = Color.red;

        }
        else if (type == LogType.Log)
        {
            debugText.color = Color.white;
        }
        else if (type == LogType.Warning)
        {
            debugText.color = Color.yellow;
        }
        else
        {
            debugText.color = Color.grey;
        }
    }
    private void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {

        if (debugText != null)
        {
           // SetTextColor(type);

            newMessageArrived = true;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("\n");
            stringBuilder.Append(condition);

            if (includeStackTrace)
            {
                stringBuilder.Append("\nStackTrace: ");
                stringBuilder.Append(stackTrace);
            }

            condition = stringBuilder.ToString();
            messageQueue.Enqueue(condition);

            if (messageQueue.Count > maxNumberOfMessages)
            {
                messageQueue.Dequeue();
            }
        }
        
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= Application_logMessageReceivedThreaded;
    }

    /// <summary>
    /// Print the queue to the text mesh.
    /// </summary>

    void PrintQueue()
    {
        StringBuilder stringBuilder = new StringBuilder();
        string[] messageList = messageQueue.ToArray();

        for (int i = 0; i < messageList.Length; i++) {
            stringBuilder.Append(messageList[i]);
            stringBuilder.Append("\n");
        }        

        string message = stringBuilder.ToString();
        debugText.text = message;
    }

    /// <summary>
    /// This Update method checks if a new message has arrived. The check is placed here to ensure
    /// that only the main thread will try to access the Text Mesh.
    /// </summary>

    void Update()
    {
        if (newMessageArrived)
        {
            PrintQueue();
            newMessageArrived = false;
        }
    }
}
