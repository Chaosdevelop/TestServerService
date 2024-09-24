using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class EventService : MonoBehaviour
{
	// Путь к папке хранения данных проекта
	string Path => System.IO.Path.Combine(Application.persistentDataPath, savedEventsFilePath);

	[SerializeField]
	private string savedEventsFilePath = "events.json";
	[SerializeField]
	private string serverUrl = "https://your-analytics-server.com/events";
	[SerializeField]
	private float cooldownBeforeSend = 1f;

	// Список всех событий для отправки
	private List<EventData> events = new List<EventData>();
	// Список событий ожидающих отправки, т.к. в момент отправки общий список events может изменится.
	private List<EventData> pendingEvents = new List<EventData>();
	// Task отправки событий
	private Coroutine sendEventsCoroutine;

	//Лучше инициализировать значения из какого-нибудь конфига проекта до вызова Start
	public void Initialize(string savedEventsFilePath, string serverUrl, float cooldownBeforeSend)
	{
		this.savedEventsFilePath = savedEventsFilePath;
		this.serverUrl = serverUrl;
		this.cooldownBeforeSend = cooldownBeforeSend;
	}

	private void Start()
	{
		LoadEvents();
		// Запускаем отправку событий
		sendEventsCoroutine = StartCoroutine(SendEvents());
	}



	private void LoadEvents()
	{
		// Проверяем, существует ли файл с событиями
		if (File.Exists(Path))
		{
			// Читаем события из файла
			string json = File.ReadAllText(Path);
			var eventsList = JsonConvert.DeserializeObject<EventsList>(json);

			// Добавляем сохраненные события в список отправки
			events.AddRange(eventsList.events);
		}
	}

	// Метод для отслеживания события
	public void TrackEvent(string type, string data)
	{
		// Добавляем событие в список
		events.Add(new EventData { type = type, data = data });

		// Если отправка событий еще не запущена, запускаем ее
		if (sendEventsCoroutine == null)
		{
			sendEventsCoroutine = StartCoroutine(SendEvents());
		}
	}

	// Асинхронный метод отправки событий
	private IEnumerator SendEvents()
	{
		// Только если есть события для отправки
		if (events.Count == 0)
		{
			yield break;
		}

		pendingEvents.AddRange(events);
		events.Clear();

		// Преобразуем события в JSON
		var json = JsonConvert.SerializeObject(new EventsList { events = pendingEvents });

		// Создаем UnityWebRequest для отправки POST-запроса
		using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, json, "application/json"))
		{
			byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);

			// Отправляем запрос
			yield return request.SendWebRequest();

			// Проверяем результат
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

		//В любом случае сохраняем список событий(пустой или не очень)
		SaveEvents();

		// Ждем кулдауна
		yield return new WaitForSeconds(cooldownBeforeSend);

		// Сбрасываем корутину
		sendEventsCoroutine = null;
	}

	//Не сохраняем все события каждый раз при добавлении нового, а только в случае неудачной отправки. Будем считать, что сохранение игрового прогресса синхронизировано с сохранением ивентов.
	private void SaveEvents()
	{
		events.AddRange(pendingEvents);
		pendingEvents.Clear();
		// Сохраняем события в файл
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

	//Обертка списка для сохранения формата отправляемых данных
	private struct EventsList
	{
		public List<EventData> events;
	}

	// Модель события
	private struct EventData
	{
		public string type;
		public string data;
	}
}