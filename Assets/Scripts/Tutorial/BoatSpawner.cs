using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class BoatSpawner : MonoBehaviourPunCallbacks
{
    [Header("Boat Setup")]
    public string[] boatPrefabNames; // Names of prefabs in Resources/Prefabs (e.g., "BoatPrefab1")

    [Header("Spawn Points")]
    public Transform[] spawnPoints;  // Assign 4+ spawn points in Inspector

    [Header("Spawn Settings")]
    public int boatsToSpawn = 4;

    private bool spawnPointsAssigned = false;
    private List<Transform> availableSpawnPoints = new List<Transform>();
    private Dictionary<int, int> playerSpawnPointMap = new Dictionary<int, int>(); // playerActorNumber -> spawnPointIndex

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room! Starting spawn coroutine...");

        if (PhotonNetwork.IsMasterClient)
        {
            // Master client initializes available spawn points
            availableSpawnPoints = new List<Transform>(spawnPoints);
            ShuffleList(availableSpawnPoints);
            spawnPointsAssigned = true;

            // Assign spawn point for master client first
            AssignSpawnPoint(PhotonNetwork.LocalPlayer.ActorNumber);

            Debug.Log("Master client initialized spawn points: " + availableSpawnPoints.Count);
        }

        // All players spawn their own boat
        StartCoroutine(SpawnMyBoat());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.ActorNumber} entered room");

        if (PhotonNetwork.IsMasterClient)
        {
            // Master client assigns spawn point for new player
            AssignSpawnPoint(newPlayer.ActorNumber);
        }
    }

    private void AssignSpawnPoint(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (availableSpawnPoints.Count > 0)
        {
            int spawnIndex = System.Array.IndexOf(spawnPoints, availableSpawnPoints[0]);
            playerSpawnPointMap[actorNumber] = spawnIndex;
            availableSpawnPoints.RemoveAt(0);

            // Sync with all clients
            photonView.RPC("RPC_AssignSpawnPoint", RpcTarget.All, actorNumber, spawnIndex);

            Debug.Log($"Assigned spawn point {spawnIndex} to player {actorNumber}. Remaining: {availableSpawnPoints.Count}");
        }
        else
        {
            Debug.LogWarning("No available spawn points for player: " + actorNumber);
        }
    }

    [PunRPC]
    private void RPC_AssignSpawnPoint(int actorNumber, int spawnIndex)
    {
        playerSpawnPointMap[actorNumber] = spawnIndex;
        spawnPointsAssigned = true;
        Debug.Log($"RPC: Player {actorNumber} got spawn point {spawnIndex}");
    }

    private IEnumerator SpawnMyBoat()
    {
        Debug.Log("Starting boat spawn process...");

        // Wait until spawn points are assigned
        int maxWaitTime = 100; // 10 seconds max
        int currentWait = 0;

        while (!spawnPointsAssigned && currentWait < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            currentWait++;
            Debug.Log("Waiting for spawn point assignment... " + currentWait);
        }

        if (currentWait >= maxWaitTime)
        {
            Debug.LogError("Timeout waiting for spawn point assignment!");
            yield break;
        }

        // Additional safety wait
        yield return new WaitForSeconds(0.5f);

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned in inspector!");
            yield break;
        }

        if (boatPrefabNames.Length == 0)
        {
            Debug.LogError("No boat prefab names assigned!");
            yield break;
        }

        // Get spawn point for this player
        Transform mySpawnPoint = GetMySpawnPoint();
        if (mySpawnPoint == null)
        {
            Debug.LogError("No available spawn point found for player!");
            yield break;
        }

        Debug.Log($"My spawn point: {mySpawnPoint.position}");

        // Pick a random boat prefab name
        string chosenBoatName = boatPrefabNames[Random.Range(0, boatPrefabNames.Length)];

        Debug.Log($"Spawning boat: {chosenBoatName} at position {mySpawnPoint.position}");

        // Spawn the boat
        GameObject myBoat = PhotonNetwork.Instantiate(chosenBoatName, mySpawnPoint.position, mySpawnPoint.rotation);

        if (myBoat != null)
        {
            Debug.Log($"Successfully spawned boat: {myBoat.name}");
        }
        else
        {
            Debug.LogError("Failed to instantiate boat!");
        }
    }

    private Transform GetMySpawnPoint()
    {
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (playerSpawnPointMap.ContainsKey(myActorNumber))
        {
            int spawnIndex = playerSpawnPointMap[myActorNumber];
            if (spawnIndex >= 0 && spawnIndex < spawnPoints.Length)
            {
                return spawnPoints[spawnIndex];
            }
        }

        Debug.LogWarning($"No spawn point mapped for player {myActorNumber}, using random spawn");
        // Fallback: use random spawn point
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}