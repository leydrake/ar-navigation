using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EventsFetcherDebugUI : MonoBehaviour
{
    [SerializeField] private EventsFetcher eventsFetcher;
    [SerializeField] private Text debugText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button refreshButton;
    
    private List<string> debugMessages = new List<string>();
    private int maxMessages = 20;

    void Start()
    {
        if (eventsFetcher == null)
            eventsFetcher = FindObjectOfType<EventsFetcher>();

        if (eventsFetcher != null)
        {
            eventsFetcher.EventsChanged += OnEventsChanged;
            eventsFetcher.LoadingChanged += OnLoadingChanged;
            eventsFetcher.ErrorOccurred += OnErrorOccurred;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(() => eventsFetcher?.FetchAllEvents());

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshDebugInfo);

        AddDebugMessage("Debug UI initialized");
    }

    void OnDestroy()
    {
        if (eventsFetcher != null)
        {
            eventsFetcher.EventsChanged -= OnEventsChanged;
            eventsFetcher.LoadingChanged -= OnLoadingChanged;
            eventsFetcher.ErrorOccurred -= OnErrorOccurred;
        }
    }

    private void OnEventsChanged(List<EventData> events)
    {
        AddDebugMessage($"Events loaded: {events.Count} items");
        RefreshDebugInfo();
    }

    private void OnLoadingChanged(bool isLoading)
    {
        AddDebugMessage($"Loading state: {isLoading}");
        RefreshDebugInfo();
    }

    private void OnErrorOccurred(string error)
    {
        AddDebugMessage($"ERROR: {error}");
        RefreshDebugInfo();
    }

    private void AddDebugMessage(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logMessage = $"[{timestamp}] {message}";
        
        debugMessages.Add(logMessage);
        
        // Keep only the last maxMessages
        if (debugMessages.Count > maxMessages)
        {
            debugMessages.RemoveAt(0);
        }
        
        Debug.Log($"[EventsFetcherDebugUI] {message}");
    }

    private void RefreshDebugInfo()
    {
        if (debugText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"Internet: {Application.internetReachability}");
        sb.AppendLine($"Events Count: {eventsFetcher?.events?.Count ?? 0}");
        sb.AppendLine();
        sb.AppendLine("Debug Log:");
        
        foreach (string message in debugMessages)
        {
            sb.AppendLine(message);
        }

        debugText.text = sb.ToString();
    }

    void Update()
    {
        // Refresh every few seconds to show current state
        if (Time.time % 3f < 0.1f)
        {
            RefreshDebugInfo();
        }
    }
}
