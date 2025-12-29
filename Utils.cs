using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using static Something.Plugin;

namespace Something
{
    public static class Utils
    {
        public static bool isBeta = false; // TODO
        public static bool testing => _testing && isBeta;
        public static bool _testing = false;

        public static bool trailerMode = false;

        public static bool inTestRoom => StartOfRound.Instance?.testRoom != null;
        public static bool DEBUG_disableSpawning = false;
        public static bool DEBUG_disableTargetting = false;
        public static bool DEBUG_disableHostTargetting = false;
        public static bool DEBUG_disableMoving = false;

        public static bool localPlayerFrozen = false;

        public static GameObject[] allAINodes => insideAINodes.Concat(outsideAINodes).ToArray();

        public static GameObject[] insideAINodes
        {
            get
            {
                if (RoundManager.Instance.insideAINodes != null && RoundManager.Instance.insideAINodes.Length > 0)
                {
                    return RoundManager.Instance.insideAINodes;
                }

                return GameObject.FindGameObjectsWithTag("AINode");
            }
        }
        public static GameObject[] outsideAINodes
        {
            get
            {
                if (RoundManager.Instance.outsideAINodes != null && RoundManager.Instance.outsideAINodes.Length > 0)
                {
                    return RoundManager.Instance.outsideAINodes;
                }

                return GameObject.FindGameObjectsWithTag("OutsideAINode");
            }
        }

        public static void ChatCommand(string[] args)
        {
            switch (args[0])
            {
                case "/spawning":
                    DEBUG_disableSpawning = !DEBUG_disableSpawning;
                    HUDManager.Instance.DisplayTip("Disable Spawning", DEBUG_disableSpawning.ToString());
                    break;
                case "/hazards":
                    Dictionary<string, GameObject> hazards = Utils.GetAllHazards();

                    foreach (var hazard in hazards)
                    {
                        logger.LogDebug(hazard);
                    }
                    break;
                case "/testing":
                    _testing = !_testing;
                    HUDManager.Instance.DisplayTip("Testing", _testing.ToString());
                    break;
                case "/surfaces":
                    foreach (var surface in StartOfRound.Instance.footstepSurfaces)
                    {
                        logger.LogDebug(surface.surfaceTag);
                    }
                    break;
                case "/enemies":
                    foreach (var enemy in Utils.GetEnemies())
                    {
                        logger.LogDebug(enemy.enemyType.name);
                    }
                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                case "/levels":
                    foreach (var level in StartOfRound.Instance.levels)
                    {
                        logger.LogDebug(level.name);
                    }
                    break;
                case "/dungeon":
                    logger.LogDebug(RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name);
                    break;
                case "/dungeons":
                    foreach (var dungeon in RoundManager.Instance.dungeonFlowTypes)
                    {
                        logger.LogDebug(dungeon.dungeonFlow.name);
                    }
                    break;
                default:
                    break;
            }
        }

        public static void LogChat(string msg)
        {
            HUDManager.Instance.AddChatMessage(msg, "Server");
        }

        public static Transform? GetClosestAINodeToPosition(Vector3 pos)
        {
            Transform? closestTransform = null;
            float closestDistance = Mathf.Infinity;

            foreach (var node in allAINodes)
            {
                if (node == null) { continue; }

                float distance = Vector3.Distance(pos, node.transform.position);
                if (distance > closestDistance) { continue; }

                closestDistance = distance;
                closestTransform = node.transform;
            }

            return closestTransform;
        }

        public static Vector3 GetBestThrowDirection(Vector3 origin, Vector3 forward, int rayCount, float maxDistance, LayerMask layerMask)
        {
            Vector3 bestDirection = forward;
            float farthestHit = 0f;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * (360f / rayCount);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward.normalized;

                // Raycast from origin outward
                if (Physics.Raycast(origin + Vector3.up * 0.5f, dir, out RaycastHit hit, maxDistance, layerMask))
                {
                    if (hit.distance > farthestHit)
                    {
                        bestDirection = dir;
                        farthestHit = hit.distance;
                    }
                }
                else
                {
                    // If nothing is hit, assume max distance (ideal throw)
                    return dir;
                }
            }

