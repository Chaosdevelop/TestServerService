using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class EventService : MonoBehaviour
{
	// ���� � ����� �������� ������ �������
	string Path => System.IO.Path.Combine(Application.persistentDataPath, savedEventsFilePath);

	private string savedEventsFilePath = "events.json";
	private string serverUrl = "http://your-analytics-server.com/events";
	private float cooldownBeforeSend = 1f;

	// ���������� ���� ��������� HttpClient, ������� ����� �������������� ��������
	private HttpClient client;
	// ������ ���� ������� ��� ��������
	private List<EventData> events = new List<EventData>();
	// ������ ������� ��������� ��������, �.�. � ������ �������� ����� ������ events ����� ���������.
	private List<EventData> pendingEvents = new List<EventData>();
	// Task �������� �������
	private Task sendEventsTask = Task.CompletedTask;



	//������ ���� ������ �� ���������� Start, �������� �� ������ Awake
	public void Initialize(string savedEventsFilePath, string serverUrl, float cooldownBeforeSend, HttpClient client)
	{
		this.savedEventsFilePath = savedEventsFilePath;
		this.serverUrl = serverUrl;
		this.cooldownBeforeSend = cooldownBeforeSend;
		this.client = client;
	}

	private async void Start()
	{
		LoadEvents();

		// ���������� ������� �� ������
		await SendEventsAsync();
	}

	private void LoadEvents()
	{
		// ���������, ���������� �� ���� � ���������
		if (File.Exists(savedEventsFilePath))
		{
			// ������ ������� �� �����
			string json = File.ReadAllText(savedEventsFilePath);
			List<EventData> savedEvents = JsonConvert.DeserializeObject<List<EventData>>(json);

			// ��������� ����������� ������� � ������ ��������
			events.AddRange(savedEvents);
		}
	}


	// ����� ��� ������������ �������
	public void TrackEvent(string type, string data)
	{
		// ��������� ������� � ������
		events.Add(new EventData { type = type, data = data });

		// ���� ������� ��� �� �������, ��������� ���
		if (sendEventsTask.IsCompleted)
		{
			sendEventsTask = SendEventsAsync();
		}
	}

	// ����������� ����� �������� �������
	private async Task SendEventsAsync()
	{
		// ������ ���� ���� ������� ��� ��������
		if (events.Count == 0) return;

		pendingEvents.AddRange(events);
		events.Clear();

		// ����������� ������� � JSON
		var json = JsonConvert.SerializeObject(new { events = pendingEvents });

		var content = new StringContent(json, Encoding.UTF8, "application/json");

		// ���������� ������ � ��������� ���������
		try
		{
			var response = await client.PostAsync(serverUrl, content);

			// ���� �������� ������ �������
			if (response.IsSuccessStatusCode)
			{
				pendingEvents.Clear();
			}
		}
		catch (Exception)
		{
			// ��������� ������
			Debug.LogError("Something went wrong when posting events.");
		}

		//� ����� ������ ��������� ������ �������(������ ��� �� �����)
		SaveEvents();
		// ���� ��������
		await Task.Delay((int)(cooldownBeforeSend * 1000));
	}


	//�� ��������� ��� ������� ������ ��� ��� ���������� ������, � ������ � ������ ��������� ��������. ����� �������, ��� ���������� �������� ��������� ���������������� � ����������� �������.
	private void SaveEvents()
	{
		events.AddRange(pendingEvents);
		pendingEvents.Clear();
		// ��������� ������� � ����
		if (events.Count > 0)
		{
			var json = JsonConvert.SerializeObject(new { events = events });
			File.WriteAllText(Path, json);
		}
	}


	// ������ �������
	private struct EventData
	{
		public string type;
		public string data;
	}
}