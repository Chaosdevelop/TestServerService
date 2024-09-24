# TestServerService
 
This simple service allows you to track events in your game and send them to an analytics server.

Features:

Track events with TrackEvent(string type, string data).

Send events to the server using UnityWebRequest.

Save events to disk for guaranteed delivery, even if the app crashes.

Built-in cooldown to limit the number of requests to the server.

Usage:

Configure the service settings: serverUrl, cooldownBeforeSend, and savedEventsFilePath.

Use the TrackEvent method to track events.

Example:

// Track level start
EventService.instance.TrackEvent("levelStart", "level:1");

// Track reward received
EventService.instance.TrackEvent("rewardReceived", "item:gold");

// Track coins spent
EventService.instance.TrackEvent("coinsSpent", "amount:100");
Use code with caution.
C#
Important:

Remember to replace serverUrl with the actual URL of your analytics server.

The service uses Newtonsoft.Json for JSON serialization and deserialization. Make sure you have added this library to your Unity project.

Further Reading:

Newtonsoft.Json Documentation