            return bestDirection;
        }

        public static Vector3 GetSpeed()
        {
            float num3 = localPlayer.movementSpeed / localPlayer.carryWeight;
            if (localPlayer.sinkingValue > 0.73f)
            {
                num3 = 0f;
            }
            else
            {
                if (localPlayer.isCrouching)
                {
                    num3 /= 1.5f;
                }
                else if (localPlayer.criticallyInjured && !localPlayer.isCrouching)
                {
                    num3 *= localPlayer.limpMultiplier;
                }
                if (localPlayer.isSpeedCheating)
                {
                    num3 *= 15f;
                }
                if (localPlayer.movementHinderedPrev > 0)
                {
                    num3 /= 2f * localPlayer.hinderedMultiplier;
                }
                if (localPlayer.drunkness > 0f)
                {
                    num3 *= StartOfRound.Instance.drunknessSpeedEffect.Evaluate(localPlayer.drunkness) / 5f + 1f;
                }
                if (!localPlayer.isCrouching && localPlayer.crouchMeter > 1.2f)
                {
                    num3 *= 0.5f;
                }
                if (!localPlayer.isCrouching)
                {
                    float num4 = Vector3.Dot(localPlayer.playerGroundNormal, localPlayer.walkForce);
                    if (num4 > 0.05f)
                    {
                        localPlayer.slopeModifier = Mathf.MoveTowards(localPlayer.slopeModifier, num4, (localPlayer.slopeModifierSpeed + 0.45f) * Time.deltaTime);
                    }
                    else
                    {
                        localPlayer.slopeModifier = Mathf.MoveTowards(localPlayer.slopeModifier, num4, localPlayer.slopeModifierSpeed / 2f * Time.deltaTime);
                    }
                    num3 = Mathf.Max(num3 * 0.8f, num3 + localPlayer.slopeIntensity * localPlayer.slopeModifier);
                }
            }

            Vector3 vector3 = new Vector3(0f, 0f, 0f);
            int num5 = Physics.OverlapSphereNonAlloc(localPlayer.transform.position, 0.65f, localPlayer.nearByPlayers, StartOfRound.Instance.playersMask);
            for (int i = 0; i < num5; i++)
            {
                vector3 += Vector3.Normalize((localPlayer.transform.position - localPlayer.nearByPlayers[i].transform.position) * 100f) * 1.2f;
            }
            int num6 = Physics.OverlapSphereNonAlloc(localPlayer.transform.position, 1.25f, localPlayer.nearByPlayers, 524288);
            for (int j = 0; j < num6; j++)
            {
                EnemyAICollisionDetect component = localPlayer.nearByPlayers[j].gameObject.GetComponent<EnemyAICollisionDetect>();
                if (component != null && component.mainScript != null && !component.mainScript.isEnemyDead && Vector3.Distance(localPlayer.transform.position, localPlayer.nearByPlayers[j].transform.position) < component.mainScript.enemyType.pushPlayerDistance)
                {
                    vector3 += Vector3.Normalize((localPlayer.transform.position - localPlayer.nearByPlayers[j].transform.position) * 100f) * component.mainScript.enemyType.pushPlayerForce;
                }
            }

            Vector3 vector4 = localPlayer.walkForce * num3 * localPlayer.sprintMultiplier + new Vector3(0f, localPlayer.fallValue, 0f) + vector3;
            vector4 += localPlayer.externalForces;
            return vector4;
        }

        public static void FreezePlayer(PlayerControllerB player, bool value)
        {
            localPlayerFrozen = value;
            player.disableInteract = value;
            player.disableLookInput = value;
            player.disableMoveInput = value;
        }

        public static void DespawnItemInSlotOnClient(int itemSlot)
        {
            HUDManager.Instance.itemSlotIcons[itemSlot].enabled = false;
            localPlayer.DestroyItemInSlotAndSync(itemSlot);
        }

