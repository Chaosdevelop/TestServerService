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
	// Путь к папке хранения данных проекта
	string Path => System.IO.Path.Combine(Application.persistentDataPath, savedEventsFilePath);

	private string savedEventsFilePath = "events.json";
	private string serverUrl = "http://your-analytics-server.com/events";
	private float cooldownBeforeSend = 1f;

	// Используем один экземпляр HttpClient, которые будет использоваться повторно
	private HttpClient client;
	// Список всех событий для отправки
	private List<EventData> events = new List<EventData>();
	// Список событий ожидающих отправки, т.к. в момент отправки общий список events может изменится.
	private List<EventData> pendingEvents = new List<EventData>();
	// Task отправки событий
	private Task sendEventsTask = Task.CompletedTask;



	//Должен быть вызван до выполнения Start, например на стадии Awake
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

		// Отправляем события на сервер
		await SendEventsAsync();
	}

	private void LoadEvents()
	{
		// Проверяем, существует ли файл с событиями
		if (File.Exists(savedEventsFilePath))
		{
			// Читаем события из файла
			string json = File.ReadAllText(savedEventsFilePath);
			List<EventData> savedEvents = JsonConvert.DeserializeObject<List<EventData>>(json);

			// Добавляем сохраненные события в список отправки
			events.AddRange(savedEvents);
		}
	}


	// Метод для отслеживания события
	public void TrackEvent(string type, string data)
	{
		// Добавляем событие в список
		events.Add(new EventData { type = type, data = data });

		// Если кулдаун еще не запущен, запускаем его
		if (sendEventsTask.IsCompleted)
		{
			sendEventsTask = SendEventsAsync();
		}
	}

	// Асинхронный метод отправки событий
	private async Task SendEventsAsync()
	{
		// Только если есть события для отправки
		if (events.Count == 0) return;

		pendingEvents.AddRange(events);
		events.Clear();

		// Преобразуем события в JSON
		var json = JsonConvert.SerializeObject(new { events = pendingEvents });

		var content = new StringContent(json, Encoding.UTF8, "application/json");

		// Отправляем запрос и проверяем результат
		try
		{
			var response = await client.PostAsync(serverUrl, content);

			// Если отправка прошла успешно
			if (response.IsSuccessStatusCode)
			{
				pendingEvents.Clear();
			}
		}
		catch (Exception)
		{
			// Обработка ошибки
			Debug.LogError("Something went wrong when posting events.");
		}

		//В любом случае сохраняем список событий(пустой или не очень)
		SaveEvents();
		// Ждем кулдауна
		await Task.Delay((int)(cooldownBeforeSend * 1000));
	}


	//Не сохраняем все события каждый раз при добавлении нового, а только в случае неудачной отправки. Будем считать, что сохранение игрового прогресса синхронизировано с сохранением ивентов.
	private void SaveEvents()
	{
		events.AddRange(pendingEvents);
		pendingEvents.Clear();
		// Сохраняем события в файл
		if (events.Count > 0)
		{
			var json = JsonConvert.SerializeObject(new { events = events });
			File.WriteAllText(Path, json);
		}
	}


	// Модель события
	private struct EventData
	{
		public string type;
		public string data;
	}
}