using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public string serverAddress = "localhost";
    public int serverPort = 8080;

    [Header("Network Objects")]
    public GameObject playerPrefab;
    public GameObject playerRocketPrefab;
    public GameObject bossPrefab;
    public GameObject bossRocketPrefab;
    public GameObject playerallyPrefab;

    // Network pool
    // Player pool
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    // Player rocket pool
    public Dictionary<int, GameObject> playerRockets = new Dictionary<int, GameObject>();
    // Boss pool
    public Dictionary<int, GameObject> bosses = new Dictionary<int, GameObject>();
    // Boss rocket pool
    public Dictionary<int, GameObject> bossRockets = new Dictionary<int, GameObject>();

    //private host player
    private GameObject hostPlayer;

    //private boss
    private GameObject hostBoss;

    public static TcpClient client;

    [System.Serializable]
    public class MoveDataRequest
    {
        public string type;
        public float x;
        public float y;
        public int uuid;
        public int target_uuid; // This was added to match the server's expected format.
    }

    [System.Serializable]
    public class NewPlayerData
    {
        public int uuid;
        public string name;
        public Vector2 position;
        // Add any other fields you received
    }

    [System.Serializable]
    public class BroadcastMessage
    {
        public string type;
        public int health;
        public int attack;
        public Position position;
        public int uuid;
        public string username;
    }

    [System.Serializable]
    public class Position
    {
        public float X;
        public float Y;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(ConnectToServer());
    }

    IEnumerator ConnectToServer()
    {
        int attempts = 0;
        while (attempts < 3)
        {
            if (TryConnect())
            {
                Debug.Log("Connected to server successfully.");
                ReadInitialDataFromServer();
                StartListeningForBroadcasts();
                yield break; // Connection successful, exit the loop
            }
            else
            {
                Debug.Log("Failed to connect to server, retrying in 10 seconds...");
                attempts++;
                yield return new WaitForSeconds(10);
            }
        }
        Debug.Log("Failed to connect to server after 3 attempts.");
    }

    private bool TryConnect()
    {
        try
        {
            client = new TcpClient("localhost", 8080);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public void SendMoveData(int uuid, Vector2 move)
    {
        if (client == null || !client.Connected)
        {
            Debug.LogError("Not connected to server");
            return;
        }

        MoveDataRequest request = new MoveDataRequest {
            type = "move",
            x = move.x,
            y = move.y,
            uuid = uuid,
            target_uuid = 0 // This can be set to the appropriate target UUID if necessary.
        };

        string jsonRequest = JsonUtility.ToJson(request);

        // Debug output for the serialized JSON
        // Debug.Log("Serialized JSON to send: " + jsonRequest);

        jsonRequest += "\n"; // Add newline character, because your server code is reading until it encounters a newline

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
            client.GetStream().Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending data to server: " + e.Message);
        }
    }

    public void SendAttackData()
    {
        if (client == null || !client.Connected)
        {
            Debug.LogError("Not connected to server");
            return;
        }
        // check host player and boss
        if (hostPlayer == null || hostBoss == null)
        {
            Debug.LogError("Host player or boss is null");
            return;
        }
        // attack request
        MoveDataRequest request = new MoveDataRequest {
            type = "attack",
            x = 0,
            y = 0,
            uuid = hostPlayer.GetComponent<PlayerData>().uuid,
            // boss uuid
            target_uuid = hostBoss.GetComponent<PlayerData>().uuid
        };

        string jsonRequest = JsonUtility.ToJson(request);

        // Debug output for the serialized JSON
        // Debug.Log("Serialized JSON to send: " + jsonRequest);

        jsonRequest += "\n"; // Add newline character, because your server code is reading until it encounters a newline

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
            client.GetStream().Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending data to server: " + e.Message);
        }

    }

    private void ReadInitialDataFromServer()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string jsonData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        NewPlayerData playerData = JsonUtility.FromJson<NewPlayerData>(jsonData);

        // instantiate the player prefab
        GameObject player = Instantiate(playerPrefab, playerData.position, Quaternion.identity);
        // set the player's uuid
        player.GetComponent<PlayerData>().uuid = playerData.uuid;
        // add the player to the player pool
        players.Add(playerData.uuid, player);
        hostPlayer = player;
    }

    public void StartListeningForBroadcasts()
    {
        StartCoroutine(ListenForBroadcasts());
    }

    [System.Serializable]
    public class BroadcastMessageWrapper
    {
        public List<BroadcastMessage> messages;
    }

    private IEnumerator ListenForBroadcasts()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            if (stream.DataAvailable)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string jsonData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Debug.Log("Received broadcast message: " + jsonData);
                    BroadcastMessageWrapper broadcastMessageWrapper = JsonUtility.FromJson<BroadcastMessageWrapper>("{\"messages\":" + jsonData + "}");
                    List<BroadcastMessage> broadcastMessages = broadcastMessageWrapper.messages;

                    // 更新游戏对象的位置
                    foreach (BroadcastMessage message in broadcastMessages)
                    {
                        // call method to update all nerwork objects using meassage data
                        UpdateNetwork(message);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while receiving data: " + e.Message);
                }
            }

            yield return new WaitForSeconds(0.1f); // 等待0.1秒后再检查是否有新的广播数据
        }
    }

    // using the message data, update the network objects
    private void UpdateNetwork(BroadcastMessage message)
    {
        // if the message type is a player
        if (message.type == "player")
        {
            // print player uuid and position
            // Debug.Log("Player " + message.uuid + " is at position " + message.position);

            // 根据UUID找到游戏对象并更新位置
            // 如果找不到，就实例化一个新的游戏对象
            // 如果对象没被更新，就销毁它
            if (players.ContainsKey(message.uuid))
            {
                if (message.health <= 0)
                {
                    // Destroy the player
                    Destroy(players[message.uuid]);
                    players.Remove(message.uuid);
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                GameObject player = players[message.uuid];
                player.GetComponent<PlayerData>().health = message.health;
                // player.GetComponent<PlayerData>().attack = message.Attack;
                // player.GetComponent<PlayerData>().username = message.Username;
                player.transform.position = position;
            }
            else
            {
                if (message.health <= 0)
                {
                    // conitnue to next message
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                // use playerallprefab to instantiate
                GameObject player = Instantiate(playerallyPrefab, position, Quaternion.identity);
                player.GetComponent<PlayerData>().uuid = message.uuid;
                player.GetComponent<PlayerData>().health = message.health;
                // player.GetComponent<PlayerData>().attack = message.Attack;
                // player.GetComponent<PlayerData>().username = message.Username;
                players.Add(message.uuid, player);
            }
        }
        // if the message type is boss
        else if (message.type == "boss")
        {
            // print boss uuid and position
            // Debug.Log("Boss " + message.uuid + " is at position " + message.position);

            // 根据UUID找到游戏对象并更新位置
            // 如果找不到，就实例化一个新的游戏对象
            // 如果对象没被更新，就销毁它
            if (bosses.ContainsKey(message.uuid))
            {
                if (message.health <= 0)
                {
                    // Destroy the boss
                    Destroy(bosses[message.uuid]);
                    bosses.Remove(message.uuid);
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                GameObject boss = bosses[message.uuid];
                boss.GetComponent<PlayerData>().health = message.health;
                // boss.GetComponent<BossData>().attack = message.Attack;
                // boss.GetComponent<BossData>().username = message.Username;
                boss.transform.position = position;
            }
            else
            {
                if (message.health <= 0)
                {
                    // conitnue to next message
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                // use bossallprefab to instantiate
                GameObject boss = Instantiate(bossPrefab, position, Quaternion.identity);
                boss.GetComponent<PlayerData>().uuid = message.uuid;
                boss.GetComponent<PlayerData>().health = message.health;
                // boss.GetComponent<BossData>().attack = message.Attack;
                // boss.GetComponent<BossData>().username = message.Username;
                bosses.Add(message.uuid, boss);
                hostBoss = boss;
            }
        }

        // if type is "playerRocket"
        else if (message.type == "playerRocket")
        {
            // print player uuid and position
            // Debug.Log("Player " + message.uuid + " is at position " + message.position);

            // 根据UUID找到游戏对象并更新位置
            // 如果找不到，就实例化一个新的游戏对象
            // 如果对象没被更新，就销毁它
            if (playerRockets.ContainsKey(message.uuid))
            {
                if (message.health <= 0)
                {
                    // Destroy the playerRocket
                    Destroy(playerRockets[message.uuid]);
                    playerRockets.Remove(message.uuid);
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                GameObject playerRocket = playerRockets[message.uuid];
                playerRocket.GetComponent<PlayerData>().health = message.health;
                // playerRocket.GetComponent<PlayerData>().attack = message.Attack;
                // playerRocket.GetComponent<PlayerData>().username = message.Username;
                // calculate the direction from current position to target position
                Vector2 direction = position - (Vector2)playerRocket.transform.position;
                // calculate the angle from current position to target position
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // rotate the bossRocket to the target position
                playerRocket.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                playerRocket.transform.position = position;
            }
            else
            {
                if (message.health <= 0)
                {
                    // conitnue to next message
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                // use playerRocketPrefab to instantiate
                GameObject playerRocket = Instantiate(playerRocketPrefab, position, Quaternion.identity);
                playerRocket.GetComponent<PlayerData>().uuid = message.uuid;
                playerRocket.GetComponent<PlayerData>().health = message.health;
                // playerRocket.GetComponent<PlayerData>().attack = message.Attack;
                // playerRocket.GetComponent<PlayerData>().username = message.Username;
                playerRockets.Add(message.uuid, playerRocket);
            }


        }
        // if type is "bossRocket"
        else if (message.type == "bossRocket")
        {
            // print boss uuid and position
            // Debug.Log("Boss " + message.uuid + " is at position " + message.position);
            
            // 根据UUID找到游戏对象并更新位置
            // 如果找不到，就实例化一个新的游戏对象
            // 如果对象没被更新，就销毁它
            if (bossRockets.ContainsKey(message.uuid))
            {
                if (message.health <= 0)
                {
                    // Destroy the bossRocket
                    Destroy(bossRockets[message.uuid]);
                    bossRockets.Remove(message.uuid);
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                GameObject bossRocket = bossRockets[message.uuid];
                bossRocket.GetComponent<PlayerData>().health = message.health;
                // bossRocket.GetComponent<BossData>().attack = message.Attack;
                // bossRocket.GetComponent<BossData>().username = message.Username;
                
                // calculate the direction from current position to target position
                Vector2 direction = position - (Vector2)bossRocket.transform.position;
                // calculate the angle from current position to target position
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // rotate the bossRocket to the target position
                bossRocket.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                bossRocket.transform.position = position;
            }
            else
            {
                if (message.health <= 0)
                {
                    // conitnue to next message
                    return;
                }
                // 将 Position 类的 X 和 Y 字段转换为 Vector2
                Vector2 position = new Vector2(message.position.X, message.position.Y);

                // use bossRocketPrefab to instantiate
                GameObject bossRocket = Instantiate(bossRocketPrefab, position, Quaternion.identity);
                bossRocket.GetComponent<PlayerData>().uuid = message.uuid;
                bossRocket.GetComponent<PlayerData>().health = message.health;
                // bossRocket.GetComponent<BossData>().attack = message.Attack;
                // bossRocket.GetComponent<BossData>().username = message.Username;
                bossRockets.Add(message.uuid, bossRocket);
            }
        }
    }

}