        public static void MakePlayerInvisible(PlayerControllerB player, bool value)
        {
            GameObject scavengerModel = player.gameObject.transform.Find("ScavengerModel").gameObject;
            if (scavengerModel == null) { logger.LogError("ScavengerModel not found"); return; }
            scavengerModel.transform.Find("LOD1").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD2").gameObject.SetActive(!value);
            scavengerModel.transform.Find("LOD3").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/LevelSticker").gameObject.SetActive(!value);
            scavengerModel.transform.Find("metarig/spine/spine.001/spine.002/spine.003/BetaBadge").gameObject.SetActive(!value);
            player.playerBadgeMesh.gameObject.SetActive(!value);

        }

        public static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            List<SpawnableEnemyWithRarity> enemies = new List<SpawnableEnemyWithRarity>();
            enemies = GameObject.Find("Terminal")
                .GetComponentInChildren<Terminal>()
                .moonsCatalogueList
                .SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies))
                .Where(x => x != null && x.enemyType != null && x.enemyType.name != null)
                .GroupBy(x => x.enemyType.name, (k, v) => v.First())
                .ToList();

            return enemies;
        }

        public static EnemyVent GetClosestVentToPosition(Vector3 pos)
        {
            float mostOptimalDistance = 2000f;
            EnemyVent targetVent = null!;
            foreach (var vent in RoundManager.Instance.allEnemyVents)
            {
                float distance = Vector3.Distance(pos, vent.floorNode.transform.position);

                if (distance < mostOptimalDistance)
                {
                    mostOptimalDistance = distance;
                    targetVent = vent;
                }
            }

            return targetVent;
        }

        public static bool CalculatePath(Vector3 fromPos, Vector3 toPos)
        {
            Vector3 from = RoundManager.Instance.GetNavMeshPosition(fromPos, RoundManager.Instance.navHit, 1.75f);
            Vector3 to = RoundManager.Instance.GetNavMeshPosition(toPos, RoundManager.Instance.navHit, 1.75f);

            NavMeshPath path = new();
            return NavMesh.CalculatePath(from, to, -1, path) && Vector3.Distance(path.corners[path.corners.Length - 1], RoundManager.Instance.GetNavMeshPosition(to, RoundManager.Instance.navHit, 2.7f)) <= 1.55f;
        }

        public static bool CalculatePath(Vector3 fromPos, Vector3 toPos, Vector3 mainEntranceInsidePosition, Vector3 mainEntranceOutsidePosition, bool isOutside)
        {
            if (!CalculatePath(fromPos, toPos))
            {
                Vector3 entrancePos = isOutside ? mainEntranceOutsidePosition : mainEntranceInsidePosition;
                Vector3 otherSideEntrancePos = isOutside ? mainEntranceInsidePosition : mainEntranceOutsidePosition;
                return CalculatePath(fromPos, entrancePos) && CalculatePath(otherSideEntrancePos, toPos);
            }
            return true;
        }

        public static T? GetClosestGameObjectOfType<T>(Vector3 position) where T : Component
        {
            T[] objects = GameObject.FindObjectsOfType<T>();
            T closest = null!;
            float closestDistance = Mathf.Infinity;

            foreach (T obj in objects)
            {
                float distance = Vector3.Distance(position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = obj;
                }
            }

            return closest;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static T GetRandom<T>(this IEnumerable<T> source, System.Random? random = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            random ??= new System.Random();

            T? chosen = default!;
            int count = 0;

            foreach (var item in source)
            {
                count++;
                if (random.Next(count) == 0) // replace previous with decreasing probability
                    chosen = item;
            }

            if (count == 0)
                throw new InvalidOperationException("Sequence contains no elements.");

            return chosen;
        }

        public static T? GetClosestToPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector) where T : class
        {
            T? closest = null;
            float closestDistance = Mathf.Infinity;

            foreach (var item in list)
            {
                if (item == null) continue;

                float distance = Vector3.Distance(position, positionSelector(item));
                if (distance >= closestDistance) continue;

                closest = item;
                closestDistance = distance;
            }

            return closest;
        }

        public static T? GetFarthestFromPosition<T>(this IEnumerable<T> list, Vector3 position, Func<T, Vector3> positionSelector) where T : class
        {
            T? farthest = null;
            float farthestDistance = 0f;

            foreach (var item in list)
            {
                if (item == null) continue;

                float distance = Vector3.Distance(position, positionSelector(item));
                if (distance <= farthestDistance) continue;

                farthest = item;
                farthestDistance = distance;
            }

            return farthest;
        }

        public static Dictionary<string, GameObject> GetAllHazards()
        {
            Dictionary<string, GameObject> hazards = new Dictionary<string, GameObject>();
            List<SpawnableMapObject> spawnableMapObjects = (from x in StartOfRound.Instance.levels.SelectMany((SelectableLevel level) => level.spawnableMapObjects)
                                                            group x by ((UnityEngine.Object)x.prefabToSpawn).name into g
                                                            select g.First()).ToList();
            foreach (SpawnableMapObject item in spawnableMapObjects)
            {
                hazards.Add(item.prefabToSpawn.name, item.prefabToSpawn);
            }
            return hazards;
        }

        public static Vector3 GetRandomNavMeshPositionInAnnulus(Vector3 center, float minRadius, float maxRadius, int sampleCount = 10)
        {
            Vector3 randomDirection;
            float y = center.y;

            // Make sure minRadius is less than maxRadius
            if (minRadius >= maxRadius)
            {
                logger.LogWarning("minRadius should be less than maxRadius. Returning original position.");
                return center;
            }

            // Try a few times to get a valid point
            for (int i = 0; i < sampleCount; i++)
            {
                // Get a random direction
                randomDirection = UnityEngine.Random.insideUnitSphere;
                randomDirection.y = 0f;
                randomDirection.Normalize();

                // Random distance between min and max radius
                float distance = UnityEngine.Random.Range(minRadius, maxRadius);

                // Calculate the new position
                Vector3 pos = center + randomDirection * distance;
                pos.y = y;

                // Check if it's on the NavMesh
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            logger.LogWarning("Unable to find valid NavMesh position in annulus. Returning original position.");
            return center;
        }


        public static List<Vector3> GetEvenlySpacedNavMeshPositions(Vector3 center, int count, float minRadius, float maxRadius)
        {
            List<Vector3> positions = new List<Vector3>();

            // Validate
            if (count <= 0 || minRadius > maxRadius)
            {
                logger.LogWarning("Invalid parameters for turret spawn positions.");
                return positions;
            }

            float y = center.y;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // Angle in degrees
                float angle = i * angleStep;

                // Convert angle to radians
                float radians = angle * Mathf.Deg2Rad;

                // Use random radius between min and max for some variation (optional)
                float radius = UnityEngine.Random.Range(minRadius, maxRadius);

                // Direction on XZ plane
                float x = Mathf.Cos(radians) * radius;
                float z = Mathf.Sin(radians) * radius;

                Vector3 pos = new Vector3(center.x + x, y, center.z + z);

                // Try to snap to NavMesh
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    positions.Add(hit.position);
                }
                else
                {
                    logger.LogWarning($"Could not find valid NavMesh position for turret {i}. Skipping.");
                }
            }

            return positions;
        }

        public static PlayerControllerB[] GetNearbyPlayers(Vector3 position, float distance = 10f, List<PlayerControllerB>? ignoredPlayers = null)
        {
            List<PlayerControllerB> players = [];

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled) { continue; }
                if (ignoredPlayers != null && ignoredPlayers.Contains(player)) { continue; }
                if (Vector3.Distance(position, player.transform.position) > distance) { continue; }
                players.Add(player);
            }

            return players.ToArray();
        }

        public static void RebuildRig(PlayerControllerB pcb)
        {
            if (pcb != null && pcb.playerBodyAnimator != null)
            {
                pcb.playerBodyAnimator.WriteDefaultValues();
                pcb.playerBodyAnimator.GetComponent<RigBuilder>()?.Build();
            }
        }

        public static bool IsPlayerChild(PlayerControllerB player)
        {
            return player.thisPlayerBody.localScale.y < 1f;
        }

        public static PlayerControllerB? GetFarthestPlayerFromPosition(Vector3 position, float minDistance = 0f)
        {
            float farthestDistance = minDistance;
            PlayerControllerB? farthestPlayer = null;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled) { continue; }
                float distance = Vector3.Distance(position, player.transform.position);
                if (distance < farthestDistance) { continue; }
                farthestDistance = distance;
                farthestPlayer = player;
            }

            return farthestPlayer;
        }

        public static float CanPathToPoint(Vector3 startPos, Vector3 endPos)
        {
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(startPos, endPos, -1, path) || (int)path.status != 0)
            {
                return -1f;
            }
            float pathDistance = 0f;
            if (path.corners.Length > 1)
            {
                for (int i = 1; i < path.corners.Length; i++)
                {
                    pathDistance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }
            return pathDistance;
        }

        public static void PlaySoundAtPosition(Vector3 pos, AudioClip clip, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f)
        {
            GameObject soundObj = GameObject.Instantiate(new GameObject("TempSoundEffectObj"), pos, Quaternion.identity);
            AudioSource source = soundObj.AddComponent<AudioSource>();

            OccludeAudio occlude = soundObj.AddComponent<OccludeAudio>();
            occlude.lowPassOverride = 20000f;

            source.rolloffMode = AudioRolloffMode.Linear;

            if (randomizePitch)
                source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);

            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatial3D ? 1 : 0;
            source.minDistance = min3DDistance;
            source.maxDistance = max3DDistance;
            source.Play();
            GameObject.Destroy(soundObj, source.clip.length);
        }

        public static void PlaySoundAtPosition(Vector3 pos, AudioClip[] clips, float volume = 1f, bool randomizePitch = true, bool spatial3D = true, float min3DDistance = 1f, float max3DDistance = 10f)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
            PlaySoundAtPosition(pos, clips[index], volume, randomizePitch, spatial3D, min3DDistance, max3DDistance);
        }

        public static PlayerControllerB? GetRandomPlayer()
        {
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts.Where(x => x != null && x.isPlayerControlled).ToArray();
            if (players.Length <= 0) { return null; }
            int index = UnityEngine.Random.Range(0, players.Length);
            return players[index];
        }

        public static GrabbableObject? SpawnItem(NamespacedKey<DawnItemInfo> key, Vector3 position, Quaternion rotation = default, float fallTime = 0f)
        {
            if (!IsServerOrHost) { return null; }
            GameObject obj = GameObject.Instantiate(LethalContent.Items[key].Item.spawnPrefab, position, rotation, StartOfRound.Instance.propsContainer);
            GrabbableObject grabObj = obj.GetComponent<GrabbableObject>();
            grabObj.fallTime = fallTime;
            grabObj.NetworkObject.Spawn();
            return grabObj;
        }

        public static void MufflePlayer(PlayerControllerB player, bool muffle)
        {
            if (player.currentVoiceChatAudioSource == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatAudioSource != null)
            {
                OccludeAudio component = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
                player.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = muffle ? 5f : 1f;
                component.overridingLowPass = muffle;
                component.lowPassOverride = muffle ? 500f : 20000f;
                player.voiceMuffledByEnemy = muffle;
            }
        }
    }

    [HarmonyPatch]
    public class UtilsPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnInsideEnemiesFromVentsIfReady))]
        public static bool SpawnInsideEnemiesFromVentsIfReadyPrefix()
        {
            if (Utils.isBeta && Utils.DEBUG_disableSpawning) { return false; }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnDaytimeEnemiesOutside))]
        public static bool SpawnDaytimeEnemiesOutsidePrefix()
        {
            if (Utils.isBeta && Utils.DEBUG_disableSpawning) { return false; }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnEnemiesOutside))]
        public static bool SpawnEnemiesOutsidePrefix()
        {
            if (Utils.isBeta && Utils.DEBUG_disableSpawning) { return false; }
            return true;
        }
    }
}