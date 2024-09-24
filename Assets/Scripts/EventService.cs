using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class EventService : MonoBehaviour
{
	// ���� � ����� �������� ������ �������
	string Path => System.IO.Path.Combine(Application.persistentDataPath, savedEventsFilePath);

	[SerializeField]
	private string savedEventsFilePath = "events.json";
	[SerializeField]
	private string serverUrl = "https://your-analytics-server.com/events";
	[SerializeField]
	private float cooldownBeforeSend = 1f;

	// ������ ���� ������� ��� ��������
	private List<EventData> events = new List<EventData>();
	// ������ ������� ��������� ��������, �.�. � ������ �������� ����� ������ events ����� ���������.
	private List<EventData> pendingEvents = new List<EventData>();
	// Task �������� �������
	private Coroutine sendEventsCoroutine;

	//����� ���������������� �������� �� ������-������ ������� ������� �� ������ Start
	public void Initialize(string savedEventsFilePath, string serverUrl, float cooldownBeforeSend)
	{
		this.savedEventsFilePath = savedEventsFilePath;
		this.serverUrl = serverUrl;
		this.cooldownBeforeSend = cooldownBeforeSend;
	}

	private void Start()
	{
		LoadEvents();
		// ��������� �������� �������
		sendEventsCoroutine = StartCoroutine(SendEvents());
	}



	private void LoadEvents()
	{
		// ���������, ���������� �� ���� � ���������
		if (File.Exists(Path))
		{
			// ������ ������� �� �����
			string json = File.ReadAllText(Path);
			var eventsList = JsonConvert.DeserializeObject<EventsList>(json);

			// ��������� ����������� ������� � ������ ��������
			events.AddRange(eventsList.events);
		}
	}

	// ����� ��� ������������ �������
	public void TrackEvent(string type, string data)
	{
		// ��������� ������� � ������
		events.Add(new EventData { type = type, data = data });

		// ���� �������� ������� ��� �� ��������, ��������� ��
		if (sendEventsCoroutine == null)
		{
			sendEventsCoroutine = StartCoroutine(SendEvents());
		}
	}

	// ����������� ����� �������� �������
	private IEnumerator SendEvents()
	{
		// ������ ���� ���� ������� ��� ��������
		if (events.Count == 0)
		{
			yield break;
		}

		pendingEvents.AddRange(events);
		events.Clear();

		// ����������� ������� � JSON
		var json = JsonConvert.SerializeObject(new EventsList { events = pendingEvents });

		// ������� UnityWebRequest ��� �������� POST-�������
		using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, json, "application/json"))
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);

			// ���������� ������
			yield return request.SendWebRequest();

			// ��������� ���������
			if (request.result == UnityWebRequest.Result.Success)
			{
				pendingEvents.Clear();
				Debug.Log("Succesfull send!");
			}
			else
			{
				Debug.LogError("Events sending error: " + request.error);
			}
		}

		//� ����� ������ ��������� ������ �������(������ ��� �� �����)
		SaveEvents();

		// ���� ��������
		yield return new WaitForSeconds(cooldownBeforeSend);

		// ���������� ��������
		sendEventsCoroutine = null;
	}

	//�� ��������� ��� ������� ������ ��� ��� ���������� ������, � ������ � ������ ��������� ��������. ����� �������, ��� ���������� �������� ��������� ���������������� � ����������� �������.
	private void SaveEvents()
	{
		events.AddRange(pendingEvents);
		pendingEvents.Clear();
		// ��������� ������� � ����
		if (events.Count > 0)
		{
			var json = JsonConvert.SerializeObject(new EventsList { events = events });
			try
			{
				File.WriteAllText(Path, json);
			}
			catch
			{
				Debug.LogError("Cant write events to file");
			}

		}
	}

	//������� ������ ��� ���������� ������� ������������ ������
	private struct EventsList
	{
		public List<EventData> events;
	}

	// ������ �������
	private struct EventData
	{
		public string type;
		public string data;
	}
}